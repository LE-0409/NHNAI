using UnityEngine;
using UnityEngine.InputSystem;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// WASD 걷기. <see cref="CharacterController"/> 위에서 돈다.
    ///
    /// 이동 방향은 **몸통의 정면** 기준이다. <see cref="PlayerLook"/> 이 마우스 좌우로
    /// 이 오브젝트를 돌려 주므로 여기서는 마우스를 읽지 않는다 — 같은 델타를 두 번
    /// 소비하면 감도가 두 배가 된다.
    ///
    /// 속도가 느린 건 의도다. 어두운 방을 더듬는 게임이라 빨리 걸으면 공간이
    /// 좁게 느껴지고, 빛 원뿔을 스쳐 지나가 버린다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMove : MonoBehaviour
    {
        [Header("걷기")]
        [SerializeField] float walkSpeed = 1.7f;
        [Tooltip("목표 속도에 도달하는 가속도(m/s²). 크면 딱딱하고 작으면 미끄러진다")]
        [SerializeField] float acceleration = 14f;

        [Header("중력")]
        [SerializeField] float gravity = -18f;

        CharacterController _controller;
        Vector3 _horizontal;
        float _verticalSpeed;

        void Awake() => _controller = GetComponent<CharacterController>();

        void Update()
        {
            // 커서가 풀린 동안(Esc)에는 멈춘다. 시야가 안 도는데 몸만 움직이면 어지럽다.
            var input = Cursor.lockState == CursorLockMode.Locked ? ReadMoveInput() : Vector2.zero;

            var wish = transform.right * input.x + transform.forward * input.y;
            _horizontal = Vector3.MoveTowards(_horizontal, wish * walkSpeed,
                                              acceleration * Time.deltaTime);

            if (_controller.isGrounded && _verticalSpeed < 0f)
            {
                // 0 으로 두면 매 프레임 바닥 판정이 깜빡인다. 살짝 눌러 붙여 둔다.
                _verticalSpeed = -2f;
            }
            _verticalSpeed += gravity * Time.deltaTime;

            var motion = _horizontal + Vector3.up * _verticalSpeed;
            _controller.Move(motion * Time.deltaTime);
        }

        static Vector2 ReadMoveInput()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;

            var x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                    - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            var y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f)
                    - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);

            var v = new Vector2(x, y);
            // 정규화하지 않으면 대각선이 √2 배 빨라진다.
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
    }
}
