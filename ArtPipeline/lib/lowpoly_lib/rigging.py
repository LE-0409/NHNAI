"""아마추어 빌더 + rigid 스키닝.

스키닝 전제: 메시의 버텍스 그룹이 본 이름과 동일하게 이미 만들어져 있음
(builders.box(bone=...) 태깅 → join_all이 이름 기준 병합).
"""
import bpy


def build_armature(name, bone_defs):
    """bone_defs: [(bone_name, head_xyz, tail_xyz, parent_name|None[, roll_deg]), ...] 순서는 부모 먼저."""
    import math

    arm_data = bpy.data.armatures.new(name)
    arm_obj = bpy.data.objects.new(name, arm_data)
    bpy.context.scene.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")
    ebs = arm_data.edit_bones
    for bdef in bone_defs:
        bone_name, head, tail, parent = bdef[:4]
        eb = ebs.new(bone_name)
        eb.head = head
        eb.tail = tail
        eb.roll = math.radians(bdef[4]) if len(bdef) > 4 else 0.0
        if parent:
            eb.parent = ebs[parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_obj


def bind_rigid(mesh_obj, arm_obj):
    """Armature 모디파이어 + 부모 설정. 웨이트는 기존 버텍스 그룹(1.0 rigid) 그대로."""
    mesh_obj.parent = arm_obj
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_obj
    return mesh_obj


def set_pose(arm_obj, pose_map):
    """pose_map: {bone_name: (rx_deg, ry_deg, rz_deg)} — 포즈 본 회전 설정 (도)."""
    import math

    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="POSE")
    for name, rot in pose_map.items():
        pb = arm_obj.pose.bones[name]
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = [math.radians(a) for a in rot]
    bpy.ops.object.mode_set(mode="OBJECT")


def clear_pose(arm_obj):
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm_obj.pose.bones:
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    bpy.ops.object.mode_set(mode="OBJECT")
