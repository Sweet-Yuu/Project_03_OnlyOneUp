import bpy, json, hashlib
from pathlib import Path
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy')
rig=bpy.data.objects['Duck_Rig']
def sig():
    result={}
    for o in bpy.data.objects:
        if o.type=='MESH':
            h=hashlib.sha256()
            for v in o.data.vertices: h.update(repr((tuple(v.co),[(g.group,g.weight) for g in v.groups])).encode())
            result[o.name]=h.hexdigest()
    return result
report={'file':bpy.data.filepath,'frame':bpy.context.scene.frame_current,'mode':bpy.context.mode,'active':bpy.context.view_layer.objects.active.name if bpy.context.view_layer.objects.active else None,'selected':[o.name for o in bpy.context.selected_objects],'rig_matrix':[list(r) for r in rig.matrix_world],'poses':{},'bones':{},'meshes':{},'mesh_weight_hashes':sig()}
for p in rig.pose.bones:
    b=p.bone
    report['poses'][p.name]={'mode':p.rotation_mode,'basis':[list(r) for r in p.matrix_basis],'location':list(p.location),'scale':list(p.scale),'euler':list(p.rotation_euler),'quaternion':list(p.rotation_quaternion),'axis_angle':list(p.rotation_axis_angle)}
    report['bones'][p.name]={'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local),'matrix':[list(r) for r in b.matrix_local],'deform':b.use_deform,'constraints':[(c.name,c.type) for c in p.constraints]}
for o in bpy.data.objects:
    if o.type!='MESH' or not o.name.startswith('Wing'): continue
    verts=[rig.matrix_world.inverted() @ o.matrix_world @ v.co for v in o.data.vertices]
    groups={}
    for vg in o.vertex_groups:
        w=[g.weight for v in o.data.vertices for g in v.groups if g.group==vg.index]
        groups[vg.name]={'count':len(w),'min':min(w,default=0),'max':max(w,default=0),'sum':sum(w)}
    report['meshes'][o.name]={'vertices':len(verts),'bounds':[[min(v[i] for v in verts),max(v[i] for v in verts)] for i in range(3)],'groups':groups,'modifiers':[(m.name,m.type,m.object.name if m.type=='ARMATURE' and m.object else None) for m in o.modifiers],'parent':o.parent.name if o.parent else None}
(OUT/'initial_state.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Wing_Initial_Snapshot.blend'),copy=True)
print('INITIAL_STATE_CAPTURED', len(report['meshes']), 'wing meshes', report['file'])
