"""동전 — 슬롯머신에 넣는 황금 동전. 무늬 없는 민짜.

FBX 하나에 오브젝트 하나만 들어간다.

    Coin   원점 = 동전 **중심**. 축은 Z (Unity 로 가면 Y)

**원점을 왜 중심에 두나**: 이 에셋은 바닥에 놓이기만 하는 게 아니라 튀고 · 돌고 ·
뒤집힌다. 원점이 아래 면에 있으면 회전이 전부 편심이 되어 손으로 오프셋을 다시
맞춰야 한다. 중심에 두면 Unity 에서 로컬 X/Z 축 회전이 그대로 '뒤집기' 가 된다.
다른 에셋(방·전등)처럼 바닥 기준이 아닌 것은 이 때문이다.

**황금인데 왜 흑백인가**: 팔레트는 무채색 10단계뿐이다(`palette_registry.py`).
색조를 에셋에 구우면 되돌릴 수 없고, 톤은 URP 포스트 프로세싱에서 **한 곳으로** 낸다.
그래서 여기서는 '가장 밝은 금속' 으로만 만든다 — 어두운 방에서 캐비닛(iron 0.22)
위에 놓이면 명도 차만으로 충분히 튄다. 실제 금색이 필요해지면 팔레트를 건드리지 말고
`M_Gold` 같은 **머티리얼 슬롯**을 새로 선언해 Unity 쪽에서 리맵한다.

**무늬 없음**: 앞뒤 면은 민짜다. 무늬가 없는 만큼 형태가 전부 실루엣과 명암에서 나오므로
단면을 이렇게 짠다 — 가장자리에 **얇은 림**이 한 바퀴 돌고 그 안쪽이 한 단 파여 있다.
민짜 면을 그냥 평평하게 두면 어느 각도에서도 밝기가 한 덩어리라 원반이 얼룩처럼 보인다.
림이 얇을수록 파인 면이 넓어 보이므로 림 폭은 최소로 잡았다.

파임 벽은 **직각**이다. 경사로 내려가면 그 면이 빛을 받아 밝아지면서 파임이 완만한
그릇처럼 읽힌다 — 동전이 아니라 접시가 된다. 수직으로 세우면 벽이 위쪽 빛을 거의 못
받아 얇은 그늘 선이 되고, 찍어 눌러 만든 금속처럼 단이 딱 떨어진다.

옆면은 위아래를 깎지 않고 **수직으로 떨어뜨린다.** 챔퍼를 두르면 실루엣이 뭉개져
두께가 얇아 보이고, 로우폴리에서 굳이 분할을 늘릴 이유가 없다. 대신 분할을 12각으로
낮춰 옆면이 원통이 아니라 각진 금속 덩어리로 읽히게 했다.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector

from lowpoly_lib import builders, export, palette, paths, preview

# --- 치수 --------------------------------------------------------------------
# 슬롯머신 투입구(generate_slot_machine.py 의 CoinSlot)는 세로 슬릿 0.012 x 0.070 이다.
# 동전이 **세로로 서서** 들어가므로 두께 < 슬릿 폭, 지름 < 슬릿 높이여야
# '넣는' 연출이 성립한다 — 아래 검사에서 확인한다.
R = 0.030          # 반지름 (지름 6 cm). 실제 주화보다 크다 — 어두운 방에서 손에 들려도 읽혀야 한다
T = 0.007          # 두께. 지름 대비 0.12. 더 얇게 가면 옆면이 조명을 못 받아 실루엣이 종잇장이 된다
N = 12             # 옆면 분할. **각이 보여야 한다** — 16 이상은 원통으로 읽혀 로우폴리 톤에서 뜬다

RIM_W = 0.0025     # 림(테두리) 폭. 이만큼이 파인 면을 깎아 먹는다 — 늘리면 면이 좁아진다
# 파인 깊이는 **두께에 매단다.** 절대값으로 박아 두면 두께를 줄였을 때 앞뒤에서 파고든
# 만큼이 그대로 남아 가운데 살이 사라지고, 동전이 아니라 얕은 접시로 읽힌다.
RECESS_D = T * 0.15

R_FACE = R - RIM_W   # 파인 면의 반지름. **파임 벽이 수직이라** 림 안쪽 끝과 같은 값이다

# 아래에서 위로 가는 (반지름, z) 링. 앞뒤 대칭이라 어느 쪽으로 떨어져도 같은 얼굴이다.
# 같은 반지름이 연달아 나오는 구간(0→1, 2→3, 4→5)이 수직 벽이다.
PROFILE = [
    (R_FACE, -T / 2 + RECESS_D),   # 아랫면 (파인 면)
    (R_FACE, -T / 2),              # 파임 벽 — 직각으로 떨어진다
    (R, -T / 2),                   # 아래 림 (평평한 고리) 바깥 = 최대 지름
    (R, +T / 2),                   # 수직 옆면
    (R_FACE, +T / 2),              # 위 림
    (R_FACE, +T / 2 - RECESS_D),   # 윗면 (파인 면)
]

# 모든 구간은 수평(림) 아니면 수직(벽)이어야 한다. 반지름과 z 가 **같이** 변하는 구간이
# 하나라도 생기면 그게 경사면이고, 파임이 접시처럼 읽히기 시작한다. 눈으로 잡기 어려워
# — 렌더에서는 그저 '조금 부드러운 단' 으로 보인다 — 여기서 막는다.
DIAGONAL = [k for k in range(len(PROFILE) - 1)
            if abs(PROFILE[k][0] - PROFILE[k + 1][0]) > 1e-9
            and abs(PROFILE[k][1] - PROFILE[k + 1][1]) > 1e-9]
if DIAGONAL:
    raise SystemExit(f"PROFILE 에 경사 구간이 있다 (링 {DIAGONAL}). "
                     "반지름과 z 중 하나만 변해야 한다.")


def lathe(name, rings, n=N):
    """(반지름, z) 링을 아래→위로 이어 붙인 회전체. 양 끝은 n각형 캡 한 장으로 막는다.

    프리즘 3개를 쌓지 않는 이유: 쌓으면 이음매마다 캡이 두 장씩 **안쪽에** 남는다.
    보이지 않는 면이라 넘어갈 것 같지만, 서로 겹친 코플래너 면이라 각도에 따라
    비쳐 보이고 삼각형 수만 늘어난다. 회전체로 한 번에 뜨면 그런 게 없다.

    감기 방향과 캡 구성은 `builders.prism` 과 같게 맞춘다 — 파이프라인 안에서
    바깥 방향 CCW 규약이 하나여야 한다.
    """
    verts = [
        (r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n), z)
        for r, z in rings for i in range(n)
    ]
    faces = []
    for k in range(len(rings) - 1):
        lo, hi = k * n, (k + 1) * n
        faces += [(lo + i, lo + (i + 1) % n, hi + (i + 1) % n, hi + i) for i in range(n)]
    faces.append(tuple(reversed(range(n))))                              # 아랫면 캡
    faces.append(tuple(range(n * (len(rings) - 1), n * len(rings))))     # 윗면 캡

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for poly in obj.data.polygons:
        poly.use_smooth = False      # 로우폴리 규약 — 면마다 법선 하나
    return obj


builders.reset_scene()
palette.write_palette_png()

coin = lathe("Coin", PROFILE)

# 파인 면(chalk 0.87)을 나머지(paper 0.74)보다 한 단계 밝게 둔다.
# 림은 파인 면과 **법선이 같아** 조명만으로는 구분되지 않는다 — 명도를 갈라야 얇은
# 테두리 선으로 읽힌다. 옆면·파임 벽은 법선이 달라 조명이 알아서 갈라 준다.
palette.apply_color(coin, "paper")
CAPS = [len(coin.data.polygons) - 2, len(coin.data.polygons) - 1]
palette.apply_color(coin, "chalk", faces=CAPS)

# ⚠️ 오브젝트 하나라도 join_all 을 거친다. 트랜스폼을 정점에 굽지 않으면 위치가
# FBX 노드 트랜스폼에만 실려 Unity 에서 기본값으로 맞물리지 않는다.
coin = builders.join_all([coin], "Coin")

# --- 프리뷰 ------------------------------------------------------------------
# 기본 4방향(front/back/left/persp34)을 쓰지 않는다. 회전체라 front · back · left 가
# 전부 같은 그림이고, persp34 는 원근 카메라라 6 cm 짜리가 화면에서 손톱만 해진다.
# (프레이밍은 ortho 뷰에만 걸린다 — preview.render_turnaround 참조)
COIN_VIEWS = {
    "face": Vector((0.0, -0.35, 1.0)),    # 원반 면. 정수직은 카메라 업벡터가 불안정해 살짝 눕힌다
    "edge": Vector((0.0, -1.0, 0.0)),     # 옆 실루엣 — 두께와 챔퍼를 여기서 본다
    "tilt34": Vector((-0.8, -1.0, 0.55)),  # 3/4. 면과 테두리 명도 차가 같이 보이는 각
}
preview.render_turnaround("coin", [coin], views=COIN_VIEWS)

# Environment 밑에 둔다. 프롭이지만 ArtMaterialLibrary.cs 의 리맵 대상 디렉터리가
# Assets/Art/Environment 하나뿐이라, 다른 데 두면 머티리얼이 안 붙는다.
# 프롭이 늘어나면 그때 Props 폴더와 리맵 범위를 같이 넓힌다.
export.export_static([coin], paths.art("Environment", "Coin.fbx"))
print("EXPORTED Coin")

# --- 검사 --------------------------------------------------------------------
SLIT_W, SLIT_H = 0.012, 0.070   # generate_slot_machine.py 의 투입 슬릿 크기 복사본
print("--- 동전 제원 ---")
print(f"  지름 · 두께   : {R * 2:.3f} x {T:.3f} m")
print(f"  림 · 파임     : 폭 {RIM_W * 1000:.1f} mm · 깊이 {RECESS_D * 1000:.2f} mm "
      f"(파인 면 반지름 {R_FACE * 1000:.1f} mm)")
print(f"  버텍스 · 면   : {len(coin.data.vertices)} v / {len(coin.data.polygons)} f")
zs = [v.co.z for v in coin.data.vertices]
print(f"  로컬 Z 범위   : {min(zs):+.4f} ~ {max(zs):+.4f}  (중심 원점이면 대칭이어야 한다)")
fits = T < SLIT_W and R * 2 < SLIT_H   # 세로 투입: 두께가 폭을, 지름이 높이를 통과
print(f"  투입 슬릿 통과: {'OK' if fits else '⚠️ 안 들어간다'} "
      f"(슬릿 {SLIT_W:.3f} x {SLIT_H:.3f})")
