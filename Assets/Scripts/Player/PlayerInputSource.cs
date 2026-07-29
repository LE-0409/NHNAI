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
        /// 메인메뉴가 사라진 뒤 불린다. **이때부터 게임이 조작을 받는다.**
        /// 커서 잠금도 여기서 결정된다 — 메뉴가 떠 있는 동안 잠그면 버튼을 못 누른다.
        /// </summary>
        public void Begin(ControlMode mode)
        {
            _mode = mode;
            _running = true;
            SetCursorLocked(mode == ControlMode.Pc);
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

            LookDelta = _touchLook * touchLookScale;
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

        /// <summary>시점 패드를 끈 양(패널 논리 좌표). 프레임 안에서 여러 번 와도 쌓인다.</summary>
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
