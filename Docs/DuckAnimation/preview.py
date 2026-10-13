exec(compile(open('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckAnimation/render.py',encoding='utf-8').read().split('frames=[1,30')[0],'setup','exec'))
import json
r=bpy.data.objects['Duck_Rig']; checks={}; first={}; last={}; max_foot=0
s.frame_set(1); feet={n:r.pose.bones[n].matrix.copy() for n in ['Foot.L','Foot.R']}
for f in range(1,256):
 s.frame_set(f); bpy.context.view_layer.update()
 max_foot=max(max_foot,max(abs(r.pose.bones[n].matrix[i][j]-feet[n][i][j]) for n in feet for i in range(4) for j in range(4)))
 if f in [1,255]:
  target=first if f==1 else last
  target.update({p.name:[list(row) for row in p.matrix] for p in r.pose.bones})
checks['max_foot_matrix_drift']=max_foot; checks['first_last_pose_matrix_error']=max(abs(first[n][i][j]-last[n][i][j]) for n in first for i in range(4) for j in range(4))
(P/'motion_validation.json').write_text(json.dumps(checks,indent=2))
s.render.resolution_x=480; s.render.resolution_y=480
c.location=(3,-8,2.4); c.rotation_euler=(Vector((0,0,1.8))-c.location).to_track_quat('-Z','Y').to_euler()
for f in range(1,256,3):
 s.frame_set(f); s.render.filepath=str(P/f'preview_{f:03d}.png'); bpy.ops.render.render(write_still=True)
print(checks)
