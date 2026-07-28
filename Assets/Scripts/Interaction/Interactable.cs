using UnityEngine;

namespace NHNAI.Game.Interaction
{
    /// <summary>
    /// 조준해서 쓸 수 있는 것. <see cref="Player.PlayerInteractor"/> 가 화면 중앙에서
    /// 레이캐스트로 찾는다. 찾히려면 이 컴포넌트와 Collider 가 **같은 오브젝트**에 있어야 한다.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>지금 쓸 수 있는가. false 면 조준해도 조준점이 강조되지 않는다.</summary>
        public virtual bool CanInteract => true;

        public abstract void Interact();
    }
}
