using NHNAI.Game.Interaction;
using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 환불 버튼 — 조작대 왼쪽의 RefundButton 오브젝트에 붙는다. 조준해서 클릭하면
    /// 넣어 둔 크레딧을 전부 배출구로 되뱉는다 (<see cref="SlotMachineView.Refund"/>).
    ///
    /// 크레딧이 없거나 스핀·배출 중이면 <see cref="CanInteract"/> 가 false 라
    /// 조준점이 강조되지 않는다 — 지금 누를 수 있는지가 화면에 그대로 드러난다.
    ///
    /// 누름 연출은 로컬 Y 로 살짝 내려갔다 돌아오는 것뿐이다. FBX 원점이 버튼
    /// 중심이라(생성 스크립트의 set_pivot) localPosition 만 만지면 된다.
    /// 레버와 같은 이유로 상태 기계에 넣지 않는다 — 결과에 영향 없는 순수 연출이다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SlotMachineRefundButton : Interactable
    {
        [SerializeField] SlotMachineView machine;

        [Header("누름 연출")]
        [Tooltip("눌렸을 때 내려가는 깊이(m). 버튼 노출 높이(약 0.024)보다 작아야 파묻히지 않는다")]
        [SerializeField] float pressDepth = 0.012f;
        [SerializeField] float pressDownTime = 0.05f;
        [SerializeField] float pressHoldTime = 0.08f;
        [SerializeField] float pressReturnTime = 0.12f;

        Vector3 _restPosition;
        float _timer = -1f;   // 음수 = 쉬는 중

        public override bool CanInteract => machine != null && machine.CanRefund && _timer < 0f;

        public override void Interact()
        {
            if (!CanInteract) return;
            machine.Refund();
            _timer = 0f;
        }

        public void Bind(SlotMachineView view) => machine = view;

        void Awake() => _restPosition = transform.localPosition;

        void Update()
        {
            if (_timer < 0f) return;

            _timer += Time.deltaTime;

            var total = pressDownTime + pressHoldTime + pressReturnTime;
            float t;   // 0 = 원위치, 1 = 끝까지 눌린 상태

            if (_timer < pressDownTime)
            {
                t = _timer / pressDownTime;
            }
            else if (_timer < pressDownTime + pressHoldTime)
            {
                t = 1f;
            }
            else if (_timer < total)
            {
                var r = (_timer - pressDownTime - pressHoldTime) / pressReturnTime;
                t = 1f - r * r;   // 돌아올 때는 스프링처럼 처음이 빠르다
            }
            else
            {
                t = 0f;
                _timer = -1f;
            }

            transform.localPosition = _restPosition + Vector3.down * (pressDepth * t);
        }
    }
}
