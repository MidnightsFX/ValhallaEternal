"""Renders a rigged humanoid's left hand at rest and with every finger bone curled into a fist, to check its hand
weights.

    blender -b <rigged .blend> --python preview_fist.py -- <out prefix>
"""

import math
import sys

import bpy
from mathutils import Quaternion, Vector

OUT = sys.argv[sys.argv.index("--") + 1]
scene = bpy.context.scene
arm = next(o for o in scene.objects if o.type == "ARMATURE")
mesh = next(o for o in scene.objects if o.type == "MESH" and o.name.endswith("LOD0"))
for o in scene.objects:
    if o.type == "MESH":
        o.hide_render = o != mesh
if arm.animation_data:
    arm.animation_data.action = None

scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 8
scene.cycles.use_denoising = False
scene.render.resolution_x = scene.render.resolution_y = 420
world = scene.world or bpy.data.worlds.new("preview")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.6, 0.62, 0.66, 1)
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
sun.data.energy = 3.5
sun.rotation_euler = (math.radians(40), 0, math.radians(-20))
scene.collection.objects.link(sun)

hand = arm.pose.bones["LeftHand"]
centre = arm.matrix_world @ (hand.bone.tail_local + Vector((0.05, 0, -0.03)))
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = 0.42
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = centre + Vector((0.25, -1.0, 0.2)).normalized() * 2
cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()

for pb in arm.pose.bones:
    pb.rotation_mode = "QUATERNION"
    pb.rotation_quaternion = Quaternion()
scene.render.filepath = f"{OUT}_rest.png"
bpy.ops.render.render(write_still=True)
# Curl about each finger bone's own X axis, as the rig sheet's Fist does.
for pb in arm.pose.bones:
    if pb.name.startswith("LeftHand") and pb.name != "LeftHand":
        pb.rotation_quaternion = Quaternion((1, 0, 0), math.radians(70 if "Thumb" not in pb.name else 35))
bpy.context.view_layer.update()
scene.render.filepath = f"{OUT}_fist.png"
bpy.ops.render.render(write_still=True)
