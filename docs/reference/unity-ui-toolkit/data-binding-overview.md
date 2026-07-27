# 데이터 바인딩 개요

> UI 요소와 C# 데이터를 자동으로 동기화하는 MVVM 패턴 구현 — 수동 UI 업데이트 코드 제거

## 두 가지 바인딩 시스템

| 시스템 | 용도 | 특징 |
|--------|------|------|
| **Runtime Data Binding** | 런타임 게임 UI | 범용 C# 오브젝트, UXML 선언적 바인딩 |
| **SerializedObject Binding** | Editor UI (Inspector) | Unity 직렬화 시스템 연동 |

## 데이터 소스 설정

모든 C# 오브젝트(ScriptableObject, MonoBehaviour, 커스텀 클래스)가 데이터 소스 가능:

```csharp
using Unity.Properties;

[GeneratePropertyBag]  // 컴파일 타임 프로퍼티 백 생성
public class PlayerData : ScriptableObject, INotifyBindablePropertyChanged
{
    [SerializeField, DontCreateProperty]
    int m_Health;

    [CreateProperty]  // 바인딩 가능한 프로퍼티로 노출
    public int Health
    {
        get => m_Health;
        set
        {
            m_Health = value;
            propertyChanged?.Invoke(this,
                new BindablePropertyChangedEventArgs(nameof(Health)));
        }
    }

    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
}
```

## UXML 바인딩 선언

```xml
<!-- 데이터 소스 타입을 UXML에서 미리 선언 (Unresolved Binding) -->
<ui:Label name="health-label" data-source-type="PlayerData, Assembly">
    <Bindings>
        <ui:DataBinding
            property="text"
            data-source-path="Health"
            binding-mode="ToTarget" />
    </Bindings>
</ui:Label>
```

런타임에 실제 소스 할당:
```csharp
healthLabel.dataSource = playerData;
```

## C# 바인딩 설정

```csharp
var label = new Label();
label.SetBinding("text", new DataBinding
{
    dataSource = playerData,
    dataSourcePath = new PropertyPath(nameof(PlayerData.Health)),
    bindingMode = BindingMode.ToTarget
});
root.Add(label);
```

## 바인딩 모드

| 모드 | 흐름 | 용도 |
|------|------|------|
| `TwoWay` | 양방향 (기본값) | TextField 등 입력 요소 |
| `ToTarget` | 소스 → UI | 읽기 전용 표시 |
| `ToSource` | UI → 소스 | 드물게 사용 |
| `ToTargetOnce` | 일회성 | 초기값 설정 |

### INotifyBindablePropertyChanged
프로퍼티 수준 변경 추적 → 영향받은 바인딩만 갱신 (가장 세밀한 제어):

```csharp
public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

public int Health
{
    get => m_Health;
    set
    {
        m_Health = value;
        propertyChanged?.Invoke(this,
            new BindablePropertyChangedEventArgs(nameof(Health)));
    }
}
```

### IDataSourceViewHashProvider
버전 해싱으로 전체 데이터 소스 변경 감지 (준정적 데이터에 적합):

```csharp
private long _version;
public long GetViewHashCode() => _version;

public int SomeValue
{
    set { m_SomeValue = value; _version++; }
}
```

## ListView 바인딩

```csharp
// ListView에 데이터 소스 바인딩
m_ListView.dataSource = m_TeamData;
m_ListView.SetBinding("itemsSource", new DataBinding
{
    dataSourcePath = new PropertyPath("Players")
});

// 아이템 템플릿에서 각 항목 바인딩
// ListView가 자동으로 itemsSource에서 각 아이템의 데이터 소스 설정
```

## 성능 최적화

```csharp
// ✅ 권장 — 컴파일 타임 코드 생성 (Reflection 제거)
[GeneratePropertyBag]
public partial class PlayerData : INotifyBindablePropertyChanged { }

// ✅ 해시 기반 변경 감지로 불필요한 UI 갱신 방지
public long GetViewHashCode() => _version;

// ✅ 필요한 프로퍼티만 [CreateProperty]로 노출
// ❌ 모든 필드를 바인딩 대상으로 만들지 않음
```

## 주의사항
- `[SerializeField, DontCreateProperty]` 쌍: 직렬화는 하되 바인딩 미노출
- Unresolved Binding으로 UXML과 런타임 데이터 소스 분리 가능
- 복잡한 데이터 계층 → 플랫 구조로 단순화 권장
- 비용 큰 연산은 바인딩 대신 캐시 사용

## 관련 페이지
- [성능 최적화](performance-optimization.md)
- [커스텀 컨트롤](custom-controls.md)

## 출처
- [Data binding (E-Book)](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/data-binding.html)
- [Data binding overview](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-data-binding.html)
