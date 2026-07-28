using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 당첨 연출 — **빛으로 방을 물들인다.**
    ///
    /// 팔레트가 완전 흑백이라 색으로는 강조할 수 없다. 남은 신호는 밝기와 움직임뿐인데,
    /// 방이 어둡고 광원이 전등 하나라 기계에서 나온 빛은 벽·바닥까지 그대로 드러난다.
    ///
    /// 번쩍임의 단위는 **동전**이다. 당첨 등급마다 고정 횟수를 치던 예전 방식을 버리고,
    /// <see cref="CoinDispenser"/> 가 동전을 하나 뱉을 때마다 <see cref="FlashCoin"/> 을
    /// 불러 펄스를 하나 낸다 — 번쩍임 수 = 배출 동전 수가 구조적으로 보장된다.
    /// 큰 성공(100개)은 펄스 100번이 이어져 10초짜리 빛의 소나기가 된다.
    /// 기계 진동만 스핀이 멎는 순간(<see cref="OnSpinEnd"/>) 한 번 친다.
    ///
    /// **발광 머티리얼(M_Emissive)을 건드리지 않는다.** 그건 에셋이라 런타임에 바꾸면
    /// 에디터의 .mat 이 더러워지고, 다음 <c>NHNAI/Setup/1</c> 실행까지 값이 남는다.
    /// 빛은 광원으로 낸다. (패널의 소등은 <see cref="SlotMachinePower"/> 가
    /// 런타임 **인스턴스**로 처리한다 — 에셋은 여전히 무사하다.)
    ///
    /// 릴 창 조명(reelLight)은 작성자가 둘이다 — 펄스 중에는 여기, 쉴 때는
    /// <see cref="SlotMachinePower"/>. 경계는 <see cref="Playing"/> 하나로 가른다.
    /// 쉴 때의 세기도 전원 상태에 따라 변하므로 캐시하지 않고
    /// <see cref="SlotMachinePower.ReelRestIntensity"/> 를 매번 읽는다.
    ///
    /// 코루틴 대신 Update 타이머를 쓴다 — <see cref="SlotMachineView"/> 의 레버 연출과
    /// 같은 방식이라, 펄스가 겹치게 다시 걸려도 상태가 하나뿐이라 꼬이지 않는다.
    /// </summary>
    public sealed class SlotMachineWinEffect : MonoBehaviour
    {
        [Header("부품 — 부트스트랩이 채운다")]
        [Tooltip("기계 앞 광원. 평소엔 세기 0 으로 꺼져 있다")]
        [SerializeField] Light roomLight;
        [Tooltip("릴 창 조명. 쉴 때의 세기는 전원 상태가 정한다")]
        [SerializeField] Light reelLight;
        [Tooltip("흔들 대상. 기계 루트다")]
        [SerializeField] Transform shakeTarget;
        [Tooltip("전원 상태. 릴 조명의 쉼 세기를 여기서 읽는다")]
        [SerializeField] SlotMachinePower power;

        // ⚠️ 광원이 기계 코앞이라 감쇠(거리 제곱)가 잔인하다. 4 m 떨어진 벽을 물들일
        // 세기를 주면 0.2 m 앞 마퀴는 반드시 하얗게 탄다 — 거리비가 400 배다.
        // 그 탄 자국이 곧 '번쩍' 이라 그대로 두었다.
        [Header("동전 펄스")]
        [Tooltip("펄스 정점에서 방 광원의 세기")]
        [SerializeField] float roomIntensity = 6f;
        [Tooltip("릴 창 조명이 쉴 때의 몇 배까지 오르는가")]
        [SerializeField] float reelBoost = 1.8f;
        [Tooltip("펄스 하나의 길이(초). CoinDispenser.interval 과 같아야 끊김 없이 이어진다")]
        [SerializeField] float pulseTime = 0.1f;
        [Tooltip("펄스 한 번에서 밝아지는 데 쓰는 비율. 작을수록 '탁' 켜진다")]
        [SerializeField, Range(0.02f, 0.5f)] float attackRatio = 0.14f;

        [Header("징글 홀드 — 노래가 흐르는 동안 반짝임 없이 켜 둔다")]
        [Tooltip("노래 동안 방 광원의 세기. 펄스 정점(roomIntensity)보다 낮아야 이후의 반짝임이 사건으로 읽힌다")]
        [SerializeField] float holdRoomIntensity = 2.5f;
        [Tooltip("노래 동안 릴 창 조명 = 쉼 세기 x 이 배율")]
        [SerializeField] float holdReelBoost = 1.4f;

        [Header("진동 (큰 성공만)")]
        [SerializeField] float shakeAmplitude = 0.006f;
        [SerializeField] float shakeTime = 0.32f;
        [SerializeField] float shakeFrequency = 26f;

        Vector3 _shakeHome;

        float _flashTimer = -1f;   // 음수 = 쉬는 중. 진동과 타이머를 나눈 이유:
        float _shakeTimer = -1f;   // 펄스는 동전마다, 진동은 스핀 끝에 한 번 — 주기가 다르다.
        bool _holding;             // 징글 홀드 중 — 빛을 일정하게 켜 둔다

        /// <summary>펄스나 홀드가 진행 중인가. 이 동안 릴 조명의 작성자는 이 컴포넌트고,
        /// 전원 상태(<see cref="SlotMachinePower"/>)도 이 값으로 기계를 살아 있다고 친다 —
        /// 크레딧 0 으로 노래를 듣는 동안 패널이 식으면 고장처럼 보인다.</summary>
        public bool Playing => _flashTimer >= 0f || _holding;

        float RestIntensity => power != null ? power.ReelRestIntensity : 0f;

        void Awake()
        {
            if (shakeTarget != null) _shakeHome = shakeTarget.localPosition;
        }

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(Light room, Light reel, Transform shake, SlotMachinePower machinePower)
        {
            roomLight = room;
            reelLight = reel;
            shakeTarget = shake;
            power = machinePower;
        }

        /// <summary>
        /// 세 릴이 다 멈춘 순간 <see cref="SlotMachineView"/> 가 부른다.
        /// 빛은 여기서 내지 않는다 — 배출 동전이 하나씩 낸다. 진동만 큰 성공에 친다.
        /// 작은 성공까지 흔들면 무게가 사라진다.
        /// </summary>
        public void OnSpinEnd(SlotMachine.Win result)
        {
            if (result == SlotMachine.Win.Big && shakeTarget != null) _shakeTimer = 0f;
        }

        /// <summary><see cref="CoinDispenser"/> 가 동전 하나를 뱉을 때마다 부른다.
        /// 진행 중인 펄스는 처음부터 다시 — 배출 간격과 맞물려 연속 펄스가 된다.</summary>
        public void FlashCoin() => _flashTimer = 0f;

        /// <summary>징글이 흐르는 동안 <see cref="SlotMachineView"/> 가 잡아 둔다.
        /// 반짝임 대신 일정한 빛 — 반짝임은 동전이 나올 때만의 신호로 남긴다.</summary>
        public void BeginHold() => _holding = true;

        /// <summary>징글이 끝났다. 빛을 내리면 곧바로 배출 펄스가 이어받는다.</summary>
        public void EndHold()
        {
            _holding = false;
            if (roomLight != null) roomLight.intensity = 0f;
            if (reelLight != null) reelLight.intensity = RestIntensity;
        }

        void Update()
        {
            TickFlash();
            TickShake();
        }

        void TickFlash()
        {
            // 홀드 중에는 펄스 대신 일정한 빛. 배출은 징글 뒤로 미뤄지므로 둘이 겹칠 일은
            // 없지만, 겹쳐도 홀드가 이긴다 — 노래 중에는 반짝이지 않는다는 약속이 우선이다.
            if (_holding)
            {
                if (roomLight != null) roomLight.intensity = holdRoomIntensity;
                if (reelLight != null) reelLight.intensity = RestIntensity * holdReelBoost;
                return;
            }

            if (_flashTimer < 0f) return;

            _flashTimer += Time.deltaTime;
            if (_flashTimer >= pulseTime)
            {
                _flashTimer = -1f;
                if (roomLight != null) roomLight.intensity = 0f;
                if (reelLight != null) reelLight.intensity = RestIntensity;
                return;
            }

            var e = Envelope(_flashTimer / pulseTime);
            if (roomLight != null) roomLight.intensity = roomIntensity * e;
            if (reelLight != null)
                reelLight.intensity = Mathf.Lerp(RestIntensity, RestIntensity * reelBoost, e);
        }

        /// <summary>
        /// 펄스 하나의 모양 (x: 0~1). 확 밝아지고 천천히 꺼진다. 반대로 하면 '켜졌다' 가
        /// 아니라 '켜져 있다' 로 읽혀서 사건이 아니라 상태처럼 보인다.
        /// </summary>
        float Envelope(float x)
        {
            var e = x < attackRatio
                ? x / attackRatio
                : 1f - (x - attackRatio) / (1f - attackRatio);
            return e * e;   // 제곱해 꼬리를 짧게. 선형이면 내내 훤한 느낌이 남는다
        }

        void TickShake()
        {
            if (_shakeTimer < 0f || shakeTarget == null) return;

            _shakeTimer += Time.deltaTime;
            var left = 1f - _shakeTimer / shakeTime;
            if (left <= 0f)
            {
                _shakeTimer = -1f;
                shakeTarget.localPosition = _shakeHome;
                return;
            }

            // 난수 대신 Perlin — 프레임마다 튀지 않고 흔들리는 결이 이어진다.
            // 앞뒤(Z)로는 흔들지 않는다. 플레이어 쪽으로 다가왔다 멀어지면 기계가
            // 미끄러지는 것처럼 보인다.
            var a = shakeAmplitude * left * left;
            var p = _shakeTimer * shakeFrequency;
            shakeTarget.localPosition = _shakeHome + new Vector3(
                (Mathf.PerlinNoise(p, 0.7f) - 0.5f) * 2f * a,
                (Mathf.PerlinNoise(p, 3.1f) - 0.5f) * 2f * a,
                0f);
        }
    }
}
