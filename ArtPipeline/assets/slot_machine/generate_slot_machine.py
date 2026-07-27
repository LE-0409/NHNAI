"""슬롯머신 — 방 안에 놓인 유일한 오브젝트.

앤틱 업라이트 캐비닛. 아래에서 위로 받침 → 본체 → 조작대 → 릴 창 → 마퀴 순이고,
오른쪽 옆에 레버가 붙는다. 전체 높이 약 1.72 m.

**릴 창을 뚫는 방법**: 로우폴리에는 불리언을 쓰지 않는다. 상단을 통짜 박스로 만들면
릴이 안 보이므로 위·아래·좌·우 프레임 4장과 뒷판으로 짜서 가운데를 비운다.
릴 실린더는 그 빈 공간 안에 들어가고, 앞을 유리가 덮는다.

**전방 = −Y** (kit 규약). 플레이어가 서는 쪽이 −Y 다.
**좌표계**: 원점 = 기계 바닥 중앙, Z 위쪽. 방 좌표에 그대로 놓을 수 있다.

세부 형태(릴 심볼·마퀴 문구·버튼 배치)는 아직 정하지 않았다. 지금은 실루엣과
명도 배분만 맞춘다.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

from lowpoly_lib import builders, export, palette, paths, preview

# --- 치수 --------------------------------------------------------------------
W = 0.58           # 본체 폭 (X)
D = 0.52           # 본체 깊이 (Y)
FRONT = -D / 2     # 앞면 Y (−0.26)

PLINTH_H = 0.10                       # 받침       0.00 ~ 0.10
BODY_H = 0.85                         # 본체       0.10 ~ 0.95
BODY_TOP = PLINTH_H + BODY_H          # 0.95

WIN_H = 0.30                          # 릴 창 개구부 높이
FRAME_BOT = 0.08                      # 창 아래 프레임 두께
FRAME_TOP = 0.09                      # 창 위 프레임 두께
FRAME_SIDE = 0.08                     # 창 좌우 기둥 폭
WIN_Z = BODY_TOP + FRAME_BOT + WIN_H / 2          # 창 중심 Z = 1.18
HEAD_TOP = BODY_TOP + FRAME_BOT + WIN_H + FRAME_TOP   # 1.42

MARQUEE_H = 0.30                      # 마퀴      1.42 ~ 1.72

REEL_R = 0.13
REEL_W = 0.11      # 간격(0.03)을 남겨 릴 3개가 한 덩어리로 보이지 않게 한다
REEL_X = (-0.14, 0.0, 0.14)

builders.reset_scene()
palette.write_palette_png()

parts = []   # 팔레트 슬롯으로 합칠 부품

# --- 받침 · 본체 -------------------------------------------------------------
parts.append(builders.box("Plinth", (W + 0.04, D + 0.04, PLINTH_H),
                          loc=(0, 0, PLINTH_H / 2), color="charcoal"))
parts.append(builders.box("Body", (W, D, BODY_H),
                          loc=(0, 0, PLINTH_H + BODY_H / 2), color="iron"))
# 본체 앞면 장식 패널 — 어두운 면을 하나 끼워 캐비닛이 통짜로 보이지 않게 한다
parts.append(builders.box("BodyPanel", (0.46, 0.02, 0.56),
                          loc=(0, FRONT - 0.005, 0.54), color="charcoal"))

# --- 코인 트레이 · 투입구 ----------------------------------------------------
parts.append(builders.box("CoinTray", (0.34, 0.12, 0.06),
                          loc=(0, FRONT - 0.05, 0.22), color="ash"))
parts.append(builders.box("CoinSlot", (0.09, 0.02, 0.025),
                          loc=(0.19, FRONT - 0.008, BODY_TOP + 0.02), color="void"))

# --- 조작대 (앞으로 돌출, 뒤로 기운 경사면) ----------------------------------
parts.append(builders.box("ControlDeck", (W, 0.20, 0.06),
                          loc=(0, FRONT - 0.07, BODY_TOP - 0.01),
                          rot=(-14, 0, 0), color="ash"))
for i, bx in enumerate((-0.15, 0.0, 0.15)):
    parts.append(builders.prism(f"Button_{i}", 0.028, 0.028, n=8,
                                loc=(bx, FRONT - 0.07, BODY_TOP + 0.03),
                                color="concrete"))

# --- 릴 창 프레임 (가운데를 비운다) ------------------------------------------
parts.append(builders.box("Frame_Bottom", (W, D, FRAME_BOT),
                          loc=(0, 0, BODY_TOP + FRAME_BOT / 2), color="iron"))
parts.append(builders.box("Frame_Top", (W, D, FRAME_TOP),
                          loc=(0, 0, HEAD_TOP - FRAME_TOP / 2), color="iron"))
for sx in (-1, 1):
    parts.append(builders.box(f"Frame_Side{sx}", (FRAME_SIDE, D, WIN_H),
                              loc=(sx * (W - FRAME_SIDE) / 2, 0, WIN_Z), color="iron"))
parts.append(builders.box("Frame_Back", (W, 0.06, WIN_H),
                          loc=(0, D / 2 - 0.03, WIN_Z), color="void"))

# 창 테두리 — 개구부 둘레를 한 단계 밝게. 어두운 방에서 기계의 얼굴이 되는 선이다.
# ⚠️ 판 하나로 만들면 개구부를 덮어 릴이 사라진다. 반드시 바 4개로 두른다.
WIN_HALF_X = (W - FRAME_SIDE * 2) / 2       # 개구부 좌우 끝 (±0.21)
WIN_TOP = WIN_Z + WIN_H / 2                 # 1.33
WIN_BOT = WIN_Z - WIN_H / 2                 # 1.03
BEZEL = 0.04                                # 테두리 폭
BEZEL_Y = FRONT - 0.004
for name, size, loc in (
    ("Bezel_Top", (WIN_HALF_X * 2 + BEZEL * 2, 0.02, BEZEL), (0, BEZEL_Y, WIN_TOP + BEZEL / 2)),
    ("Bezel_Bottom", (WIN_HALF_X * 2 + BEZEL * 2, 0.02, BEZEL), (0, BEZEL_Y, WIN_BOT - BEZEL / 2)),
    ("Bezel_Left", (BEZEL, 0.02, WIN_H + BEZEL * 2), (-(WIN_HALF_X + BEZEL / 2), BEZEL_Y, WIN_Z)),
    ("Bezel_Right", (BEZEL, 0.02, WIN_H + BEZEL * 2), (WIN_HALF_X + BEZEL / 2, BEZEL_Y, WIN_Z)),
):
    parts.append(builders.box(name, size, loc=loc, color="ash"))

# --- 릴 3개 ------------------------------------------------------------------
# 실린더 축은 좌우(X) 방향이라 Z축 프리즘을 Y 기준 90° 눕힌다.
for i, rx in enumerate(REEL_X):
    parts.append(builders.prism(f"Reel_{i}", REEL_R, REEL_W, n=12,
                                loc=(rx, 0.02, WIN_Z), rot=(0, 90, 0), color="bone"))

# --- 마퀴 (상단 간판) --------------------------------------------------------
parts.append(builders.box("Marquee", (W + 0.04, 0.30, MARQUEE_H),
                          loc=(0, 0.02, HEAD_TOP + MARQUEE_H / 2), color="iron"))
parts.append(builders.box("MarqueePanel", (0.48, 0.02, 0.20),
                          loc=(0, 0.02 - 0.15 - 0.005, HEAD_TOP + MARQUEE_H / 2),
                          color="paper"))

# --- 레버 (오른쪽 옆) --------------------------------------------------------
LEVER_X = W / 2 + 0.05
parts.append(builders.prism("LeverHub", 0.045, 0.07, n=10,
                            loc=(LEVER_X - 0.02, -0.04, BODY_TOP + 0.05),
                            rot=(0, 90, 0), color="ash"))
parts.append(builders.prism("LeverArm", 0.016, 0.30, n=6,
                            loc=(LEVER_X, 0.01, BODY_TOP + 0.19),
                            rot=(-20, 0, 0), color="bone"))
parts.append(builders.icosphere("LeverKnob", 0.048, subdiv=1,
                                loc=(LEVER_X, 0.06, BODY_TOP + 0.33), color="chalk"))

machine = builders.join_all(parts, "SlotMachine")

# 유리는 별도 머티리얼 슬롯이라 join 하지 않는다. join_all 은 활성 오브젝트의 슬롯0 을
# 기준으로 합치므로 여기서 합치면 유리 슬롯이 팔레트 슬롯에 먹힌다.
glass = builders.box("ReelGlass", (0.42, 0.015, 0.30), loc=(0, FRONT + 0.02, WIN_Z))
palette.use_glass_material(glass)

# 프리뷰는 유리를 숨기고 렌더한다. Workbench 는 알파를 반영하지 않아 유리를 두면
# 불투명 판이 릴을 덮어버려, 릴이 제대로 만들어졌는지 확인할 방법이 없어진다.
# 게임에서는 유리가 투명하므로 이쪽이 오히려 최종 화면에 가깝다.
#
# ⚠️ render_turnaround 의 objs 인자는 **카메라 프레이밍용 바운즈 계산에만** 쓰인다.
# 렌더는 씬 전체를 그리므로 목록에서 빼는 것만으로는 숨겨지지 않는다. hide_render 를 써야 한다.
glass.hide_render = True
preview.render_turnaround("slot_machine", [machine])
glass.hide_render = False
export.export_static([machine, glass], paths.art("Environment", "SlotMachine.fbx"))
print("EXPORTED SlotMachine")
