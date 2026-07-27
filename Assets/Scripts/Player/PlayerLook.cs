using UnityEngine;
using UnityEngine.InputSystem;

namespace NHNAI.Game.Player
{
    /// <summary>
    /// 마우스로 시야를 돌린다. 1인칭이고 플레이어는 자신을 볼 수 없어서 카메라가 곧 플레이어다.
    ///
    /// 지금은 yaw·pitch 를 둘 다 카메라에 건다. 이동이 붙으면 **yaw 는 몸통(부모)으로 옮기고
    /// 여기는 pitch 만 남긴다** — 몸이 생기면 좌우 회전은 몸이 돌아야 이동 방향과 맞는다.
    /// 그때 <see cref="pitchOnly"/> 를 켜면 된다.
    ///
    /// 입력은 Input System 의 저수준 API(<see cref="Mouse.current"/>)를 직접 읽는다.
    /// InputActionAsset 을 문자열로 찾는 방식은 이름이 어긋나도 컴파일이 통과하고
    /// 런타임에야 조용히 실패해서 쓰지 않았다. 게임패드가 필요해지면 그때 액션으로 옮긴다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerLook : MonoBehaviour
    {
        [Header("감도")]
        [Tooltip("마우스 카운트 1당 회전 각도(도). 0.05 = 둔함, 0.25 = 예민함")]
        [SerializeField] float sensitivity = 0.12f;

        [Header("고개 제한")]
        [SerializeField] float pitchMin = -85f;
        [SerializeField] float pitchMax = 85f;

        [Header("동작")]
        [Tooltip("켜면 좌우 회전을 하지 않는다. 이동이 붙어 yaw 를 몸통이 맡을 때 켠다")]
        [SerializeField] bool pitchOnly;

        [Tooltip("시작할 때 커서를 잠근다. Esc 로 풀고 클릭으로 다시 잠근다")]
        [SerializeField] bool lockCursorOnStart = true;

        float _yaw;
        float _pitch;

        void OnEnable()
        {
            // 씬에 배치된 초기 각도를 이어받는다. 0 으로 리셋하면 부트스트랩이 맞춰 둔
            // 시작 시선(슬롯머신을 바라보는 방향)이 첫 프레임에 날아간다.
            var euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = SignedAngle(euler.x);

            if (lockCursorOnStart) SetCursorLocked(true);
        }

        void OnDisable()
        {
            // 플레이를 멈췄는데 커서가 잠긴 채 남으면 에디터를 못 쓴다.
            SetCursorLocked(false);
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (mouse.leftButton.wasPressedThisFrame && lockCursorOnStart)
                SetCursorLocked(true);

            if (Cursor.lockState != CursorLockMode.Locked) return;

            // ⚠️ Time.deltaTime 을 곱하지 않는다. 마우스 델타는 '속도'가 아니라 이번 프레임에
            // 실제로 움직인 양이라 이미 시간이 반영돼 있다. 곱하면 프레임률에 따라 감도가
            // 달라진다 — 흔하고 눈치채기 어려운 버그다.
            var delta = mouse.delta.ReadValue();

            if (!pitchOnly) _yaw += delta.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * sensitivity, pitchMin, pitchMax);

            // 누적 회전(transform.Rotate)이 아니라 매 프레임 각도로 다시 만든다.
            // 누적하면 부동소수 오차가 쌓여 roll 이 생기고 수평선이 기운다.
            transform.localRotation = pitchOnly
                ? Quaternion.Euler(_pitch, 0f, 0f)
                : Quaternion.Euler(_pitch, _yaw, 0f);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>eulerAngles 는 0~360 으로 돌아온다. 위를 보는 각을 음수로 되돌린다.</summary>
        static float SignedAngle(float degrees)
        {
            degrees %= 360f;
            return degrees > 180f ? degrees - 360f : degrees;
        }
    }
}
