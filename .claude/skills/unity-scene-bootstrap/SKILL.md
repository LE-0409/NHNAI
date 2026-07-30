---
name: unity-scene-bootstrap
description: >
  NHNAI 의 씬·조작·룩을 만질 때 읽는 스킬. CellRoom 씬 생성 메뉴, 부트스트랩이 정본이라는 규칙,
  메인메뉴에서 게임으로 들어가는 시작 흐름(ControlMode · 커서 잠금 · 페이드 길이),
  PC/모바일 조작과 입력 추가 절차(PlayerInputSource), 상호작용 Collider, 시점 높이,
  조명·안개·포스트 프로세싱·빛 기둥 같은 룩 조정 위치를 다룬다.
  "씬 다시 만들어줘", "조명 어둡게", "포스트 프로세싱", "카메라", "스폰", "키 추가",
  "조작 바꿔줘", "커서가 안 잠겨", "상호작용이 안 돼", "빛 기둥", "CellRoomBootstrap",
  "PlayerInputSource" 같은 요청이면 트리거한다.
---

# unity-scene-bootstrap — 씬 · 조작 · 룩

CLAUDE.md 에서 옮겨 온 절: 「씬 생성 메뉴」 · 「시작 흐름」 · 「조작」 ·
「룩을 조정할 때 어디를 만지나」.

---

## 씬 생성 메뉴

```
NHNAI > Setup  > 1. 아트 머티리얼 생성 · 갱신   ← 머티리얼만 다시 만든다
NHNAI > Setup  > 2. PanelSettings 생성 · 갱신   ← UI Toolkit 런타임 기반
NHNAI > Scenes > 독방 (CellRoom)                ← 위 둘과 UI 세 층을 포함해 통째로 만든다
NHNAI > Build  > WebGL → WebGLBuild             ← 배포 스크립트가 보는 폴더로 뱉는다
```

씬을 만들면 `EditorBuildSettings`에 **자동으로 등록**되고, 파일이 사라진 씬은
목록에서 정리된다 (`SceneBuildList`). `ProjectSettings/EditorBuildSettings.asset`은
손으로 고치지 않는다. **`CellRoom`이 0번**이다 — 빌드된 게임은 목록의 첫 씬으로 열린다.

⚠️ **씬에서 직접 만진 것은 다음 생성 때 전부 날아간다.** `.unity`·`.mat`·`VolumeProfile.asset`은
GUID 참조가 들어간 YAML 이라 손으로 쓰지 않고 에디터 코드로 만든다 — 그래서 정본은 씬이
아니라 `Assets/Editor/CellRoomBootstrap.cs`다. 조명·카메라·포스트 프로세싱 값을 바꾸려면
그 파일을 고치고 메뉴를 다시 실행한다.

인스펙터에서 값을 굴려 보며 찾는 것 자체는 정상적인 작업 방식이다. **찾은 값을
부트스트랩에 옮겨 적고 메뉴를 다시 돌려 확정한다.** 씬에만 남기면 다음 생성에서 사라진다.

---

## 시작 흐름 — 메인메뉴는 씬이 아니라 층이다

**씬은 `CellRoom` 하나뿐이다.** 메인메뉴는 그 위에 겹치는 UIDocument(`sortingOrder: 20`)다.
(그 위에 하나 더 있다 — 세로로 들면 `RotateGate`(30)가 메뉴까지 덮는다. 세로로 고른 뒤
그대로 시작하면 조작 UI 가 안 맞는 자리에 놓인 채 첫 화면을 맞기 때문이다.)
씬을 나누지 않은 이유:

- **배경음이 끊기지 않는다.** `BuildAmbience()`가 씬이 열리는 순간부터 틀고 있어서
  메뉴가 떠 있는 동안에도 그대로 울린다. 씬을 나누면 전환에서 한 번 끊긴다.
- **뒤로 방이 비친다.** 메뉴 배경은 불투명한 판이 아니라 스크림(`--menu-scrim`)이다.
  고르는 순간 이 층만 걷혀 방이 드러나므로 "화면이 바뀌었다"가 아니라
  "메뉴가 걷혔다"로 읽힌다.
