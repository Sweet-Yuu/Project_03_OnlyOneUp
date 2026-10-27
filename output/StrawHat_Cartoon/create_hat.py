import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
parts=[]
def mat(n,c):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.5
 return m
straw=mat('Honey straw',(1,.57,.12));light=mat('Light straw',(1,.65,.20));base=mat('Deep straw',(.72,.37,.055));red=mat('Scarlet ribbon',(.72,.008,.025))
def patch(n,fn,U,V,m,thick=0):
 vs=[fn(i/U,j/V) for j in range(V+1) for i in range(U+1)];fs=[]
 for j in range(V):
  for i in range(U):k=j*(U+1)+i;fs.append((k,k+1,k+U+2,k+U+1))
 if n.startswith('Brim'):fs=[tuple(reversed(f)) for f in fs]
 me=bpy.data.meshes.new(n);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(n,me);bpy.context.collection.objects.link(o);o.data.materials.append(m)
 for p in me.polygons:p.use_smooth=True
 if thick:
  mod=o.modifiers.new('Straw thickness','SOLIDIFY');mod.thickness=thick
  mod=o.modifiers.new('Soft strip edges','BEVEL');mod.width=.008;mod.segments=2
 parts.append(o);return o
def tube(n,pts,r,m):
 c=bpy.data.curves.new(n,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=4;s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,v in zip(s.points,pts):p.co=(*v,1)
 s.use_cyclic_u=True;o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o);o.data.materials.append(m);parts.append(o)
K=24;T=8;step=math.pi/2/T
def crown(a,t,h=0):
 r=math.cos(t);return ((1.1*r+h*math.cos(t))*math.cos(a),(1.04*r+h*math.cos(t))*math.sin(a),.13+1.18*math.sin(t)+h*math.sin(t))
patch('Hollow straw shell',lambda u,v:crown(u*math.tau,v*math.pi/2),96,32,base,.055)
# Hai hệ nan có độ nâng ngược nhau tại mỗi giao điểm để tạo mặt đan thật.
for k in range(K):
 a=k*math.tau/K
 patch('Crown vertical strip %02d'%k,lambda u,v,a=a,k=k:crown(a+(u-.5)*math.tau/K*.96,.02+v*(math.pi/2-.07),.025+.013*math.cos(math.pi*((.02+v*(math.pi/2-.07))/step-.5+k))),4,64,light if k%3==0 else straw,.018)
for j in range(T):
 t=(j+.5)*step
 patch('Crown woven ring %02d'%j,lambda u,v,t=t,j=j:crown(u*math.tau,t+(v-.5)*step*.94,.026-.014*math.cos(math.pi*(u*K+j))),192,4,straw if j%2 else light,.018)
# Lỗ đội đầu được giữ hở; vành chỉ là một vòng khuyên.
def brim(a,r,h=0):return (r*math.cos(a),.95*r*math.sin(a),.14-.11*(r-1.02)+.035*math.cos(2*a)*(r-1.02)+h)
patch('Brim base',lambda u,v:brim(u*math.tau,1.02+.77*v),128,12,base,.065)
BK=40;BR=4
for k in range(BK):
 a=k*math.tau/BK
 patch('Brim radial weave %02d'%k,lambda u,v,a=a,k=k:brim(a+(u-.5)*math.tau/BK*.88,1.02+.77*v,.024+.011*math.cos(math.pi*(v*BR+k))),4,24,light if k%3==0 else straw,.025)
for j in range(BR):
 r=1.02+.77*(j+.5)/BR
 patch('Brim circular weave %02d'%j,lambda u,v,r=r,j=j:brim(u*math.tau,r+(v-.5)*.77/BR*.88,.025-.012*math.cos(math.pi*(u*BK+j+.5))),240,4,straw,.024)
tube('Rolled straw edge',[brim(i/160*math.tau,1.79,.005) for i in range(160)],.063,light)
# Các vòng buộc ôm mép vành.
for k in range(36):
 a=k*math.tau/36
 patch('Rim binding %02d'%k,lambda u,v,a=a:brim(a+(u-.5)*.07,1.79+.066*math.cos(v*math.tau),.005+.066*math.sin(v*math.tau)),4,16,straw,.008)
def band(u,v):
 z=.16+.27*v;t=math.asin((z-.13)/1.18);return crown(u*math.tau,t,.068)
o=patch('Red fabric band',band,128,12,red,.035)
# Xuất model ở tư thế thẳng, pivot tại tâm lỗ đội đầu.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH');parts=list(bpy.context.selected_objects)
for o in parts:
 bpy.context.view_layer.objects.active=o
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'StrawHat_Cartoon.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'StrawHat_Cartoon.glb'),use_selection=True,export_format='GLB')
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
floor=mat('Cream studio',(.8,.66,.43));bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.1));bpy.context.object.name='Studio floor';bpy.context.object.data.materials.append(floor)
bpy.ops.object.camera_add(location=(3,-7,2.5));cam=bpy.context.object;aim(cam,(0,0,.65));cam.data.type='ORTHO';cam.data.ortho_scale=4.6;bpy.context.scene.camera=cam
for loc,power,size in [((-3,-4,6),650,4),((4,-1,4),300,4),((0,4,5),800,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,.7))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.world.color=(.3,.3,.3)
scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.75
scene.render.filepath=os.path.join(OUT,'StrawHat_Preview.png')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=6;area.spaces.active.region_3d.view_location=(0,0,.6)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'StrawHat_Cartoon.blend'));bpy.ops.render.render(write_still=True)
print('FINISHED',sum(len(o.data.vertices) for o in parts),'vertices',flush=True)

