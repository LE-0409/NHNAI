# UI Toolkit 성능 최적화

> 드로우 콜 최소화, 업데이트 메커니즘 감소, 메모리 효율을 위한 UI Toolkit 최적화 가이드

## 핵심 업데이트 메커니즘 (비용 순)

| 메커니즘 | 비용 | 트리거 |
|---------|------|--------|
| Style resolution | 높음 (중첩 계층) | 클래스/스타일 변경 |
| Layout 재계산 | 높음 (잦은 업데이트 시) | 크기/위치 수정 |
| Vertex buffer 업데이트 | 리소스 집약적 | 지오메트리 변경 |
| Rendering state 변경 | CPU 오버헤드 | 텍스처/마스크 전환 |

## 배칭 (Batching)

동일 GPU 요구사항(셰이더·텍스처·메시·GPU 파라미터)을 가진 요소 그룹화 → 단일 드로우 콜.

**배치 분할 예방**: 텍스트 요소 사이에 이미지 삽입 시 배치 분할 발생.

```text
// Panel Settings에서 Vertex Budget 설정
// 기본: 0 (자동 결정)
// 복잡한 UI: 20,000+ 권장
// Frame Debugger로 메모리/드로우 콜 균형 확인
```

### 8-텍스처 제한
uber shader는 최대 8개 텍스처를 하나의 드로우 콜로 처리. 초과 시 배치 분할.

### Sprite Atlas + Dynamic Atlas 조합
```text
- 2D Sprite Atlas: 정적 콘텐츠
- Dynamic Texture Atlas: 런타임 변경 콘텐츠
```

```csharp
// 단편화된 아틀라스 리셋
RuntimePanelUtils.ResetDynamicAtlas(panelName);
```

## 마스킹

| 유형 | 장점 | 단점 | 권장 |
|------|------|------|------|
| 직사각형 마스크 | 배치 분할 없음, 중첩 무제한 | - | 가능하면 사용 |
| 복잡 마스크(둥근 모서리) | 자유로운 형태 | 스텐실, 배치 분할, 최대 7단계 | 최소화 |

## 애니메이션 최적화

```csharp
// ❌ 레이아웃 재계산 트리거 (비쌈)
element.style.width = new Length(100, LengthUnit.Pixel);
element.style.left = new Length(50, LengthUnit.Pixel);

// ✅ GPU에서 처리 (레이아웃 재계산 우회)
element.style.translate = new Translate(50, 0);
element.style.scale = new Scale(1.2f, 1.2f);
element.style.rotate = new Rotate(45);
```

### Usage Hints
```csharp
// 개별 요소 자주 이동 시
element.usageHints = UsageHints.DynamicTransform;

// 그룹 전체 이동 시 (부모에 설정, GPU에서 자식에 전파)
parentContainer.usageHints = UsageHints.GroupTransform;
```

## 가시성 제어

| 방법 | 렌더링 | 레이아웃 | 메모리 | 용도 |
|------|--------|---------|--------|------|
| `opacity = 0` | O | O | 높음 | 트랜지션 효과 |
| `visible = false` | X | O | 중간 | 임시 숨김 |
| `display = None` | X | X | 낮음 | **잦은 토글 (권장)** |
| `RemoveFromHierarchy()` | X | X | 없음 | 드문 요소 |

```csharp
// ✅ 성능 중요 토글
element.style.display = DisplayStyle.None;
element.style.display = DisplayStyle.Flex;

// ✅ 드문 다이얼로그
dialogElement.RemoveFromHierarchy();
// ... 나중에 필요할 때
root.Add(dialogElement);
```

## 데이터 바인딩 성능 (Source Generation)

```csharp
using Unity.Properties;

// Reflection 대신 컴파일 타임 코드 생성
[GeneratePropertyBag]
public class CharacterData : ScriptableObject, 
    INotifyBindablePropertyChanged, 
    IDataSourceViewHashProvider
{
    private long _version;
    
    [SerializeField, DontCreateProperty] 
    string _characterName;
    
    [CreateProperty]  // 컴파일 타임 바인딩 코드 생성
    public string CharacterName
    {
        get => _characterName;
        set
        {
            _characterName = value;
            _version++;
            propertyChanged?.Invoke(this, 
                new BindablePropertyChangedEventArgs(nameof(CharacterName)));
        }
    }
    
    public long GetViewHashCode() => _version;  // 해시 기반 변경 감지
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
}
```

**핵심 어트리뷰트**:
- `[GeneratePropertyBag]` — Reflection 제거, 컴파일 타임 프로퍼티 백 생성
- `[CreateProperty]` — 프로퍼티별 바인딩 코드 생성
- `INotifyBindablePropertyChanged` — 실제 데이터 변경 시에만 UI 갱신
- `IDataSourceViewHashProvider` — 해시 기반 변경 감지

## USS 셀렉터 성능
```uss
/* ❌ 성능 저하 — 넓은 셀렉터 */
* { color: white; }
.unity-button { background: grey; }

/* ❌ 성능 저하 — 깊은 자식 셀렉터 */
.container .panel .item Label { ... }

/* ✅ 권장 — 구체적인 클래스 셀렉터 */
.my-specific-label { color: white; }
```

## 피해야 할 것
- 애니메이션 중 잦은 CSS 클래스 전환 → 인라인 스타일 업데이트 사용
- 배치당 8개 이상 텍스처
- 스텐실 마스크 7단계 이상 중첩
- 레이아웃 프로퍼티(width/height/position) 애니메이션 → translate/scale/rotate 사용
- `opacity = 0` 가시성 처리 → `display = None` 사용
- Reflection 기반 데이터 바인딩 → Source Generation 사용

## 프로파일링 도구

| 도구 | 역할 |
|------|------|
| **Unity Profiler** | 전체 성능 지표 |
| **UI Toolkit Debugger** | 요소 검사 + Dynamic Atlas Viewer |
| **Frame Debugger** | 드로우 콜 분석 |
| `SetPanelChangeReceiver` | 세밀한 변경 추적 (개발 빌드) |

```csharp
// 변경 수신기 설정 (개발/에디터 전용)
panelSettings.SetPanelChangeReceiver(this);

public void OnVisualElementChange(VisualElement element, VersionChangeType changeType)
{
    Debug.Log($"Change: {element.name} - {changeType}");
}
```

## Unity 6 성능 향상
- 이벤트 디스패치 속도 2배
- 클래식 요소 Jobified 지오메트리 생성
- 텍스트 생성 병렬화
- 깊은 계층 레이아웃 성능 향상 (연산 캐싱)
- Entities 백엔드의 TreeView 대규모 데이터셋 최적화

## 관련 페이지
- [레이아웃 엔진](uss-layout-engine.md)
- [데이터 바인딩 개요](data-binding-overview.md)

## 출처
- [Optimizing performance (E-Book)](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html)
