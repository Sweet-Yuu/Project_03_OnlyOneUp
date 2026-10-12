import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckNeckIK')
rig=bpy.data.objects['Duck_Rig']
bpy.context.view_layer.objects.active=rig
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
beak=bpy.data.objects['Beak']
tip=min(beak.data.vertices,key=lambda v:(rig.matrix_world.inverted() @ beak.matrix_world @ v.co).y)
contact=rig.matrix_world.inverted() @ beak.matrix_world @ tip.co
head=rig.data.bones['Head']; headmat=head.matrix_local.copy(); pivot=head.head_local.copy()
bpy.ops.object.mode_set(mode='EDIT')
eb=rig.data.edit_bones
ctrl=eb.new('CTRL_Beak'); ctrl.head=contact; ctrl.tail=contact+Vector((0,0,.22)); ctrl.use_deform=False
mch=eb.new('MCH_HeadTarget'); mch.head=pivot; mch.tail=pivot+headmat.col[1].to_3d()*.2; mch.align_roll(headmat.col[2].to_3d()); mch.parent=ctrl; mch.use_deform=False
pole=eb.new('CTRL_NeckPole'); pole.head=(0,.85,2.4); pole.tail=(0,.85,2.6); pole.use_deform=False
bpy.ops.object.mode_set(mode='POSE')
ik=rig.pose.bones['Neck_03'].constraints.new('IK'); ik.name='Neck IK - 3 bones - no stretch'
ik.target=rig; ik.subtarget='MCH_HeadTarget'; ik.chain_count=3; ik.use_stretch=False; ik.iterations=256
ik.pole_target=rig; ik.pole_subtarget='CTRL_NeckPole'; ik.pole_angle=-math.pi/2
for i,n in enumerate(['Neck_01','Neck_02','Neck_03']):
    p=rig.pose.bones[n]; p.ik_stretch=0; p.lock_ik_y=True
    p.use_ik_limit_x=True; p.ik_min_x=math.radians(-155 if i==0 else -110); p.ik_max_x=math.radians(155 if i==0 else 110)
    p.use_ik_limit_z=True; p.ik_min_z=math.radians(-75); p.ik_max_z=math.radians(75)
    p.rotation_mode='XYZ'; p.rotation_euler.x=[-.35,.55,.25][i]
cr=rig.pose.bones['Head'].constraints.new('COPY_ROTATION'); cr.name='Head orientation about beak contact'; cr.target=rig; cr.subtarget='MCH_HeadTarget'; cr.target_space='WORLD'; cr.owner_space='WORLD'
for n in ['CTRL_Beak','CTRL_NeckPole']:
    rig.pose.bones[n].rotation_mode='XYZ'; rig.pose.bones[n].lock_scale=(True,True,True)
rig.pose.bones['CTRL_NeckPole'].lock_rotation=(True,True,True)
collections={n:rig.data.collections.get(n) or rig.data.collections.new(n) for n in ['Controls','Deform','Mechanism']}
for b in rig.data.bones:
    for coll in list(b.collections): coll.unassign(b)
    collections['Mechanism' if b.name.startswith('MCH_') else 'Controls' if b.name.startswith('CTRL_') else 'Deform'].assign(b)
collections['Mechanism'].is_visible=False
shapes=bpy.data.collections.new('Neck IK Shapes'); bpy.context.scene.collection.children.link(shapes); shapes.hide_render=True
def shape(name,verts,edges):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,edges,[]); ob=bpy.data.objects.new(name,me); shapes.objects.link(ob); ob.hide_render=True; ob.hide_set(True); return ob
verts=[]; edges=[]
for plane in range(3):
    start=len(verts)
    for j in range(32):
        a=j*math.tau/32; v=[0,0,0]; v[(plane+1)%3]=.17*math.cos(a); v[(plane+2)%3]=.17*math.sin(a); verts.append(v); edges.append((start+j,start+(j+1)%32))
ob=shape('WGT_Beak',verts,edges)
p=rig.pose.bones['CTRL_Beak']; p.custom_shape=ob; p.use_custom_shape_bone_size=False; p.color.palette='THEME04'
ob=shape('WGT_NeckPole',[(.12,0,0),(-.12,0,0),(0,.12,0),(0,-.12,0),(0,0,.12),(0,0,-.12)],[(a,b) for a in [0,1] for b in [2,3,4,5]]+[(a,b) for a in [2,3] for b in [4,5]])
p=rig.pose.bones['CTRL_NeckPole']; p.custom_shape=ob; p.use_custom_shape_bone_size=False; p.color.palette='THEME03'
rig.show_in_front=True
# Smooth only the neck vertices where the inspected deep bend showed sharp bands.
body=bpy.data.objects['Body']; changed=0
neckgroups=['Chest','Neck_01','Neck_02','Neck_03','Head']
centers=[1.72,2.12,2.46,2.74,3.02]
for v in body.data.vertices:
    z=(rig.matrix_world.inverted() @ body.matrix_world @ v.co).z
    if not 1.78<z<2.96: continue
    old={body.vertex_groups[g.group].name:g.weight for g in v.groups}
    mass=sum(old.get(n,0) for n in neckgroups)
    mix=min(1,(z-1.78)/.14,(2.96-z)/.12); mix=mix*mix*(3-2*mix)
    raw=[math.exp(-.5*((z-c)/.22)**2) for c in centers]; total=sum(raw)
    for n,w in zip(neckgroups,raw):
        body.vertex_groups[n].add([v.index],(1-mix)*old.get(n,0)+mix*mass*w/total,'REPLACE')
    changed+=1
rig['NeckWeightVerticesUpdated']=changed
rig['BeakContactVertex']=tip.index; rig['BeakContactRest']=list(contact)
rig['NeckIKNote']='Move CTRL_Beak; rotate it to orient head about contact. CTRL_NeckPole sets bend plane. Move CTRL_Body, not armature object, for planted beak. Bake deform animation on export.'
for b in rig.pose.bones: b.select=False
rig.pose.bones['CTRL_Beak'].select=True; rig.data.bones.active=rig.data.bones['CTRL_Beak']
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Duck_NeckIK.blend'))
print('RIG_BUILT',list(contact),(contact-pivot).length)
