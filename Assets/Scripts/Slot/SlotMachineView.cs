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
    /// 레버는 상태 기계에 넣지 않았다. 결과에 영향을 주지 않는 연출이고,
    /// 규칙과 섞으면 애니메이션 길이가 게임 로직을 붙잡게 된다. 다만 **시점** 하나는
    /// 레버가 정한다 — 스핀은 레버가 바닥을 치는 순간 시작한다 (TickLever 참조).
    ///
    /// 당첨 사운드도 여기서 오케스트레이션한다: 당첨 등급(2개·3개 일치)마다 징글을
    /// 틀고, 배출은 징글이 끝날 때까지 미룬다. 그동안 불빛은 <see cref="SlotMachineWinEffect"/> 의
    /// 홀드로 반짝임 없이 켜 둔다 — 반짝임은 동전이 나올 때만의 신호다.
    /// 레버는 판정 → 징글 → 배출이 다 끝날 때까지 잠긴다 (<see cref="CanPull"/>).
    /// 노래가 겹치면 큰 성공만 진행 중인 노래를 끊고 처음부터 다시 튼다 —
    /// 작은 성공이 큰 노래를 자르면 위계가 뒤집힌다.
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

        // 레버는 2단으로 내려간다 — LeverDown.mp3 에 걸림음이 두 번 들어 있어서다.
        // 시각값 둘(First/SecondHitTime)은 그 클립을 파형으로 잰 값이라, 클립을 갈면 같이 다시 잰다.
        [Header("레버 연출 — LeverDown.mp3 의 걸림음 2회와 맞물린다")]
        [Tooltip("당겼을 때 레버가 도는 각도(도, 로컬 X). 반대로 꺾이면 부호를 뒤집는다")]
        [SerializeField] float leverPullAngle = 62f;
        [Tooltip("1단이 멈추는 시각(초) = 다운 클립의 첫(약한) 걸림음")]
        [SerializeField] float leverFirstHitTime = 0.28f;
        [Tooltip("2단이 바닥을 치는 시각(초) = 다운 클립의 둘째(강한) 걸림음. 이 순간 릴이 돌기 시작한다")]
        [SerializeField] float leverSecondHitTime = 0.58f;
        [Tooltip("2단 움직임의 길이(초). 시작 시각은 '둘째 걸림음 − 이 값' 이다")]
        [SerializeField] float leverStage2Time = 0.12f;
        [Tooltip("1단이 끝났을 때 내려간 비율")]
        [SerializeField, Range(0f, 1f)] float leverMidFraction = 0.55f;
        [SerializeField] float leverHoldTime = 0.06f;
        [Tooltip("복귀 시간(초). LeverUp.mp3 의 걸림음(~0.46초)이 도착 순간 울리도록 맞춰져 있다")]
        [SerializeField] float leverReturnTime = 0.45f;

        [Header("레버 사운드")]
        [Tooltip("당길 때 재생. 걸림음 2회가 들어 있고 레버 시각이 여기 맞춰져 있다")]
        [SerializeField] AudioClip leverDownClip;
        [Tooltip("복귀할 때 재생. 걸림음 1회 — 레버가 제자리를 때리는 소리다")]
        [SerializeField] AudioClip leverUpClip;
        [SerializeField, Range(0f, 1f)] float leverVolume = 0.9f;

        [Header("당첨 사운드 — 동전은 노래가 다 끝난 뒤에야 나온다")]
        [Tooltip("큰 성공(3개 일치)에서 재생")]
        [SerializeField] AudioClip winClip;
        [Tooltip("작은 성공(2개 일치)에서 재생. 흐름은 큰 성공과 같다 — 노래 → 배출")]
        [SerializeField] AudioClip smallWinClip;
        [SerializeField, Range(0f, 1f)] float winVolume = 0.9f;

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

        AudioSource _leverAudio;    // 레버 위치에서 당김·복귀음을 낸다
        AudioSource _winAudio;      // 기계 루트에서 당첨 징글을 낸다. Play() 라 재당첨이면 처음부터

        float _leverTimer = -1f;   // 음수 = 쉬는 중
        int _queuedSeed;           // 클릭 순간 뽑아 두고, 스핀은 레버가 바닥을 칠 때 시작한다
        bool _spinQueued;
        bool _upSoundPlayed;

        float _winWait = -1f;      // 징글이 끝나기까지 남은 시간. 음수 = 징글 없음
        int _pendingPayout;        // 징글이 끝나면 한꺼번에 배출기로 넘긴다

        /// <summary>
        /// **승리가 정해지는 순간부터** 배출이 끝날 때까지 당길 수 없다.
        ///
        /// 기준을 배출 시작이 아니라 판정 순간으로 잡는 이유: 당첨의 한 판은
        /// 판정 → 징글 → 배출까지가 한 덩어리다. 배출만 막으면 징글이 흐르는
        /// 동안 게이트가 비어, 승리 노래 위로 다음 판의 릴이 도는 그림이 나온다.
        /// 릴이 멈춰 있고(Spinning 아님) 배출은 아직 시작 전이라 두 조건 다
        /// 통과해 버리기 때문이다. <see cref="_winWait"/> 는 마지막 릴이 멈춘
        /// 프레임에 세워지므로 그 틈이 정확히 메워진다.
        ///
        /// 배출 중을 막는 이유는 그대로다 — 앞 판의 소나기와 새 판의 릴 소리가
        /// 섞이면 어느 판의 돈인지 읽을 수 없다.
        /// 조준점 강조도 이 게이트를 그대로 따라 흐려진다.
        /// </summary>
        public bool CanPull => _machine.CanPull && _leverTimer < 0f && _winWait < 0f
                            && (dispenser == null || !dispenser.Dispensing);

        /// <summary>넣어 둔 동전 수. 전원 상태(SlotMachinePower)가 읽는다.</summary>
        public int Credits => _machine.Credits;

        /// <summary>도는 중인가. 크레딧을 먼저 소모하므로 전원 판정에 이 값도 필요하다.</summary>
        public bool IsSpinning => _machine.State == SlotMachine.Phase.Spinning;

        /// <summary>캐리어가 흡입을 마친 동전을 크레딧으로 바꾼다.</summary>
        public void InsertCoin() => _machine.InsertCoin();

        /// <summary>
        /// 환불이 가능한가. 스핀 중에는 막는다 — 이미 크레딧 하나가 릴에 걸려 있어
        /// '전부 돌려받았다' 는 인상과 실제 잔액이 어긋난다. 배출 중에도 막는다 —
        /// 당첨 소나기에 환불 동전이 섞이면 딴 돈과 되찾은 돈이 구분되지 않는다.
        /// 레버 동작 중과 징글 중에도 막는다 — 레버 타임라인에는 아직 크레딧을
        /// 소모하지 않은 스핀 예약이 걸려 있어 환불이 그 크레딧을 빼앗으면 레버가
        /// 헛손질이 되고, 징글 뒤로 미뤄 둔 배출에는 환불이 끼어들면 안 된다.
        /// </summary>
        public bool CanRefund => dispenser != null && !dispenser.Dispensing
                              && _machine.Credits > 0 && !IsSpinning
                              && _leverTimer < 0f && _winWait < 0f;

        /// <summary>넣어 둔 동전을 전부 되뱉는다. 배출 경로는 당첨과 같다 — 기계의 출구는 하나다.</summary>
        public void Refund()
        {
            if (!CanRefund) return;
            dispenser.Dispense(_machine.TakeAllCredits());
        }

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

            // 레버음은 레버 위치에서, 징글은 기계 루트에서. 설정은 틱 스피커와 같다 —
            // 3D 로 기계에서 들려오고, 도플러는 위의 Unity 버그 때문에 끈다.
            _leverAudio = MakeSpeaker(lever != null ? lever.gameObject : gameObject);
            _winAudio = MakeSpeaker(gameObject);   // 클립은 당첨 등급에 따라 재생 직전에 꽂는다
        }

        static AudioSource MakeSpeaker(GameObject host)
        {
            var src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.dopplerLevel = 0f;
            return src;
        }

        public void Pull()
        {
            if (!CanPull) return;
            // 시드는 지금 뽑아 두지만 스핀은 레버가 바닥을 치는 순간 시작한다 —
            // 릴은 레버가 돌리는 것이지 클릭이 돌리는 것이 아니다. 그 사이 크레딧이
            // 줄어들 길은 환불뿐인데, CanRefund 가 레버 동작 중을 막아 예약이 무산될
            // 일은 없다 (늘어나는 것은 무해하다 — 소모는 예약된 스핀이 한다).
            _queuedSeed = Random.Range(int.MinValue, int.MaxValue);
            _spinQueued = true;
            _upSoundPlayed = false;
            _leverTimer = 0f;
            if (leverDownClip != null) _leverAudio.PlayOneShot(leverDownClip, leverVolume);
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
                var jingle = _machine.Result == SlotMachine.Win.Big ? winClip
                           : _machine.Result == SlotMachine.Win.Small ? smallWinClip
                           : null;
                if (payout > 0 && jingle != null
                    && (_machine.Result == SlotMachine.Win.Big || _winWait < 0f))
                {
                    // 노래가 다 끝난 뒤에야 동전이 나온다. 노래 동안 불빛은 반짝이지
                    // 않고 켜져만 있다 (WinEffect 의 홀드).
                    //
                    // 조건의 뒷부분(큰 성공이거나 노래 중이 아닐 것)은 **지금은 닿지
                    // 않는다** — CanPull 이 징글 구간을 막은 뒤로 노래 중에 새 스핀이
                    // 시작될 길이 없어 재당첨 자체가 생기지 않는다. 그래도 남겨 둔 것은
                    // 게이트가 느슨해졌을 때의 위계다: 큰 성공만 진행 중인 노래를 끊고
                    // 처음부터 다시 틀고, 작은 성공은 아래 분기로 빠져 노래를 존중한다.
                    _winAudio.clip = jingle;
                    _winAudio.volume = winVolume;   // 인스펙터에서 굴린 값이 다음 재생부터 먹게
                    _winAudio.Play();
                    _winWait = jingle.length;
                    _pendingPayout += payout;
                    if (winEffect != null) winEffect.BeginHold();
                }
                else if (payout > 0)
                {
                    // 노래를 못 트는 당첨 — 실질적으로는 클립 미배선뿐이다 (위 주석의
                    // '노래 중의 작은 성공' 은 게이트가 막아 더는 오지 않는다).
                    // 그래도 징글이 흐르는 중이면 노래를 존중해 뒤로 미루고, 아니면 즉시
                    // 뱉는다 — 노래 중에는 반짝이지 않는다는 약속이 여기서도 지켜져야 한다.
                    if (_winWait >= 0f) _pendingPayout += payout;
                    else if (dispenser != null) dispenser.Dispense(payout);
                }
            }

            TickWinWait(dt);

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

        /// <summary>징글이 끝나기를 기다렸다가 쌓인 배출을 한꺼번에 넘긴다.</summary>
        void TickWinWait(float deltaTime)
        {
            if (_winWait < 0f) return;

            _winWait -= deltaTime;
            if (_winWait >= 0f) return;

            _winWait = -1f;
            if (winEffect != null) winEffect.EndHold();
            if (_pendingPayout > 0 && dispenser != null) dispenser.Dispense(_pendingPayout);
            _pendingPayout = 0;
        }

        void TickLever(float deltaTime)
        {
            if (_leverTimer < 0f) return;

            _leverTimer += deltaTime;
            var returnStart = leverSecondHitTime + leverHoldTime;

            // 바닥을 치는 순간(둘째 걸림음) 스핀 시작 — 레버가 릴을 돌리는 것처럼 보인다.
            if (_spinQueued && _leverTimer >= leverSecondHitTime)
            {
                _spinQueued = false;
                _machine.Pull(_queuedSeed);
            }

            // 복귀음은 복귀가 시작될 때 튼다. 클립의 걸림음이 ~0.46초 뒤라,
            // leverReturnTime(0.45초)을 달려 제자리를 때리는 순간과 겹친다.
            if (!_upSoundPlayed && _leverTimer >= returnStart)
            {
                _upSoundPlayed = true;
                if (leverUpClip != null) _leverAudio.PlayOneShot(leverUpClip, leverVolume);
            }

            var t = LeverPose(_leverTimer, returnStart);   // 0 = 원위치, 1 = 바닥
            if (_leverTimer >= returnStart + leverReturnTime)
            {
                t = 0f;
                _leverTimer = -1f;
            }

            if (lever != null) lever.localRotation = Quaternion.Euler(leverPullAngle * t, 0f, 0f);
        }

        /// <summary>
        /// 레버 타임라인의 시각 → 당김 비율. 2단 하강이다:
        /// 클릭 즉시 움직여 첫 걸림음에 눌러앉고(1단), 잠깐 버티다 가속하며 바닥을
        /// 때린다(2단, 둘째 걸림음). 복귀는 예전처럼 한 번에 스프링으로 돌아온다.
        /// </summary>
        float LeverPose(float timer, float returnStart)
        {
            // 시각을 인스펙터에서 굴려도 구간이 뒤집히지 않게 묶는다.
            var stage2Start = Mathf.Max(leverSecondHitTime - leverStage2Time, leverFirstHitTime);

            if (timer < leverFirstHitTime)
            {
                // 1단: smoothstep — 걸림에 눌러앉는 느낌. 손이 없어도 어색하지 않은 속도다.
                var x = timer / Mathf.Max(leverFirstHitTime, 0.0001f);
                return leverMidFraction * x * x * (3f - 2f * x);
            }
            if (timer < stage2Start) return leverMidFraction;
            if (timer < leverSecondHitTime)
            {
                // 2단: 가속하며 바닥을 때린다 — 둘째 걸림음이 크고 무거운 이유.
                var x = (timer - stage2Start) / Mathf.Max(leverSecondHitTime - stage2Start, 0.0001f);
                return leverMidFraction + (1f - leverMidFraction) * x * x;
            }
            if (timer < returnStart) return 1f;

            var r = (timer - returnStart) / Mathf.Max(leverReturnTime, 0.0001f);
            return Mathf.Max(1f - r * r, 0f);   // 스프링 복귀. 끝점을 때리는 순간 복귀음의 걸림음이 울린다
        }

        /// <summary>부트스트랩이 FBX 계층에서 찾은 부품을 꽂아 준다.</summary>
        public void Bind(Transform[] reelTransforms, Transform leverTransform,
                         SlotMachineWinEffect effect, CoinDispenser coinDispenser,
                         AudioClip spinTickClip, AudioClip leverDown, AudioClip leverUp,
                         AudioClip winJingle, AudioClip smallWinJingle)
        {
            reels = reelTransforms;
            lever = leverTransform;
            winEffect = effect;
            dispenser = coinDispenser;
            tickClip = spinTickClip;
            leverDownClip = leverDown;
            leverUpClip = leverUp;
            winClip = winJingle;
            smallWinClip = smallWinJingle;
        }
    }
}
