using NHNAI.Game.App;
using NHNAI.Game.Player;
using NHNAI.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.MobileControls
{
    /// <summary>
    /// 모바일 조작 층. 조이스틱 · 시점 패드 · 버튼이 만든 값을
    /// <see cref="PlayerInputSource"/> 에 밀어 넣는다.
    ///
    /// 씬에는 늘 있지만 **메인메뉴가 걷히기 전까지는 보이지 않는다** (Hud 와 같이
    /// <c>opacity: 0</c> 으로 시작한다). <see cref="Begin"/> 이 불릴 때 PC 를 골랐으면
    /// 트리에서 접히고, 모바일이면 떠오른다.
    ///
    /// 게임을 구독하는 HUD 와 방향이 반대다: 여기는 UI 가 게임을 **민다**.
    /// 어느 쪽이든 어셈블리 방향은 NHNAI.UI → NHNAI.Game 하나뿐이다.
    ///
    /// 버튼은 <c>clicked</c>(뗄 때) 가 아니라 <see cref="PointerDownEvent"/>(누를 때)
    /// 로 받는다. 게임 조작은 손가락이 닿는 순간 나가야 하고, 뗄 때까지 기다리면
    /// 레버가 한 박자 늦게 내려간다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MobileControlsScreen : MonoBehaviour
    {
        const string VisibleClass = "mobile-controls--visible";

        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] PlayerInputSource input;

        VisualElement _root;
        VirtualJoystick _joystick;
        TouchLookPad _lookPad;
        Button _interact;
        Button _store;
        Button _retrieve;

        bool _wired;

        /// <summary>부트스트랩이 입력을 꽂아 준다.</summary>
        public void Bind(PlayerInputSource source) => input = source;

        // UIDocument 가 비주얼 트리를 만드는 시점이 이 컴포넌트의 OnEnable 보다
        // 늦을 수 있다. 두 번 시도하고 _wired 로 중복을 막는다.
        void OnEnable() => Wire();

        void Start() => Wire();

        void OnDisable() => Unwire();

        /// <summary>
        /// 메인메뉴가 걷히기 시작할 때 불린다. PC 를 골랐으면 이 층은 없는 것과
        /// 같아야 하므로 트리에서 접는다 — 투명해도 남아 있으면 포인터를 먹는다.
        /// </summary>
        public void Begin(ControlMode mode)
        {
            if (_root == null) return;

            if (mode != ControlMode.Mobile)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _root.AddToClassList(VisibleClass);
        }

        void Wire()
        {
            if (_wired) return;

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _root = root.Q<VisualElement>("mobile-root");
            _joystick = root.Q<VirtualJoystick>("joystick");
            _lookPad = root.Q<TouchLookPad>("look-pad");
            _interact = root.Q<Button>("interact");
            _store = root.Q<Button>("store");
            _retrieve = root.Q<Button>("retrieve");

            if (_joystick != null) _joystick.ValueChanged += OnMove;
            if (_lookPad != null) _lookPad.Dragged += OnLook;
            _interact?.RegisterCallback<PointerDownEvent>(OnInteractDown);
            _store?.RegisterCallback<PointerDownEvent>(OnStoreDown);
            _retrieve?.RegisterCallback<PointerDownEvent>(OnRetrieveDown);

            _wired = true;
        }

        void Unwire()
        {
            if (!_wired) return;

            if (_joystick != null) _joystick.ValueChanged -= OnMove;
            if (_lookPad != null) _lookPad.Dragged -= OnLook;
            _interact?.UnregisterCallback<PointerDownEvent>(OnInteractDown);
            _store?.UnregisterCallback<PointerDownEvent>(OnStoreDown);
            _retrieve?.UnregisterCallback<PointerDownEvent>(OnRetrieveDown);

            // 손가락을 올린 채 화면이 꺼지면 계속 걷는다.
            if (input != null) input.SetMoveAxis(Vector2.zero);

            _root = null;
            _joystick = null;
            _lookPad = null;
            _interact = null;
            _store = null;
            _retrieve = null;
            _wired = false;
        }

        void OnMove(Vector2 axis) => input?.SetMoveAxis(axis);

        void OnLook(Vector2 delta) => input?.AddLookDelta(delta);

        void OnInteractDown(PointerDownEvent evt) => input?.PressInteract();

        void OnStoreDown(PointerDownEvent evt) => input?.PressStore();

        void OnRetrieveDown(PointerDownEvent evt) => input?.PressRetrieve();
    }
}
