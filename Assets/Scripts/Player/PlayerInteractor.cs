using NHNAI.Game.Interaction;
using UnityEngine;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 화면 중앙에서 앞으로 레이캐스트해 쓸 수 있는 것을 찾고, '사용'이 눌리면 실행한다.
    /// PC 는 좌클릭, 모바일은 화면 위 버튼이다 — 어느 쪽인지는
    /// <see cref="PlayerInputSource"/> 가 흡수하므로 여기서는 구분하지 않는다.
    ///
    /// 조준 상태는 <see cref="TargetChanged"/> 로 내보낸다. HUD 를 직접 부르지 않는 이유는
    /// 어셈블리 방향 때문이다 — NHNAI.UI 가 NHNAI.Game 을 참조하지 그 반대가 아니다.
    /// 그래서 UI 가 이 이벤트를 구독한다.
    ///
    /// **트리거 콜라이더 = 조준 전용** 이 이 프로젝트의 규약이다. 레버·환불 버튼처럼
    /// 실루엣보다 넉넉한 조준 판정이 필요한 것은 트리거로 만들어 물리(충돌·낙하 동전·
    /// 손 위치 보정)에서 빼되, 이 컴포넌트의 레이만은 트리거를 **본다.**
    /// 그래서 여기서만 QueryTriggerInteraction.Collide 를 쓴다 — 물리 쪽 레이는 전부 Ignore 다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("손이 닿는 거리(m). 이보다 멀면 조준점이 강조되지 않는다")]
        [SerializeField] float reach = 3.6f;

        [Tooltip("조준 판정에 쓸 레이어. 기본은 전부")]
        [SerializeField] LayerMask mask = ~0;

        [Tooltip("정밀 레이가 빗나갔을 때의 보조 반경(m). 바닥의 동전처럼 작은 것을 노리기 쉽게 한다")]
        [SerializeField] float aimAssistRadius = 0.06f;

        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] PlayerInputSource input;

        Camera _camera;
        Interactable _target;

        /// <summary>조준 대상이 생기거나 사라질 때. 값은 '지금 쓸 수 있는 것을 보고 있는가'.</summary>
        public event System.Action<bool> TargetChanged;

        /// <summary>구독 시점에 이미 조준 중일 수 있으므로 UI 가 초기 상태를 읽어간다.</summary>
        public bool HasTarget => _target != null;

        void Awake() => _camera = GetComponent<Camera>();

        /// <summary>부트스트랩이 입력을 꽂아 준다.</summary>
        public void Bind(PlayerInputSource source) => input = source;

        void Update()
        {
            var found = Probe();

            if (!ReferenceEquals(found, _target))
            {
                _target = found;
                TargetChanged?.Invoke(_target != null);
            }

            // 입력을 받지 않는 동안(PC 에서 커서가 풀린 동안)은 소스가 거짓을 내보낸다.
            if (_target != null && input != null && input.InteractPressed)
            {
                _target.Interact();
            }
        }

        // 동전 캐리어가 드는 동안 이 컴포넌트를 끈다. 조준점이 강조된 채 남지 않게
        // 상태를 정리하고 나간다 — HUD 는 이벤트만 보므로 여기서 꺼 줘야 한다.
        void OnDisable()
        {
            if (_target == null) return;
            _target = null;
            TargetChanged?.Invoke(false);
        }

        Interactable Probe()
        {
            // 화면 정중앙에서 카메라 정면으로. 1인칭이라 카메라가 곧 시선이다.
            // Collide — 조준 전용 트리거(레버·환불 버튼)를 봐야 한다 (클래스 주석의 규약).
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out var hit, reach, mask, QueryTriggerInteraction.Collide))
            {
                var found = Valid(hit.collider);
                if (found != null) return found;
            }

            // 폴백: 바닥의 동전처럼 작은 것은 실루엣이 곧 조준 판정이라 바늘구멍이다.
            // 콜라이더를 키우는 대신(물리는 실루엣대로 타이트해야 한다) 조준만 넉넉하게
            // 한다. 레버처럼 큰 대상은 위의 정밀 레이가 먼저 잡아 감각이 변하지 않는다.
            if (Physics.SphereCast(ray, aimAssistRadius, out hit, reach, mask,
                                   QueryTriggerInteraction.Collide))
                return Valid(hit.collider);

            return null;
        }

        // Collider 와 같은 오브젝트에 있는 것만 인정한다. 부모까지 뒤지면
        // 캐비닛 아무 데나 조준해도 레버가 잡혀 '어디를 봐야 하는지' 가 흐려진다.
        static Interactable Valid(Collider collider)
        {
            var interactable = collider.GetComponent<Interactable>();
            return interactable != null && interactable.CanInteract ? interactable : null;
        }
    }
}
