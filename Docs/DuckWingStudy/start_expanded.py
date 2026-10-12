import bpy,json
from pathlib import Path
p=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy')
dest=p/'Duck_Wings_Expanded.blend'; i=2
while dest.exists():
    dest=p/f'Duck_Wings_Expanded_v{i:02d}.blend'; i+=1
source=dest.with_name(dest.stem+'_Source.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(source),copy=True)
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
(p/'expanded_session.json').write_text(json.dumps({'source':str(source),'output':str(dest)},indent=2))
print('NEW_WORKING_COPY',str(dest))
