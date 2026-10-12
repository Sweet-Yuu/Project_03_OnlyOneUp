import bpy,json
from pathlib import Path
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckAnimation')
i=1
while (P/f'Duck_Look_Scratch_v{i:02d}.blend').exists(): i+=1
out=P/f'Duck_Look_Scratch_v{i:02d}.blend'; src=P/f'Duck_Look_Scratch_v{i:02d}_Source.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(src))
r=bpy.data.objects.get('Duck_Rig') or next(o for o in bpy.data.objects if o.type=='ARMATURE')
info={'source':str(src),'output':str(out),'rig':r.name,'bones':{b.name:{'head':list(b.head_local),'tail':list(b.tail_local),'matrix':[list(x) for x in b.matrix_local],'parent':b.parent.name if b.parent else None,'constraints':[c.type for c in r.pose.bones[b.name].constraints]} for b in r.data.bones},'meshes':[o.name for o in bpy.data.objects if o.type=='MESH'],'actions':[a.name for a in bpy.data.actions]}
(P/'session.json').write_text(json.dumps(info,indent=2))
print('ANIMATION_SNAPSHOT',str(src))
