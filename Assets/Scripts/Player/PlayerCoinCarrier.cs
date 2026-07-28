using NHNAI.Game.Coins;
using NHNAI.Game.Slot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 잡은 동전을 들고 다니는 손. 카메라에 붙는다.
    ///
    /// 동선은 셋뿐이다 — 잡는다(<see cref="TryPickUp"/>, Coin.Interact 가 부른다),
    /// 다시 클릭해 놓는다, 투입구 앵커에 가까이 가져가면 **자동으로** 빨려 들어간다.
    /// 투입에 별도 클릭이 없는 것은 의도다: 잡은 동전의 용도가 하나뿐이라
    /// '가져가면 넣어진다' 가 가장 짧은 동선이다.
    ///
    /// 드는 동안 <see cref="PlayerInteractor"/> 를 통째로 끈다. 켜 두면 클릭 하나가
    /// '레버 당기기' 와 '동전 놓기' 로 갈라져 어느 쪽이 먹었는지 화면만 봐서는 알 수 없다.
    /// 끄면 조준점도 같이 사라져(인터랙터의 OnDisable 정리) 상태가 화면과 일치한다.
    ///
    /// 판정이 트리거가 아니라 **거리**인 이유: 인터랙터의 레이캐스트가 트리거를
    /// 무시하는 규약이라, 트리거를 섞으면 조준과 투입이 서로 다른 규칙으로 움직인다.
    /// 앵커(FBX 의 CoinSlot 원점 = 슬릿 입구)와의 거리 하나면 충분하다.
    /// </summary>
    public sealed class PlayerCoinCarrier : MonoBehaviour
    {
        [Header("들기")]
        [Tooltip("잡은 동전이 떠 있는 카메라 로컬 위치. 시선 정중앙 앞 — 드는 동안은 " +
                 "조준점이 꺼져 있어 가리는 것이 없고, 투입구를 바라보면 동전이 " +
                 "그대로 앵커 위에 겹쳐 흡입 판정이 정직해진다")]
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

        [Header("부품 — 부트스트랩이 채운다")]
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

        public bool IsCarrying => _held != null;

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(PlayerInteractor playerInteractor, SlotMachineView view, Transform intake)
        {
            interactor = playerInteractor;
            slotView = view;
            intakeAnchor = intake;
        }

        /// <summary><see cref="Coin.Interact"/> 가 부른다. 이미 들고 있으면 거절한다.</summary>
        public bool TryPickUp(Coin coin)
        {
            if (_held != null || coin == null) return false;

            _held = coin;
            _grabFrame = Time.frameCount;
            coin.AttachToHand();
            if (interactor != null) interactor.enabled = false;
            return true;
        }

        void Update()
        {
            TickInsert(Time.deltaTime);

            // 들고 있을 때의 클릭 = 놓기. 잡은 바로 그 클릭은 프레임 가드로 거른다.
            if (_held == null) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (Time.frameCount == _grabFrame) return;

            Drop();
        }

        // PlayerLook(Update) 이후에 돌아야 이 프레임의 시선을 따라온다.
        // Update 에서 하면 동전이 한 프레임 늦게 따라와 시선을 돌릴 때마다 헛돈다.
        void LateUpdate()
        {
            if (_held == null) return;

            var target = transform.TransformPoint(holdOffset);

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

            if (slotView != null) slotView.InsertCoin();
            Destroy(_inserting.gameObject);
            _inserting = null;
        }

        void Drop()
        {
            var coin = _held;
            _held = null;
            if (interactor != null) interactor.enabled = true;
            coin.Release(transform.forward * dropForward + Vector3.down * 0.2f);
        }
    }
}
