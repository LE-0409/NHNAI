using System;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 슬롯머신 규칙 상태 기계. 씬도 UI 도 모른다 — 각도만 계산한다.
    /// 화면에 옮기는 일은 <c>SlotMachineView</c> 가 맡는다.
    ///
    /// 릴은 면 8개짜리 드럼이라 심볼 하나가 45도를 차지한다. 각도가 곧 결과다.
    /// <see cref="SymbolCount"/> 는 생성 스크립트의 REEL_N 과 **반드시** 같아야 한다.
    /// 어긋나면 릴이 심볼 사이 어중간한 각도에서 멈춘다.
    ///
    /// 판정은 심볼 **인덱스**만 본다. 그것이 '화면에 같은 무늬가 보인다' 와 같은 뜻이려면
    /// 세 릴의 무늬 배열이 같아야 한다 — generate_slot_machine.py 가 릴마다 배열을
    /// 어긋나게 두던 것을 없앤 이유다. 배열을 다시 어긋나게 하면 여기 판정이 거짓말이 된다.
    ///
    /// 화폐는 동전이다. 동전 하나가 크레딧 하나, 크레딧 하나가 스핀 한 번이다.
    /// 크레딧이 없으면 <see cref="CanPull"/> 이 거짓이 되고, 그 게이트가
    /// Lever → View 체인을 타고 올라가 조준점까지 막는다. 배당(<see cref="PayoutOf"/>)은
    /// 여기서 개수만 정한다 — 동전을 실제로 뱉는 것은 <c>CoinDispenser</c> 다.
    /// </summary>
    public sealed class SlotMachine
    {
        public const int SymbolCount = 8;
        public const int ReelCount = 3;

        // 배당. 확률(큰 성공 1/64, 작은 성공 21/64)과 짝을 이루는 값이라 여기 둔다 —
        // 뷰나 배출기에 흩어 두면 확률을 바꿀 때 기대값이 조용히 어긋난다.
        public const int SmallPayout = 10;
        public const int BigPayout = 100;

        public const float SymbolStep = 360f / SymbolCount;

        // 튜닝 값. 감속이 시작되는 시점이 릴마다 달라야 왼쪽부터 차례로 멈춘다.
        const float SpinSpeed = 900f;      // 도/초
        const float FirstStopAt = 0.9f;    // 첫 릴이 감속을 시작하는 시각(초)
        const float ReelGap = 0.45f;       // 릴 사이 간격(초)
        const float StopDuration = 1.1f;   // 감속에 걸리는 시간(초)
        const float MinTurnBeforeStop = 540f;  // 감속 전 최소 회전량(도)

        public enum Phase { Idle, Spinning, Done }

        /// <summary>
        /// 성공 등급. 세 릴이 **다** 멈춘 순간에 정해진다.
        ///
        /// 8종 · 릴 3개라 확률은 큰 성공 1/64 (1.6%), 작은 성공 21/64 (33%) 다.
        /// 작은 성공이 잦은 것은 의도다 — 기계가 계속 반응해야 살아 있어 보인다.
        /// </summary>
        public enum Win { None, Small, Big }

        struct Reel
        {
            public float Angle;
            public float Speed;
            public float StopAt;
            public bool Stopping;
            public bool Stopped;
            public float StopStart;
            public float From;
            public float To;
            public int Symbol;
        }

        readonly Reel[] _reels = new Reel[ReelCount];
        Random _rng = new(0);
        float _elapsed;

        public Phase State { get; private set; } = Phase.Idle;

        /// <summary>직전 회전의 결과. 도는 동안에는 <see cref="Win.None"/> 이다.</summary>
        public Win Result { get; private set; } = Win.None;

        /// <summary>릴 i 의 현재 회전각(도). 뷰가 이 값을 그대로 Transform 에 넣는다.</summary>
        public float AngleOf(int reel) => _reels[reel].Angle;

        /// <summary>멈춘 뒤의 심볼 인덱스. 도는 중에는 마지막 결과가 남아 있다.</summary>
        public int SymbolOf(int reel) => _reels[reel].Symbol;

        /// <summary>넣어 둔 동전 수. 스핀 한 번에 하나씩 준다.</summary>
        public int Credits { get; private set; }

        /// <summary>동전이 들어왔다. 도는 중에도 받는다 — 적립만 되고 스핀에는 다음에 쓴다.</summary>
        public void InsertCoin() => Credits++;

        /// <summary>
        /// 환불 — 남은 크레딧을 전부 꺼내 0 으로 만들고 개수를 돌려준다.
        /// 실제로 동전을 뱉는 것은 호출자(뷰 → CoinDispenser) 몫이다.
        /// 스핀 중 호출을 막는 게이트는 뷰(CanRefund)에 있다 — 규칙은 언제 꺼내든
        /// '남은 만큼만' 이라는 산수만 책임진다.
        /// </summary>
        public int TakeAllCredits()
        {
            var taken = Credits;
            Credits = 0;
            return taken;
        }

        public bool CanPull => State != Phase.Spinning && Credits > 0;

        /// <summary>결과에 따라 뱉을 동전 개수. 배출기가 이 값만큼 스폰한다.</summary>
        public static int PayoutOf(Win win)
            => win == Win.Big ? BigPayout : win == Win.Small ? SmallPayout : 0;

        /// <summary>
        /// 레버를 당긴다. 크레딧 하나를 **먼저** 소모한다 — 그래서 스핀 중에는
        /// 크레딧이 0 이어도 기계가 살아 있는 상태다 (전원 판정이 이 순서에 기댄다).
        /// 같은 seed 면 같은 결과가 나온다 — 재현 가능해야
        /// 버그를 다시 만들어 볼 수 있고, 나중에 런 시드를 붙이기도 쉽다.
        /// </summary>
        public void Pull(int seed)
        {
            if (!CanPull) return;

            Credits--;
            _rng = new Random(seed);
            _elapsed = 0f;
            State = Phase.Spinning;
            Result = Win.None;

            for (var i = 0; i < ReelCount; i++)
            {
                // 속도를 조금씩 다르게 준다. 똑같으면 세 릴이 한 덩어리로 보인다.
                _reels[i].Speed = SpinSpeed * (0.9f + 0.2f * (float)_rng.NextDouble());
                _reels[i].StopAt = FirstStopAt + ReelGap * i;
                _reels[i].Stopping = false;
                _reels[i].Stopped = false;
            }
        }

        public void Tick(float deltaTime)
        {
            if (State != Phase.Spinning) return;

            _elapsed += deltaTime;
            var allStopped = true;

            for (var i = 0; i < ReelCount; i++)
            {
                ref var reel = ref _reels[i];
                if (reel.Stopped) continue;
                allStopped = false;

                if (!reel.Stopping)
                {
                    reel.Angle += reel.Speed * deltaTime;
                    if (_elapsed >= reel.StopAt) BeginStop(ref reel);
                    continue;
                }

                var t = (_elapsed - reel.StopStart) / StopDuration;
                if (t >= 1f)
                {
                    reel.Angle = reel.To;
                    reel.Stopped = true;
                }
                else
                {
                    // 3차 ease-out. 끝에서 부드럽게 붙어야 '걸려 멈췄다' 는 느낌이 난다.
                    var e = 1f - (1f - t) * (1f - t) * (1f - t);
                    reel.Angle = reel.From + (reel.To - reel.From) * e;
                }
            }

            if (!allStopped) return;

            State = Phase.Done;
            Result = Judge();
        }

        /// <summary>
        /// 셋 다 같으면 큰 성공, 둘만 같으면 작은 성공이다.
        /// 릴 3개를 전제로 쓴 식이라 <see cref="ReelCount"/> 를 늘리면 여기부터 고친다.
        /// </summary>
        Win Judge()
        {
            var a = _reels[0].Symbol;
            var b = _reels[1].Symbol;
            var c = _reels[2].Symbol;

            if (a == b && b == c) return Win.Big;
            return a == b || b == c || a == c ? Win.Small : Win.None;
        }

        void BeginStop(ref Reel reel)
        {
            reel.Symbol = _rng.Next(SymbolCount);
            reel.Stopping = true;
            reel.StopStart = _elapsed;
            reel.From = reel.Angle;

            // 목표 각도는 (심볼 각도 + 정수 바퀴) 중에서 최소 회전량을 넘는 첫 값이다.
            // 그냥 가장 가까운 심볼로 붙이면 감속 구간이 0 에 가까워져 뚝 끊긴다.
            var want = reel.Symbol * SymbolStep;
            var turns = Math.Ceiling((reel.From + MinTurnBeforeStop - want) / 360f);
            reel.To = want + (float)turns * 360f;
        }
    }
}
