import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy')
rig=bpy.data.objects['Duck_Rig']; pb=rig.pose.bones
names=['Wing.L','WingTip.L','Wing.R','WingTip.R']
initial={p.name:p.matrix_basis.copy() for p in pb}
wingobjects=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Wing')]
def reset():
    for n in names: pb[n].matrix_basis=initial[n]
    bpy.context.view_layer.update()
def verts(o):
    ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get()); m=ev.to_mesh()
    result=[rig.matrix_world.inverted() @ ev.matrix_world @ v.co for v in m.vertices]; ev.to_mesh_clear(); return result
def capture(): return {o.name:verts(o) for o in wingobjects}
reset(); base=capture()
axes={}
for n in names:
    b=rig.data.bones[n]; along=(b.tail_local-b.head_local).normalized(); side=-1 if n.endswith('.L') else 1
    hinge=along.cross(Vector((side,0,0))).normalized()
    axes[n]=b.matrix_local.to_3x3().inverted() @ hinge
def apply(wing,tip,roll=0):
    reset()
    for n in names:
        angle=tip if 'Tip' in n else wing
        q=Quaternion(axes[n],math.radians(angle))
        if 'Tip' not in n and roll: q=q @ Quaternion((0,1,0),math.radians(roll*(-1 if n.endswith('.L') else 1)))
        pb[n].rotation_mode='QUATERNION'; pb[n].rotation_quaternion=initial[n].to_quaternion() @ q
    bpy.context.view_layer.update()
report={'axes':{n:list(a) for n,a in axes.items()},'isolated_axes':{},'tip_influence':{},'poses':{}}
for n in names:
    b=pb[n]; reset(); originaltail=b.tail.copy()
    report['isolated_axes'][n]={}
    for axis in range(3):
        reset(); vec=Vector((axis==0,axis==1,axis==2)); b.rotation_mode='QUATERNION'; b.rotation_quaternion=initial[n].to_quaternion() @ Quaternion(vec,math.radians(25)); bpy.context.view_layer.update()
        report['isolated_axes'][n]['XYZ'[axis]]={'tail_delta':list(b.tail-originaltail),'axis_in_armature':list(b.bone.matrix_local.col[axis].to_3d())}
    if 'Tip' in n:
        reset(); b.rotation_quaternion=initial[n].to_quaternion() @ Quaternion(axes[n],math.radians(25)); bpy.context.view_layer.update(); result=capture()
        report['tip_influence'][n]={key:{'max_move':max((a-c).length for a,c in zip(value,base[key])),'mean_move':sum((a-c).length for a,c in zip(value,base[key]))/len(value),'moved_vertices':sum((a-c).length>1e-5 for a,c in zip(value,base[key]))} for key,value in result.items()}
scene=bpy.context.scene; scene.render.engine='BLENDER_WORKBENCH'; scene.render.resolution_x=800; scene.render.resolution_y=800; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
scene.display.shading.light='STUDIO'; scene.display.shading.color_type='MATERIAL'; scene.display.shading.show_cavity=True; scene.display.shading.show_shadows=True
scene.display.shading.background_type='WORLD'
if not scene.world: scene.world=bpy.data.worlds.new('QA World')
scene.world.color=(.1,.1,.1)
for o in bpy.data.objects:
    if o.name.startswith('WGT_') or o.name=='Ground': o.hide_render=True
cam=bpy.data.objects.new('QA_WingCamera',bpy.data.cameras.new('QA_WingCamera')); scene.collection.objects.link(cam); scene.camera=cam
cam.data.type='ORTHO'; cam.data.ortho_scale=4.35
views={'front':(0,-8,1.8),'side':(8,0,1.8),'back':(0,8,1.8)}
cases={'closed':(0,0,0),'medium':(35,12,0),'wide':(58,12,0),'wing_only':(64.1,0,0),'tip_excess':(58,40,0),'rolled':(58,12,25)}
for label,args in cases.items():
    apply(*args); result=capture(); allv=[v for vs in result.values() for v in vs]
    report['poses'][label]={'angles':args,'span':max(v.x for v in allv)-min(v.x for v in allv),'rotations':{n:{'quaternion_wxyz':list(pb[n].rotation_quaternion),'euler_xyz_deg':[math.degrees(v) for v in pb[n].rotation_quaternion.to_euler('XYZ')],'head':list(pb[n].head),'tail':list(pb[n].tail)} for n in names}}
    for view,loc in views.items():
        cam.location=loc; cam.rotation_euler=(Vector((0,0,1.8))-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(OUT/(label+'_'+view+'.png')); bpy.ops.render.render(write_still=True)
reset(); (OUT/'study_results.json').write_text(json.dumps(report,indent=2))
print('WING STUDY',[(k,v['span']) for k,v in report['poses'].items()])
