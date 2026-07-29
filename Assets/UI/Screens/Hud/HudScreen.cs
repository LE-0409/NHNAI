using NHNAI.Game.App;
using NHNAI.Game.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.Hud
{
    /// <summary>
    /// HUD — 조준점과 인벤토리 동전 개수. 조준점은 항상 떠 있고,
    /// <see cref="PlayerInteractor"/> 가 내보내는 조준 상태를 받아 강조 클래스만
    /// 토글한다 — 실제 강조·해제는 Hud.uss 의 transition 이 그린다.
    /// 개수는 <see cref="PlayerCoinInventory"/> 의 이벤트를 받아 숫자만 갈아 끼운다.
    ///
    /// UI 가 게임을 구독하지 그 반대가 아니다. NHNAI.Game 은 NHNAI.UI 를 참조할 수 없다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudScreen : MonoBehaviour
    {
        const string ActiveClass = "hud__reticle--active";
        const string MobileClass = "hud--mobile";
        const string VisibleClass = "hud--visible";

        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PlayerCoinInventory inventory;

        VisualElement _root;
        VisualElement _reticle;
        Label _coinCount;

        /// <summary>
        /// UIDocument 가 비주얼 트리를 만드는 시점이 이 컴포넌트의 OnEnable 보다
        /// 늦을 수 있어 미리 캐시하지 않고 처음 쓸 때 잡는다.
        /// </summary>
        VisualElement Root =>
            _root ??= GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("hud-root");

        VisualElement Reticle =>
            _reticle ??= GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("reticle");

        Label CoinCount =>
            _coinCount ??= GetComponent<UIDocument>().rootVisualElement?.Q<Label>("coin-count");

        void OnEnable()
        {
            if (interactor != null)
            {
                interactor.TargetChanged += OnTargetChanged;
                OnTargetChanged(interactor.HasTarget);   // 구독 전에 이미 조준 중일 수 있다
            }

            if (inventory != null)
            {
                inventory.CountChanged += OnCountChanged;
                OnCountChanged(inventory.Count);         // 초기 개수를 그려 둔다
            }
        }

        void OnDisable()
        {
            if (interactor != null) interactor.TargetChanged -= OnTargetChanged;
            if (inventory != null) inventory.CountChanged -= OnCountChanged;
        }

        /// <summary>
        /// 메인메뉴가 걷히기 시작할 때 불린다. HUD 는 그때까지 <c>opacity: 0</c> 으로
        /// 숨어 있다가 (Hud.uss) 여기서 떠오른다 — 메뉴 위에 조준점이 떠 있으면
        /// 아직 시작하지 않은 게임이 이미 시작한 것처럼 보인다.
        ///
        /// 모바일에서는 동전 개수를 위로 올린다. 우측 하단은 행동 버튼이 차지하는
        /// 자리라 그대로 두면 '사용' 버튼 뒤에 숨는다.
        /// </summary>
        public void Begin(ControlMode mode)
        {
            var root = Root;
            if (root == null) return;

            root.EnableInClassList(MobileClass, mode == ControlMode.Mobile);
            root.AddToClassList(VisibleClass);
        }

        void OnTargetChanged(bool hasTarget)
        {
            var reticle = Reticle;
            if (reticle == null) return;
            reticle.EnableInClassList(ActiveClass, hasTarget);
        }

        void OnCountChanged(int count)
        {
            var label = CoinCount;
            if (label == null) return;
            label.text = count.ToString();
        }

        public void Bind(PlayerInteractor value) => interactor = value;

        public void Bind(PlayerCoinInventory value) => inventory = value;
    }
}
