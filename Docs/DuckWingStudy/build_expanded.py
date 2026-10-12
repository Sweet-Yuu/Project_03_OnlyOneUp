import bpy, math, json, hashlib, numpy as np
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckWingStudy'); session=json.loads((P/'expanded_session.json').read_text()); OUTPUT=session['output']
r=bpy.data.objects['Duck_Rig']; bpy.context.view_layer.objects.active=r
if bpy.context.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT'); r.select_set(True)
def meshhash(o):
    h=hashlib.sha256()
    for v in o.data.vertices: h.update(repr((tuple(v.co),[(g.group,g.weight) for g in v.groups])).encode())
    return h.hexdigest()
baseline={'bones':{b.name:{'matrix':[list(row) for row in b.matrix_local],'parent':b.parent.name if b.parent else None,'deform':b.use_deform} for b in r.data.bones},'unrelated_meshes':{o.name:meshhash(o) for o in bpy.data.objects if o.type=='MESH' and not o.name.startswith('Wing')},'constraints':{p.name:[(c.name,c.type) for c in p.constraints] for p in r.pose.bones}}
(P/'expanded_baseline.json').write_text(json.dumps(baseline,indent=2))
axes={}
for n in ['Wing.L','Wing.R','WingTip.L','WingTip.R']:
    b=r.data.bones[n]; side=-1 if n.endswith('.L') else 1; axes[n]=b.matrix_local.to_3x3().inverted() @ (b.tail_local-b.head_local).normalized().cross(Vector((side,0,0))).normalized()
spec=[]
def transform_feather(o,root,direction,length,width):
    M=r.matrix_world.inverted() @ o.matrix_world; inv=M.inverted()
    vs=np.array([tuple(M@v.co) for v in o.data.vertices]); mean=vs.mean(axis=0)
    vals,vecs=np.linalg.eigh(np.cov((vs-mean).T)); olddir=vecs[:,-1]
    if olddir[2]>0: olddir=-olddir
    oldwidth=vecs[:,-2]; oldnormal=np.cross(olddir,oldwidth)
    coords=vs-mean; t=coords@olddir; lo,hi=t.min(),t.max(); u=coords@oldwidth; w=coords@oldnormal
    d=direction.normalized(); normal=Vector((0,-1,0)); widthaxis=d.cross(normal).normalized(); normal=widthaxis.cross(d).normalized()
    # Match the existing feather's front surface rather than reverse its thickness.
    if np.dot(oldnormal,np.array(normal))<0: w=-w
    if np.dot(oldwidth,np.array(widthaxis))<0: u=-u
    for i,v in enumerate(o.data.vertices):
        q=root+d*float((t[i]-lo)/(hi-lo)*length)+widthaxis*float(u[i]*width/(u.max()-u.min()))+normal*float(w[i]*.80)
        v.co=inv@q
    o.data.update(); return [(float(x-lo)/(hi-lo)) for x in t]
for side,label in [(-1,'L'),(1,'R')]:
    o=bpy.data.objects['Wing_'+label]; M=r.matrix_world.inverted()@o.matrix_world; inv=M.inverted()
    for v in o.data.vertices:
        q=M@v.co; f=max(0,min(1,(1.72-q.z)/.7)); f=f*f*(3-2*f)
        q.z-=.16*f; q.x+=side*.045*f; q.y=.05+(q.y-.05)*(1+.12*f); v.co=inv@q
    o.data.update()
    for i in range(5):
        o=bpy.data.objects[f'Wing_{label}_Primary_{i:02d}']
        root=Vector((side*(.68+.035*i),-.14+.042*i,1.53-.025*i))
        d=Vector((side*.13,.10,-1)).normalized(); length=[1.29,1.26,1.19,1.10,.98][i]
        t=transform_feather(o,root,d,length,[.265,.27,.265,.25,.23][i]); name=f'DEF_Primary_{i+1:02d}.{label}'
        spec.append((name,root,d,length,o.name,t,side*[24,12,0,-12,-24][i]))
    for i in range(3):
        o=bpy.data.objects[f'Wing_{label}_Covert_{i:02d}']
        root=Vector((side*(.59+.07*i),-.23+.06*i,1.66-.045*i)); d=Vector((side*.24,0,-1)).normalized()
        transform_feather(o,root,d,[.78,.73,.66][i],[.25,.25,.23][i])
        # Coverts form a continuous cover over the shaft roots; retain existing 2-bone groups.
        for v in o.data.vertices:
            q=(r.matrix_world.inverted()@o.matrix_world)@v.co; f=max(0,min(1,(1.42-q.z)/.40)); f=f*f*(3-2*f)
            o.vertex_groups['Wing.'+label].add([v.index],1-.65*f,'REPLACE'); o.vertex_groups['WingTip.'+label].add([v.index],.65*f,'REPLACE')
