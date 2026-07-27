"""독방 — 게임이 시작되는 작은 방.

안에서 보는 공간이라 **단일 박스로 만들지 않는다.** 박스는 면이 바깥을 향해
1인칭 카메라가 안에 들어가면 백페이스 컬링으로 전부 사라진다. 두께 있는 판 6장을
안쪽을 향하도록 배치해 어느 쪽에서 봐도 면이 있게 만든다.

치수: 내부 4.0 × 4.0 × 3.0 m, 벽 두께 0.15 m. 원점은 방 바닥 중앙(Z=0).
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

from lowpoly_lib import builders, export, palette, paths, preview

# --- 치수 --------------------------------------------------------------------
W = 4.0        # 내부 폭 (X)
D = 4.0        # 내부 깊이 (Y)
H = 3.0        # 내부 높이 (Z)
T = 0.15       # 벽 두께

HW, HD = W / 2, D / 2
SKIRT_H = 0.18   # 걸레받이 높이
SKIRT_T = 0.04   # 걸레받이가 벽에서 튀어나온 두께

builders.reset_scene()
palette.write_palette_png()

parts = []
cutaway = []   # 프리뷰에서 걷어낼 면 — 아래 설명 참조

# --- 바닥 · 천장 -------------------------------------------------------------
# 바닥 윗면이 정확히 Z=0 이 되도록 박스 중심을 반 두께만큼 내린다.
parts.append(builders.box("Floor", (W + T * 2, D + T * 2, T),
                          loc=(0, 0, -T / 2), color="charcoal"))
ceiling = builders.box("Ceiling", (W + T * 2, D + T * 2, T),
                       loc=(0, 0, H + T / 2), color="ink")
parts.append(ceiling)
cutaway.append(ceiling)

# --- 벽 4장 ------------------------------------------------------------------
# 안쪽 면이 각각 ±HW, ±HD 에 오도록 중심을 반 두께만큼 바깥으로 민다.
for name, size, loc in (
    ("Wall_N", (W + T * 2, T, H), (0, HD + T / 2, H / 2)),
    ("Wall_S", (W + T * 2, T, H), (0, -(HD + T / 2), H / 2)),
    ("Wall_E", (T, D, H), (HW + T / 2, 0, H / 2)),
    ("Wall_W", (T, D, H), (-(HW + T / 2), 0, H / 2)),
):
    obj = builders.box(name, size, loc=loc, color="ink")
    parts.append(obj)
    if name == "Wall_S":
        cutaway.append(obj)

# --- 걸레받이 ----------------------------------------------------------------
# 바닥과 벽이 맞닿는 선을 한 단계 밝게 끊어 준다. 빛이 거의 없어도 이 선 덕분에
# 방의 크기가 읽힌다 — 어두운 방에서 공간감을 만드는 건 면이 아니라 경계다.
for name, size, loc in (
    ("Skirt_N", (W, SKIRT_T, SKIRT_H), (0, HD - SKIRT_T / 2, SKIRT_H / 2)),
    ("Skirt_S", (W, SKIRT_T, SKIRT_H), (0, -(HD - SKIRT_T / 2), SKIRT_H / 2)),
    ("Skirt_E", (SKIRT_T, D, SKIRT_H), (HW - SKIRT_T / 2, 0, SKIRT_H / 2)),
    ("Skirt_W", (SKIRT_T, D, SKIRT_H), (-(HW - SKIRT_T / 2), 0, SKIRT_H / 2)),
):
    obj = builders.box(name, size, loc=loc, color="ash")
    parts.append(obj)
    if name == "Skirt_S":
        cutaway.append(obj)

# --- 프리뷰: 앞벽과 천장을 걷어낸 단면 ---------------------------------------
# 방을 통째로 렌더하면 바깥에서 본 검은 상자만 나와 아무것도 검증되지 않는다.
# 합치기 전에 앞쪽(−Y) 벽과 천장을 숨기고 렌더해 내부를 본다 — 천장을 남기면
# 위에서 내려다보는 persp34 뷰가 다시 막힌다. 익스포트에는 전부 들어간다.
#
# ⚠️ render_turnaround 의 objs 인자는 **카메라 프레이밍용 바운즈 계산에만** 쓰인다.
# 렌더는 씬 전체를 그리므로 목록에서 빼는 것만으로는 숨겨지지 않는다. hide_render 를 써야 한다.
for o in cutaway:
    o.hide_render = True
preview.render_turnaround("cell_room", [o for o in parts if o not in cutaway])
for o in cutaway:
    o.hide_render = False

room = builders.join_all(parts, "CellRoom")
export.export_static([room], paths.art("Environment", "CellRoom.fbx"))
print("EXPORTED CellRoom")
