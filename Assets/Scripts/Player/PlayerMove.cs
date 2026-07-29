using UnityEngine;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 걷기. <see cref="CharacterController"/> 위에서 돈다.
    ///
    /// 이동 방향은 **몸통의 정면** 기준이다. <see cref="PlayerLook"/> 이 좌우 입력으로
    /// 이 오브젝트를 돌려 주므로 여기서는 시선을 읽지 않는다.
    ///
    /// 축은 <see cref="PlayerInputSource"/> 에서 받는다 — 키보드인지 화면 위 조이스틱인지
    /// 여기서는 구분하지 않는다. 입력을 받지 않는 동안(PC 에서 Esc 로 커서를 푼 동안)은
    /// 소스가 0 을 내보내므로 여기에 따로 게이트가 없다.
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

        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] PlayerInputSource input;

        CharacterController _controller;
        Vector3 _horizontal;
        float _verticalSpeed;

        void Awake() => _controller = GetComponent<CharacterController>();

        /// <summary>부트스트랩이 입력을 꽂아 준다.</summary>
        public void Bind(PlayerInputSource source) => input = source;

        void Update()
        {
            var axis = input != null ? input.Move : Vector2.zero;

            var wish = transform.right * axis.x + transform.forward * axis.y;
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
    }
}
