import bpy,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy'); r=bpy.data.objects['Duck_Rig']; scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH'; scene.render.resolution_x=900; scene.render.resolution_y=900; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
scene.display.shading.light='STUDIO'; scene.display.shading.color_type='MATERIAL'; scene.display.shading.show_shadows=True; scene.display.shading.show_cavity=True; scene.display.shading.background_type='WORLD'
if not scene.world: scene.world=bpy.data.worlds.new('QA World')
scene.world.color=(.1,.1,.1)
for o in bpy.data.objects:
    if o.name.startswith('WGT_') or o.name=='Ground': o.hide_render=True
cam=bpy.data.objects.new('QA_Expanded',bpy.data.cameras.new('QA_Expanded')); scene.collection.objects.link(cam); scene.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=4.65
report={}
for label,frame,lean in [('closed',1,0),('medium',30,0),('wide',60,0),('carry',60,18)]:
    scene.frame_set(frame)
    if lean:
        p=r.pose.bones['Chest']; p.rotation_mode='QUATERNION'; p.rotation_quaternion=Quaternion((1,0,0),math.radians(lean))
        # Extend the neck through rotation only, without stretch or changing any constraint.
        for n,angle in [('Neck_01',-8),('Neck_02',-5),('Neck_03',-3),('Head',-2)]:
            if not r.pose.bones[n].constraints:
                p=r.pose.bones[n]; p.rotation_mode='QUATERNION'; p.rotation_quaternion=Quaternion((1,0,0),math.radians(angle))
    bpy.context.view_layer.update(); allv=[]
    for o in bpy.data.objects:
        if o.type!='MESH' or not o.name.startswith('Wing'): continue
        ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get()); me=ev.to_mesh(); allv += [r.matrix_world.inverted()@ev.matrix_world@v.co for v in me.vertices]; ev.to_mesh_clear()
    report[label]={'span':max(v.x for v in allv)-min(v.x for v in allv),'min_z':min(v.z for v in allv)}
    for view,loc in [('front',(0,-8,1.8)),('side',(8,0,1.8)),('back',(0,8,1.8))]:
        cam.location=loc; cam.rotation_euler=(Vector((0,0,1.8))-cam.location).to_track_quat('-Z','Y').to_euler(); scene.render.filepath=str(P/f'expanded_{label}_{view}.png'); bpy.ops.render.render(write_still=True)
(P/'expanded_metrics.json').write_text(json.dumps(report,indent=2)); print(report)
