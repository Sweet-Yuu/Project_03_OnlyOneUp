import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Quaternion,Vector
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy'); r=bpy.data.objects['Duck_Rig']; s=bpy.context.scene
base=json.loads((P/'expanded_baseline.json').read_text()); checks={}
checks['existing_bone_count']=len(base['bones'])
checks['max_existing_rest_matrix_error']=max(abs(b.matrix_local[i][j]-v['matrix'][i][j]) for n,v in base['bones'].items() for b in [r.data.bones[n]] for i in range(4) for j in range(4))
checks['existing_parents_preserved']=all((r.data.bones[n].parent.name if r.data.bones[n].parent else None)==v['parent'] for n,v in base['bones'].items())
checks['existing_deform_flags_preserved']=all(r.data.bones[n].use_deform==v['deform'] for n,v in base['bones'].items())
def meshhash(o):
 h=hashlib.sha256()
 for v in o.data.vertices: h.update(repr((tuple(v.co),[(g.group,g.weight) for g in v.groups])).encode())
 return h.hexdigest()
checks['unrelated_mesh_coords_weights_exact']=all(meshhash(bpy.data.objects[n])==h for n,h in base['unrelated_meshes'].items())
checks['constraint_names_types_preserved']=all([[c.name,c.type] for c in r.pose.bones[n].constraints]==v for n,v in base['constraints'].items())
checks['new_bones']=[b.name for b in r.data.bones if b.name not in base['bones']]
checks['wing_armature_modifiers']={o.name:[m.object.name if m.object else None for m in o.modifiers if m.type=='ARMATURE'] for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Wing')}
checks['wing_max_weight_sum_error']=max(abs(sum(g.weight for g in v.groups)-1) for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Wing') for v in o.data.vertices)
assert checks['max_existing_rest_matrix_error']<1e-6
assert all(checks[k] for k in ['existing_parents_preserved','existing_deform_flags_preserved','unrelated_mesh_coords_weights_exact','constraint_names_types_preserved'])
assert all(v==['Duck_Rig'] for v in checks['wing_armature_modifiers'].values())
assert checks['wing_max_weight_sum_error']<1e-5
s.frame_set(1)
initial={p.name:(p.rotation_mode,p.matrix_basis.copy()) for p in r.pose.bones}; preview=r.animation_data.action
s.frame_set(60)
carry_names=['Chest','Neck_01','Neck_02','Neck_03','Head']
for n,angle in zip(carry_names,[18,-8,-5,-3,-2]):
 p=r.pose.bones[n]
 if not p.constraints: p.rotation_mode='QUATERNION'; p.rotation_quaternion=Quaternion((1,0,0),math.radians(angle))
controls=list(json.loads((P/'expanded_pose_transforms.json').read_text())['Wide'])
a=bpy.data.actions.new('WINGS_CarryTest'); a.use_fake_user=True; r.animation_data.action=a
for n in controls+carry_names: r.pose.bones[n].keyframe_insert(data_path='rotation_quaternion',frame=1,group=n)
a.asset_mark()
carry={n:{'rotation_mode':r.pose.bones[n].rotation_mode,'quaternion_wxyz':list(r.pose.bones[n].rotation_quaternion)} for n in controls+carry_names}
(P/'expanded_carry_pose.json').write_text(json.dumps(carry,indent=2))
r.animation_data.action=preview
for n,(mode,matrix) in initial.items(): r.pose.bones[n].rotation_mode=mode; r.pose.bones[n].matrix_basis=matrix
s.frame_set(1); bpy.context.view_layer.update()
if bpy.context.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active; space.shading.type='SOLID'; space.shading.color_type='MATERIAL'; space.overlay.show_extras=False; space.overlay.show_relationship_lines=False
   space.region_3d.view_location=Vector((0,0,1.7)); space.region_3d.view_distance=5.5; space.region_3d.view_rotation=Quaternion((1,0,0),math.radians(90)); space.region_3d.view_perspective='ORTHO'
text=bpy.data.texts.get('WINGS_README') or bpy.data.texts.new('WINGS_README')
text.clear(); text.write('Expanded wings: frame 1 CLOSED, 30 MEDIUM, 60 WIDE. Actions WINGS_Closed, WINGS_Medium, WINGS_Wide are reusable pose assets. WINGS_CarryTest adds Chest/neck rotations for visual testing only. Ten new DEF_Primary_01..05.L/R bones are parented to WingTip.L/R. All existing rest matrices and hierarchy preserved. Direct deform rotations; no Unity dependency on Blender drivers/constraints. Full report and local quaternions: EXPANDED_REPORT.md and expanded_pose_transforms.json beside this blend. No FBX exported. Source snapshot retained.\n')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=json.loads((P/'expanded_session.json').read_text())['output'])
(P/'expanded_validation.json').write_text(json.dumps(checks,indent=2)); print(json.dumps(checks,indent=2))
