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

**무늬 없음**: 앞뒤 면은 민짜다. 대신 테두리에 챔퍼를 둘러 옆에서 봐도 판때기가 아니라
두툼한 금속 원반으로 읽히게 했다 — 무늬가 없는 만큼 형태가 전부 실루엣과 명암에서 나온다.
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
# 슬롯머신 투입구(generate_slot_machine.py 의 CoinSlot)는 0.09 x 0.025 다.
# 지름과 두께가 그 안에 들어가야 '넣는' 연출이 성립한다 — 아래 검사에서 확인한다.
R = 0.030          # 반지름 (지름 6 cm). 실제 주화보다 크다 — 어두운 방에서 손에 들려도 읽혀야 한다
T = 0.012          # 두께. 지름 대비 0.2 로 두툼하게. 얇으면 조명이 옆면에 걸리지 않는다
CHAMFER_R = 0.004  # 챔퍼가 안으로 파고드는 깊이 (반지름 방향)
CHAMFER_H = 0.003  # 챔퍼가 차지하는 높이. T - 2*CHAMFER_H 가 옆면 직선 구간이 된다
N = 16             # 옆면 분할. 12 는 6 cm 에서 각이 보이고, 24 는 이 크기에서 낭비다

R_FACE = R - CHAMFER_R

# 아래에서 위로 가는 (반지름, z) 링. 대칭이라 앞뒤가 같다.
PROFILE = [
    (R_FACE, -T / 2),                 # 아랫면 가장자리
    (R, -T / 2 + CHAMFER_H),          # 챔퍼 끝 = 최대 지름
    (R, +T / 2 - CHAMFER_H),          # 옆면 직선 구간 끝
    (R_FACE, +T / 2),                 # 윗면 가장자리
]


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

# 앞뒤 면을 테두리보다 한 단계 밝게 둔다. 전등 하나짜리 방에서는 동전이 어떤 자세로
# 놓이든 조명이 한쪽 면에만 걸리는데, 명도까지 같으면 원반이 그냥 밝은 얼룩이 된다.
# 면(chalk 0.87) > 테두리(paper 0.74) 차이가 두께를 만들어 준다.
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
SLOT_W, SLOT_H = 0.09, 0.025   # generate_slot_machine.py 의 CoinSlot 크기
print("--- 동전 제원 ---")
print(f"  지름 · 두께   : {R * 2:.3f} x {T:.3f} m")
print(f"  버텍스 · 면   : {len(coin.data.vertices)} v / {len(coin.data.polygons)} f")
zs = [v.co.z for v in coin.data.vertices]
print(f"  로컬 Z 범위   : {min(zs):+.4f} ~ {max(zs):+.4f}  (중심 원점이면 대칭이어야 한다)")
fits = R * 2 < SLOT_W and T < SLOT_H
print(f"  투입구 통과   : {'OK' if fits else '⚠️ 안 들어간다'} "
      f"(투입구 {SLOT_W:.3f} x {SLOT_H:.3f})")
