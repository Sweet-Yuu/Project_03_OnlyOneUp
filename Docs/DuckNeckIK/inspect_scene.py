import bpy, json
from pathlib import Path
from collections import defaultdict
out = Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckNeckIK')
rig = bpy.data.objects['Duck_Rig']
report = {'filepath':bpy.data.filepath, 'matrix': [list(r) for r in rig.matrix_world], 'bones':{}, 'meshes':{}}
for b in rig.data.bones:
    p=rig.pose.bones[b.name]
    report['bones'][b.name]={'matrix':[list(r) for r in b.matrix_local], 'connect':b.use_connect, 'pose':[list(r) for r in p.matrix_basis]}
for o in bpy.data.objects:
    if o.type!='MESH' or not any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers): continue
    counts=defaultdict(int); sums=defaultdict(float)
    coords=[rig.matrix_world.inverted() @ o.matrix_world @ v.co for v in o.data.vertices]
    for v in o.data.vertices:
        for g in v.groups:
            counts[o.vertex_groups[g.group].name]+=1
            sums[o.vertex_groups[g.group].name]+=g.weight
    report['meshes'][o.name]={'vertices':len(coords),'bounds':[[min(v[i] for v in coords),max(v[i] for v in coords)] for i in range(3)],'groups':dict(counts),'weight_sums':dict(sums)}
    if o.name in ['Body','Head','Beak','Beak_Lower']:
        report['meshes'][o.name]['data']=[{'i':v.index,'co':list(coords[v.index]),'w':{o.vertex_groups[g.group].name:g.weight for g in v.groups}} for v in o.data.vertices]
(out/'scene_audit.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Duck_Original_Snapshot.blend'), copy=True)
print('AUDIT_AND_ORIGINAL_SNAPSHOT_SAVED')
