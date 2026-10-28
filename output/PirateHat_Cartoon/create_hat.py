import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def material(name,color):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Roughness'].default_value=.38
 return m
navy=material('Midnight navy',(.025,.03,.055)); gold=material('Golden trim',(1,.57,.025)); red=material('Scarlet scarf',(.7,.018,.025)); ivory=material('Ivory emblem',(1,.92,.72)); dark=material('Eye sockets',(.012,.009,.015))
parts=[]
def finish(ob,name,mat):
 ob.name=name; ob.data.materials.append(mat); parts.append(ob)
 if ob.type=='MESH':
  for p in ob.data.polygons:p.use_smooth=True
 return ob
def uv(name,loc,scale,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=loc); o=bpy.context.object; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); return finish(o,name,mat)
def tube(name,points,r,mat,closed=False):
 cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.resolution_u=16; cu.bevel_depth=r; cu.bevel_resolution=4
 sp=cu.splines.new('POLY'); sp.points.add(len(points)-1)
 for p,v in zip(sp.points,points):p.co=(*v,1)
 sp.use_cyclic_u=closed
 o=bpy.data.objects.new(name,cu); bpy.context.collection.objects.link(o); return finish(o,name,mat)
def mesh(name,verts,faces,mat,solid=0):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);finish(o,name,mat)
 if solid:
  mod=o.modifiers.new('Fabric thickness','SOLIDIFY');mod.thickness=solid
 return o
# Open-bottom crown, shaped as a smooth dome.
N=96; R=28; verts=[]; faces=[]
for j in range(R):
 t=j/(R-1)*math.pi/2; rad=max(.002,math.cos(t))
 for i in range(N):
  a=i/N*math.tau;verts.append((1.12*rad*math.cos(a),.83*rad*math.sin(a),.16+1.5*math.sin(t)))
for j in range(R-1):
 for i in range(N):
  k=j*N+i;n=j*N+(i+1)%N;faces.append((k,n,n+N,k+N))
mesh('Open crown',verts,faces,navy,.055)
# Continuous folded brim: tall front arch and softer rear arch.
verts=[];faces=[];edge=[]
for j in range(17):
 t=j/16
 for i in range(N):
  a=i/N*math.tau; front=max(0,-math.sin(a));back=max(0,math.sin(a))
  x=(1.08+(2.2-1.08)*t)*math.cos(a)
  y=(.8+(.98-.8)*t)*math.sin(a)
  z=.12+t*(.34+1.72*front**4+1.05*back**4)
  verts.append((x,y,z))
  if j==16:edge.append((x,y,z))
for j in range(16):
 for i in range(N):
  k=j*N+i;n=j*N+(i+1)%N;faces.append((k,n,n+N,k+N))
mesh('Sculpted upturned brim',verts,faces,navy,.095);tube('Thick gold piping',edge,.075,gold,True)
for z,r in [(.13,.105),(.23,.09)]:
 tube('Red head band',[(1.13*math.cos(i/96*math.tau),.84*math.sin(i/96*math.tau),z) for i in range(96)],r,red,True)
# Raised skull on the front, facing negative Y.
uv('Skull',(0,-1.045,1.48),(.32,.095,.34),ivory)
for x in [-.19,-.065,.065,.19]:uv('Rounded tooth',(x,-1.07,1.205),(.068,.08,.105),ivory)
for x in [-.125,.125]:uv('Inset eye',(x,-1.132,1.51),(.077,.019,.097),dark)
uv('Nose',(0,-1.142,1.35),(.038,.014,.047),dark)
for sign in [-1,1]:
 p1=Vector((-.39,-1.08,.79 if sign==1 else 1.04));p2=Vector((.39,-1.08,1.04 if sign==1 else .79))
 tube('Cross bone',[p1.lerp(p2,i/20) for i in range(21)],.043,ivory)
 for p in [p1,p2]:
  for dz in [-.035,.035]:uv('Bone tip',(p.x,p.y-.008,p.z+dz),(.064,.055,.055),ivory)
uv('Scarf knot',(1.05,-.56,.16),(.19,.18,.21),red)
for idx,(dx,dy) in enumerate([(.13,-.07),(.53,.02)]):
 vs=[];fs=[]
 for j in range(25):
  t=j/24; width=.13*math.sin(math.pi*t)**.65+.025*(1-t)
  for k in range(7):
   s=k/6*2-1;vs.append((1.07+dx*t+s*width,-.58+dy*t-.04*(1-s*s)*math.sin(math.pi*t),.1-.62*t+.05*s*s*math.sin(math.pi*t)))
 for j in range(24):
  for k in range(6):q=j*7+k;fs.append((q,q+1,q+8,q+7))
 o=mesh('Scarf tail '+str(idx),vs,fs,red,.045);mod=o.modifiers.new('Soft cloth','SUBSURF');mod.levels=2
# Convert exported parts to meshes and apply modifiers; preserve separate named pieces.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH')
parts=list(bpy.context.selected_objects)
for o in parts:
 bpy.context.view_layer.objects.active=o
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'PirateHat_Cartoon.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'PirateHat_Cartoon.glb'),use_selection=True,export_format='GLB')
ground=material('Backdrop',(.76,.63,.43));bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.6));bpy.context.object.name='Studio floor';bpy.context.object.data.materials.append(ground)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-8,3));cam=bpy.context.object;aim(cam,(0,0,.85));cam.data.type='ORTHO';cam.data.ortho_scale=5.6;bpy.context.scene.camera=cam
for loc,power,size in [((-3,-4,6),800,5),((4,-1,4),650,4),((0,4,5),1000,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,.7))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world.color=(.3,.3,.3);scene.render.image_settings.file_format='PNG';scene.render.filepath=os.path.join(OUT,'PirateHat_Preview.png')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'PirateHat_Cartoon.blend'))
bpy.ops.render.render(write_still=True)
print('DONE PIRATE HAT',sum(len(o.data.vertices) for o in parts),'vertices')
