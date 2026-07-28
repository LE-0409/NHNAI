using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// <see cref="SlotMachine"/> 이 계산한 각도를 실제 Transform 에 옮기고, 레버를 애니메이션한다.
    /// 스핀 틱 사운드도 여기서 낸다 — 각도가 분면 경계를 넘는 순간이 기준이라
    /// 회전이 느려지면 틱 간격도 같이 벌어진다.
    ///
    /// 릴과 레버는 FBX 안에서 별도 오브젝트로 나와 있고 **원점이 각자의 회전축**이다
    /// (생성 스크립트의 set_pivot). 그래서 여기서는 localRotation 만 건드리면 된다.
    /// 축은 셋 다 로컬 X 다 — Blender 에서 X 축으로 눕혀 뽑았다.
    ///
    /// 레버는 상태 기계에 넣지 않았다. 결과에 영향을 주지 않는 순수 연출이고,
    /// 규칙과 섞으면 애니메이션 길이가 게임 로직을 붙잡게 된다.
    /// </summary>
    public sealed class SlotMachineView : MonoBehaviour
    {
        [Header("부품")]
        [Tooltip("Reel_0 · Reel_1 · Reel_2 순서. 부트스트랩이 채운다")]
        [SerializeField] Transform[] reels = new Transform[SlotMachine.ReelCount];
        [SerializeField] Transform lever;
        [Tooltip("당첨 연출. 없어도 릴은 돈다")]
        [SerializeField] SlotMachineWinEffect winEffect;
        [Tooltip("당첨 동전을 뱉는 배출기. 없어도 릴은 돈다")]
        [SerializeField] CoinDispenser dispenser;

        [Header("스핀 사운드")]
        [Tooltip("릴이 분면(심볼 한 칸, 45도) 경계를 넘을 때마다 재생. 릴당 TickVoices 겹까지 겹친다")]
        [SerializeField] AudioClip tickClip;
        [Tooltip("틱 음량")]
        [SerializeField, Range(0f, 1f)] float tickVolume = 0.9f;

        [Header("레버 연출")]
        [Tooltip("당겼을 때 레버가 도는 각도(도, 로컬 X). 반대로 꺾이면 부호를 뒤집는다")]
        [SerializeField] float leverPullAngle = 62f;
        [SerializeField] float leverDownTime = 0.12f;
        [SerializeField] float leverHoldTime = 0.06f;
        [SerializeField] float leverReturnTime = 0.45f;

        readonly SlotMachine _machine = new();

        /// <summary>
        /// 릴 하나가 동시에 울릴 수 있는 틱 수(보이스 풀 크기).
        ///
        /// PlayOneShot 으로 꼬리(~2초)를 전부 살리면 빠른 스핀(릴당 초당 ~20틱 x 3릴)에서
        /// 동시 보이스가 100개를 넘어 Unity 상한(Max Real Voices, 기본 32)에 걸린다.
        /// 상한을 넘으면 엔진이 매 프레임 보이스를 뺏고 되살리며 **재생 중인 소리가
        /// 끊긴다** — 처음엔 멀쩡하다가 꼬리가 쌓이면서 끊기기 시작하는 증상이 그것이다.
        /// 풀 방식은 다르다: 4겹까지는 그대로 겹치고, 5번째 틱은 가장 오래된 틱을 끊고
        /// 그 소스를 재사용한다. 최고 속도(간격 50ms)에서도 끊기는 것은 200ms 지나
        /// 이미 잦아든 틱이라 귀에 안 잡히고, 총 보이스는 12개로 묶인다.
        /// </summary>
        const int TickVoices = 4;

        AudioSource[] _reelAudio;   // 릴마다 TickVoices 개 — [릴 x TickVoices + 슬롯]
        int[] _tickNext;            // 릴별 다음에 쓸 풀 슬롯 (라운드로빈)
        float[] _lastAngle;         // 분면 경계 통과 검출용 직전 각도

        float _leverTimer = -1f;   // 음수 = 쉬는 중

        public bool CanPull => _machine.CanPull && _leverTimer < 0f;

        /// <summary>넣어 둔 동전 수. 전원 상태(SlotMachinePower)가 읽는다.</summary>
        public int Credits => _machine.Credits;

        /// <summary>도는 중인가. 크레딧을 먼저 소모하므로 전원 판정에 이 값도 필요하다.</summary>
        public bool IsSpinning => _machine.State == SlotMachine.Phase.Spinning;

        /// <summary>캐리어가 흡입을 마친 동전을 크레딧으로 바꾼다.</summary>
        public void InsertCoin() => _machine.InsertCoin();

        void Awake()
        {
            // 릴마다 전용 스피커 풀. 릴 위치에서 각자 울려야 3D 에서 릴 셋이 따로
            // 도는 게 귀로도 갈라져 들린다. 풀 크기의 근거는 TickVoices 주석 참조.
            _reelAudio = new AudioSource[reels.Length * TickVoices];
            _tickNext = new int[reels.Length];
            _lastAngle = new float[reels.Length];
            for (var i = 0; i < reels.Length; i++)
            {
                if (reels[i] == null) continue;
                for (var v = 0; v < TickVoices; v++)
                {
                    var src = reels[i].gameObject.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.spatialBlend = 1f;   // 3D — 릴 위치에서 들려온다
                    // 도플러를 끈다. 켜 두면 리스너(카메라)가 기계 반대쪽을 볼 때 소리가
                    // 끊기는 Unity 버그가 있다 (Unity 6 에서도 재현). 기계는 정지물이라
                    // 도플러가 있어도 들리는 차이가 없다 — 끄는 쪽이 순손실이 없다.
                    src.dopplerLevel = 0f;
                    src.clip = tickClip;
                    _reelAudio[i * TickVoices + v] = src;
                }
                _lastAngle[i] = _machine.AngleOf(i);
            }
        }

        public void Pull()
        {
            if (!CanPull) return;
            // 시드를 시간에서 뽑는다. 런 시드가 생기면 그쪽에서 받아 재현 가능하게 만든다.
            _machine.Pull(Random.Range(int.MinValue, int.MaxValue));
            _leverTimer = 0f;
        }

        void Update()
        {
            var dt = Time.deltaTime;

            // 당첨은 **마지막 릴이 멈추는 순간** 한 번만 터져야 한다. Phase 를 매 프레임
            // 확인해 Done 이면 부르는 식으로 짜면 멈춘 내내 다시 켜진다.
            var wasSpinning = _machine.State == SlotMachine.Phase.Spinning;
            _machine.Tick(dt);
            if (wasSpinning && _machine.State == SlotMachine.Phase.Done)
            {
                if (winEffect != null) winEffect.OnSpinEnd(_machine.Result);
                // 빛의 펄스는 여기서 내지 않는다 — 배출기가 동전 하나당 하나씩 낸다.
                var payout = SlotMachine.PayoutOf(_machine.Result);
                if (payout > 0 && dispenser != null) dispenser.Dispense(payout);
            }

            for (var i = 0; i < reels.Length; i++)
            {
                if (reels[i] == null) continue;
                var angle = _machine.AngleOf(i);
                reels[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
                TickSound(i, angle);
            }

            TickLever(dt);
        }

        /// <summary>
        /// 릴 i 가 이번 프레임에 분면 경계(SymbolStep = 45도)를 넘었으면 틱을 한 번 낸다.
        ///
        /// 기준이 시간이 아니라 **각도**라서 재생 간격이 스핀 속도를 저절로 따라간다 —
        /// 빠를 때는 드르륵 몰아치고 감속하면 성기어지다가, 심볼에 붙는 마지막 스냅에서
        /// 한 번 더 울리며 끝난다. 각도는 스핀마다 리셋 없이 계속 자라므로 floor 비교면 된다.
        /// </summary>
        void TickSound(int i, float angle)
        {
            var prev = _lastAngle[i];
            _lastAngle[i] = angle;
            if (tickClip == null) return;

            var crossed = Mathf.FloorToInt(angle / SlotMachine.SymbolStep)
                        - Mathf.FloorToInt(prev / SlotMachine.SymbolStep);
            if (crossed <= 0) return;

            // 저프레임에서 한 프레임에 두 칸 이상 지나도 한 번만 낸다 — 같은 시각에 겹친
            // 동일 클립은 구분되지 않고 음량만 튄다.
            //
            // PlayOneShot 이 아니라 풀 소스의 Play() 다. 이 슬롯에서 아직 울리고 있던
            // 가장 오래된 틱 하나만 조용히 끊고 재사용한다 (근거는 TickVoices 주석).
            var src = _reelAudio[i * TickVoices + _tickNext[i]];
            if (src == null) return;
            _tickNext[i] = (_tickNext[i] + 1) % TickVoices;

            src.volume = tickVolume;   // 인스펙터에서 굴리는 값이 다음 틱부터 바로 먹게
            src.Play();
        }

        void TickLever(float deltaTime)
        {
            if (_leverTimer < 0f || lever == null) return;

            _leverTimer += deltaTime;

            var total = leverDownTime + leverHoldTime + leverReturnTime;
            float t;   // 0 = 원위치, 1 = 끝까지 당겨진 상태

            if (_leverTimer < leverDownTime)
            {
                // 당기는 구간은 빠르게 — 손이 없어서 레버 스스로 움직이는 것처럼 보이는데,
                // 느리게 움직이면 그 어색함이 드러난다.
                t = _leverTimer / leverDownTime;
            }
            else if (_leverTimer < leverDownTime + leverHoldTime)
            {
                t = 1f;
            }
            else if (_leverTimer < total)
            {
                var r = (_leverTimer - leverDownTime - leverHoldTime) / leverReturnTime;
                t = 1f - r * r;   // 되돌아올 때는 스프링처럼 처음이 빠르다
            }
            else
            {
                t = 0f;
                _leverTimer = -1f;
            }

            lever.localRotation = Quaternion.Euler(leverPullAngle * t, 0f, 0f);
        }

        /// <summary>부트스트랩이 FBX 계층에서 찾은 부품을 꽂아 준다.</summary>
        public void Bind(Transform[] reelTransforms, Transform leverTransform,
                         SlotMachineWinEffect effect, CoinDispenser coinDispenser,
                         AudioClip spinTickClip)
        {
            reels = reelTransforms;
            lever = leverTransform;
            winEffect = effect;
            dispenser = coinDispenser;
            tickClip = spinTickClip;
        }
    }
}
