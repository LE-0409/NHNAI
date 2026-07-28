"""슬롯머신 — 방 안에 놓인 유일한 오브젝트.

앤틱 업라이트 캐비닛. 아래에서 위로 받침 → 본체 → 조작대 → 릴 창 → 마퀴 순이고,
오른쪽 옆에 레버가 붙는다. 전체 높이 약 1.72 m.

**FBX 하나에 오브젝트 6개가 들어간다.** 움직이는 부품은 따로 나와야 Unity 가 돌릴 수 있다.

    SlotMachine   캐비닛 전체 (고정). 원점 = 기계 바닥 중앙
    Reel_0/1/2    릴 드럼 3개.  원점 = 각자의 회전축
    Lever         레버 팔 + 손잡이. 원점 = 허브 회전축
    ReelGlass     릴 창 유리 (M_Glass)
    ReelBacklight 릴 뒤 발광 패널 (M_Emissive) — 기계는 처음부터 켜져 있다

**회전하는 부품은 원점이 회전축에 있어야 한다.** 다른 에셋처럼 트랜스폼을 정점에
구워버리면 원점이 기계 바닥에 남아, 레버를 돌렸을 때 밑동을 축으로 휘둘러진다.
그래서 이 스크립트만 `set_pivot()` 으로 원점을 축에 다시 놓는다.
회전은 정점에 굽고 **이동만 오브젝트 트랜스폼에 남긴다** — FBX 익스포트가
회전까지 얹으면 축 변환과 겹쳐 엉키기 때문이다.

**릴 창을 뚫는 방법**: 로우폴리에는 불리언을 쓰지 않는다. 상단을 통짜 박스로 만들면
릴이 안 보이므로 위·아래·좌·우 프레임 4장과 뒷판으로 짜서 가운데를 비운다.

**전방 = −Y** (kit 규약). 플레이어가 서는 쪽이 −Y 다.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

from mathutils import Matrix, Vector

from lowpoly_lib import builders, export, palette, paths, preview

# --- 치수 --------------------------------------------------------------------
W = 0.58           # 본체 폭 (X)
D = 0.52           # 본체 깊이 (Y)
FRONT = -D / 2     # 앞면 Y (−0.26)

PLINTH_H = 0.10
BODY_H = 0.85
BODY_TOP = PLINTH_H + BODY_H          # 0.95

WIN_H = 0.30
FRAME_BOT = 0.08
FRAME_TOP = 0.09
FRAME_SIDE = 0.08
WIN_Z = BODY_TOP + FRAME_BOT + WIN_H / 2              # 1.18
HEAD_TOP = BODY_TOP + FRAME_BOT + WIN_H + FRAME_TOP   # 1.42
MARQUEE_H = 0.30

# 릴 — 면 8개짜리 드럼. 면마다 심볼이 하나씩 붙는다.
REEL_N = 8
REEL_R = 0.13
REEL_W = 0.11
REEL_Y = 0.03                                        # 창 안쪽으로 물러난 위치
REEL_X = (-0.14, 0.0, 0.14)
APOTHEM = REEL_R * math.cos(math.pi / REEL_N)        # 면 중심까지의 거리

SYM_T = 0.012                                        # 심볼 판 두께 (반지름 방향)
SYM_R = APOTHEM + SYM_T / 2                          # 심볼 판이 놓이는 반지름

LEVER_X = W / 2 + 0.05
LEVER_HUB = (LEVER_X - 0.02, -0.04, BODY_TOP + 0.05)  # 레버 회전축

# 심볼 8종. **실루엣만으로 구분한다** — 흑백 팔레트라 색으로 나눌 수 없고,
# 직사각형끼리는 창 안에서 가로세로 비율 차이로만 갈려 결국 다 같아 보인다.
# 그래서 동그라미 · 네모 · 세모처럼 윤곽이 서로 안 닮은 형태로 짠다.
#
# 마크 하나가 판 하나다. 심볼 하나에 여러 개를 겹칠 수 있다 (십자 · 별).


def poly(n, radius, spin=0.0, dr=0.0):
    """정n각형 판. n=3 세모, n=4 마름모, n≥12 는 이 크기에서 사실상 원.

    spin=0 이면 꼭짓점 하나가 창의 **위쪽**을 향한다. dr 은 반지름 방향 덧댐 —
    겹치는 마크를 살짝 띄워 같은 평면에서 z-파이팅 하는 것을 막는다.
    """
    return ("poly", n, radius, spin, dr)


def rect(up, across, dr=0.0):
    """직사각형 판. up = 릴이 흐르는 방향(창에서 세로), across = 드럼 축 방향(가로)."""
    return ("rect", up, across, dr)


# 면 하나의 크기는 세로 0.099 (= 2·R·sin22.5°) · 가로 0.11 (= REEL_W) 이다.
# 여유를 두고 어느 쪽도 0.076 을 넘기지 않는다.
SYMBOLS = [
    ("circle",   (poly(12, 0.035),)),
    ("square",   (rect(0.060, 0.060),)),
    ("triangle", (poly(3, 0.044),)),
    ("diamond",  (poly(4, 0.038),)),
    ("cross",    (rect(0.068, 0.022), rect(0.022, 0.068, dr=0.0006))),
    ("star",     (poly(3, 0.036), poly(3, 0.036, spin=180, dr=0.0006))),   # 세모 둘을 엇갈려 6각별
    ("bar",      (rect(0.024, 0.074),)),
    ("blank",    ()),
]


def set_pivot(obj, pivot):
    """오브젝트 원점을 pivot 으로 옮긴다 (모양은 그대로).

    정점을 pivot 만큼 반대로 밀고 오브젝트를 그만큼 이동시킨다. 결과적으로
    월드 위치는 같고 회전축만 바뀐다. Unity 에서 이 오브젝트를 회전시키면
    pivot 을 중심으로 돈다.
    """
    p = Vector(pivot)
    for v in obj.data.vertices:
        v.co -= p
    obj.location = p
    return obj


def mark_center(theta, dr):
    """각도 theta 인 면 바깥 SYM_R+dr 지점 (릴 로컬 좌표)."""
    r = SYM_R + dr
    return (r * math.cos(theta), r * math.sin(theta), 0.0)


def face_rotation(theta, spin):
    """프리즘 축(로컬 Z)을 면 바깥으로 눕히고, 단면을 면 안에서 spin(도) 만큼 돌린다.

    spin=0 에서 꼭짓점 하나가 **창의 위쪽**을 향한다. 릴을 나중에 통째로 눕히는
    회전(Y 90도)까지 따라가야 나오는 값이라 오일러 각을 손으로 적지 않고 행렬을
    곱해 뽑는다 — 눈으로 맞추려고 각도를 하나 건드리면 축이 같이 돌아 어긋난다.
    """
    return (Matrix.Rotation(theta, 4, "Z")
            @ Matrix.Rotation(math.radians(90), 4, "Y")
            @ Matrix.Rotation(math.radians(spin - 90), 4, "Z")).to_euler("XYZ")


def build_mark(name, mark, theta):
    """면 위에 심볼 마크 하나를 세운다."""
    if mark[0] == "rect":
        _, up, across, dr = mark
        return builders.box(name, (SYM_T, up, across), loc=mark_center(theta, dr),
                            rot=(0, 0, math.degrees(theta)), color="chalk")

    _, n, radius, spin, dr = mark
    obj = builders.prism(name, radius, SYM_T, n=n, loc=mark_center(theta, dr), color="chalk")
    obj.rotation_euler = face_rotation(theta, spin)
    return obj


builders.reset_scene()
palette.write_palette_png()

cabinet = []   # 캐비닛으로 합칠 고정 부품

# --- 받침 · 본체 -------------------------------------------------------------
cabinet.append(builders.box("Plinth", (W + 0.04, D + 0.04, PLINTH_H),
                            loc=(0, 0, PLINTH_H / 2), color="charcoal"))
cabinet.append(builders.box("Body", (W, D, BODY_H),
                            loc=(0, 0, PLINTH_H + BODY_H / 2), color="iron"))
cabinet.append(builders.box("BodyPanel", (0.46, 0.02, 0.56),
                            loc=(0, FRONT - 0.005, 0.54), color="charcoal"))

# --- 코인 트레이 · 투입구 ----------------------------------------------------
cabinet.append(builders.box("CoinTray", (0.34, 0.12, 0.06),
                            loc=(0, FRONT - 0.05, 0.22), color="ash"))
cabinet.append(builders.box("CoinSlot", (0.09, 0.02, 0.025),
                            loc=(0.19, FRONT - 0.008, BODY_TOP + 0.02), color="void"))

# --- 조작대 -----------------------------------------------------------------
cabinet.append(builders.box("ControlDeck", (W, 0.20, 0.06),
                            loc=(0, FRONT - 0.07, BODY_TOP - 0.01),
                            rot=(-14, 0, 0), color="ash"))
for i, bx in enumerate((-0.15, 0.0, 0.15)):
    cabinet.append(builders.prism(f"Button_{i}", 0.028, 0.028, n=8,
                                  loc=(bx, FRONT - 0.07, BODY_TOP + 0.03),
                                  color="concrete"))

# --- 릴 창 프레임 (가운데를 비운다) ------------------------------------------
cabinet.append(builders.box("Frame_Bottom", (W, D, FRAME_BOT),
                            loc=(0, 0, BODY_TOP + FRAME_BOT / 2), color="iron"))
cabinet.append(builders.box("Frame_Top", (W, D, FRAME_TOP),
                            loc=(0, 0, HEAD_TOP - FRAME_TOP / 2), color="iron"))
for sx in (-1, 1):
    cabinet.append(builders.box(f"Frame_Side{sx}", (FRAME_SIDE, D, WIN_H),
                                loc=(sx * (W - FRAME_SIDE) / 2, 0, WIN_Z), color="iron"))

WIN_HALF_X = (W - FRAME_SIDE * 2) / 2
WIN_TOP = WIN_Z + WIN_H / 2
WIN_BOT = WIN_Z - WIN_H / 2
BEZEL = 0.04
BEZEL_Y = FRONT - 0.004
for name, size, loc in (
    ("Bezel_Top", (WIN_HALF_X * 2 + BEZEL * 2, 0.02, BEZEL), (0, BEZEL_Y, WIN_TOP + BEZEL / 2)),
    ("Bezel_Bottom", (WIN_HALF_X * 2 + BEZEL * 2, 0.02, BEZEL), (0, BEZEL_Y, WIN_BOT - BEZEL / 2)),
    ("Bezel_Left", (BEZEL, 0.02, WIN_H + BEZEL * 2), (-(WIN_HALF_X + BEZEL / 2), BEZEL_Y, WIN_Z)),
    ("Bezel_Right", (BEZEL, 0.02, WIN_H + BEZEL * 2), (WIN_HALF_X + BEZEL / 2, BEZEL_Y, WIN_Z)),
):
    cabinet.append(builders.box(name, size, loc=loc, color="ash"))

# --- 마퀴 · 레버 허브 --------------------------------------------------------
cabinet.append(builders.box("Marquee", (W + 0.04, 0.30, MARQUEE_H),
                            loc=(0, 0.02, HEAD_TOP + MARQUEE_H / 2), color="iron"))
cabinet.append(builders.box("MarqueePanel", (0.48, 0.02, 0.20),
                            loc=(0, 0.02 - 0.15 - 0.005, HEAD_TOP + MARQUEE_H / 2),
                            color="paper"))
cabinet.append(builders.prism("LeverHub", 0.045, 0.07, n=10,
                              loc=LEVER_HUB, rot=(0, 90, 0), color="ash"))

machine = builders.join_all(cabinet, "SlotMachine")

# --- 릴 3개 ------------------------------------------------------------------
# 드럼은 Z축 프리즘으로 만들어 심볼을 붙인 뒤 통째로 눕힌다. 처음부터 누워 있는
# 상태로 계산하면 각도·오프셋이 전부 뒤틀려 읽기 어려워진다.
reels = []
for ri, rx in enumerate(REEL_X):
    parts = [builders.prism(f"Drum_{ri}", REEL_R, REEL_W, n=REEL_N, color="concrete")]

    # 면마다 밝기를 번갈아 준다. 심볼이 없어도 회전이 눈에 보이게 하는 장치다.
    # prism 의 폴리곤 0..n-1 이 옆면이고 그 뒤가 바닥·천장 캡이다.
    for fi in range(REEL_N):
        palette.apply_color(parts[0], "bone" if fi % 2 else "concrete", faces=[fi])

    for fi in range(REEL_N):
        sym, marks = SYMBOLS[(fi + ri * 3) % len(SYMBOLS)]   # 릴마다 배열을 어긋나게
        theta = 2 * math.pi * (fi + 0.5) / REEL_N            # 면 중심 각도
        for mi, mark in enumerate(marks):
            parts.append(build_mark(f"Sym_{ri}_{fi}_{sym}_{mi}", mark, theta))

    reel = builders.join_all(parts, f"Reel_{ri}")
    # 눕혀서 제자리로 보낸 뒤 다시 구워 회전을 정점에 흡수시킨다.
    reel.rotation_euler = (0, math.radians(90), 0)
    reel.location = (rx, REEL_Y, WIN_Z)
    reel = builders.join_all([reel], f"Reel_{ri}")
    set_pivot(reel, (rx, REEL_Y, WIN_Z))
    reels.append(reel)

# --- 레버 (팔 + 손잡이). 허브는 캐비닛에 남고 이 둘만 돈다 --------------------
lever_parts = [
    builders.prism("LeverArm", 0.016, 0.30, n=6,
                   loc=(LEVER_X, 0.01, BODY_TOP + 0.19), rot=(-20, 0, 0), color="bone"),
    builders.icosphere("LeverKnob", 0.048, subdiv=1,
                       loc=(LEVER_X, 0.06, BODY_TOP + 0.33), color="chalk"),
]
lever = builders.join_all(lever_parts, "Lever")
set_pivot(lever, LEVER_HUB)

# --- 유리 · 백라이트 ---------------------------------------------------------
# 백라이트는 릴 **뒤**에 선다. 기계가 처음부터 켜져 있다는 인상이 여기서 나온다 —
# 어두운 방에서 이 패널이 유일하게 스스로 빛나는 면이다.
glass = builders.box("ReelGlass", (0.42, 0.015, 0.30), loc=(0, FRONT + 0.02, WIN_Z))
palette.use_glass_material(glass)

backlight = builders.box("ReelBacklight", (0.44, 0.015, WIN_H - 0.02),
                         loc=(0, D / 2 - 0.07, WIN_Z))
palette.use_emissive_material(backlight)

# --- 프리뷰 · 익스포트 -------------------------------------------------------
# 유리를 숨긴다. Workbench 는 알파를 반영하지 않아 두면 불투명 판이 릴을 덮는다.
glass.hide_render = True
preview.render_turnaround("slot_machine", [machine] + reels + [lever])
# 심볼 클로즈업. 전체 렌더에서는 릴 창이 손톱만 해 형태가 구분되는지 알 수 없다.
# 릴만 넘기면 프레이밍이 릴 크기로 좁혀진다 — 카메라 설정은 건드릴 게 없다.
preview.render_turnaround("slot_machine_reels", reels,
                          out_dir=os.path.join(paths.PREVIEWS, "slot_machine"),
                          views={"front": Vector((0.0, -1.0, 0.0))})
glass.hide_render = False

objs = [machine] + reels + [lever, glass, backlight]
export.export_static(objs, paths.art("Environment", "SlotMachine.fbx"))
print("EXPORTED SlotMachine")

print("--- 오브젝트별 원점 (회전축) ---")
for o in objs:
    print(f"  {o.name:<14} origin = ({o.location.x:.3f}, {o.location.y:.3f}, {o.location.z:.3f})")
print(f"  릴 심볼 수 = {REEL_N} (SlotMachine.cs 의 SymbolCount 와 같아야 한다)")
