using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.Components
{
    /// <summary>
    /// 화면 위 조이스틱. 손잡이를 끌면 −1~1 축을 내보낸다.
    ///
    /// **자리가 고정된 조이스틱이다.** 손가락이 닿은 곳에 받침이 생기는 방식(floating)이
    /// 아닌 이유는 이 조작을 PC 에서 마우스로도 테스트하기 때문이다 — 보이는 자리가
    /// 있어야 집는다. 모바일에서도 받침이 항상 같은 자리에 있으면 엄지가 화면을 보지
    /// 않고도 찾는다.
    ///
    /// 손잡이는 USS 가 flex 로 중앙에 두고, 이 클래스는 <c>translate</c> 로만 민다.
    /// 그래서 <c>VirtualJoystick.uss</c> 는 손잡이의 <c>translate</c> 를 정의하지 않는다 —
    /// 인라인이 USS 를 이기므로 정의해 봤자 조용히 무시된다 (CLAUDE.md).
    ///
    /// 값은 이벤트로 내보내고 스스로는 아무것도 하지 않는다. 게임에 밀어 넣는 것은
    /// 화면(<c>MobileControlsScreen</c>)의 몫이다.
    /// </summary>
    [UxmlElement]
    public partial class VirtualJoystick : VisualElement
    {
        const string BlockClass = "joystick";
        const string KnobClass = "joystick__knob";

        readonly VisualElement _knob;

        // 지금 이 조이스틱을 잡고 있는 손가락. 두 손가락이 동시에 잡으면 값이 튄다.
        int _pointerId = PointerId.invalidPointerId;

        /// <summary>기울기가 바뀔 때. 손을 떼면 마지막으로 <see cref="Vector2.zero"/> 가 온다.</summary>
        public event Action<Vector2> ValueChanged;

        /// <summary>현재 기울기. x = 오른쪽, y = 앞. 각각 −1~1.</summary>
        public Vector2 Value { get; private set; }

        public VirtualJoystick()
        {
            AddToClassList(BlockClass);

            _knob = new VisualElement
            {
                name = "knob",
                // 히트 판정은 받침(루트)이 통째로 받는다. 손잡이가 가로채면 손잡이
                // 바깥을 눌렀을 때만 반응하는 이상한 조이스틱이 된다.
                pickingMode = PickingMode.Ignore,
                // 매 프레임 translate 가 바뀐다 — 레이아웃을 건드리지 않는 경로로 보낸다.
                usageHints = UsageHints.DynamicTransform,
            };
            _knob.AddToClassList(KnobClass);
            Add(_knob);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            // 손가락이 패널 밖으로 나가거나 캡처를 빼앗겨도 손잡이가 기울어진 채
            // 남지 않게 한다 — 남으면 손을 뗐는데 계속 걷는다.
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            RegisterCallback<DetachFromPanelEvent>(_ => Release(_pointerId));
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointerId != PointerId.invalidPointerId) return;   // 이미 한 손가락이 잡고 있다

            _pointerId = evt.pointerId;
            // 캡처하지 않으면 손가락이 받침을 벗어나는 순간 이동 이벤트가 끊긴다.
            this.CapturePointer(evt.pointerId);
            Apply(this.WorldToLocal(evt.position));
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId) return;
            Apply(this.WorldToLocal(evt.position));
        }

        void OnPointerUp(PointerUpEvent evt) => Release(evt.pointerId);

        void OnPointerCancel(PointerCancelEvent evt) => Release(evt.pointerId);

        void OnPointerCaptureOut(PointerCaptureOutEvent evt) => Release(evt.pointerId);

        void Apply(Vector2 local)
        {
            var width = resolvedStyle.width;
            var height = resolvedStyle.height;
            // 첫 프레임에는 레이아웃이 아직 없다. 0 으로 나누지 않는다.
            if (width <= 0f || height <= 0f) return;

            var radius = Mathf.Min(width, height) * 0.5f;
            var offset = local - new Vector2(width, height) * 0.5f;
            if (offset.sqrMagnitude > radius * radius) offset = offset.normalized * radius;

            _knob.style.translate = new Translate(offset.x, offset.y);

            // UI 의 y 는 아래로 자란다. 게임의 전진(+y)과 뒤집혀 있다.
            var value = new Vector2(offset.x / radius, -offset.y / radius);
            if (value == Value) return;

            Value = value;
            ValueChanged?.Invoke(value);
        }

        void Release(int pointerId)
        {
            // 잡고 있는 손가락이 없을 때도 불린다 (패널에서 떨어질 때).
            if (pointerId == PointerId.invalidPointerId || pointerId != _pointerId) return;

            // ⚠️ 먼저 비운다. ReleasePointer 가 PointerCaptureOutEvent 를 다시 쏘는데,
            // 그때 이 값이 남아 있으면 여기로 재진입한다.
            _pointerId = PointerId.invalidPointerId;
            if (this.HasPointerCapture(pointerId)) this.ReleasePointer(pointerId);

            _knob.style.translate = new Translate(0f, 0f);

            if (Value == Vector2.zero) return;
            Value = Vector2.zero;
            ValueChanged?.Invoke(Vector2.zero);
        }
    }
}
