using NHNAI.Game.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 화면 중앙에서 앞으로 레이캐스트해 쓸 수 있는 것을 찾고, 클릭하면 실행한다.
    ///
    /// 조준 상태는 <see cref="TargetChanged"/> 로 내보낸다. HUD 를 직접 부르지 않는 이유는
    /// 어셈블리 방향 때문이다 — NHNAI.UI 가 NHNAI.Game 을 참조하지 그 반대가 아니다.
    /// 그래서 UI 가 이 이벤트를 구독한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("손이 닿는 거리(m). 이보다 멀면 조준점이 뜨지 않는다")]
        [SerializeField] float reach = 3.6f;

        [Tooltip("조준 판정에 쓸 레이어. 기본은 전부")]
        [SerializeField] LayerMask mask = ~0;

        Camera _camera;
        Interactable _target;

        /// <summary>조준 대상이 생기거나 사라질 때. 값은 '지금 쓸 수 있는 것을 보고 있는가'.</summary>
        public event System.Action<bool> TargetChanged;

        /// <summary>구독 시점에 이미 조준 중일 수 있으므로 UI 가 초기 상태를 읽어간다.</summary>
        public bool HasTarget => _target != null;

        void Awake() => _camera = GetComponent<Camera>();

        void Update()
        {
            var found = Probe();

            if (!ReferenceEquals(found, _target))
            {
                _target = found;
                TargetChanged?.Invoke(_target != null);
            }

            if (_target != null
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame
                && Cursor.lockState == CursorLockMode.Locked)
            {
                _target.Interact();
            }
        }

        Interactable Probe()
        {
            // 화면 정중앙에서 카메라 정면으로. 1인칭이라 카메라가 곧 시선이다.
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, reach, mask, QueryTriggerInteraction.Ignore))
                return null;

            // Collider 와 같은 오브젝트에 있는 것만 인정한다. 부모까지 뒤지면
            // 캐비닛 아무 데나 조준해도 레버가 잡혀 '어디를 봐야 하는지' 가 흐려진다.
            var interactable = hit.collider.GetComponent<Interactable>();
            return interactable != null && interactable.CanInteract ? interactable : null;
        }
    }
}
