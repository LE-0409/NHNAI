# 커스텀 컨트롤 만들기

> VisualElement를 상속해 UI Toolkit에서 재사용 가능한 커스텀 UI 컨트롤을 만드는 방법

## 핵심 구조

커스텀 컨트롤은 `VisualElement` 또는 적합한 서브클래스를 상속하고 두 어트리뷰트로 정의:

```csharp
[UxmlElement]                           // (1) UXML/UI Builder에 등록
public partial class CustomToggle : VisualElement
{
    [UxmlAttribute]                     // (2) Inspector에서 편집 가능한 속성
    public string Label { get; set; }

    [UxmlAttribute(name: "bg-color")]   // Inspector 표시명 커스터마이즈
    public Color BackgroundColor { get; set; }

    public CustomToggle()               // 생성자에서 초기화
    {
        AddToClassList("custom-toggle");
        var label = new Label();
        label.AddToClassList("custom-toggle__label");
        Add(label);
    }
}
```

## [UxmlElement]

- 클래스를 `public partial`로 선언 필수
- UI Builder Library의 **Custom Controls (C#)** 아래 자동 등록 → 드래그앤드롭으로 사용 가능

## [UxmlAttribute]

- Inspector에서 설정 가능한 프로퍼티로 노출
- `name` 파라미터로 Inspector 표시명 커스터마이즈
- `Range`, `Tooltip`, `Header` 등 데코레이터 어트리뷰트 지원 (MonoBehaviour와 유사)

## 초기화 패턴

VisualElement는 `Awake`/`OnEnable` 없음 → **생성자에서 초기화**:

```csharp
public CustomToggle()
{
    // 시각 구조 설정
    AddToClassList("custom-toggle");
    
    var background = new VisualElement();
    background.AddToClassList("custom-toggle__background");
    Add(background);
}
```

**지연 초기화** (panel에 연결된 후):
```csharp
public CustomToggle()
{
    RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
}

private void OnAttachToPanel(AttachToPanelEvent evt) { /* panel 연결 후 초기화 */ }
private void OnDetachFromPanel(DetachFromPanelEvent evt) { /* 정리 */ }
```

## 기반 클래스 선택

| 용도 | 상속할 클래스 |
|------|------------|
| 일반 컨테이너 | `VisualElement` |
| 버튼 유사 컨트롤 | `Button` |
| 값 입력 폼 요소 | `BaseField<T>` |
| 텍스트 표시 | `TextElement` |

## 슬라이드 토글 예시 (SlideToggle)

```csharp
[UxmlElement]
public partial class SlideToggle : BaseField<bool>
{
    [UxmlAttribute] public string ToggleLabel { get; set; }
    [UxmlAttribute] public Color ActiveColor { get; set; }

    private VisualElement _knob;

    public SlideToggle() : base(null, null)
    {
        AddToClassList("slide-toggle");
        _knob = new VisualElement();
        _knob.AddToClassList("slide-toggle__knob");
        Add(_knob);
        
        RegisterCallback<ClickEvent>(OnClick);
        RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnClick(ClickEvent evt)
    {
        // SetValueWithoutNotify로 무한 루프 방지
        SetValueWithoutNotify(!value);
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        EnableInClassList("slide-toggle--active", value);
    }
}
```

## 이벤트 처리

```csharp
// 클릭 이벤트
RegisterCallback<ClickEvent>(evt => {
    SetValueWithoutNotify(!value);
    UpdateVisualState();
    using var e = ChangeEvent<bool>.GetPooled(!value, value);
    e.target = this;
    SendEvent(e);
});

// 키보드 이벤트
RegisterCallback<KeyDownEvent>(evt => {
    if (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return)
        OnClick(null);
});
```

## 모범 사례

| 원칙 | 방법 |
|------|------|
| 캡슐화 | 자기 완결적·재사용 가능한 요소 |
| 기반 클래스 | 목적에 맞는 서브클래스 상속 |
| 스타일링 | USS 클래스로 분리 |
| 무한 루프 방지 | `SetValueWithoutNotify()` 사용 |
| 상태 동기화 | `EnableInClassList()` + USS pseudo-classes |

## 출처
- [Custom controls (E-Book)](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/custom-controls.html)
- [Create custom controls](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-create-custom-controls.html)
