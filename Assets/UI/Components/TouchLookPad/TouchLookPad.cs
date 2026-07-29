using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.Components
{
    /// <summary>
    /// 시점 패드. 끌린 만큼을 내보내는, 보이지 않는 판이다.
    ///
    /// 조이스틱과 달리 자리를 잡아 주지 않는다 — 화면 오른쪽 절반 어디를 끌어도
    /// 시점이 돌아야 한다. 그래서 배경을 칠하지 않고 넓게 깔린다.
    ///
    /// 델타는 <see cref="PointerMoveEvent.deltaPosition"/> 대신 직접 계산한다.
    /// 눌린 순간의 위치를 기준으로 삼아야, 손가락이 들어온 첫 이벤트에서 화면
    /// 반대편부터의 거리가 통째로 한 번에 들어오는 일이 없다.
    ///
    /// 값은 이벤트로 내보내고 스스로는 아무것도 하지 않는다 — 조이스틱과 같다.
    /// </summary>
    [UxmlElement]
    public partial class TouchLookPad : VisualElement
    {
        const string BlockClass = "look-pad";
        const string HintClass = "look-pad__hint";
        const string HintGoneClass = "look-pad__hint--gone";

        readonly Label _hint;

        int _pointerId = PointerId.invalidPointerId;
        Vector2 _last;

        /// <summary>끌린 양(패널 논리 좌표). x = 오른쪽, y = 아래.</summary>
        public event Action<Vector2> Dragged;

        /// <summary>
        /// 처음 한 번만 보이는 안내 문구. 패널이 투명이라 여기가 끄는 자리라는
        /// 표시가 없으면 알 수 없다. 한 번 끌면 사라지고 다시 나오지 않는다.
        /// </summary>
        [UxmlAttribute("hint")]
        public string Hint
        {
            get => _hint.text;
            set => _hint.text = value ?? string.Empty;
        }

        public TouchLookPad()
        {
            AddToClassList(BlockClass);

            _hint = new Label
            {
                name = "hint",
                // 안내가 손가락을 가로채면 그 자리만 시점이 안 돈다.
                pickingMode = PickingMode.Ignore,
            };
            _hint.AddToClassList(HintClass);
            Add(_hint);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            RegisterCallback<DetachFromPanelEvent>(_ => Release(_pointerId));
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointerId != PointerId.invalidPointerId) return;

            _pointerId = evt.pointerId;
            _last = evt.position;
            // 캡처하지 않으면 손가락이 판을 벗어나는 순간 시점이 멈춘다.
            this.CapturePointer(evt.pointerId);

            _hint.AddToClassList(HintGoneClass);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId) return;

            var position = (Vector2)evt.position;
            var delta = position - _last;
            _last = position;

            if (delta == Vector2.zero) return;
            Dragged?.Invoke(delta);
        }

        void OnPointerUp(PointerUpEvent evt) => Release(evt.pointerId);

        void OnPointerCancel(PointerCancelEvent evt) => Release(evt.pointerId);

        void OnPointerCaptureOut(PointerCaptureOutEvent evt) => Release(evt.pointerId);

        void Release(int pointerId)
        {
            // 잡고 있는 손가락이 없을 때도 불린다 (패널에서 떨어질 때).
            if (pointerId == PointerId.invalidPointerId || pointerId != _pointerId) return;

            // ⚠️ 먼저 비운다 — ReleasePointer 가 쏘는 PointerCaptureOutEvent 로 재진입한다.
            _pointerId = PointerId.invalidPointerId;
            if (this.HasPointerCapture(pointerId)) this.ReleasePointer(pointerId);
        }
    }
}
