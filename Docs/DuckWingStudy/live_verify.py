import bpy, json, math, hashlib
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy')
orig=json.loads((OUT/'initial_state.json').read_text()); study=json.loads((OUT/'study_results.json').read_text())
rig=bpy.data.objects['Duck_Rig']; names=list(study['axes'])
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='POSE')
trials={}
try:
    for n in names:
        p=rig.pose.bones[n]; trials[n]={}
        for axis in range(3):
            for sign in [-1,1]:
                p.rotation_mode='QUATERNION'; p.rotation_quaternion=Quaternion(orig['poses'][n]['quaternion']) @ Quaternion(Vector((axis==0,axis==1,axis==2)),math.radians(sign*25))
                bpy.context.view_layer.update(); trials[n]['XYZ'[axis]+str(sign*25)]={'head':list(p.head),'tail':list(p.tail)}
        p.rotation_quaternion=orig['poses'][n]['quaternion']; bpy.context.view_layer.update()
    for n in names: rig.pose.bones[n].rotation_quaternion=study['poses']['wide']['rotations'][n]['quaternion_wxyz']
    bpy.context.view_layer.update()
    trials['wide_pose_evaluated']=True
finally:
    for n in names:
        p=rig.pose.bones[n]; s=orig['poses'][n]; p.rotation_mode=s['mode']; p.location=s['location']; p.scale=s['scale']; p.rotation_quaternion=s['quaternion']; p.rotation_euler=s['euler']; p.rotation_axis_angle=s['axis_angle']
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode='OBJECT'); bpy.ops.object.select_all(action='DESELECT')
    for n in orig['selected']: bpy.data.objects[n].select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects.get(orig['active'])
trials['all_pose_basis_restored']=all(max(abs(p.matrix_basis[i][j]-orig['poses'][p.name]['basis'][i][j]) for i in range(4) for j in range(4))<1e-6 for p in rig.pose.bones)
trials['selection_restored']=sorted(o.name for o in bpy.context.selected_objects)==sorted(orig['selected'])
trials['active_restored']=bpy.context.view_layer.objects.active.name==orig['active']
trials['mode_restored']=bpy.context.mode==orig['mode']
trials['mesh_weight_hashes_unchanged']=True
for o in bpy.data.objects:
    if o.type!='MESH': continue
    h=hashlib.sha256()
    for v in o.data.vertices: h.update(repr((tuple(v.co),[(g.group,g.weight) for g in v.groups])).encode())
    trials['mesh_weight_hashes_unchanged'] &= h.hexdigest()==orig['mesh_weight_hashes'][o.name]
assert all(trials[k] for k in ['all_pose_basis_restored','selection_restored','active_restored','mode_restored','mesh_weight_hashes_unchanged'])
(OUT/'live_verification.json').write_text(json.dumps(trials,indent=2))
print('POSE_TRIALS_COMPLETE_ORIGINAL_RESTORED')
bpy.context.area.type='VIEW_3D'
