using NHNAI.Game.App;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 플레이어 입력이 들어오는 **유일한 문**. 걷기 · 시야 · 상호작용 · 동전 넣기/꺼내기가
    /// 전부 여기를 거친다.
    ///
    /// 왜 한 곳으로 모으나: 조작이 PC(키보드·마우스)와 모바일(화면 위 조이스틱·버튼)
    /// 둘로 갈라졌다. 각 스크립트가 <see cref="Keyboard.current"/> 를 직접 읽던 구조로는
    /// 분기가 다섯 군데로 복제되고, "커서가 잠긴 동안만 받는다" 같은 규칙도 같이 복제된다.
    /// 규칙이 복제되면 한 곳만 고쳐진 채로 남는다.
    ///
    /// 읽는 쪽(<see cref="PlayerMove"/> 등)은 조작 방식을 모른다. 값이 0 이면 안 움직이고
    /// 눌림이 거짓이면 안 한다 — <see cref="Active"/> 를 따로 볼 필요가 없게, 입력을
    /// 받지 않는 동안은 여기서 전부 0 으로 내보낸다.
    ///
    /// **씬이 열린 직후에는 아무것도 받지 않는다.** 그 위에 메인메뉴가 떠 있고,
    /// 조작 방식이 정해지지 않았다. 메뉴가 사라지면서 <see cref="Begin"/> 을 부르고,
    /// 그때부터 게임이 시작된다.
    ///
    /// <see cref="DefaultExecutionOrder"/> 로 다른 스크립트보다 먼저 돈다. 이 프레임의
    /// 입력을 이 프레임에 소비하게 하려면 순서가 보장돼야 한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputSource : MonoBehaviour
    {
        [Header("터치")]
        [Tooltip("터치 드래그 1단위를 마우스 카운트 몇 개로 환산할지. 감도 자체는 " +
                 "PlayerLook.sensitivity 하나로 통일돼 있고, 여기서는 두 장치의 " +
                 "단위 차이만 메운다. 값은 패널 논리 좌표 기준이라 기기 해상도에 흔들리지 않는다")]
        [SerializeField] float touchLookScale = 0.9f;

        /// <summary>걷기 축. 몸통 정면 기준의 −1~1. 입력을 안 받는 동안은 0.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>
        /// 이번 프레임의 시야 회전량. **속도가 아니라 이미 시간이 반영된 양이다** —
        /// 읽는 쪽에서 <see cref="Time.deltaTime"/> 을 곱하면 안 된다.
        /// </summary>
        public Vector2 LookDelta { get; private set; }

        /// <summary>이번 프레임에 '사용'이 눌렸다 — 조준한 것과 상호작용 · 들고 있으면 놓기.</summary>
        public bool InteractPressed { get; private set; }

        /// <summary>이번 프레임에 '넣기'가 눌렸다 (PC: E).</summary>
        public bool StorePressed { get; private set; }

        /// <summary>이번 프레임에 '꺼내기'가 눌렸다 (PC: Q).</summary>
        public bool RetrievePressed { get; private set; }

        /// <summary>
        /// 지금 입력을 받는가. PC 는 커서가 잠긴 동안만, 모바일은 항상 참이다.
        /// 읽는 쪽은 대개 볼 필요가 없다 — 거짓이면 위 값들이 전부 비어 있다.
        /// </summary>
        public bool Active { get; private set; }

        // 모바일 UI 가 밀어 넣는 값. UI Toolkit 의 포인터 이벤트가 이 컴포넌트의
        // Update 앞에 오는지 뒤에 오는지는 보장되지 않는다. 그래서 눌림은 **래치**다 —
        // 늦게 들어오면 다음 프레임에 소비될 뿐, 사라지지는 않는다.
        Vector2 _touchMove;
        Vector2 _touchLook;      // 누적. Update 가 비운다
        bool _touchInteract;
        bool _touchStore;
        bool _touchRetrieve;

        ControlMode _mode;
        bool _running;           // 메인메뉴가 사라져야 참

        void OnEnable()
        {
            // 씬이 열리면 메인메뉴가 떠 있다. 커서가 보여야 버튼을 누른다.
            SetCursorLocked(false);
        }

        void OnDisable()
        {
            // 플레이를 멈췄는데 커서가 잠긴 채 남으면 에디터를 못 쓴다.
            SetCursorLocked(false);
            ClearTouch();
            Clear();
            _running = false;
        }

        /// <summary>
        /// 창이 포커스를 잃으면 브라우저·OS 가 포인터 잠금을 **스스로** 푼다
        /// (탭 전환 · alt-tab · 다른 창 클릭). 그때 우리 쪽 희망 상태를 Locked 로
        /// 남겨 두면 두 상태가 어긋나고, 포커스가 돌아올 때 잠금이 사용자 조작
        /// 없이 다시 요청된다 — 브라우저는 그것을 거부한다.
        ///
        /// 여기서 같이 풀어 두면 <see cref="ReadDesktop"/> 의 되잡기 클릭 하나로
        /// 정상 경로를 탄다. 되잡기는 **클릭에서만** 한다 — 포커스가 돌아온 것
        /// 자체는 사용자 조작이 아니라서 여기서 다시 잠그면 또 거부된다.
        /// </summary>
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SetCursorLocked(false);
        }

        /// <summary>
        /// 조작 방식을 고른 **그 순간**, 메뉴 버튼의 클릭 핸들러 안에서 불린다.
        /// 하는 일은 커서 잠금 하나뿐이다 — 입력은 아직 살지 않는다
        /// (<see cref="Begin"/> 이 그 일을 한다).
        ///
        /// **왜 <see cref="Begin"/> 에서 떼어냈나 — WebGL 때문이다.** 브라우저는 포인터
        /// 잠금을 사용자 조작이 방금 있었을 때만 허용한다. Begin 은 메뉴 페이드가 끝난
        /// 뒤에 불리므로 그 창에서 벗어날 수 있고, 그러면 잠금이 **조용히 거부된다** —
        /// PC 를 골랐는데 시야가 안 도는 상태가 되고 원인이 화면에 안 나타난다.
        /// 클릭에 가장 가까운 시점에 요청하면 이 조건을 확실히 만족한다.
        ///
        /// 잠금이 그래도 거부되면 <see cref="ReadDesktop"/> 의 되잡기 경로가 받아 준다 —
        /// 화면을 한 번 더 클릭하면 잠긴다. 그 클릭은 상호작용으로 세지 않는다.
        ///
        /// ⚠️ 거부는 **게임 쪽에만** 조용하다. 브라우저에서는 거부된 Promise 로 남고,
        /// 아무도 받지 않으면 Unity 로더가 그것을 오류로 잡아 alert 를 띄운다. 삼키는
        /// 것은 WebGL 템플릿(<c>Assets/WebGLTemplates/NHNAI/index.html</c>)의 일이다 —
        /// 그 조각을 지우면 플레이 도중 "A user gesture is required to request Pointer
        /// Lock." 창이 뜬다.
        /// </summary>
        public void ClaimCursor(ControlMode mode) => SetCursorLocked(mode == ControlMode.Pc);

        /// <summary>
        /// 메인메뉴가 사라진 뒤 불린다. **이때부터 게임이 조작을 받는다.**
        /// 커서 잠금은 여기가 아니라 <see cref="ClaimCursor"/> 가 한다 — 메뉴가 떠 있는
        /// 동안 잠그면 버튼을 못 누르고, 너무 늦게 잠그면 WebGL 에서 거부된다.
        /// </summary>
        public void Begin(ControlMode mode)
        {
            _mode = mode;
            _running = true;
        }

        void Update()
        {
            if (!_running)
            {
                Active = false;
                Clear();
                return;
            }

            if (_mode == ControlMode.Mobile) ReadTouch();
            else ReadDesktop();
        }

        // --- PC ---------------------------------------------------------------

        void ReadDesktop()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // 커서를 되잡는 클릭은 상호작용으로 세지 않는다. 창을 다시 집으려고
            // 누른 클릭이 레버까지 당기면 스핀 하나를 의도 없이 잃는다.
            var reclaimed = false;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame
                     && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
                reclaimed = true;
            }

            Active = Cursor.lockState == CursorLockMode.Locked;
            if (!Active)
            {
                Clear();
                return;
            }

            Move = ReadKeyboardAxis(keyboard);

            // ⚠️ Time.deltaTime 을 곱하지 않는다. 마우스 델타는 '속도'가 아니라 이번
            // 프레임에 실제로 움직인 양이라 이미 시간이 반영돼 있다. 곱하면 프레임률에
            // 따라 감도가 달라진다 — 흔하고 눈치채기 어려운 버그다.
            LookDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

            InteractPressed = !reclaimed && mouse != null && mouse.leftButton.wasPressedThisFrame;
            StorePressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            RetrievePressed = keyboard != null && keyboard.qKey.wasPressedThisFrame;
        }

        static Vector2 ReadKeyboardAxis(Keyboard k)
        {
            if (k == null) return Vector2.zero;

            var x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                    - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            var y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f)
                    - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);

            var v = new Vector2(x, y);
            // 정규화하지 않으면 대각선이 √2 배 빨라진다.
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        // --- 모바일 -----------------------------------------------------------

        void ReadTouch()
        {
            Active = true;

            Move = _touchMove;

            // ⚠️ y 를 뒤집는다. UI 패널 좌표는 y 가 **아래**로 자라고, 이 속성의 기준인
            // 마우스 델타(ReadDesktop)는 y 가 **위**로 자란다. 그대로 넘기면 위로 쓸었을
            // 때 아래를 본다 — 조이스틱에서 한 것과 똑같은 보정이다.
            //
            // 뒤집은 결과가 곧 FPS 의 기본값이다: 쓴 방향으로 시야가 따라간다
            // (위로 쓸면 위를 본다). 반전을 켜고 끄는 선택지가 필요해지면 그 손잡이는
            // 여기 붙는다 — 읽는 쪽(PlayerLook)은 장치도 부호도 몰라야 한다.
            LookDelta = new Vector2(_touchLook.x, -_touchLook.y) * touchLookScale;
            _touchLook = Vector2.zero;

            InteractPressed = _touchInteract;
            StorePressed = _touchStore;
            RetrievePressed = _touchRetrieve;
            _touchInteract = false;
            _touchStore = false;
            _touchRetrieve = false;
        }

        // --- 모바일 UI 가 부르는 면 -------------------------------------------
        //
        // NHNAI.UI 쪽 조작 화면이 호출한다. 반대 방향(게임이 UI 를 부르는 것)은
        // 어셈블리 의존 방향상 불가능하다 — NHNAI.Game 은 NHNAI.UI 를 모른다.

        /// <summary>조이스틱의 현재 기울기(−1~1). 뗀 순간 0 을 넣어 준다.</summary>
        public void SetMoveAxis(Vector2 axis) => _touchMove = axis;

        /// <summary>
        /// 시점 패드를 끈 양. **UI 패널 좌표 그대로** 넘긴다 (y 가 아래로 자란다) —
        /// 마우스와 부호를 맞추는 것은 <see cref="ReadTouch"/> 의 일이다.
        /// 프레임 안에서 여러 번 와도 쌓인다.
        /// </summary>
        public void AddLookDelta(Vector2 delta) => _touchLook += delta;

        /// <summary>'사용' 버튼. 소비될 때까지 남는 래치다.</summary>
        public void PressInteract() => _touchInteract = true;

        /// <summary>'넣기' 버튼 (PC 의 E).</summary>
        public void PressStore() => _touchStore = true;

        /// <summary>'꺼내기' 버튼 (PC 의 Q).</summary>
        public void PressRetrieve() => _touchRetrieve = true;

        // --- 공통 -------------------------------------------------------------

        void Clear()
        {
            Move = Vector2.zero;
            LookDelta = Vector2.zero;
            InteractPressed = false;
            StorePressed = false;
            RetrievePressed = false;
        }

        void ClearTouch()
        {
            _touchMove = Vector2.zero;
            _touchLook = Vector2.zero;
            _touchInteract = false;
            _touchStore = false;
            _touchRetrieve = false;
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
