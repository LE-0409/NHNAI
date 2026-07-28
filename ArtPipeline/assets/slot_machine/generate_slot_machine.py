"""슬롯머신 — 방 안에 놓인 유일한 오브젝트.

앤틱 업라이트 캐비닛. 아래에서 위로 받침 → 본체 → 조작대 → 릴 창 → 마퀴 순이고,
오른쪽 옆에 레버가 붙는다. 전체 높이 약 1.72 m.

**FBX 하나에 오브젝트 12개가 들어간다.** 움직이는 부품은 따로 나와야 Unity 가 돌릴 수 있고,
레버는 위치를 다시 잡을 일이 잦아 뿌리를 감추는 허브까지 따로 뽑는다.
동전 관련 부품은 고정이지만 **원점이 곧 Unity 의 앵커**라 따로 뽑는다 —
합쳐 버리면 부트스트랩이 좌표를 손으로 옮겨 적어야 하고, 여기 치수를 바꿀 때마다 어긋난다.
환불 버튼도 상호작용(조준 콜라이더 · 누름 연출) 대상이라 따로 뽑는다.

    SlotMachine   캐비닛 전체 (고정). 원점 = 기계 바닥 중앙
    Reel_0/1/2    릴 드럼 3개.  원점 = 각자의 회전축
    LeverHub      레버 뿌리를 감추는 통 (고정). 원점 = 레버와 같은 회전축
    Lever         레버 팔 + 손잡이. 원점 = 허브 회전축
    RefundButton  환불 버튼 (조작대 왼쪽). 원점 = 버튼 중심 — 조준 · 누름 연출 앵커
    ReelGlass     릴 창 유리 (M_Glass)
    ReelBacklight 릴 뒤 발광 패널 (M_Emissive) — 켜고 끄는 것은 Unity 의 전원 상태다
    CoinSlot      동전 투입구 (고정). 원점 = 슬릿 입구 — 흡입 판정 앵커
    CoinTray      배출 트레이 (고정). 원점 = 안쪽 바닥 중앙 — 트레이 조명 · 부근 판정 앵커
    PayoutMouth   동전 배출구 (고정). 원점 = 개구 앞 — 배출 스폰 앵커

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

# 드럼을 반 칸(22.5도) 돌려 놓는다. 프리즘의 면 중심은 22.5도 + 45도·k 에 있는데
# SlotMachine.cs 는 45도 배수에서만 멈추므로, 그대로 두면 **창 정중앙에 늘 심볼
# 두 개가 반반 걸친다.** 반 칸 돌리면 면 중심이 45도 배수로 와 하나가 딱 놓인다.
# 회전 0 에서 페이라인에 오는 것은 면 5 (= 각도 270도, 전방 −Y) 다.
# 릴 각도와 심볼 인덱스의 대응은 Unity 쪽 회전 부호에 달렸다 —
# 당첨 판정을 붙일 때 화면으로 한 번 확인하고 정한다.
FACE_PHASE = math.pi / REEL_N

SYM_T = 0.012                                        # 심볼 판 두께 (반지름 방향)
SYM_R = APOTHEM + SYM_T / 2                          # 심볼 판이 놓이는 반지름

LEVER_X = W / 2 + 0.05
# 레버는 옆면 **앞쪽**에 단다. 깊이 한가운데(예전 −0.04)에 달면 정면에서 봤을 때
# 시선이 캐비닛 오른쪽 옆면을 뚫고 지나가 손잡이가 몸통에 가린다 — 플레이어가
# 옆으로 움직여야 레버가 보이게 된다. 앞으로 당길수록 정면에서 잘 보인다.
LEVER_HUB = (LEVER_X - 0.02, FRONT + 0.05, BODY_TOP + 0.05)   # 레버 회전축
LEVER_LEN = 0.30
LEVER_KNOB_AT = LEVER_LEN + 0.05     # 축에서 손잡이 중심까지
# 팔이 눕는 각도(도). 양수 = 플레이어 쪽(−Y). **뒤로 눕히지 않는다** —
# 축을 앞으로 옮겨도 팔이 뒤로 누우면 손잡이 끝이 다시 캐비닛 뒤로 들어가 가린다.
LEVER_TILT = 0.0

# --- 동전 치수 계약 ----------------------------------------------------------
# 투입 슬릿은 동전(generate_coin.py)이 세로로 서서 들어가는 구멍이다 —
# 폭은 동전 두께보다, 높이는 지름보다 여유 있게 커야 한다.
# ⚠️ 여기 값을 바꾸면 generate_coin.py 말미의 통과 검사 복사본도 같이 고친다.
COIN_R = 0.030   # generate_coin.py 의 R 복사본. 말미 검산에만 쓴다
COIN_T = 0.007   # generate_coin.py 의 T 복사본
SLIT_W = 0.012   # 투입 슬릿 폭 (X) — 동전 두께 통과
SLIT_H = 0.070   # 투입 슬릿 높이 (Z) — 동전 지름 통과

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


def rect(up, across, u=0.0, dr=0.0):
    """직사각형 판. up = 릴이 흐르는 방향(창에서 세로), across = 드럼 축 방향(가로).

    u 는 면 안에서의 세로 오프셋 — 막대를 위아래로 나눠 놓을 때 쓴다.
    """
    return ("rect", up, across, u, dr)


# 면 하나의 크기는 세로 0.099 (= 2·R·sin22.5°) · 가로 0.11 (= REEL_W) 이다.
# 여유를 두고 어느 쪽도 0.076 을 넘기지 않는다.
#
# **빈 면을 두지 않는다.** 당첨 판정이 심볼 인덱스로만 이뤄지는데 빈 면이 섞여 있으면
# 아무것도 안 보이는 자리 셋이 맞아 '큰 성공' 이 터진다 — 화면이 거짓말을 하게 된다.
SYMBOLS = [
    ("circle",   (poly(12, 0.035),)),
    ("square",   (rect(0.060, 0.060),)),
    ("triangle", (poly(3, 0.044),)),
    ("diamond",  (poly(4, 0.038),)),
    ("cross",    (rect(0.068, 0.022), rect(0.022, 0.068, dr=0.0006))),
    ("star",     (poly(3, 0.036), poly(3, 0.036, spin=180, dr=0.0006))),   # 세모 둘을 엇갈려 6각별
    ("bar",      (rect(0.024, 0.074),)),
    ("barbar",   (rect(0.020, 0.074, u=0.019), rect(0.020, 0.074, u=-0.019))),
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


def mark_center(theta, dr, u=0.0):
    """각도 theta 인 면 바깥 SYM_R+dr 지점에서, 면을 따라 세로로 u 만큼 옮긴 자리.

    면은 평평하므로 접선 방향으로 밀어도 면 위에 그대로 남는다.
    """
    r = SYM_R + dr
    return (r * math.cos(theta) - u * math.sin(theta),
            r * math.sin(theta) + u * math.cos(theta),
            0.0)


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
        _, up, across, u, dr = mark
        return builders.box(name, (SYM_T, up, across), loc=mark_center(theta, dr, u),
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
# 몸통 아래쪽은 배출구(PayoutMouth, z≈0.34)와 트레이가 차지하므로 패널을 위로 밀어 올린다.
cabinet.append(builders.box("BodyPanel", (0.46, 0.02, 0.40),
                            loc=(0, FRONT - 0.005, 0.62), color="charcoal"))

# 코인 트레이 · 투입구 · 배출구는 캐비닛과 합치지 않는다 — 원점이 곧 Unity 앵커라
# 별도 오브젝트로 뽑는다. 백라이트 다음의 「동전 부품」 절에서 만든다.

# --- 조작대 -----------------------------------------------------------------
# 깊이 0.14 — 버튼 셋이 늘어서던 때는 0.20 이었다. 남은 것은 환불 버튼 하나와
# 투입구뿐이라 돌출을 줄인다. 여기를 바꾸면 그 위 부품(환불 버튼 · CoinSlot)의
# y 도 같이 옮겨야 한다 — 셋 다 DECK_Y 기준으로 적는다.
DECK_D = 0.14
DECK_Y = FRONT - 0.04
cabinet.append(builders.box("ControlDeck", (W, DECK_D, 0.06),
                            loc=(0, DECK_Y, BODY_TOP - 0.01),
                            rot=(-14, 0, 0), color="ash"))
# 버튼은 환불 버튼 하나뿐이고 상호작용 대상이라 캐비닛과 합치지 않는다 —
# 레버 다음의 「환불 버튼」 절에서 별도 오브젝트로 만든다.

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

machine = builders.join_all(cabinet, "SlotMachine")

# --- 릴 3개 ------------------------------------------------------------------
# 드럼은 Z축 프리즘으로 만들어 심볼을 붙인 뒤 통째로 눕힌다. 처음부터 누워 있는
# 상태로 계산하면 각도·오프셋이 전부 뒤틀려 읽기 어려워진다.
reels = []
for ri, rx in enumerate(REEL_X):
    parts = [builders.prism(f"Drum_{ri}", REEL_R, REEL_W, n=REEL_N,
                            rot=(0, 0, math.degrees(FACE_PHASE)), color="concrete")]

    # 면마다 밝기를 번갈아 준다. 심볼이 없어도 회전이 눈에 보이게 하는 장치다.
    # prism 의 폴리곤 0..n-1 이 옆면이고 그 뒤가 바닥·천장 캡이다.
    for fi in range(REEL_N):
        palette.apply_color(parts[0], "bone" if fi % 2 else "concrete", faces=[fi])

    # **세 릴이 같은 배열을 쓴다.** 예전에는 릴마다 3칸씩 어긋나게 두었는데, 그러면
    # 세 릴이 같은 각도(= 같은 심볼 인덱스)에서 멈춰도 화면에는 서로 다른 무늬가
    # 보인다. SlotMachine.cs 는 인덱스로 당첨을 판정하므로 배열이 어긋나 있으면
    # 판정과 화면이 어긋난다. 돌 때 릴이 따로 노는 것은 속도·정지 시각이 만든다.
    for fi in range(REEL_N):
        sym, marks = SYMBOLS[fi]
        theta = 2 * math.pi * (fi + 0.5) / REEL_N + FACE_PHASE   # 면 중심 각도 (드럼과 같이 돌린다)
        for mi, mark in enumerate(marks):
            parts.append(build_mark(f"Sym_{ri}_{fi}_{sym}_{mi}", mark, theta))

    reel = builders.join_all(parts, f"Reel_{ri}")
    # 눕혀서 제자리로 보낸 뒤 다시 구워 회전을 정점에 흡수시킨다.
    reel.rotation_euler = (0, math.radians(90), 0)
    reel.location = (rx, REEL_Y, WIN_Z)
    reel = builders.join_all([reel], f"Reel_{ri}")
    set_pivot(reel, (rx, REEL_Y, WIN_Z))
    reels.append(reel)

# --- 레버 허브 (고정) --------------------------------------------------------
# 캐비닛과 **합치지 않는다.** 합쳐 두면 레버 위치를 다시 잡을 때 팔만 따라오고
# 뿌리를 감추는 이 통은 원래 자리에 남는다. 원점을 레버와 같은 회전축에 맞춰
# 뽑아, 둘을 같은 localPosition 으로 옮기면 조립체가 통째로 움직이게 한다.
hub = builders.join_all(
    [builders.prism("LeverHub", 0.045, 0.07, n=10, loc=LEVER_HUB, rot=(0, 90, 0), color="ash")],
    "LeverHub")
set_pivot(hub, LEVER_HUB)

# --- 레버 (팔 + 손잡이). 허브는 고정이고 이 둘만 돈다 ------------------------
# 팔·손잡이 자리는 축에서 계산한다. 좌표를 따로 적어 두면 축을 옮길 때 뿌리가
# 허브에서 떨어져 공중에 뜬다.
LEVER_DIR = Vector((0.0, -math.sin(math.radians(LEVER_TILT)), math.cos(math.radians(LEVER_TILT))))
lever_parts = [
    builders.prism("LeverArm", 0.016, LEVER_LEN, n=6,
                   loc=Vector(LEVER_HUB) + LEVER_DIR * (LEVER_LEN / 2),
                   rot=(LEVER_TILT, 0, 0), color="bone"),
    builders.icosphere("LeverKnob", 0.048, subdiv=1,
                       loc=Vector(LEVER_HUB) + LEVER_DIR * LEVER_KNOB_AT, color="chalk"),
]
lever = builders.join_all(lever_parts, "Lever")
set_pivot(lever, LEVER_HUB)

# --- 환불 버튼 (조작대 왼쪽, 별도 오브젝트) ----------------------------------
# 넣어 둔 크레딧을 전부 되뱉는 버튼. 조준 콜라이더와 누름 연출이 붙는 상호작용
# 대상이라 캐비닛과 합치지 않는다. 원점 = 버튼 중심 — Unity 가 로컬로 눌러 내린다.
# 자리는 조작대 중심선(DECK_Y). 기운 윗면에 밑동이 박혀 바닥 틈이 없다.
BUTTON_POS = (-0.15, DECK_Y, BODY_TOP + 0.03)
refund_button = builders.join_all(
    [builders.prism("RefundButton", 0.028, 0.028, n=8, loc=BUTTON_POS, color="concrete")],
    "RefundButton")
set_pivot(refund_button, BUTTON_POS)

# --- 유리 · 백라이트 ---------------------------------------------------------
# 백라이트는 릴 **뒤**에 선다. 기계가 처음부터 켜져 있다는 인상이 여기서 나온다 —
# 어두운 방에서 이 패널이 유일하게 스스로 빛나는 면이다.
glass = builders.box("ReelGlass", (0.42, 0.015, 0.30), loc=(0, FRONT + 0.02, WIN_Z))
palette.use_glass_material(glass)

backlight = builders.box("ReelBacklight", (0.44, 0.015, WIN_H - 0.02),
                         loc=(0, D / 2 - 0.07, WIN_Z))
palette.use_emissive_material(backlight)

# --- 동전 투입구 (고정, 별도 오브젝트) ---------------------------------------
# 조작대 오른쪽에 서는 세로 슬릿 블록. 원래는 앞면에 붙인 납작한 void 판이었는데,
# 동전을 실제로 넣는 대상이 되면서 '구멍' 으로 읽히는 형태가 필요해졌다.
# 앞면에는 자리가 없다 — 릴 창 베젤 하단(z 0.99~1.03)과 기운 조작대 윗면(~0.96)
# 사이가 3 cm 뿐이라 지름 6 cm 동전이 설 슬릿이 못 들어간다. 그래서 앤틱 기계처럼
# 조작대 위에 블록으로 세운다. 블록 아랫단은 기운 조작대에 박혀 바닥 틈이 없다.
#
# 불리언이 없으므로 기둥 2 + 캡 2 로 앞벽을 짜고 골 끝에 void 판을 세운다 —
# 밝은(bone) 에스커천 가운데 4 cm 깊이의 검은 세로 골이 구멍으로 읽힌다.
SLOT_X = 0.20                     # 환불 버튼(x=-0.15)과 반대쪽 끝. 겹칠 일이 없다
SLOT_Y = DECK_Y - 0.04            # 조작대 앞쪽 절반 위. 조작대가 얕아지면 같이 물러난다
SLOT_D = 0.05                     # 블록 깊이 (Y)
ESC_W, ESC_H = 0.06, 0.11         # 에스커천(블록) 폭 · 높이
SLOT_Z = 1.02                     # 블록 중심 높이
SLIT_Z = SLOT_Z + 0.005           # 슬릿 중심. 위 캡을 얇게 — 시선이 투입 동선 쪽으로 쏠린다
PILLAR_W = (ESC_W - SLIT_W) / 2
CAP_BOT_H = (SLIT_Z - SLIT_H / 2) - (SLOT_Z - ESC_H / 2)
CAP_TOP_H = (SLOT_Z + ESC_H / 2) - (SLIT_Z + SLIT_H / 2)
slot_parts = [
    builders.box("SlotPillar_L", (PILLAR_W, SLOT_D, ESC_H),
                 loc=(SLOT_X - (SLIT_W + PILLAR_W) / 2, SLOT_Y, SLOT_Z), color="bone"),
    builders.box("SlotPillar_R", (PILLAR_W, SLOT_D, ESC_H),
                 loc=(SLOT_X + (SLIT_W + PILLAR_W) / 2, SLOT_Y, SLOT_Z), color="bone"),
    builders.box("SlotCap_Bottom", (SLIT_W, SLOT_D, CAP_BOT_H),
                 loc=(SLOT_X, SLOT_Y, SLOT_Z - (ESC_H - CAP_BOT_H) / 2), color="bone"),
    builders.box("SlotCap_Top", (SLIT_W, SLOT_D, CAP_TOP_H),
                 loc=(SLOT_X, SLOT_Y, SLOT_Z + (ESC_H - CAP_TOP_H) / 2), color="bone"),
    # 골의 끝. 슬릿으로 들여다보면 이 어두운 면이 깊이를 만든다.
    builders.box("SlotThroat", (SLIT_W, 0.006, SLIT_H),
                 loc=(SLOT_X, SLOT_Y + SLOT_D / 2 - 0.003, SLIT_Z), color="void"),
]
coin_slot = builders.join_all(slot_parts, "CoinSlot")
set_pivot(coin_slot, (SLOT_X, SLOT_Y - SLOT_D / 2, SLIT_Z))   # 원점 = 슬릿 입구

# --- 배출 트레이 (고정, 별도 오브젝트) ---------------------------------------
# 속이 찬 선반이었다. 동전이 실제로 떨어져 쌓이는 그릇이 되면서 바닥 + 벽 3면의
# 오목한 형태로 다시 짠다 — 뒷벽은 캐비닛 앞면이 겸한다. 안쪽 바닥은 어두운
# charcoal 이라 밝은 동전(paper·chalk)이 그릇 안에서 도드라진다.
# 벽 높이는 동전 지름보다 높아 낙하 동전이 웬만해선 밖으로 안 튄다 —
# 대박 100개가 넘쳐 흐르는 것은 의도된 연출이다.
TRAY_W, TRAY_D = 0.36, 0.16       # 바깥 폭 · 깊이
TRAY_T = 0.015                    # 바닥 · 벽 두께
TRAY_WALL = 0.075                 # 안쪽 바닥 기준 벽 높이
TRAY_FLOOR_TOP = 0.20             # 안쪽 바닥 z
TRAY_Y = FRONT - TRAY_D / 2       # 트레이 중심 y. 뒤 가장자리가 캐비닛 앞면에 붙는다
TRAY_WALL_H = TRAY_T + TRAY_WALL  # 벽 박스는 바닥 옆면까지 감싼다
tray_parts = [
    builders.box("TrayFloor", (TRAY_W, TRAY_D, TRAY_T),
                 loc=(0, TRAY_Y, TRAY_FLOOR_TOP - TRAY_T / 2), color="charcoal"),
    builders.box("TrayWall_Front", (TRAY_W, TRAY_T, TRAY_WALL_H),
                 loc=(0, FRONT - TRAY_D - TRAY_T / 2,
                      TRAY_FLOOR_TOP - TRAY_T + TRAY_WALL_H / 2), color="ash"),
    builders.box("TrayWall_L", (TRAY_T, TRAY_D, TRAY_WALL_H),
                 loc=(-(TRAY_W - TRAY_T) / 2, TRAY_Y,
                      TRAY_FLOOR_TOP - TRAY_T + TRAY_WALL_H / 2), color="ash"),
    builders.box("TrayWall_R", (TRAY_T, TRAY_D, TRAY_WALL_H),
                 loc=((TRAY_W - TRAY_T) / 2, TRAY_Y,
                      TRAY_FLOOR_TOP - TRAY_T + TRAY_WALL_H / 2), color="ash"),
    # 받침 — 트레이가 몸체에서 그냥 튀어나오면 공중에 뜬 것처럼 보인다.
    builders.box("TrayBracket", (0.32, 0.12, 0.025),
                 loc=(0, FRONT - 0.06, TRAY_FLOOR_TOP - TRAY_T - 0.0125), color="charcoal"),
]
tray = builders.join_all(tray_parts, "CoinTray")
set_pivot(tray, (0, TRAY_Y, TRAY_FLOOR_TOP))   # 원점 = 안쪽 바닥 중앙

# --- 배출구 (고정, 별도 오브젝트) --------------------------------------------
# 트레이 위 몸체 앞면의 가로 슬릿. 당첨 동전이 여기서 나와 트레이로 떨어진다.
# 투입구와 같은 수법(기둥 + 캡 + void 골)이되, 동전이 눕다시피 미끄러져 나오는
# 가로 구멍이다. 원점 = 개구 앞 — Unity 의 배출 스폰 앵커.
MOUTH_Z = 0.335                   # 트레이 앞벽 상단(0.275)보다 위 — 낙하 궤적 확보
MOUTH_W, MOUTH_H = 0.10, 0.024    # 개구 폭 · 높이 (동전 지름 0.060 · 두께 0.007)
MOUTH_FRAME = 0.015               # 좌우 기둥 폭
MOUTH_CAP = 0.0155                # 상하 캡 높이
MOUTH_D = 0.012                   # 프레임의 앞면 돌출 깊이
MOUTH_OUTER_H = MOUTH_H + MOUTH_CAP * 2
mouth_parts = [
    builders.box("MouthPillar_L", (MOUTH_FRAME, MOUTH_D, MOUTH_OUTER_H),
                 loc=(-(MOUTH_W + MOUTH_FRAME) / 2, FRONT - MOUTH_D / 2, MOUTH_Z),
                 color="bone"),
    builders.box("MouthPillar_R", (MOUTH_FRAME, MOUTH_D, MOUTH_OUTER_H),
                 loc=((MOUTH_W + MOUTH_FRAME) / 2, FRONT - MOUTH_D / 2, MOUTH_Z),
                 color="bone"),
    builders.box("MouthCap_Top", (MOUTH_W, MOUTH_D, MOUTH_CAP),
                 loc=(0, FRONT - MOUTH_D / 2, MOUTH_Z + (MOUTH_H + MOUTH_CAP) / 2),
                 color="bone"),
    builders.box("MouthCap_Bottom", (MOUTH_W, MOUTH_D, MOUTH_CAP),
                 loc=(0, FRONT - MOUTH_D / 2, MOUTH_Z - (MOUTH_H + MOUTH_CAP) / 2),
                 color="bone"),
    builders.box("MouthThroat", (MOUTH_W, 0.004, MOUTH_H),
                 loc=(0, FRONT - 0.003, MOUTH_Z), color="void"),
]
mouth = builders.join_all(mouth_parts, "PayoutMouth")
set_pivot(mouth, (0, FRONT - 0.02, MOUTH_Z))   # 원점 = 개구 앞

# --- 프리뷰 · 익스포트 -------------------------------------------------------
# 유리를 숨긴다. Workbench 는 알파를 반영하지 않아 두면 불투명 판이 릴을 덮는다.
glass.hide_render = True
preview.render_turnaround("slot_machine",
                          [machine] + reels + [hub, lever, refund_button, coin_slot, tray, mouth])
# 심볼 클로즈업. 전체 렌더에서는 릴 창이 손톱만 해 형태가 구분되는지 알 수 없다.
# 릴만 넘기면 프레이밍이 릴 크기로 좁혀진다 — 카메라 설정은 건드릴 게 없다.
preview.render_turnaround("slot_machine_reels", reels,
                          out_dir=os.path.join(paths.PREVIEWS, "slot_machine"),
                          views={"front": Vector((0.0, -1.0, 0.0))})
# 동전 부품 클로즈업. 전체 렌더에서는 슬릿과 트레이 오목함이 픽셀 몇 개라 검수가 안 된다.
preview.render_turnaround("slot_machine_coinparts", [coin_slot, tray, mouth],
                          out_dir=os.path.join(paths.PREVIEWS, "slot_machine"),
                          views={"front": Vector((0.0, -1.0, 0.0)),
                                 "persp34": Vector((-0.8, -1.0, 0.55))})
glass.hide_render = False

objs = [machine] + reels + [hub, lever, refund_button, glass, backlight, coin_slot, tray, mouth]
export.export_static(objs, paths.art("Environment", "SlotMachine.fbx"))
print("EXPORTED SlotMachine")

print("--- 오브젝트별 원점 (회전축) ---")
for o in objs:
    print(f"  {o.name:<14} origin = ({o.location.x:.3f}, {o.location.y:.3f}, {o.location.z:.3f})")
print(f"  릴 심볼 수 = {REEL_N} (SlotMachine.cs 의 SymbolCount 와 같아야 한다)")
print(f"  무늬 배열 = {', '.join(name for name, _ in SYMBOLS)}  ← 세 릴 공통")

print("--- 동전 앵커 (Unity 로컬 = 블렌더 (x, z, -y)) ---")
for o in (coin_slot, tray, mouth):
    print(f"  {o.name:<12} unity = ({o.location.x:.3f}, {o.location.z:.3f}, {-o.location.y:.3f})")

# 투입 슬릿 통과 검산 — generate_coin.py 말미의 검사와 같은 식이어야 한다.
ok_slit = COIN_T < SLIT_W and 2 * COIN_R < SLIT_H
print(f"  투입 슬릿 {SLIT_W}x{SLIT_H}: 동전(지름 {2 * COIN_R}, 두께 {COIN_T}) "
      f"통과 {'OK' if ok_slit else 'FAIL'}")

# 배출 궤적 검산 — 개구 앞 5 cm 에서 전방 0.3 m/s 로 나온 동전이 트레이 안에 떨어지는가.
# 0.3 은 Unity 쪽 CoinDispenser.ejectSpeed 와 같은 값이다.
EJECT_SPEED = 0.30
drop = MOUTH_Z - TRAY_FLOOR_TOP
t_fall = math.sqrt(2 * drop / 9.81)
land_y = mouth.location.y - 0.05 - EJECT_SPEED * t_fall
ok_land = (FRONT - TRAY_D + COIN_R) < land_y < (FRONT - COIN_R)
print(f"  배출 낙하: 개구 z={MOUTH_Z} → 트레이 바닥 z={TRAY_FLOOR_TOP}, "
      f"착지 y={land_y:.3f} (트레이 안쪽 {FRONT - TRAY_D:.3f}~{FRONT:.2f}) "
      f"{'OK' if ok_land else 'FAIL'}")

if not (ok_slit and ok_land):
    raise SystemExit("동전 기하 검산 실패 — 위 FAIL 항목의 치수를 맞추고 다시 돌린다")
