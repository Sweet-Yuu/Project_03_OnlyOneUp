import bpy,math
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy'); r=bpy.data.objects['Duck_Rig']; scene=bpy.context.scene
if r.animation_data: r.animation_data.action=None
for n in ['Wing.L','Wing.R','WingTip.L','WingTip.R']:
 b=r.data.bones[n]; side=-1 if n.endswith('.L') else 1
 axis=b.matrix_local.to_3x3().inverted()@(b.tail_local-b.head_local).normalized().cross(Vector((side,0,0))).normalized()
 p=r.pose.bones[n]; p.rotation_mode='QUATERNION'; p.rotation_quaternion=Quaternion(axis,math.radians(12 if 'Tip' in n else 58))
scene.render.engine='BLENDER_WORKBENCH'; scene.render.resolution_x=900; scene.render.resolution_y=900; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
scene.display.shading.light='STUDIO'; scene.display.shading.color_type='MATERIAL'; scene.display.shading.show_shadows=True; scene.display.shading.show_cavity=True; scene.display.shading.background_type='WORLD'
if not scene.world: scene.world=bpy.data.worlds.new('QA World')
scene.world.color=(.1,.1,.1)
for o in bpy.data.objects:
 if o.name.startswith('WGT_') or o.name=='Ground': o.hide_render=True
cam=bpy.data.objects.new('QA_Before',bpy.data.cameras.new('QA_Before')); scene.collection.objects.link(cam); scene.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=4.65
cam.location=(0,-8,1.8); cam.rotation_euler=(Vector((0,0,1.8))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(P/'expanded_before_wide_front.png'); bpy.ops.render.render(write_still=True)
