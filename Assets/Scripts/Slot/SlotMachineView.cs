using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// <see cref="SlotMachine"/> 이 계산한 각도를 실제 Transform 에 옮기고, 레버를 애니메이션한다.
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
        [Tooltip("성공 연출. 없어도 릴은 돈다")]
        [SerializeField] SlotMachineWinEffect winEffect;

        [Header("레버 연출")]
        [Tooltip("당겼을 때 레버가 도는 각도(도, 로컬 X). 반대로 꺾이면 부호를 뒤집는다")]
        [SerializeField] float leverPullAngle = 62f;
        [SerializeField] float leverDownTime = 0.12f;
        [SerializeField] float leverHoldTime = 0.06f;
        [SerializeField] float leverReturnTime = 0.45f;

        readonly SlotMachine _machine = new();

        float _leverTimer = -1f;   // 음수 = 쉬는 중

        public bool CanPull => _machine.CanPull && _leverTimer < 0f;

        /// <summary>넣어 둔 동전 수. 전원 상태(SlotMachinePower)가 읽는다.</summary>
        public int Credits => _machine.Credits;

        /// <summary>도는 중인가. 크레딧을 먼저 소모하므로 전원 판정에 이 값도 필요하다.</summary>
        public bool IsSpinning => _machine.State == SlotMachine.Phase.Spinning;

        /// <summary>캐리어가 흡입을 마친 동전을 크레딧으로 바꾼다.</summary>
        public void InsertCoin() => _machine.InsertCoin();

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

            // 성공은 **마지막 릴이 멈추는 순간** 한 번만 터져야 한다. Phase 를 매 프레임
            // 확인해 Done 이면 부르는 식으로 짜면 멈춘 내내 다시 켜진다.
            var wasSpinning = _machine.State == SlotMachine.Phase.Spinning;
            _machine.Tick(dt);
            if (wasSpinning && _machine.State == SlotMachine.Phase.Done && winEffect != null)
                winEffect.Play(_machine.Result);

            for (var i = 0; i < reels.Length; i++)
            {
                if (reels[i] == null) continue;
                reels[i].localRotation = Quaternion.Euler(_machine.AngleOf(i), 0f, 0f);
            }

            TickLever(dt);
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
                         SlotMachineWinEffect effect)
        {
            reels = reelTransforms;
            lever = leverTransform;
            winEffect = effect;
        }
    }
}
