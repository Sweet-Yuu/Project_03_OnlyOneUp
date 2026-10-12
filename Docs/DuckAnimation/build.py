import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckAnimation'); info=json.loads((P/'session.json').read_text()); r=bpy.data.objects[info['rig']]; s=bpy.context.scene
def signature():
 h=hashlib.sha256()
 for o in sorted((o for o in bpy.data.objects if o.type=='MESH'),key=lambda o:o.name):
  for v in o.data.vertices: h.update(repr((tuple(v.co),[(g.group,g.weight) for g in v.groups])).encode())
 return h.hexdigest()
before=signature()
if r.animation_data and r.animation_data.action: r.animation_data.action.use_fake_user=True
r.animation_data_create(); action=bpy.data.actions.new('Duck_LookAround_HeadScratch'); action.use_fake_user=True; r.animation_data.action=action; action.asset_mark()
names=['Chest','Neck_01','Neck_02','Neck_03','Head','Wing.L','Wing.R','WingTip.L','WingTip.R']+[f'DEF_Primary_{i:02d}.{side}' for side in ['L','R'] for i in range(1,6)]
base={n:r.pose.bones[n].rotation_quaternion.copy() if r.pose.bones[n].rotation_mode=='QUATERNION' else r.pose.bones[n].matrix_basis.to_quaternion() for n in names}
def qglobal(n,xyz):
 R=r.data.bones[n].matrix_local.to_quaternion(); q=Quaternion((0,0,1),math.radians(xyz[2]))@Quaternion((0,1,0),math.radians(xyz[1]))@Quaternion((1,0,0),math.radians(xyz[0]))
 return base[n]@R.inverted()@q@R
# Các góc khai báo trong hệ Armature được đổi về local rest từng bone.
# Nhịp quay đầu dẫn trước, Chest theo sau nhẹ để giữ chân đứng yên.
keys=[
 (1,0,0,0,0,0,0),(12,0,0,0,0,0,0),
 (30,-28,-3,-1,0,0,0),(43,-31,-2,-2,0,0,0),
 (66,30,2,2,0,0,0),(80,34,1,2.5,0,0,0),
 (102,8,13,.4,0,0,0),(116,5,10,-.4,0,0,0),
 (131,0,0,0,0,0,0),
 (144,-3,-4,-2,55,0,0),
 (158,-6,-6,-3,115,4,0),
 (171,-7,-6,-3,158,4,0),
 (177,-7,-6,-3,161,7,1),(183,-6,-5,-2.5,157,2,-1),
 (189,-7,-6,-3,161,7,1),(195,-6,-5,-2.5,157,2,-1),
 (201,-7,-6,-3,161,7,1),(207,-6,-5,-2.5,157,2,-1),
 (216,-4,-3,-2,124,2,0),(231,0,1,.8,45,0,0),
 (246,0,0,0,0,0,0),(255,0,0,0,0,0,0)]
for frame,yaw,pitch,sway,lift,tip,scratch in keys:
 s.frame_set(frame)
 for n in names: r.pose.bones[n].rotation_mode='QUATERNION'; r.pose.bones[n].rotation_quaternion=base[n]
 r.pose.bones['Chest'].rotation_quaternion=qglobal('Chest',(0,sway,yaw*.09))
 for n,fraction in [('Neck_01',.15),('Neck_02',.22),('Neck_03',.25),('Head',.38)]:
  bend=8 if lift>110 else 0
  r.pose.bones[n].rotation_quaternion=qglobal(n,(pitch*fraction,bend*fraction,yaw*fraction))
 # Nâng cánh phải theo cung phía ngoài, mút lông chạm vùng thái dương.
 r.pose.bones['Wing.R'].rotation_quaternion=qglobal('Wing.R',(-lift*.04,-lift,0))
 r.pose.bones['WingTip.R'].rotation_quaternion=qglobal('WingTip.R',(scratch*3,-tip,0))
 r.pose.bones['Wing.L'].rotation_quaternion=qglobal('Wing.L',(0,4*math.sin(math.radians(lift)),0))
 for side in ['L','R']:
  for i in range(1,6):
   n=f'DEF_Primary_{i:02d}.{side}'; amount=min(lift/110,1)*([3,1.5,0,-1.5,-3][i-1]) if side=='R' else 0
   r.pose.bones[n].rotation_quaternion=base[n]@Quaternion((0,0,1),math.radians(amount))
 for n in names: r.pose.bones[n].keyframe_insert(data_path='rotation_quaternion',frame=frame,group=n)
for layer in action.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for fc in bag.fcurves:
    for k in fc.keyframe_points: k.interpolation='BEZIER'; k.handle_left_type='AUTO_CLAMPED'; k.handle_right_type='AUTO_CLAMPED'
s.render.fps=30; s.frame_start=1; s.frame_end=255
for frame,label in [(1,'START'),(30,'LOOK LEFT'),(66,'LOOK RIGHT'),(102,'LOOK UP'),(144,'RAISE WING'),(177,'SCRATCH 1'),(189,'SCRATCH 2'),(201,'SCRATCH 3'),(216,'LOWER WING'),(255,'END')]: s.timeline_markers.new(label,frame=frame)
s.frame_set(1)
assert signature()==before
assert all(max(abs(r.data.bones[n].matrix_local[i][j]-b['matrix'][i][j]) for i in range(4) for j in range(4))<1e-7 for n,b in info['bones'].items())
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=info['output'])
(P/'validation.json').write_text(json.dumps({'mesh_weight_unchanged':True,'all_rest_matrices_unchanged':True,'action':action.name,'frame_range':[1,255],'fps':30,'animated_bones':names},indent=2))
print('ANIMATION_SAVED',info['output'])
