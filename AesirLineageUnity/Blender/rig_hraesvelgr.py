"""Rigs and animates Hraesvelgr's AssetForge model (a static prop, since AssetForge rigs no fliers) and exports it
for AesirLineageUnity.

    blender -b <AssetForge work>/hraesvelgr/04_paint/v001/painted.blend --python rig_hraesvelgr.py -- <out.blend> <out.fbx> [bones.json]

The model faces -Y with Z up, its feet at Z 0: a gargoyle-like bat hanging upright with its wings raised in a V, a
pair of clawed arms in front, legs with backward knees and a spiked tail. Bones were placed from plots of the mesh.
Clips are in place (the root never travels), at 30 fps; each attack's strike frame is listed in CLIPS for the Unity
side to put the game's OnAttackTrigger event on.
"""

import json
import math
import sys

import bpy
from mathutils import Quaternion, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT_BLEND, OUT_FBX = argv[0], argv[1]
BONES_JSON = argv[2] if len(argv) > 2 else None
NAME = "Hraesvelgr"
FPS = 30

# name: (head, tail, parent). L is the creature's left, +X.
CENTRE = {
    "Root": ((0, 0, 0), (0, 0, 0.1), None),
    "Hips": ((0, 0.01, 0.29), (0, 0.0, 0.40), "Root"),
    "Spine": ((0, 0.0, 0.40), (0, -0.03, 0.50), "Hips"),
    "Chest": ((0, -0.03, 0.50), (0, -0.07, 0.58), "Spine"),
    "Neck": ((0, -0.07, 0.58), (0, -0.12, 0.61), "Chest"),
    "Head": ((0, -0.12, 0.61), (0, -0.27, 0.55), "Neck"),
    "Jaw": ((0, -0.12, 0.55), (0, -0.24, 0.48), "Head"),
    "Tail1": ((0, 0.04, 0.36), (0, 0.15, 0.30), "Hips"),
    "Tail2": ((0, 0.15, 0.30), (0, 0.27, 0.20), "Tail1"),
}
SIDE = {
    "UpperArm": ((0.12, -0.04, 0.47), (0.16, -0.06, 0.36), "Chest"),
    "Forearm": ((0.16, -0.06, 0.36), (0.17, -0.07, 0.26), "UpperArm"),
    "Hand": ((0.17, -0.07, 0.26), (0.16, -0.08, 0.19), "Forearm"),
    "Thigh": ((0.07, -0.02, 0.28), (0.09, 0.07, 0.18), "Hips"),
    "Shin": ((0.09, 0.07, 0.18), (0.10, 0.0, 0.05), "Thigh"),
    "Foot": ((0.10, 0.0, 0.05), (0.10, 0.07, 0.01), "Shin"),
    "Wing1": ((0.10, 0.03, 0.62), (0.20, -0.08, 0.76), "Chest"),
    "Wing2": ((0.20, -0.08, 0.76), (0.32, -0.15, 0.87), "Wing1"),
    "Finger1": ((0.32, -0.15, 0.87), (0.52, -0.11, 0.89), "Wing2"),
    "Finger1b": ((0.52, -0.11, 0.89), (0.70, -0.08, 0.86), "Finger1"),
    "Finger2": ((0.32, -0.15, 0.87), (0.46, -0.05, 0.80), "Wing2"),
    "Finger2b": ((0.46, -0.05, 0.80), (0.58, 0.01, 0.71), "Finger2"),
    "Finger3": ((0.32, -0.15, 0.87), (0.37, -0.01, 0.74), "Wing2"),
    "Finger3b": ((0.37, -0.01, 0.74), (0.41, 0.03, 0.60), "Finger3"),
}
WING = {"Wing1", "Wing2", "Finger1", "Finger1b", "Finger2", "Finger2b", "Finger3", "Finger3b"}


def all_bones():
    bones = dict(CENTRE)
    for side, sx in (("L", 1), ("R", -1)):
        for n, (h, t, p) in SIDE.items():
            parent = p if p in CENTRE else f"{side}.{p}"
            bones[f"{side}.{n}"] = ((h[0] * sx, h[1], h[2]), (t[0] * sx, t[1], t[2]), parent)
    return bones


def build_armature():
    data = bpy.data.armatures.new(NAME)
    arm = bpy.data.objects.new(NAME, data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = data.edit_bones
    bones = all_bones()
    for name, (h, t, _) in bones.items():
        b = eb.new(name)
        b.head, b.tail = Vector(h), Vector(t)
        b.align_roll(Vector((0, 0, 1)) if abs((b.tail - b.head).normalized().z) < 0.9 else Vector((0, -1, 0)))
    for name, (_, _, p) in bones.items():
        if p:
            eb[name].parent = eb[p]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-12)))
    return (a + ab * t - p).length


