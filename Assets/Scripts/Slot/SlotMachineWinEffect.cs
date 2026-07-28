using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 성공 연출 — **빛으로 방을 물들인다.**
    ///
    /// 팔레트가 완전 흑백이라 색으로는 강조할 수 없다. 남은 신호는 밝기와 움직임뿐인데,
    /// 방이 어둡고 광원이 전등 하나라 기계에서 나온 빛은 벽·바닥까지 그대로 드러난다.
    /// 그래서 세 가지를 겹친다.
    ///
    ///   1. 기계 앞 광원(WinLight)이 켜져 마퀴 · 조작대를 밝히고 그 빛이 방으로 번진다
    ///   2. 릴 창 조명이 같이 타올라 결과가 난 자리를 가리킨다
    ///   3. 큰 성공에만 기계가 짧게 떤다 — 작은 성공까지 흔들면 무게가 사라진다
    ///
    /// **발광 머티리얼(M_Emissive)을 건드리지 않는다.** 그건 에셋이라 런타임에 바꾸면
    /// 에디터의 .mat 이 더러워지고, 다음 <c>NHNAI/Setup/1</c> 실행까지 값이 남는다.
    /// 빛은 광원으로 낸다.
    ///
    /// 코루틴 대신 Update 타이머를 쓴다 — <see cref="SlotMachineView"/> 의 레버 연출과
    /// 같은 방식이라, 연출 도중에 다시 당겨도 상태가 하나뿐이라 겹쳐 꼬이지 않는다.
    /// </summary>
    public sealed class SlotMachineWinEffect : MonoBehaviour
    {
        [Header("부품 — 부트스트랩이 채운다")]
        [Tooltip("기계 앞 광원. 평소엔 세기 0 으로 꺼져 있다")]
        [SerializeField] Light roomLight;
        [Tooltip("릴 창 조명. 쉴 때의 세기를 Awake 에서 기억해 두고 그 위로 올린다")]
        [SerializeField] Light reelLight;
        [Tooltip("흔들 대상. 기계 루트다")]
        [SerializeField] Transform shakeTarget;

        // ⚠️ 광원이 기계 코앞이라 감쇠(거리 제곱)가 잔인하다. 4 m 떨어진 벽을 물들일
        // 세기를 주면 0.2 m 앞 마퀴는 반드시 하얗게 탄다 — 거리비가 400 배다.
        // 그 탄 자국이 곧 '번쩍' 이라 그대로 두었다. 기계가 너무 타면 여기를 낮추는데,
        // 낮추면 방도 같이 어두워진다. 둘을 따로 놀게 하려면 광원을 하나 더 놓아야 한다.
        [Header("세기")]
        [SerializeField] float bigRoomIntensity = 20f;
        [SerializeField] float smallRoomIntensity = 6f;
        [Tooltip("릴 창 조명이 쉴 때의 몇 배까지 오르는가")]
        [SerializeField] float bigReelBoost = 3f;
        [SerializeField] float smallReelBoost = 1.8f;

        [Header("길이")]
        [SerializeField] int bigPulses = 3;
        [SerializeField] int smallPulses = 1;
        [SerializeField] float pulseTime = 0.42f;
        [Tooltip("펄스 한 번에서 밝아지는 데 쓰는 비율. 작을수록 '탁' 켜진다")]
        [SerializeField, Range(0.02f, 0.5f)] float attackRatio = 0.14f;

        [Header("진동 (큰 성공만)")]
        [SerializeField] float shakeAmplitude = 0.006f;
        [SerializeField] float shakeTime = 0.32f;
        [SerializeField] float shakeFrequency = 26f;

        float _reelRest;
        Vector3 _shakeHome;

        float _timer = -1f;   // 음수 = 쉬는 중
        float _duration;
        float _roomPeak;
        float _reelPeak;
        bool _shaking;

        public bool Playing => _timer >= 0f;

        void Awake()
        {
            _reelRest = reelLight != null ? reelLight.intensity : 0f;
            if (shakeTarget != null) _shakeHome = shakeTarget.localPosition;
            Rest();
        }

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(Light room, Light reel, Transform shake)
        {
            roomLight = room;
            reelLight = reel;
            shakeTarget = shake;
        }

        /// <summary>세 릴이 다 멈춘 순간 <see cref="SlotMachineView"/> 가 부른다.</summary>
        public void Play(SlotMachine.Win result)
        {
            if (result == SlotMachine.Win.None) return;

            var big = result == SlotMachine.Win.Big;
            _roomPeak = big ? bigRoomIntensity : smallRoomIntensity;
            _reelPeak = _reelRest * (big ? bigReelBoost : smallReelBoost);
            _duration = pulseTime * Mathf.Max(1, big ? bigPulses : smallPulses);
            _shaking = big && shakeTarget != null;
            _timer = 0f;
        }

        void Update()
        {
            if (_timer < 0f) return;

            _timer += Time.deltaTime;
            if (_timer >= _duration)
            {
                _timer = -1f;
                _shaking = false;
                Rest();
                return;
            }

            var e = Envelope(_timer);
            if (roomLight != null) roomLight.intensity = _roomPeak * e;
            if (reelLight != null) reelLight.intensity = Mathf.Lerp(_reelRest, _reelPeak, e);
            if (_shaking) TickShake();
        }

        /// <summary>
        /// 펄스 하나의 모양. 확 밝아지고 천천히 꺼진다. 반대로 하면 '켜졌다' 가 아니라
        /// '켜져 있다' 로 읽혀서 사건이 아니라 상태처럼 보인다.
        /// </summary>
        float Envelope(float t)
        {
            var x = Mathf.Repeat(t, pulseTime) / pulseTime;
            var e = x < attackRatio
                ? x / attackRatio
                : 1f - (x - attackRatio) / (1f - attackRatio);
            return e * e;   // 제곱해 꼬리를 짧게. 선형이면 내내 훤한 느낌이 남는다
        }

        void TickShake()
        {
            var left = 1f - _timer / shakeTime;
            if (left <= 0f)
            {
                _shaking = false;
                shakeTarget.localPosition = _shakeHome;
                return;
            }

            // 난수 대신 Perlin — 프레임마다 튀지 않고 흔들리는 결이 이어진다.
            // 앞뒤(Z)로는 흔들지 않는다. 플레이어 쪽으로 다가왔다 멀어지면 기계가
            // 미끄러지는 것처럼 보인다.
            var a = shakeAmplitude * left * left;
            var p = _timer * shakeFrequency;
            shakeTarget.localPosition = _shakeHome + new Vector3(
                (Mathf.PerlinNoise(p, 0.7f) - 0.5f) * 2f * a,
                (Mathf.PerlinNoise(p, 3.1f) - 0.5f) * 2f * a,
                0f);
        }

        /// <summary>쉬는 상태로 되돌린다. 중간에 끊겨도 기계가 어긋난 자리에 남지 않게.</summary>
        void Rest()
        {
            if (roomLight != null) roomLight.intensity = 0f;
            if (reelLight != null) reelLight.intensity = _reelRest;
            if (shakeTarget != null) shakeTarget.localPosition = _shakeHome;
        }
    }
}
