using NHNAI.Game.Interaction;
using NHNAI.Game.Player;
using UnityEngine;

namespace NHNAI.Game.Coins
{
    /// <summary>
    /// 동전 — 게임의 화폐이자 물리 오브젝트. 잡아서(<see cref="Interact"/>) 들고 다니다
    /// 투입구에 넣으면 크레딧이 된다. 들리는 동안은 물리에서 빠지고
    /// (<see cref="AttachToHand"/>), 놓으면 다시 떨어져 구른다(<see cref="Release"/>).
    ///
    /// 물리 상수를 여기 const 로 두는 이유: 이 값들의 정본이 프리팹 인스펙터에 있으면
    /// 씬을 다시 생성할 때(부트스트랩) 값이 어디서 왔는지 알 수 없게 된다.
    /// 부트스트랩이 템플릿을 만들 때 이 상수를 읽어 Rigidbody · Collider 에 박는다.
    ///
    /// 콜라이더는 **convex MeshCollider 하나**다 — Coin.fbx 의 12각 원반 그대로라
    /// 물리 실루엣이 타이트하다. 작은 동전을 조준하기 어려운 문제는 콜라이더를
    /// 키우지 않고 <see cref="PlayerInteractor"/> 의 SphereCast 폴백이 해결한다.
    /// "물리는 타이트하게, 조준은 넉넉하게" 를 한 오브젝트에서 같이 만족시키는 방법이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Coin : Interactable
    {
        // --- 물리 상수의 정본 -------------------------------------------------
        /// <summary>질량(kg). 실제 대형 주화 무게 근처. 너무 가벼우면 튕김이 호들갑스럽다.</summary>
        public const float Mass = 0.02f;
        /// <summary>반발 계수. 금속 동전의 '쨍' 한 튕김. 1 에 가까우면 멎지 않는다.</summary>
        public const float Bounciness = 0.35f;
        /// <summary>마찰. 낮아야 트레이 안에서 미끄러지며 자리를 잡는다.</summary>
        public const float Friction = 0.25f;
        /// <summary>
        /// 콜라이더 접촉 여유(m). 기본 0.01 은 동전 두께 0.007 보다 **두꺼워서**
        /// 서로 닿기도 전에 접촉 판정이 나 공중에 뜨거나 떤다. 두께의 1/3 로 줄인다.
        /// </summary>
        public const float ContactOffset = 0.002f;
        /// <summary>
        /// 전역 반발 임계(m/s). 기본 2 는 이보다 느린 충돌의 반발을 전부 죽이는데,
        /// 트레이 안에서 동전끼리 부딪히는 속도는 대부분 그 아래다.
        /// CoinDispenser 가 Awake 에서 Physics.bounceThreshold 에 적용한다.
        /// </summary>
        public const float BounceThreshold = 0.4f;

        [Header("부품 — 부트스트랩이 템플릿에 채우고, 복제가 물려받는다")]
        [Tooltip("잡기를 받아 주는 캐리어. 카메라에 붙어 있다")]
        [SerializeField] PlayerCoinCarrier carrier;
        [Tooltip("플레이어 충돌체. 발에 밟혀 사출되지 않게 충돌을 서로 무시한다")]
        [SerializeField] Collider playerBody;

        Rigidbody _body;
        Collider _collider;

        /// <summary>캐리어 손에 들려 있는가. 들린 동전은 다시 잡을 수 없다.</summary>
        public bool IsHeld { get; private set; }

        public override bool CanInteract => !IsHeld;

        public override void Interact()
        {
            if (carrier != null) carrier.TryPickUp(this);
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }

        void OnEnable()
        {
            // CharacterController 는 동전을 밀어내지 못하고 밟으면 튕겨 올린다.
            // 발은 화면에 안 보이므로 그냥 서로 통과시키는 쪽이 조용하다.
            if (playerBody != null && _collider != null)
                Physics.IgnoreCollision(_collider, playerBody, true);
        }

        /// <summary>부트스트랩이 씬의 템플릿에 꽂아 준다. 복제는 값을 그대로 물려받는다.</summary>
        public void Bind(PlayerCoinCarrier coinCarrier, Collider player)
        {
            carrier = coinCarrier;
            playerBody = player;
        }

        /// <summary>손에 들린다. 물리에서 빠지고 조준 대상에서도 빠진다.</summary>
        public void AttachToHand()
        {
            IsHeld = true;
            _body.isKinematic = true;
            _collider.enabled = false;
        }

        /// <summary>손에서 떨어진다. 물리로 돌아가 주어진 속도로 낙하한다.</summary>
        public void Release(Vector3 velocity)
        {
            IsHeld = false;
            _collider.enabled = true;
            _body.isKinematic = false;
            _body.linearVelocity = velocity;
        }
    }
}