bpy.ops.object.mode_set(mode='EDIT')
for name,root,d,length,obj,t,angle in spec:
    b=r.data.edit_bones.new(name); b.head=root; b.tail=root+d*length; b.align_roll(Vector((0,-1,0))); b.parent=r.data.edit_bones['WingTip.'+name[-1]]; b.use_deform=True
bpy.ops.object.mode_set(mode='OBJECT')
col=r.data.collections.get('Primary Feathers') or r.data.collections.new('Primary Feathers')
for name,root,d,length,obj,t,angle in spec:
    o=bpy.data.objects[obj]
    for vg in list(o.vertex_groups): o.vertex_groups.remove(vg)
    feather=o.vertex_groups.new(name=name); parent=o.vertex_groups.new(name='WingTip.'+name[-1])
    for v,s in zip(o.data.vertices,t):
        f=min(1,max(0,s/.13)); f=f*f*(3-2*f); weight=.65+.35*f
        feather.add([v.index],weight,'REPLACE')
        if weight<1: parent.add([v.index],1-weight,'REPLACE')
    col.assign(r.data.bones[name]); r.pose.bones[name].rotation_mode='QUATERNION'; r.pose.bones[name].color.palette='THEME04'
    r.pose.bones[name]['Fan_Open_Angle_Deg']=angle
rigpose={p.name:p.matrix_basis.copy() for p in r.pose.bones}
def pose(amount,chest=0):
    for n,m in rigpose.items(): r.pose.bones[n].matrix_basis=m
    for n,a in axes.items():
        p=r.pose.bones[n]; p.rotation_mode='QUATERNION'; p.rotation_quaternion=Quaternion(a,math.radians((12 if 'Tip' in n else 58)*amount))
    for name,root,d,length,obj,t,angle in spec: r.pose.bones[name].rotation_quaternion=Quaternion((0,0,1),math.radians(angle*amount))
    if chest: r.pose.bones['Chest'].rotation_mode='QUATERNION'; r.pose.bones['Chest'].rotation_quaternion=Quaternion((1,0,0),math.radians(chest))
    bpy.context.view_layer.update()
poses={}; controls=list(axes)+[s[0] for s in spec]
old_action=r.animation_data.action if r.animation_data else None
if old_action: old_action.use_fake_user=True
for label,amount in [('Closed',0),('Medium',.60),('Wide',1)]:
    pose(amount)
    poses[label]={n:{'rotation_mode':r.pose.bones[n].rotation_mode,'quaternion_wxyz':list(r.pose.bones[n].rotation_quaternion),'rest_matrix':[list(row) for row in r.data.bones[n].matrix_local],'parent':r.data.bones[n].parent.name} for n in controls}
    a=bpy.data.actions.new('WINGS_'+label); a.use_fake_user=True; r.animation_data_create(); r.animation_data.action=a
    for n in controls: r.pose.bones[n].keyframe_insert(data_path='rotation_quaternion',frame=1,group=n)
    a.asset_mark()
r.animation_data.action=None; pose(0)
# Timeline contains only wing channels; reuse the marked frame poses without touching neck animation.
a=bpy.data.actions.new('WINGS_PosePreview'); r.animation_data.action=a
for frame,amount,label in [(1,0,'CLOSED'),(30,.60,'MEDIUM'),(60,1,'WIDE')]:
    pose(amount)
    for n in controls: r.pose.bones[n].keyframe_insert(data_path='rotation_quaternion',frame=frame,group=n)
    bpy.context.scene.timeline_markers.new(label,frame=frame)
bpy.context.scene.frame_start=1; bpy.context.scene.frame_end=60; bpy.context.scene.frame_set(1); pose(0)
(P/'expanded_pose_transforms.json').write_text(json.dumps(poses,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT)
print('EXPANDED_BUILT',OUTPUT)
