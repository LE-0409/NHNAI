using NHNAI.Game.Coins;
using NHNAI.Game.Slot;
using UnityEngine;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 잡은 동전을 들고 다니는 손. 카메라에 붙는다.
    ///
    /// 동선은 셋뿐이다 — 잡는다(<see cref="TryPickUp"/>, Coin.Interact 가 부른다),
    /// '사용'을 다시 눌러 놓는다, 투입구 앵커에 가까이 가져가면 **자동으로** 빨려 들어간다.
    /// 투입에 별도 입력이 없는 것은 의도다: 잡은 동전의 용도가 하나뿐이라
    /// '가져가면 넣어진다' 가 가장 짧은 동선이다.
    ///
    /// 인벤토리(<see cref="PlayerCoinInventory"/>)는 이 손을 거쳐 간다 — 넣기는
    /// <see cref="TakeHeld"/> 로 손을 비우고, 꺼내기는 <see cref="TryPickUp"/> 을
    /// 그대로 태워 마우스로 잡았을 때와 같은 들기 상태를 만든다.
    ///
    /// 드는 동안 <see cref="PlayerInteractor"/> 를 통째로 끈다. 켜 두면 '사용' 하나가
    /// '레버 당기기' 와 '동전 놓기' 로 갈라져 어느 쪽이 먹었는지 화면만 봐서는 알 수 없다.
    /// 끄면 조준점 강조도 같이 풀려(인터랙터의 OnDisable 정리) 상태가 화면과 일치한다.
    ///
    /// 판정이 트리거가 아니라 **거리**인 이유: 이 프로젝트에서 트리거는 '조준 전용
    /// 콜라이더' 표식이다 (<see cref="PlayerInteractor"/> 의 규약 — 레버·환불 버튼).
    /// 투입 볼륨까지 트리거로 만들면 같은 표식이 두 가지 뜻을 갖게 된다.
    /// 앵커(FBX 의 CoinSlot 원점 = 슬릿 입구)와의 거리 하나면 충분하다.
    /// </summary>
    public sealed class PlayerCoinCarrier : MonoBehaviour
    {
        [Header("들기")]
        [Tooltip("잡은 동전이 떠 있는 카메라 로컬 위치. 시선 정중앙 앞 — 드는 동안은 " +
                 "조준점이 희미한 기본 상태라 거슬리는 것이 없고, 투입구를 바라보면 " +
                 "동전이 그대로 앵커 위에 겹쳐 흡입 판정이 정직해진다")]
        [SerializeField] Vector3 holdOffset = new Vector3(0f, 0f, 0.45f);
        [Tooltip("추적 감쇠. 클수록 손이 시선을 빨리 따라잡는다")]
        [SerializeField] float followSharpness = 18f;

        [Header("투입")]
        [Tooltip("들린 동전이 투입구 앵커와 이 거리(m) 안이면 흡입이 시작된다. " +
                 "플레이어 충돌체가 조작대에 막혀 정면에서는 동전-앵커 거리가 " +
                 "0.16 아래로 잘 안 내려간다 — 그보다 여유 있게 잡는다")]
        [SerializeField] float intakeRadius = 0.2f;
        [Tooltip("흡입에 걸리는 시간(초). ease-in 이라 끝으로 갈수록 빨라진다")]
        [SerializeField] float insertTime = 0.28f;

        [Header("놓기")]
        [Tooltip("놓을 때 주는 전방 속도(m/s). 던지기가 아니라 떨어뜨리기다")]
        [SerializeField] float dropForward = 0.4f;

        [Header("사운드")]
        [Tooltip("잡기에 성공한 순간 재생")]
        [SerializeField] AudioClip pickupClip;
        [Tooltip("픽업음 음량")]
        [SerializeField, Range(0f, 1f)] float pickupVolume = 0.9f;
        [Tooltip("흡입이 끝나 동전이 슬릿으로 떨어지는 순간 재생")]
        [SerializeField] AudioClip insertClip;
        [Tooltip("투입음 음량")]
        [SerializeField, Range(0f, 1f)] float insertVolume = 0.9f;

        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] PlayerInputSource input;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] SlotMachineView slotView;
        [Tooltip("FBX 의 CoinSlot 트랜스폼. 원점이 슬릿 입구다")]
        [SerializeField] Transform intakeAnchor;

        Coin _held;          // 손에 들려 시선을 따라다니는 동전
        Coin _inserting;     // 흡입 중 — 손을 떠나 투입구로 날아가는 동전
        float _insertTimer;
        Vector3 _insertFrom;
        Quaternion _insertFromRot;
        int _grabFrame;      // 잡은 클릭이 같은 프레임에 '놓기' 로 겹치지 않게 하는 가드
        AudioSource _audio;
        AudioSource _slotAudio;

        public bool IsCarrying => _held != null;

        /// <summary>손 위치(월드). 인벤토리에서 꺼낸 동전을 이 위치로 순간이동시킨다.</summary>
        public Vector3 HandPosition => transform.TransformPoint(holdOffset);

        /// <summary>들린 동전의 자세 — 면(로컬 Y)이 카메라를 본다. LateUpdate 의 추적 목표와 같다.</summary>
        public Quaternion HandRotation => Quaternion.LookRotation(transform.up, -transform.forward);

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(PlayerInputSource source, PlayerInteractor playerInteractor,
                         SlotMachineView view, Transform intake,
                         AudioClip pickup, AudioClip insert)
        {
            input = source;
            interactor = playerInteractor;
            slotView = view;
            intakeAnchor = intake;
            pickupClip = pickup;
            insertClip = insert;
        }

        void Awake()
        {
            // 픽업음 스피커. 내 손이 내는 소리라 2D 로 낸다 — 이 컴포넌트는 리스너와
            // 같은 카메라에 붙어 있어 3D 로 둬도 거리 0 의 패닝만 남고 얻는 것이 없다.
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            // 투입음 스피커. 슬릿 입구에 붙여야 소리가 기계 쪽에서 난다.
            var speaker = intakeAnchor != null ? intakeAnchor.gameObject : gameObject;
            _slotAudio = speaker.AddComponent<AudioSource>();
            _slotAudio.playOnAwake = false;
            _slotAudio.spatialBlend = 1f;   // 3D — 투입구 위치에서 들려온다
            // 도플러를 끈다. 켜 두면 리스너(카메라)가 기계 반대쪽을 볼 때 소리가
            // 끊기는 Unity 버그가 있다 (Unity 6 에서도 재현). 투입구는 정지물이다.
            _slotAudio.dopplerLevel = 0f;
        }

        /// <summary><see cref="Coin.Interact"/> 가 부른다. 이미 들고 있으면 거절한다.</summary>
        public bool TryPickUp(Coin coin)
        {
            if (_held != null || coin == null) return false;

            _held = coin;
            _grabFrame = Time.frameCount;
            coin.AttachToHand();
            if (interactor != null) interactor.enabled = false;
            if (pickupClip != null) _audio.PlayOneShot(pickupClip, pickupVolume);
            return true;
        }

        void Update()
        {
            TickInsert(Time.deltaTime);

            // 들고 있을 때의 '사용' = 놓기. 잡은 바로 그 입력은 프레임 가드로 거른다.
            if (_held == null) return;
            if (input == null || !input.InteractPressed) return;
            if (Time.frameCount == _grabFrame) return;

            Drop();
        }

        // PlayerLook(Update) 이후에 돌아야 이 프레임의 시선을 따라온다.
        // Update 에서 하면 동전이 한 프레임 늦게 따라와 시선을 돌릴 때마다 헛돈다.
        void LateUpdate()
        {
            if (_held == null) return;

            var target = HandPosition;

            // 벽에 붙어 서면 손 위치가 벽 안이다. 시선 레이로 막힌 만큼 손을 당긴다 —
            // 안 하면 동전이 벽에 파묻힌 채 놓여 물리가 밀어내며 튄다.
            var origin = transform.position;
            var toTarget = target - origin;
            var dist = toTarget.magnitude;
            if (dist > 0.001f
                && Physics.Raycast(origin, toTarget / dist, out var hit, dist + 0.03f,
                                   ~0, QueryTriggerInteraction.Ignore))
                target = origin + toTarget / dist * Mathf.Max(hit.distance - 0.05f, 0.12f);

            var t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            var coin = _held.transform;
            coin.position = Vector3.Lerp(coin.position, target, t);
            // 면(로컬 Y)이 카메라를 본다 — 옆면만 보이면 들고 있는 게 뭔지 안 읽힌다.
            coin.rotation = Quaternion.Slerp(
                coin.rotation, Quaternion.LookRotation(transform.up, -transform.forward), t);

            if (intakeAnchor != null
                && (coin.position - intakeAnchor.position).sqrMagnitude
                   < intakeRadius * intakeRadius)
                BeginInsert();
        }

        void BeginInsert()
        {
            // 앞 동전이 아직 날아가는 중이면 즉시 완결한다. 흡입이 시작된 동전의
            // 적립은 이미 확정이라 미룰 의미가 없고, 완결하지 않으면 아래 대입이
            // _inserting 을 덮어써 앞 동전이 비물리 상태로 슬릿 앞 허공에 영영
            // 남는다 — 투입구 앞에서 Q 연타(0.28초 안에 재흡입)로 재현되던 버그.
            if (_inserting != null) FinishInsert();

            _inserting = _held;
            _held = null;
            _insertTimer = 0f;
            _insertFrom = _inserting.transform.position;
            _insertFromRot = _inserting.transform.rotation;
            // 손은 여기서 비워진다 — 동전이 날아가는 동안에도 레버를 조준할 수 있다.
            if (interactor != null) interactor.enabled = true;
        }

        void TickInsert(float deltaTime)
        {
            if (_inserting == null) return;

            _insertTimer += deltaTime;
            var t = Mathf.Clamp01(_insertTimer / insertTime);
            var e = t * t;   // ease-in. 기계가 빨아들이는 가속감

            var tr = _inserting.transform;
            tr.position = Vector3.Lerp(_insertFrom, intakeAnchor.position, e);
            // 슬릿에 맞춰 세로로 선다: 면 법선(로컬 Y)을 기계 좌우로 눕히면
            // 원반이 수직으로 서서 얇은 쪽부터 들어간다.
            tr.rotation = Quaternion.Slerp(
                _insertFromRot,
                Quaternion.LookRotation(intakeAnchor.forward, intakeAnchor.right), e);

            if (t < 1f) return;

            FinishInsert();
        }

        // 흡입 완결 — 투입음 · 크레딧 적립 · 동전 파괴. TickInsert 의 정상 종료와
        // BeginInsert 의 앞 동전 즉시 완결이 같은 경로를 타야 적립이 안 새어 나간다.
        void FinishInsert()
        {
            if (insertClip != null) _slotAudio.PlayOneShot(insertClip, insertVolume);
            if (slotView != null) slotView.InsertCoin();
            Destroy(_inserting.gameObject);
            _inserting = null;
        }

        /// <summary>
        /// 손의 동전을 물리로 되돌리지 않고 소유권째 회수한다 — 인벤토리 보관용.
        /// 동전은 들린 상태(비물리) 그대로 넘어가므로 받는 쪽이 끄든 되살리든 정한다.
        /// 들고 있지 않으면 null.
        /// </summary>
        public Coin TakeHeld()
        {
            if (_held == null) return null;
            var coin = _held;
            _held = null;
            if (interactor != null) interactor.enabled = true;
            return coin;
        }

        /// <summary>들고 있는 동전을 발 앞에 떨어뜨린다. 인벤토리의 '바꿔 들기'도 이걸 쓴다.</summary>
        public void Drop()
        {
            if (_held == null) return;
            var coin = _held;
            _held = null;
            if (interactor != null) interactor.enabled = true;
            coin.Release(transform.forward * dropForward + Vector3.down * 0.2f);
        }
    }
}
