using System;
using System.Collections.Generic;
using NHNAI.Game.Coins;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 동전 인벤토리 — E 로 손의 동전을 넣고, Q 로 하나 꺼내 든다.
    ///
    /// 꺼낸 동전은 마우스로 잡았을 때와 같은 상태다 — <see cref="PlayerCoinCarrier.TryPickUp"/>
    /// 를 그대로 태우므로 시선 추적·투입구 흡입·클릭으로 놓기가 전부 동일하게 동작한다.
    /// 이미 들고 있는데 Q 를 누르면 들고 있던 동전을 바닥에 버리고 새로 든다.
    ///
    /// 보관은 파괴가 아니라 **비활성화**다. 동전 인스턴스를 스택에 그대로 쌓아 두면
    /// 꺼낼 때 템플릿을 다시 복제할 필요가 없고, 넣은 동전이 그대로 돌아온다는
    /// 보장이 코드 구조에서 바로 읽힌다.
    ///
    /// 개수는 <see cref="CountChanged"/> 로 내보낸다. HUD 를 직접 부르지 않는 이유는
    /// <see cref="PlayerInteractor"/> 와 같다 — NHNAI.UI 가 NHNAI.Game 을 구독한다.
    /// </summary>
    public sealed class PlayerCoinInventory : MonoBehaviour
    {
        [Header("부품 — 부트스트랩이 채운다")]
        [Tooltip("손. 넣을 동전을 여기서 받고, 꺼낸 동전을 여기에 쥐여 준다")]
        [SerializeField] PlayerCoinCarrier carrier;

        readonly Stack<Coin> _stored = new();

        /// <summary>보관 중인 동전 개수. 구독 시점에 HUD 가 초기값으로 읽어간다.</summary>
        public int Count => _stored.Count;

        /// <summary>개수가 바뀔 때. 값은 바뀐 뒤의 개수다.</summary>
        public event Action<int> CountChanged;

        /// <summary>부트스트랩이 씬에서 만든 부품을 꽂아 준다.</summary>
        public void Bind(PlayerCoinCarrier coinCarrier) => carrier = coinCarrier;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || carrier == null) return;
            // 커서가 풀린 상태(Esc)에서는 시점·클릭과 마찬가지로 입력을 받지 않는다.
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (keyboard.eKey.wasPressedThisFrame) Store();
            else if (keyboard.qKey.wasPressedThisFrame) Retrieve();
        }

        void Store()
        {
            var coin = carrier.TakeHeld();
            if (coin == null) return;   // 손이 비어 있으면 넣을 것이 없다

            coin.gameObject.SetActive(false);
            _stored.Push(coin);
            CountChanged?.Invoke(_stored.Count);
        }

        void Retrieve()
        {
            if (_stored.Count == 0) return;

            // 손이 차 있으면 그 동전은 바닥에 버린다 — Q 는 '바꿔 들기'까지 겸한다.
            if (carrier.IsCarrying) carrier.Drop();

            var coin = _stored.Pop();
            // 보관 당시 위치에 그대로 있으므로 손으로 순간이동시킨다. 안 하면
            // 캐리어의 추적 보간이 방 저편에서 동전을 날아오게 만든다.
            coin.transform.SetPositionAndRotation(carrier.HandPosition, carrier.HandRotation);
            coin.gameObject.SetActive(true);

            if (!carrier.TryPickUp(coin))
            {
                // Drop 직후라 실패할 일이 없지만, 실패하면 동전을 잃지 않게 되돌린다.
                coin.gameObject.SetActive(false);
                _stored.Push(coin);
                return;
            }

            CountChanged?.Invoke(_stored.Count);
        }
    }
}