- 씬 하나면 조작 방식을 씬 너머로 들고 다닐 필요가 없다. 정적 보관소 없이
  `ControlMode`를 인자로 넘긴다.

```
씬 열림 ─ 방·조명·배경음 살아 있음. 메뉴 층이 그 위를 덮음
   │      PlayerInputSource 는 아직 아무것도 내보내지 않는다 (_running = false)
   ▼
PC / MOBILE 클릭
   │  ① 메뉴 층에 --hidden → 420ms 페이드 아웃 (SetEnabled(false) 로 입력도 끊는다)
   │  ② 같은 순간 HudScreen.Begin(mode) · MobileControlsScreen.Begin(mode)
   │     → 두 층이 520ms 페이드 인. 메뉴가 걷히는 동안 겹쳐 떠오른다
   │  ③ 같은 순간 PlayerInputSource.ClaimCursor(mode) ─ 커서 잠금**만** 한다
   │     조작은 아직 안 산다. 이것만 클릭 핸들러 안에 있는 이유는 아래 ⚠️
   ▼ (420ms 뒤)
메뉴 층 display:none · PlayerInputSource.Begin(mode) ─ 여기서부터 조작이 산다
```

⚠️ **페이드 길이가 두 곳에 있다.** `MainMenu.uss`의 `transition-duration: 420ms`와
`MainMenuScreen.FadeOutMs`. USS 는 그림을 그리고 C# 은 그 뒤에 무엇을 할지를 정한다 —
어긋나면 아직 보이는 채로 접히거나(짧음), 투명해진 메뉴가 남아 첫 조작을 먹는다(김).

⚠️ **커서 잠금(`ClaimCursor`)을 `Begin` 안으로 되돌리지 않는다.** 둘을 합치면 코드는
짧아지지만 WebGL 에서 깨진다 — 브라우저는 포인터 잠금을 **사용자 조작 직후에만**
허용하는데 `Begin` 은 페이드가 끝난 420ms 뒤에 불린다. 거부는 예외도 로그도 없이
조용해서, PC 를 골랐는데 시야만 안 도는 상태로 나타난다. 클릭에 가장 가까운 시점에
요청하려고 나눠 둔 것이다.

모드에 따라 달라지는 것은 셋뿐이다.

| | PC | 모바일 |
|---|---|---|
| 커서 | 잠근다 (Esc 로 풀기) | 잠그지 않는다 — UI 를 눌러야 한다 |
| `MobileControls` 층 | `display: none` 으로 접힌다 | 페이드 인 |
| HUD 동전 개수 | 우측 **하단** | 우측 **상단** (`.hud--mobile`) — 하단은 버튼 자리다 |

에디터에서도 MOBILE 을 눌러 마우스로 터치 조작을 테스트할 수 있다.

---

## 조작

| 동작 | PC | 모바일 |
|---|---|---|
| 시야 | 마우스. **좌우는 몸통(`Player`)이, 상하는 카메라가** 돈다 | 화면 오른쪽 영역을 끈다. **쓴 방향으로 시야가 따라간다** (마우스와 같다) |
| 걷기 | WASD · 방향키. 몸통 정면 기준 | 왼쪽 아래 조이스틱 |
| 사용 — 조준한 것과 상호작용 / 들고 있으면 놓기 | 좌클릭 | 오른쪽 아래 큰 버튼 (`USE`) |
| 넣기 — 들고 있는 동전을 인벤토리로 | E | `STORE` 버튼 |
| 꺼내기 — 인벤토리에서 1개 꺼내 들기 (들고 있으면 그건 바닥에 버린다) | Q | `TAKE` 버튼 |
| 커서 해제 | Esc | — |

**게임에 들어가면 메인메뉴로 돌아가는 길이 없다.** 양쪽 다 그렇다 — 다시 고르려면
실행을 껐다 켠다. 나중에 일시정지 화면을 만들면 그때 붙인다.

**입력을 추가할 때는 `PlayerInputSource`에 먼저 넣는다.** 게임 스크립트가
`Keyboard.current` / `Mouse.current` 를 직접 읽으면 PC 에서만 되는 조작이 생기고,
"커서가 잠긴 동안만 받는다" 같은 규칙이 복제된다 — 복제되면 한 곳만 고쳐진 채 남는다.
모바일 쪽 값은 `NHNAI.UI`의 조작 화면이 `PressXxx()` / `SetMoveAxis()` 로 밀어 넣는다.

