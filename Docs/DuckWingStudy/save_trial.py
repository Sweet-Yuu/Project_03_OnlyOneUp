import bpy, json, math, hashlib
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy')
data=json.loads((OUT/'study_results.json').read_text()); original=json.loads((OUT/'initial_state.json').read_text())
rig=bpy.data.objects['Duck_Rig']; names=list(data['axes'])
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='POSE')
for n in names:
    p=rig.pose.bones[n]; p.rotation_mode='QUATERNION'; p.rotation_quaternion=data['poses']['wide']['rotations'][n]['quaternion_wxyz']
for b in rig.pose.bones: b.select=b.name in names
rig.data.bones.active=rig.data.bones['Wing.L']
rig.show_in_front=True
for a in bpy.context.screen.areas:
    if a.type in {'CONSOLE','VIEW_3D'}:
        a.type='VIEW_3D'; s=a.spaces.active; s.region_3d.view_location=Vector((0,0,1.8)); s.region_3d.view_distance=5.5
        s.region_3d.view_rotation=Vector((0,-1,0)).to_track_quat('Z','Y'); s.region_3d.view_perspective='ORTHO'; s.shading.color_type='MATERIAL'; s.overlay.show_relationship_lines=False
checks={'mesh_and_weights_unchanged':True,'rest_hierarchy_unchanged':True,'non_wing_poses_unchanged':True,'constraints_unchanged':True}
for o in bpy.data.objects:
    if o.type!='MESH': continue
    h=hashlib.sha256()
    for v in o.data.vertices: h.update(repr((tuple(v.co),[(g.group,g.weight) for g in v.groups])).encode())
    checks['mesh_and_weights_unchanged'] &= h.hexdigest()==original['mesh_weight_hashes'][o.name]
for b in rig.data.bones:
    old=original['bones'][b.name]
    checks['rest_hierarchy_unchanged'] &= (b.parent.name if b.parent else None)==old['parent'] and max(abs(b.matrix_local[i][j]-old['matrix'][i][j]) for i in range(4) for j in range(4))<1e-6
    checks['constraints_unchanged'] &= [(c.name,c.type) for c in rig.pose.bones[b.name].constraints]==[tuple(c) for c in old['constraints']]
    if b.name not in names:
        checks['non_wing_poses_unchanged'] &= max(abs(rig.pose.bones[b.name].matrix_basis[i][j]-original['poses'][b.name]['basis'][i][j]) for i in range(4) for j in range(4))<1e-6
assert all(checks.values()), checks
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Duck_Wings_Wide_Trial.blend'))
(OUT/'trial_validation.json').write_text(json.dumps(checks,indent=2))
print(checks)