def skin(mesh, arm):
    """Bone heat for the body, then distance weights over the wing bones for the wing membranes, whose bones lie
    in the membrane's own plane, where bone heat finds them poorly."""
    for o in bpy.context.scene.objects:
        o.select_set(False)
    mesh.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    # Bone heat fails on small meshes; solve at about twice the size.
    for o in (mesh, arm):
        o.scale = (2, 2, 2)
    bpy.context.view_layer.update()
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    for o in (mesh, arm):
        o.scale = (1, 1, 1)
    # Parenting stored the doubled armature's inverse; at scale 1 that would halve the mesh.
    mesh.matrix_parent_inverse.identity()
    bpy.context.view_layer.update()
    groups = {g.name: g for g in mesh.vertex_groups}
    for b in arm.data.bones:
        if b.name not in groups:
            groups[b.name] = mesh.vertex_groups.new(name=b.name)
    segs = {b.name: (b.head_local, b.tail_local) for b in arm.data.bones}
    wing_bones = {s: [f"{s}.{n}" for n in WING] + ["Chest"] for s in ("L", "R")}
    for v in mesh.data.vertices:
        p = v.co
        wing = abs(p.x) > 0.21 and p.z > 0.56
        weighted = any(g.weight > 0 for g in v.groups)
        if wing or not weighted:
            names = wing_bones["L" if p.x > 0 else "R"] if wing else list(segs)
            ds = sorted((seg_dist(p, *segs[n]), n) for n in names)[:3]
            ws = [(n, 1.0 / (d + 0.01) ** 3) for d, n in ds]
            total = sum(w for _, w in ws)
            for g in list(v.groups):
                mesh.vertex_groups[g.group].remove([v.index])
            for n, w in ws:
                groups[n].add([v.index], w / total, "REPLACE")
    limit_and_normalise(mesh)


def limit_and_normalise(obj, limit=4):
    names = {g.index: g.name for g in obj.vertex_groups}
    for v in obj.data.vertices:
        ws = sorted(((g.group, g.weight) for g in v.groups if g.weight > 0), key=lambda t: -t[1])
        keep, drop = ws[:limit], ws[limit:]
        total = sum(w for _, w in keep) or 1.0
        for gi, _ in drop:
            obj.vertex_groups[names[gi]].remove([v.index])
        for gi, w in keep:
            obj.vertex_groups[names[gi]].add([v.index], w / total, "REPLACE")


def transfer_weights(src, dst, arm):
    """A lower LOD takes the weights of the nearest surface of LOD0, so every LOD bends alike."""
    for g in src.vertex_groups:
        dst.vertex_groups.new(name=g.name)
    m = dst.modifiers.new("weights", "DATA_TRANSFER")
    m.object = src
    m.use_vert_data = True
    m.data_types_verts = {"VGROUP_WEIGHTS"}
    m.vert_mapping = "POLYINTERP_NEAREST"
    m.layers_vgroup_select_src = "ALL"
    m.layers_vgroup_select_dst = "NAME"
    bpy.context.view_layer.objects.active = dst
    bpy.ops.object.modifier_apply(modifier=m.name)
    limit_and_normalise(dst)
    dst.parent = arm
    mod = dst.modifiers.new("Armature", "ARMATURE")
    mod.object = arm


# ---------------------------------------------------------------------------------------------------------------
# Clips: keys of poses at normalised times, smoothstepped between. A pose turns bones about the armature's axes
# (as their parents carry them) and may offset and turn the root. L turns are mirrored onto R.
# ---------------------------------------------------------------------------------------------------------------

AXES = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}


def turn(steps):
    q = Quaternion()
    for axis, deg in steps:
        q = Quaternion(AXES[axis], math.radians(deg)) @ q
    return q


def mirror(steps):
    return [(a, d if a == "X" else -d) for a, d in steps]


def expand(pose):
    """`W.Wing1` style keys mean both sides: L as given, R mirrored."""
    out = {}
    for k, v in pose.items():
        if k.startswith("W."):
            out["L." + k[2:]] = v
            out["R." + k[2:]] = mirror(v)
        else:
            out[k] = v
    return out


def smooth(t):
    t = min(max(t, 0.0), 1.0)
    return t * t * (3 - 2 * t)


