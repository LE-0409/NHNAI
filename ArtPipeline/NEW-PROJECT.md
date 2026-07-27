# 새 프로젝트에서 Blender 에셋 파이프라인 시작하기

이 파이프라인(kit)을 다른 프로젝트에 깔고 첫 에셋을 뽑기까지의 절차서.
경계·스키마·계약의 **레퍼런스**는 [KIT.md](KIT.md)에 있고, 이 문서는 **순서**를 다룬다.

전제: Windows + PowerShell. Blender가 이미 있으면 재사용하므로 보통 다운로드는 없다.

> 📌 아래에서 `<...>`는 **각자 환경에 맞게 채우는 자리**다. 그대로 실행하면 안 된다.
> - `<새 프로젝트>` — kit을 깔 프로젝트 폴더 (예: `C:\work\NewGame`)
> - `<원본 레포>` — 이 파이프라인의 원본인 Dungeon Miner 레포를 클론한 위치

---

## 1. kit 이식 — **원본 레포에서** 실행

대상 폴더는 미리 있어야 한다 (기존 Unity 프로젝트든 빈 폴더든).

```powershell
cd <원본 레포>\ArtPipeline
.\install_kit.ps1 -Target <새 프로젝트>
```

`<대상>\ArtPipeline\`에 러너 3종 + `lowpoly_lib` + `pipeline.json` + 빈 팔레트 레지스트리 +
검증 스크립트가 생긴다.

| 옵션 | 뜻 |
|---|---|
| `-ProjectName "New Game"` | `pipeline.json`의 표시 이름 (기본: 대상 폴더명) |
| `-AssetsRoot "Assets/Art"` | 에셋 출력 루트 (대상 리포 루트 기준) |
| `-WithUnityEditorScripts` | Unity 임포트 후처리 C# 2종을 **참고 구현**으로 함께 복사 (6장 참조) |
| `-Force` | 이미 있는 kit 파일 덮어쓰기 (= kit 업데이트, 8장) |

> 깔린 kit은 **자족적이다.** `install_kit.ps1`과 `kit_templates/`도 함께 복사되므로,
> 이 프로젝트가 다시 다음 프로젝트의 씨앗이 될 수 있다 — 원본 레포가 사라져도 파이프라인은
> 이어진다. 어디서 왔는지는 `ArtPipeline/KIT-ORIGIN.txt`에 기록된다.
>
> 다만 **개선은 되도록 원본 한 곳에서** 하고 `-Force`로 내보내는 편이 낫다. 사본에서 사본을
> 뜨다 보면 오래된 kit이 퍼질 수 있는데, 그럴 때 `KIT-ORIGIN.txt`의 commit을 비교하면 된다.

---

## 2. Blender 확인 — 보통 다운로드 없음

```powershell
cd <새 프로젝트>\ArtPipeline
.\setup.ps1 -Status
```

설치 위치 설정은 레포 밖(`%APPDATA%\BlenderArtKit\config.json`)에 있고 **모든 프로젝트가
공유**한다. 이 PC에서 이미 한 번 설치했다면 `RESOLVED:`가 바로 뜨고 `setup.ps1`을 돌릴
필요조차 없다.

처음이거나 위치를 바꾸고 싶다면:

```powershell
.\setup.ps1                                    # 아무데나 (기본 캐시)
.\setup.ps1 -InstallDir <원하는 경로>          # 원하는 드라이브 — 이미 받아둔 게 있으면 옮긴다
.\setup.ps1 -BlenderExe <기존 blender.exe 경로>  # 이미 설치된 Blender 등록
.\setup.ps1 -Reset                             # 설정을 없던 걸로
```

⚠️ 이건 **개발자별 로컬 설정**이다. 경로를 스크립트에 하드코딩하지 말 것 —
`setup.ps1`을 돌린 결과로 git 워킹트리가 변하면 그건 비정상이다.

---

## 3. 전 계층 검증

```powershell
.\run_blender.ps1 assets\_smoke\kit_check.py
```

설정 로드 → 팔레트 PNG → 메시 생성 → 턴어라운드 렌더 → FBX 익스포트를 한 번에 돌린다.
`KIT CHECK OK`가 나오면 준비 완료. 출력에 찍히는 `assets root`가 의도한 경로인지 확인할 것.

---

## 4. 프로젝트 레이어 채우기

kit은 게임을 모른다. 아래 둘이 이 프로젝트의 정체다.

### `ArtPipeline/pipeline.json`

출력 위치가 기본값과 다르면 고친다. 전체 스키마는 [KIT.md](KIT.md) §3.

```json
{
  "project": "New Game",
  "assetsRoot": "Assets/Art",
  "paletteRegistry": "project/palette_registry.py"
}
```

### `ArtPipeline/project/palette_registry.py`

시작용 8색이 들어 있다. 프로젝트 색으로 교체한다.

> ⚠️ **선언 순서 = 팔레트 셀 인덱스다.** 한 번 에셋을 뽑기 시작하면 순서를 바꾸거나 중간에
> 끼워 넣을 수 없다 — 이미 익스포트된 전 에셋의 UV가 어긋난다. 추가는 **항상 맨 뒤**,
> 안 쓰게 된 색도 지우지 말고 주석으로 표시만. 상한은 `GRID`×`GRID` = 64색.

특수 룩(투명·발광 등)이 필요하면 `SLOTS`에 선언한다. `alias`를 주면
`palette.<alias>(obj)` 헬퍼가 자동 생성된다. 계약은 [KIT.md](KIT.md) §4.

### 대상 프로젝트의 `.gitignore`

```
/ArtPipeline/tools/
/ArtPipeline/build/
/ArtPipeline/previews/
/ArtPipeline/reports/
__pycache__/
```

---

## 5. 첫 에셋 스크립트

`ArtPipeline/assets/<이름>/generate_<이름>.py` — **이 깊이여야** 부트스트랩의 `..\..`가 맞는다.

```python
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

