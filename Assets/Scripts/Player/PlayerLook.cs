using UnityEngine;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 시야를 돌린다. 1인칭이고 플레이어는 자신을 볼 수 없어서 카메라가 곧 시선이다.
    ///
    /// **좌우(yaw)는 몸통이 돌고 상하(pitch)는 카메라가 돈다.** 몸이 돌아야 이동 방향이
    /// 보는 방향과 맞는다 — 카메라만 돌리면 앞으로 걸을 때 엉뚱한 데로 간다.
    /// <see cref="yawTarget"/> 이 비어 있으면 자기 자신에 yaw 를 걸어, 몸통이 없을 때도 동작한다.
    ///
    /// 회전량은 <see cref="PlayerInputSource"/> 에서 받는다. 마우스든 터치든 여기서는
    /// 구분하지 않는다 — 두 장치의 단위 차이는 소스가 맞춰서 넘긴다. 그래서 감도
    /// 설정이 <see cref="sensitivity"/> 하나로 남는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerLook : MonoBehaviour
    {
        [Header("감도")]
        [Tooltip("입력 1단위당 회전 각도(도). 0.05 = 둔함, 0.25 = 예민함")]
        [SerializeField] float sensitivity = 0.12f;

        [Header("고개 제한")]
        [SerializeField] float pitchMin = -85f;
        [SerializeField] float pitchMax = 85f;

        [Header("동작")]
        [Tooltip("좌우 회전을 걸 몸통. 비우면 카메라 자신이 좌우까지 돈다")]
        [SerializeField] Transform yawTarget;

        [Header("부품 — 부트스트랩이 채운다")]
        [SerializeField] PlayerInputSource input;

        float _yaw;
        float _pitch;

        Transform Yaw => yawTarget != null ? yawTarget : transform;

        void OnEnable()
        {
            // 씬에 배치된 초기 각도를 이어받는다. 0 으로 리셋하면 부트스트랩이 맞춰 둔
            // 시작 시선(슬롯머신을 바라보는 방향)이 첫 프레임에 날아간다.
            _yaw = Yaw.eulerAngles.y;
            _pitch = SignedAngle(transform.localEulerAngles.x);
        }

        void Update()
        {
            if (input == null) return;

            // ⚠️ Time.deltaTime 을 곱하지 않는다. 이 값은 '속도'가 아니라 이번 프레임에
            // 실제로 움직인 양이라 이미 시간이 반영돼 있다 (PlayerInputSource 주석 참조).
            var delta = input.LookDelta;

            _yaw += delta.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * sensitivity, pitchMin, pitchMax);

            // 누적 회전(transform.Rotate)이 아니라 매 프레임 각도로 다시 만든다.
            // 누적하면 부동소수 오차가 쌓여 roll 이 생기고 수평선이 기운다.
            var yaw = Yaw;
            if (yaw == transform)
            {
                transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            else
            {
                yaw.rotation = Quaternion.Euler(0f, _yaw, 0f);
                transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        /// <summary>부트스트랩이 몸통과 입력을 꽂아 준다.</summary>
        public void Bind(Transform body, PlayerInputSource source)
        {
            yawTarget = body;
            input = source;
        }

        /// <summary>eulerAngles 는 0~360 으로 돌아온다. 위를 보는 각을 음수로 되돌린다.</summary>
        static float SignedAngle(float degrees)
        {
            degrees %= 360f;
            return degrees > 180f ? degrees - 360f : degrees;
        }
    }
}
