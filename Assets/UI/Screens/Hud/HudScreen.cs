using NHNAI.Game.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.Hud
{
    /// <summary>
    /// 조준점 하나짜리 HUD. <see cref="PlayerInteractor"/> 가 내보내는 조준 상태를 받아
    /// 클래스만 토글한다 — 실제 등장·퇴장은 Hud.uss 의 transition 이 그린다.
    ///
    /// UI 가 게임을 구독하지 그 반대가 아니다. NHNAI.Game 은 NHNAI.UI 를 참조할 수 없다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudScreen : MonoBehaviour
    {
        const string ActiveClass = "hud__reticle--active";

        [SerializeField] PlayerInteractor interactor;

        VisualElement _reticle;

        /// <summary>
        /// UIDocument 가 비주얼 트리를 만드는 시점이 이 컴포넌트의 OnEnable 보다
        /// 늦을 수 있어 미리 캐시하지 않고 처음 쓸 때 잡는다.
        /// </summary>
        VisualElement Reticle =>
            _reticle ??= GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("reticle");

        void OnEnable()
        {
            if (interactor == null) return;
            interactor.TargetChanged += OnTargetChanged;
            OnTargetChanged(interactor.HasTarget);   // 구독 전에 이미 조준 중일 수 있다
        }

        void OnDisable()
        {
            if (interactor != null) interactor.TargetChanged -= OnTargetChanged;
        }

        void OnTargetChanged(bool hasTarget)
        {
            var reticle = Reticle;
            if (reticle == null) return;
            reticle.EnableInClassList(ActiveClass, hasTarget);
        }

        public void Bind(PlayerInteractor value) => interactor = value;
    }
}
