using NHNAI.Game.Interaction;
using UnityEngine;

namespace NHNAI.Game.Slot
{
    /// <summary>
    /// 레버 오브젝트에 붙는다. 조준해서 클릭하면 슬롯이 돈다.
    /// 레버가 움직이는 중이거나 릴이 도는 중에는 <see cref="CanInteract"/> 가 false 라
    /// 조준점도 강조되지 않는다 — 지금 누를 수 있는지가 화면에 그대로 드러난다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SlotMachineLever : Interactable
    {
        [SerializeField] SlotMachineView machine;

        public override bool CanInteract => machine != null && machine.CanPull;

        public override void Interact()
        {
            if (machine != null) machine.Pull();
        }

        public void Bind(SlotMachineView view) => machine = view;
    }
}
