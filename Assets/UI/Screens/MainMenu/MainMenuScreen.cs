using NHNAI.Game.App;
using NHNAI.Game.Player;
using NHNAI.UI.Hud;
using NHNAI.UI.MobileControls;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.MainMenu
{
    /// <summary>
    /// 메인메뉴. **독방 씬 위에 덮이는 층**이지 별도 씬이 아니다.
    ///
    /// 씬이 열리면 방과 배경음은 이미 살아 있고 이 층만 그 위를 덮고 있다.
    /// 조작을 고르면 이 층이 페이드 아웃하고, 같은 동안 게임 UI 가 페이드 인한다.
    /// 페이드가 끝나야 <see cref="PlayerInputSource.Begin"/> 이 불려 조작이 살아난다 —
    /// 그 전에 살리면 메뉴를 고른 클릭이 그대로 레버까지 닿는다.
    ///
    /// 고른 값은 정적 보관소에 넣지 않고 <see cref="ControlMode"/> 인자로 셋에 나눠 준다.
    /// 씬이 하나라 씬을 넘어 살아야 할 값이 아니고, 누가 누구에게 알려 주는지가
    /// 코드에 그대로 보이는 편이 낫다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        const string HiddenClass = "main-menu--hidden";

        /// <summary>
        /// 페이드 아웃 길이(ms). **`MainMenu.uss` 의 `transition-duration` 과 같아야 한다.**
        /// USS 는 그림을 그리고 이 값은 그 뒤에 무엇을 할지를 정한다 — 어긋나면
        /// 아직 보이는 채로 접히거나(짧음), 투명해진 메뉴가 남아 첫 조작을 먹는다(김).
        /// </summary>
        const long FadeOutMs = 420;

        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] PlayerInputSource input;
        [SerializeField] HudScreen hud;
        [SerializeField] MobileControlsScreen mobileControls;

        VisualElement _root;
        Button _pc;
        Button _mobile;
        bool _wired;
        bool _dismissing;

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(PlayerInputSource source, HudScreen hudScreen,
                         MobileControlsScreen controls)
        {
            input = source;
            hud = hudScreen;
            mobileControls = controls;
        }

        // UIDocument 가 비주얼 트리를 만드는 시점이 이 컴포넌트의 OnEnable 보다
        // 늦을 수 있다. 두 번 시도하고 _wired 로 중복을 막는다.
        void OnEnable()
        {
            // ⚠️ UnityEngine.UIElements 에도 Cursor 가 있다 (USS 의 cursor 속성용 구조체).
            // 이 파일은 둘 다 using 하므로 정규화하지 않으면 CS0104 로 컴파일이 막힌다.
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Wire();
        }

        void Start() => Wire();

        void OnDisable()
        {
            if (!_wired) return;
            if (_pc != null) _pc.clicked -= ChoosePc;
            if (_mobile != null) _mobile.clicked -= ChooseMobile;
            _root = null;
            _pc = null;
            _mobile = null;
            _wired = false;
        }

        void Wire()
        {
            if (_wired) return;

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _root = root.Q<VisualElement>("menu-root");
            _pc = root.Q<Button>("play-pc");
            _mobile = root.Q<Button>("play-mobile");

            if (_root == null || _pc == null || _mobile == null)
            {
                Debug.LogError("[NHNAI] MainMenu.uxml 에서 menu-root / play-pc / play-mobile 을 " +
                               "못 찾았다.");
                return;
            }

            _pc.clicked += ChoosePc;
            _mobile.clicked += ChooseMobile;
            _wired = true;
        }

        void ChoosePc() => Choose(ControlMode.Pc);

        void ChooseMobile() => Choose(ControlMode.Mobile);

        void Choose(ControlMode mode)
        {
            // 페이드 중에 다른 쪽을 눌러도 한 번만 시작한다.
            if (_dismissing || _root == null) return;
            _dismissing = true;

            // 사라지는 중에는 아무것도 받지 않는다. 자식(버튼)까지 함께 꺼진다.
            _root.SetEnabled(false);
            _root.AddToClassList(HiddenClass);

            // 게임 UI 는 **지금** 나타나기 시작한다. 메뉴가 걷히는 동안 겹쳐 떠올라
            // 두 층 사이에 빈 순간이 없다.
            if (hud != null) hud.Begin(mode);
            if (mobileControls != null) mobileControls.Begin(mode);

            // 조작은 메뉴가 완전히 사라진 뒤에 살아난다.
            _root.schedule.Execute(() =>
            {
                // 투명해도 남아 있으면 포인터를 먹는다. 트리에서 접어 버린다.
                _root.style.display = DisplayStyle.None;
                if (input != null) input.Begin(mode);
            }).ExecuteLater(FadeOutMs);
        }
    }
}
