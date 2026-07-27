"""선언적 포즈 데이터 → 액션 생성 + NLA 배치.

키 데이터 형식:
    keys = {
        frame(int): {
            bone_name: (rx, ry, rz)                              # 회전(도)만
            bone_name: {"rot": (rx,ry,rz), "loc": (x,y,z)}       # 회전+본 로컬 이동
        },
        ...
    }

규칙:
- 경계 프레임(최소/최대)에서는 모든 본에 자동으로 키가 박힘 (미지정 = identity)
  → 액션 간 포즈 오염 방지, 루프 클립의 시작/끝 일치 보장
- loc은 본 로컬 축 기준 (본 local Y = 본 진행 방향; Hips는 위쪽이 Y)
"""
import math

import bpy

FPS = 30


def _parse(spec):
    if spec is None:
        return (0.0, 0.0, 0.0), None
    if isinstance(spec, dict):
        return spec.get("rot", (0.0, 0.0, 0.0)), spec.get("loc")
    return spec, None


def make_action(rig, name, keys):
    from . import rigging

    rigging.clear_pose(rig)
    action = bpy.data.actions.new(name)
    rig.animation_data_create()
    rig.animation_data.action = action

    frames = sorted(keys)
    f_first, f_last = frames[0], frames[-1]
    loc_animated = {b for f in frames for b, s in keys[f].items() if isinstance(s, dict) and "loc" in s}

    for frame in frames:
        pose = keys[frame]
        if frame in (f_first, f_last):
            targets = [pb.name for pb in rig.pose.bones]
        else:
            targets = list(pose)
        for bone_name in targets:
            pb = rig.pose.bones[bone_name]
            rot, loc = _parse(pose.get(bone_name))
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = [math.radians(a) for a in rot]
            pb.keyframe_insert("rotation_euler", frame=frame)
            if loc is not None or (bone_name in loc_animated and frame in (f_first, f_last)):
                pb.location = loc or (0.0, 0.0, 0.0)
                pb.keyframe_insert("location", frame=frame)

    rigging.clear_pose(rig)
    return action


def stash_all(rig, actions, gap=10):
    """액션들을 각자의 NLA 트랙 스트립으로 배치 (시간축에서 서로 겹치지 않게).

    export.export_skinned의 bake_anim_use_nla_strips=True가 스트립별로
    FBX 테이크를 만든다 → Unity가 클립으로 자동 분할.
    """
    rig.animation_data_create()
    ad = rig.animation_data
    ad.action = None
    start = 1
    for action in actions:
        track = ad.nla_tracks.new()
        track.name = action.name
        strip = track.strips.new(action.name, start, action)
        strip.name = action.name
        strip.mute = False
        start = int(strip.frame_end) + gap
