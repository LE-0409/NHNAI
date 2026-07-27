"""천장 전등 + 빛 원뿔 — 방 안의 유일한 광원.

FBX 2개를 뽑는다.
  CeilingLamp.fbx  갓 · 전구 · 전선 · 천장 마운트
  LightCone.fbx    전등 아래로 퍼지는 빛 기둥 (메시)

**왜 빛을 메시로 만드나**: URP 에는 볼류메트릭 라이팅이 없다(HDRP 전용). 전등 하나가
원뿔로 빛을 뿌리는 게 이 게임의 핵심 연출이라 직접 만들어야 한다. 레이마칭 대신
반투명 원뿔 메시를 쓰는 이유는 로우폴리 · 흑백 스타일에서는 '의도적으로 가짜'인 쪽이
오히려 룩에 맞고, 모바일에서도 그대로 돌기 때문이다.

**좌표계**: `generate_cell_room.py` 와 같은 방 좌표를 쓴다 — 원점은 방 바닥 중앙,
Z 위쪽. 그래서 Unity 에서 방과 전등을 둘 다 Transform 기본값으로 두면 바로 맞물린다.
방 높이(H)를 바꾸면 여기 H 도 같이 바꿔야 한다.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

from lowpoly_lib import builders, export, palette, paths, preview

# --- 치수 --------------------------------------------------------------------
H = 3.0            # 방 내부 높이. cell_room 과 맞춰야 한다

CORD_LEN = 0.46    # 천장에서 갓까지 전선 길이
SHADE_H = 0.24     # 갓 높이
SHADE_R_BOT = 0.30
SHADE_R_TOP = 0.09
BULB_R = 0.055

SHADE_TOP = H - CORD_LEN               # 2.54
SHADE_BOT = SHADE_TOP - SHADE_H        # 2.30
BULB_Z = SHADE_BOT + BULB_R * 0.4      # 갓 입구에 살짝 걸치게

CONE_TOP = SHADE_BOT - 0.02
CONE_BOT = 0.02                        # 바닥과 Z-파이팅 나지 않게 살짝 띄운다
CONE_R_TOP = 0.10
CONE_R_BOT = 1.55

builders.reset_scene()
palette.write_palette_png()

# --- 전등 --------------------------------------------------------------------
mount = builders.box("LampMount", (0.14, 0.14, 0.04),
                     loc=(0, 0, H - 0.02), color="charcoal")
cord = builders.prism("LampCord", 0.012, CORD_LEN, n=6,
                      loc=(0, 0, H - CORD_LEN / 2), color="void")
shade = builders.prism("LampShade", SHADE_R_BOT, SHADE_H, n=12,
                       radius_top=SHADE_R_TOP,
                       loc=(0, 0, (SHADE_TOP + SHADE_BOT) / 2), color="iron")

lamp = builders.join_all([shade, cord, mount], "CeilingLamp")

# 전구는 별도 머티리얼 슬롯이라 join 하지 않는다. join_all 은 활성 오브젝트의
# 슬롯0 을 기준으로 합치므로, 여기서 합치면 발광 슬롯이 팔레트 슬롯에 먹힌다.
bulb = builders.icosphere("LampBulb", BULB_R, subdiv=1, loc=(0, 0, BULB_Z))
palette.use_emissive_material(bulb)

# 갓이 전구를 가리므로 기본 4방향으로는 전구가 한 번도 안 보인다.
# 아래에서 올려다보는 뷰를 더해 전구와 갓 안쪽을 확인한다.
from mathutils import Vector  # noqa: E402  (프리뷰 뷰 정의에만 쓴다)

LAMP_VIEWS = dict(preview.VIEWS, below=Vector((0.0, -0.35, -1.0)))
preview.render_turnaround("ceiling_lamp", [lamp, bulb], views=LAMP_VIEWS)
export.export_static([lamp, bulb], paths.art("Environment", "CeilingLamp.fbx"))
print("EXPORTED CeilingLamp")

# --- 빛 원뿔 -----------------------------------------------------------------
# 별도 FBX. Unity 에서 가산 블렌딩 · 깊이 기록 없음으로 리맵하고, 세기 조절은
# 머티리얼 쪽에서 한다. 메시를 다시 뽑지 않고 룩을 조정할 수 있어야 한다.
cone = builders.prism("LightCone", CONE_R_BOT, CONE_TOP - CONE_BOT, n=16,
                      radius_top=CONE_R_TOP,
                      loc=(0, 0, (CONE_TOP + CONE_BOT) / 2))
palette.use_lightcone_material(cone)

preview.render_turnaround("light_cone", [cone])
export.export_static([cone], paths.art("Environment", "LightCone.fbx"))
print("EXPORTED LightCone")