def wings(up):
    """Wing beat position: 0 is the rest V, 1 fully down, negative higher still."""
    return {
        "W.Wing1": [("Y", 70 * up)],
        "W.Wing2": [("Y", 28 * up)],
        "W.Finger1": [("Y", 8 * up)],
        "W.Finger2": [("Y", 12 * up)],
        "W.Finger3": [("Y", 14 * up)],
        "W.Finger1b": [("Y", 6 * up)],
        "W.Finger2b": [("Y", 8 * up)],
        "W.Finger3b": [("Y", 10 * up)],
    }


def merge(*poses):
    out = {}
    for p in poses:
        for k, v in p.items():
            out[k] = out.get(k, []) + list(v) if not k.startswith("_") else v
    return out


HOVER_UP = merge(wings(-0.15), {"Tail1": [("X", 8)], "W.Thigh": [("X", -8)], "_offset": (0, 0, -0.01)})
HOVER_DOWN = merge(wings(0.95), {"Chest": [("X", 6)], "Tail1": [("X", -8)], "W.Thigh": [("X", 6)], "_offset": (0, 0, 0.03)})
FLY_BASE = {"_turn": [("X", 55)], "_offset": (0, 0.12, 0.28), "Neck": [("X", -25)], "Head": [("X", -20)],
            "W.Thigh": [("X", -45)], "W.Shin": [("X", 20)], "Tail1": [("X", -20)], "W.UpperArm": [("X", -30)]}
NEUTRAL = wings(0.3)

CLIPS = {
    # name: (frames, loop, strike time (normalised) or None, keys)
    "Hover": (30, True, None, [(0.0, HOVER_UP), (0.42, HOVER_DOWN), (1.0, HOVER_UP)]),
    "Fly": (22, True, None, [(0.0, merge(FLY_BASE, wings(-0.25))), (0.45, merge(FLY_BASE, wings(1.1), {"Chest": [("X", 6)]})),
                             (1.0, merge(FLY_BASE, wings(-0.25)))]),
    "Bite": (27, False, 0.5, [
        (0.0, NEUTRAL),
        (0.35, merge(wings(-0.3), {"Neck": [("X", -15)], "Head": [("X", -25)], "Jaw": [("X", 38)], "Chest": [("X", -12)],
                                   "W.UpperArm": [("X", -50)], "_offset": (0, 0.05, 0.02)})),
        (0.5, merge(wings(0.6), {"Neck": [("X", 28)], "Head": [("X", 30)], "Jaw": [("X", 2)], "Chest": [("X", 22)],
                                 "W.UpperArm": [("X", -70)], "W.Forearm": [("X", -30)], "_offset": (0, -0.09, -0.02)})),
        (0.7, merge(wings(0.4), {"Neck": [("X", 15)], "Head": [("X", 12)], "Chest": [("X", 10)], "_offset": (0, -0.04, 0)})),
        (1.0, NEUTRAL)]),
    "Gale": (42, False, 0.55, [
        (0.0, NEUTRAL),
        (0.4, merge(wings(-0.45), {"W.Wing1": [("Z", 28)], "Chest": [("X", -16)], "Head": [("X", -12)], "Tail1": [("X", 15)],
                                   "_offset": (0, 0.04, 0.04)})),
        (0.55, merge(wings(1.25), {"W.Wing1": [("Z", -38)], "W.Wing2": [("Z", -12)], "Chest": [("X", 22)], "Head": [("X", 10)],
                                   "Tail1": [("X", -12)], "_offset": (0, -0.03, -0.03)})),
        (0.75, merge(wings(1.0), {"W.Wing1": [("Z", -25)], "Chest": [("X", 15)]})),
        (1.0, NEUTRAL)]),
    "Call": (48, False, 0.45, [
        (0.0, NEUTRAL),
        (0.3, merge(wings(0.05), {"W.Wing1": [("Z", -8)], "Neck": [("X", -28)], "Head": [("X", -35)], "Jaw": [("X", 48)],
                                  "Chest": [("X", -16)], "W.UpperArm": [("X", -60), ("Y", -25)], "W.Forearm": [("X", -25)]})),
        (0.45, merge(wings(0.12), {"W.Wing1": [("Z", -10)], "Neck": [("X", -32)], "Head": [("X", -38)], "Jaw": [("X", 52)],
                                   "Chest": [("X", -18)], "W.UpperArm": [("X", -65), ("Y", -30)], "W.Forearm": [("X", -25)]})),
        (0.6, merge(wings(0.0), {"W.Wing1": [("Z", -6)], "Neck": [("X", -28)], "Head": [("X", -34)], "Jaw": [("X", 48)],
                                 "Chest": [("X", -15)], "W.UpperArm": [("X", -60), ("Y", -25)], "W.Forearm": [("X", -25)]})),
        (0.8, merge(wings(0.1), {"W.Wing1": [("Z", -10)], "Neck": [("X", -30)], "Head": [("X", -36)], "Jaw": [("X", 50)],
                                 "Chest": [("X", -17)], "W.UpperArm": [("X", -62), ("Y", -28)], "W.Forearm": [("X", -25)]})),
        (1.0, NEUTRAL)]),
    "Stagger": (24, False, None, [
        (0.0, NEUTRAL),
        (0.25, merge(wings(0.9), {"W.Wing2": [("Y", 25), ("Z", 15)], "Chest": [("X", -24)], "Head": [("X", -22)],
                                  "Jaw": [("X", 25)], "_offset": (0, 0.08, -0.03)})),
        (1.0, NEUTRAL)]),
    "Death": (45, False, None, [
        (0.0, NEUTRAL),
        (0.2, merge(wings(-0.2), {"Chest": [("X", -22)], "Head": [("X", -30)], "Jaw": [("X", 40)], "_offset": (0, 0.05, 0.02)})),
        (1.0, merge(wings(1.0), {"W.Wing1": [("Z", 40)], "W.Wing2": [("Y", 70)], "W.Finger1": [("Y", 35)], "W.Finger2": [("Y", 40)],
                                 "W.Finger3": [("Y", 45)], "W.Thigh": [("X", 45)], "W.Shin": [("X", -40)], "Head": [("X", 25)],
                                 "Jaw": [("X", 20)], "Tail1": [("X", 25)], "_turn": [("X", 75)], "_offset": (0, -0.05, 0.12)}))]),
}


