using UnityEngine;
using UnityEngine.UIElements;

namespace NHNAI.UI.RotateGate
{
    /// <summary>
    /// 세로로 든 화면을 막고 "가로로 돌려라"를 안내하는 층. **모든 층 위에 덮인다**
    /// (sortingOrder 30 — 메인메뉴 20 보다도 위다).
    ///
    /// 왜 필요한가: 이 게임은 landscape 고정인데 그 설정
    /// (<c>ProjectSettings.defaultScreenOrientation</c>)은 **네이티브 모바일 빌드에만
    /// 걸린다.** WebGL 에서 브라우저는 그 값을 보지 않고, Screen Orientation API 는
    /// 전체화면일 때만 방향을 잠글 수 있다. 강제할 수단이 없으니 막고 안내한다.
    ///
    /// **판단은 USS 가 못 한다** — USS 에 <c>@media</c> 가 없다. 그래서 여기서
    /// <see cref="GeometryChangedEvent"/> 로 패널을 재고 클래스만 붙였다 뗀다.
    /// 어떻게 보이는지는 전부 <c>RotateGate.uss</c> 가 그린다.
    ///
    /// 게임을 멈추지는 않는다. 세로인 동안 이 층이 화면과 포인터를 다 가리므로
    /// 터치 조작은 닿지 않지만, PC 는 키보드·마우스를 UI 를 거치지 않고 읽어서
    /// (<c>PlayerInputSource</c>) 뒤에서 계속 움직인다. 일시정지 개념이 아직
    /// 없어서 여기서 만들지 않았다 — 생기면 이 층이 그것을 부르면 된다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class RotateGateScreen : MonoBehaviour
    {
        const string ShownClass = "rotate-gate--shown";
        const string TurnedClass = "rotate-gate__phone--turned";

        /// <summary>
        /// 회전 힌트가 방향을 바꾸는 주기(ms). <c>RotateGate.uss</c> 의
        /// <c>transition-duration: 900ms</c> 보다 길어야 한 번 다 돌고 잠깐 머문다 —
        /// 짧으면 중간에 방향이 꺾여 무엇을 하라는 건지 안 읽힌다.
        /// </summary>
        const long TurnEveryMs = 1400;

        VisualElement _documentRoot;
        VisualElement _gate;
        VisualElement _phone;
        IVisualElementScheduledItem _turning;
        bool _wired;
        bool _shown;

        // UIDocument 가 비주얼 트리를 만드는 시점이 이 컴포넌트의 OnEnable 보다
        // 늦을 수 있다. 두 번 시도하고 _wired 로 중복을 막는다 — MainMenuScreen 과 같다.
        void OnEnable() => Wire();

        void Start() => Wire();

        void OnDisable()
        {
            if (!_wired) return;

            if (_documentRoot != null)
                _documentRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            _turning?.Pause();
            _turning = null;
            _documentRoot = null;
            _gate = null;
            _phone = null;
            _wired = false;
            _shown = false;
        }

        void Wire()
        {
            if (_wired) return;

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _gate = root.Q<VisualElement>("gate-root");
            _phone = root.Q<VisualElement>("gate-phone");

            if (_gate == null || _phone == null)
            {
                Debug.LogError("[NHNAI] RotateGate.uxml 에서 gate-root / gate-phone 을 못 찾았다.");
                return;
            }

            // ⚠️ 콜백은 gate-root 가 아니라 **문서 루트**에 건다.
            // gate-root 는 가로일 때 display:none 이라 크기가 0 이고, 접힌 요소에는
            // GeometryChangedEvent 가 오지 않는다 — 거기 걸면 한 번 숨은 뒤로는
            // 세로로 돌려도 영영 다시 나타나지 못한다.
            _documentRoot = root;
            _documentRoot.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _wired = true;

            // 첫 판단은 이벤트를 기다리지 않는다. 레이아웃이 이미 끝난 뒤에 붙었다면
            // 이벤트가 한 번도 안 올 수 있다. 아직 레이아웃 전이라면 값이 0/NaN 이라
            // '가로'로 읽히는데, 그건 기본 상태와 같고 곧 이벤트가 바로잡는다.
            Apply(root.resolvedStyle.height > root.resolvedStyle.width);
        }

        void OnGeometryChanged(GeometryChangedEvent evt) =>
            Apply(evt.newRect.height > evt.newRect.width);

        void Apply(bool portrait)
        {
            // GeometryChangedEvent 는 방향과 무관한 크기 변화에도 온다.
            // 판정이 그대로면 클래스도 스케줄러도 건드리지 않는다.
            if (_gate == null || portrait == _shown) return;
            _shown = portrait;

            _gate.EnableInClassList(ShownClass, portrait);

            if (portrait)
            {
                _turning ??= _phone.schedule.Execute(Turn).Every(TurnEveryMs);
                _turning.Resume();
            }
            else
            {
                _turning?.Pause();
                // 다음에 다시 세로가 되면 세워진 채로 시작한다.
                _phone.RemoveFromClassList(TurnedClass);
            }
        }

        // 클래스만 뒤집는다. 실제 회전은 RotateGate.uss 의 transition 이 그린다 —
        // 여기서 style.rotate 를 쓰면 인라인이 USS 를 이겨 transition 이 죽는다.
        void Turn() => _phone.ToggleInClassList(TurnedClass);
    }
}
