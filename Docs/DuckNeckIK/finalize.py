import bpy, json, math
from pathlib import Path
from mathutils import Vector, Quaternion
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckNeckIK')
rig=bpy.data.objects['Duck_Rig']
audit=json.loads((OUT/'scene_audit.json').read_text())
original=json.loads((OUT/'skeleton_audit.json').read_text())
checks={}
checks['original_bone_names_parents_deform_preserved']=all(n in rig.data.bones and (rig.data.bones[n].parent.name if rig.data.bones[n].parent else None)==parent and rig.data.bones[n].use_deform==deform for n,parent,h,t,deform in original['bones'])
checks['original_rest_matrices_max_error']=max(abs(rig.data.bones[n].matrix_local[i][j]-data['matrix'][i][j]) for n,data in audit['bones'].items() for i in range(4) for j in range(4))
checks['new_bones']=[b.name for b in rig.data.bones if b.name not in audit['bones']]
checks['new_bones_non_deform']=all(not rig.data.bones[n].use_deform for n in checks['new_bones'])
checks['armature_modifiers_retained']=all(any(m.type=='ARMATURE' and m.object==rig for m in bpy.data.objects[n].modifiers) for n in audit['meshes'])
outside_changed=0; inside_changed=0; max_weight_error=0
for n,info in audit['meshes'].items():
    if 'data' not in info: continue
    ob=bpy.data.objects[n]
    for d in info['data']:
        v=ob.data.vertices[d['i']]; now={ob.vertex_groups[g.group].name:g.weight for g in v.groups}
        diff=max(abs(now.get(k,0)-d['w'].get(k,0)) for k in set(now)|set(d['w']))
        if diff>1e-6:
            if n=='Body' and 1.78<d['co'][2]<2.96: inside_changed+=1
            else: outside_changed+=1
        max_weight_error=max(max_weight_error,abs(sum(now.values())-1))
checks['changed_neck_vertices']=inside_changed; checks['changed_vertices_outside_neck']=outside_changed; checks['max_weight_normalization_error']=max_weight_error
tests=json.loads((OUT/'test_report.json').read_text())
checks['max_reachable_beak_mesh_error']=max(t['mesh_error'] for t in tests if t['name']!='unreachable')
checks['max_neck_length_change']=max(abs(l-rig.data.bones[n].length) for t in tests for n,l in zip(['Neck_01','Neck_02','Neck_03'],t['lengths']))
assert checks['original_bone_names_parents_deform_preserved'] and checks['new_bones_non_deform'] and checks['armature_modifiers_retained']
assert checks['original_rest_matrices_max_error']<1e-5 and outside_changed==0 and checks['max_reachable_beak_mesh_error']<.0001 and checks['max_neck_length_change']<1e-5
(OUT/'validation.json').write_text(json.dumps(checks,indent=2))
doc='''DUCK NECK IK

POSE MODE
CTRL_Beak: translate to place the upper beak tip; rotate to orient the head about that contact. Scale is locked.
CTRL_NeckPole: move to choose the neck bending plane. Keep it behind the neck and away from the line between neck base and head target; crossing that line is a pole singularity.
CTRL_Body: move the body while CTRL_Beak stays planted in armature space. CTRL_Beak and CTRL_NeckPole have no parent. Moving the Armature object moves all controls too.

Contact = Beak vertex 37249, the front center of the upper beak. No nest was present; place the nest rim at this point. Lower jaw still uses Jaw.
MCH_HeadTarget is offset from CTRL_Beak by the measured head-to-beak vector (length 0.820534). Three-bone IK on Neck_03 targets this pivot; Head copies its orientation. No cycle, no head pivot substitution, no stretch.
Original deform names, parents, rest transforms and modifiers are retained. Only Body neck weights (rest z 1.78 to 2.96) were smoothed. No additional deform bones.
Local X bends front/back, Y follows the bone length (IK twist locked), Z bends sideways. X limits: first neck +/-155 degrees, others +/-110. Z limits +/-75. The preferred neck pose seeds a smooth forward arch.

LIMITS
Reach depends on both beak position and orientation. Total neck bone length is about 0.989628 scene units. Outside reach the beak will stop short instead of stretching. No collision solver: extreme compression or head rotation can intersect the body. Avoid a collinear pole or flipping it across the neck. Three deform segments remain a stylized rig, not a collision-aware anatomical neck.

GAME EXPORT
Blender constraints do not become engine IK automatically. Bake evaluated animation onto deform bones for clips, or recreate runtime multi-bone IK and the head-to-beak offset in the engine. The imported hierarchy includes CTRL_Root/CTRL_Body ancestors; preserve those ancestors or explicitly retarget an export copy. Do not apply the Armature modifier. This rig adds no Python handlers or runtime script dependency.

QA
See test_report.json, validation.json and the ten rendered pose previews beside this blend. Duck_Original_Snapshot.blend preserves the initial unsaved scene. Saved rig opens in its neutral pose.
'''
(OUT/'README.txt').write_text(doc,encoding='utf-8')
tx=bpy.data.texts.get('README - Neck IK') or bpy.data.texts.new('README - Neck IK'); tx.clear(); tx.write(doc)
rig.data.collections['Deform'].is_visible=False
for ob in bpy.data.objects:
    if ob.type in {'CAMERA','LIGHT'} or ob.name=='Ground': ob.hide_set(True)
for area in bpy.context.screen.areas:
    if area.type in {'CONSOLE','VIEW_3D'}:
        area.type='VIEW_3D'; space=area.spaces.active
        space.region_3d.view_distance=5.2; space.region_3d.view_location=Vector((0,-.2,1.9))
        space.region_3d.view_rotation=(Vector((6,-6,3.5))-Vector((0,-.2,1.9))).to_track_quat('Z','Y')
        space.region_3d.view_perspective='ORTHO'; space.shading.type='SOLID'; space.shading.color_type='MATERIAL'
        space.overlay.show_relationship_lines=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Duck_NeckIK.blend'))
print(json.dumps(checks,indent=2))