⚠️ `Assets/InputSystem_Actions.inputactions` 는 **지우면 안 된다.** 게임 코드는 이 에셋을
쓰지 않지만(`PlayerInputSource` 가 저수준 API 를 직접 읽는다), UI Toolkit 런타임이
포인터 입력을 이 에셋의 `UI` 액션 맵에서 가져간다. 지우면 모바일 조작 UI 가
손가락을 못 받는다 — 화면은 그려지는데 아무 반응이 없어 원인을 찾기 어렵다.

조준점은 **항상 떠 있고**, 쓸 수 있는 것을 보면 **커지고 또렷해진다.** 강조가 안 되면
거리가 `PlayerInteractor.reach` 를 넘었거나 그 오브젝트에 `Collider` 가 없는 것이다 —
`Interactable` 과 `Collider` 는 **같은 오브젝트**에 있어야 한다.

조준용 Collider 는 **보이는 모양대로 감싸지 않는다.** 레버 팔처럼 가는 것은 실루엣대로
잡으면 조준이 바늘구멍이 된다. 노리기 쉬운 크기의 캡슐·박스를 씌운다.

⚠️ **시점 높이는 `CellRoomBootstrap.EyeHeight` 로 바꾼다.** `CharacterController` 의
`Center` 를 올려도 시점이 내려가지만, 그건 캡슐을 통째로 들어 올려 Transform 원점을
바닥 아래로 잠기게 하는 것이라 **원점 = 발밑**이라는 전제가 깨진다. 당장은 티가 안 나도
발소리·스폰·바닥 판정이 걸리기 시작한다. `center = height / 2` 식은 건드리지 않는다.

---

## 룩을 조정할 때 어디를 만지나

| 바꾸고 싶은 것 | 만지는 곳 |
|---|---|
| 방·기계의 형태·치수 | `ArtPipeline/assets/*/generate_*.py` → 다시 돌린다 (`blender-art-pipeline` 스킬) |
| 표면 밝기 (어떤 부품이 몇 번 명도인가) | 같은 생성 스크립트의 `color=` 인자. **팔레트 순서는 건드리지 않는다** |
| 빛의 세기·퍼짐·그림자 | `CellRoomBootstrap.BuildLighting()` |
| 어둠의 깊이 (벽이 얼마나 녹는가) | `ConfigureRenderSettings()` 의 `fogDensity`·`ambientLight` |
| 앤틱한 톤 (대비·입자·비네트) | `BuildPostProcessing()` |
| 빛 기둥의 룩 | `M_LightCone` 파라미터 → 확정되면 `ArtMaterialLibrary.CreateLightCone()` |

빛의 **경계**가 어색할 때는 원인이 셋이라 만지는 곳이 다르다. 눈으로 어느 경계인지 먼저 가린다.

| 어떤 경계가 어색한가 | 만지는 곳 |
|---|---|
| 빛 기둥이 바닥·벽을 뚫고 지나가며 생긴 **직선 교차선** | `M_LightCone` 의 `_DepthFade` (크게 = 더 흐림, 0 = 끔) |
| 빛 기둥의 **옆면 윤곽**이 칼로 자른 듯함 | `_EdgeSoftness` (크게 = 뭉근함), `_RimBoost` (작게 = 윤곽 약함) |
| 바닥에 생긴 **광원 웅덩이의 원 테두리** | `CellRoomBootstrap.SpotEdgeSoftness` (작게 = 넓게 번짐) |

`_DepthFade` 는 카메라의 `requiresDepthTexture` 가 켜져 있어야 동작한다.
꺼지면 조용히 효과만 사라지므로, 교차선이 다시 나타나면 여기부터 확인한다.

`Assets/Settings/`의 `PC_RPAsset` · `Mobile_RPAsset`은 품질 레벨과 짝지어져 있다.
렌더 설정을 바꿀 때 **둘 다** 봐야 한다 — 한쪽만 고치면 플랫폼에 따라 화면이 달라진다.

---

증상이 있으면 `nhnai-troubleshooting` 스킬의 표를 먼저 본다.