from lowpoly_lib import builders, export, palette, paths, preview

builders.reset_scene()
palette.write_palette_png()

obj = builders.box("Crate", (0.8, 0.8, 0.8), color="wood")

preview.render_turnaround("crate", [obj])              # previews/crate/*.png — 눈으로 확인
export.export_static([obj], paths.art("Props", "Crate.fbx"))
print("EXPORTED")
```

```powershell
.\run_blender.ps1 assets\crate\generate_crate.py
```

- **렌더 미리보기를 반드시 눈으로 확인하고** 다음 단계로 간다. 이게 이 파이프라인의 핵심 루프다.
- 부품이 많으면 별도 파일(`crate_parts.py`)로 빼고 `builders.join_all(...)`로 합친다
  (`sys.path`에 `HERE`가 들어 있어 옆 파일을 그냥 import할 수 있다).
- 캐릭터는 `rigging.build_armature(...)` → `animation`으로 액션 생성 → `export.export_skinned(...)`.

### 규약 (kit이 강제하므로 그대로 유효)

- **1 Blender unit = 1 m**, 발바닥 Z=0
- **전방 = −Y** (FBX 익스포트가 엔진 +Z 전방으로 변환)
- FBX는 `export_static` / `export_skinned`만 사용 — `bpy.ops.export_scene.fbx` 직접 호출 금지
- 색은 팔레트 UV로만. 버텍스 컬러·개별 머티리얼 금지

---

## 6. Unity 연동 (해당 시)

kit은 **FBX 출력까지**만 책임진다. 팔레트 텍스처를 머티리얼로 묶고 슬롯 이름을 셰이더에
리맵하는 건 엔진 쪽 몫이다. `-WithUnityEditorScripts`로 가져온 C# 2종은 **참고 구현**이며
그대로 쓰면 안 된다 — 머티리얼 슬롯 이름·루프 클립 목록·셰이더 참조가 전부 원본 게임 것이다.
가져갈 것은 "슬롯 이름으로 식별해 `.mat`에 리맵한다"는 메커니즘이다. [KIT.md](KIT.md) §5.

---

## 7. LLM으로 작업한다면 — 대상 프로젝트 `CLAUDE.md`에 넣을 것

설치 위치는 **개발자별 로컬 설정**이라 커밋에 남으면 안 된다. 이 사실을 모르는 LLM은
"D 드라이브 쓰게 해줘" 같은 요청에 경로를 `blender_common.ps1` 같은 **공용 스크립트에
하드코딩**할 수 있다 — 남의 환경을 깨뜨리는 동시에 로컬 설정이 커밋으로 남는다.

대상 프로젝트 루트의 `CLAUDE.md`에 아래를 넣어두면 막을 수 있다 (없으면 새로 만든다):

```markdown
## Blender 환경 세팅 — 로컬 설정이다. 커밋 대상이 아니다

설치 위치 설정은 레포 밖 `%APPDATA%\BlenderArtKit\config.json`에 있다.
무엇을 하든 먼저 `cd ArtPipeline; .\setup.ps1 -Status`로 현재 상태를 본다.

| 요청 | 명령 |
|---|---|
| 설치해줘 | `.\setup.ps1` |
| 특정 위치에 설치해줘 | `.\setup.ps1 -InstallDir <사용자가 말한 경로>` |
| 이미 깔린 Blender 쓰게 해줘 | `.\setup.ps1 -BlenderExe <그 blender.exe 경로>` |
| 위치 바꿔줘 | 같은 명령을 다시 실행 |
| 설정 되돌려줘 | `.\setup.ps1 -Reset` |

⚠️ 경로를 레포 파일에 하드코딩하지 말 것. 위치 관련 요청은 전부 위 명령으로 처리한다 —
`setup.ps1`을 돌린 결과로 git 워킹트리가 변하면 그건 비정상이다.

절차는 `ArtPipeline/NEW-PROJECT.md`, 계약은 `ArtPipeline/KIT.md`.
```

---

## 8. 나중에 kit을 고쳤을 때

개선은 원본 레포에서 하고 내보낸다.

```powershell
cd <원본 레포>\ArtPipeline
.\install_kit.ps1 -Target <새 프로젝트> -Force
```

`-Force`는 **kit 파일에만** 걸린다. `pipeline.json`과 팔레트 레지스트리는 절대 덮어쓰지
않는다 — 출력 경로 설정이나 UV 계약이 kit 업데이트로 날아가면 안 되기 때문.

---

## 막혔을 때

| 증상 | 확인 |
|---|---|
| `blender.exe not found` | `.\setup.ps1 -Status` — 6개 후보 중 무엇이 잡히는지 그대로 보여준다 |
| `팔레트 레지스트리를 찾을 수 없습니다` | `pipeline.json`의 `paletteRegistry`가 가리키는 파일이 실제로 있는지 |
| `Unexpected UTF-8 BOM` | 해결됨(`utf-8-sig`). 그래도 나면 kit이 낡은 것 — `-Force`로 갱신 |
| 위치를 바꿨는데 안 바뀜 | 해결됨(설정 상호 배타). 낡은 kit이면 `-Reset` 후 다시 설정 |
| 새 인자가 조용히 무시됨 | `param()`에 없는 인자는 에러 없이 무시된다 → kit이 낡았다는 신호. `-Force`로 갱신 |
| 출력이 엉뚱한 곳에 | `kit_check.py`가 찍는 `assets root` 확인 → `pipeline.json`의 `assetsRoot`·`repoRoot` |