def pose_at(keys, t):
    for (t0, p0), (t1, p1) in zip(keys, keys[1:]):
        if t <= t1:
            return expand(p0), expand(p1), smooth((t - t0) / max(t1 - t0, 1e-9))
    return expand(keys[-1][1]), expand(keys[-1][1]), 0.0


def bake(arm, name, frames, keys):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = action
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for f in range(frames + 1):
        p0, p1, s = pose_at(keys, f / frames)
        for pb in arm.pose.bones:
            q0, q1 = turn(p0.get(pb.name, [])), turn(p1.get(pb.name, []))
            if pb.name == "Root":
                q0, q1 = turn(p0.get("_turn", [])), turn(p1.get("_turn", []))
            q = q0.slerp(q1, s)
            rest = pb.bone.matrix_local.to_quaternion()
            pb.rotation_quaternion = rest.inverted() @ q @ rest
            if pb.name == "Root":
                o0, o1 = Vector(p0.get("_offset", (0, 0, 0))), Vector(p1.get("_offset", (0, 0, 0)))
                pb.location = pb.bone.matrix_local.to_3x3().inverted() @ o0.lerp(o1, s)
            else:
                pb.location = (0, 0, 0)
            pb.keyframe_insert("rotation_quaternion", frame=f)
            pb.keyframe_insert("location", frame=f)
    action.use_frame_range = True
    action.frame_start, action.frame_end = 0, frames
    arm.animation_data.action = None
    return action


def main():
    scene = bpy.context.scene
    scene.render.fps = FPS
    lods = sorted([o for o in scene.objects if o.type == "MESH" and "_LOD" in o.name], key=lambda o: o.name)
    for o in list(scene.objects):
        if o not in lods:
            bpy.data.objects.remove(o, do_unlink=True)
    for i, o in enumerate(lods):
        o.name = f"{NAME}_LOD{i}"
    arm = build_armature()
    if BONES_JSON:
        json.dump({n: [list(h), list(t)] for n, (h, t, _) in all_bones().items()}, open(BONES_JSON, "w"))
    skin(lods[0], arm)
    for o in lods[1:]:
        transfer_weights(lods[0], o, arm)
    report = {"unweighted": sum(1 for v in lods[0].data.vertices if not v.groups)}
    for name, (frames, loop, strike, keys) in CLIPS.items():
        bake(arm, name, frames, keys)
        report[name] = {"frames": frames, "seconds": frames / FPS, "loop": loop, "strike": None if strike is None else round(strike * frames / FPS, 3)}
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for o in lods:
        o.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    for o in scene.objects:
        o.select_set(o in lods or o == arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=OUT_FBX, use_selection=True, object_types={"MESH", "ARMATURE"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
        use_mesh_modifiers=False, add_leaf_bones=False, use_armature_deform_only=True,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0, path_mode="STRIP", embed_textures=False)
    print("REPORT" + json.dumps(report))


main()
