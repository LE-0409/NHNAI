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

        [Header("레버 연출")]
        [Tooltip("당겼을 때 레버가 도는 각도(도, 로컬 X). 반대로 꺾이면 부호를 뒤집는다")]
        [SerializeField] float leverPullAngle = -62f;
        [SerializeField] float leverDownTime = 0.12f;
        [SerializeField] float leverHoldTime = 0.06f;
        [SerializeField] float leverReturnTime = 0.45f;

        readonly SlotMachine _machine = new();

        float _leverTimer = -1f;   // 음수 = 쉬는 중

        public bool CanPull => _machine.CanPull && _leverTimer < 0f;

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

            _machine.Tick(dt);
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
        public void Bind(Transform[] reelTransforms, Transform leverTransform)
        {
            reels = reelTransforms;
            lever = leverTransform;
        }
    }
}
