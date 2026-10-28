import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
parts=[]
def mat(n,c,rough=.43):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=rough
 return m
pink=mat('Petal pink',(1,.24,.38));pink2=mat('Soft pink',(1,.29,.43));yellow=mat('Golden flower center',(1,.48,.012));pollen=mat('Pollen gold',(1,.69,.055));green=mat('Stem green',(.23,.46,.045));leafmat=mat('Leaf green',(.28,.51,.065));leaflight=mat('Young leaf green',(.36,.58,.075))
def mesh(n,vs,fs,m):
 me=bpy.data.meshes.new(n);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(n,me);bpy.context.collection.objects.link(o);o.data.materials.append(m)
 for p in me.polygons:p.use_smooth=True
 parts.append(o);return o
def sphere(n,loc,scale,m,seg=32,rings=20):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc);o=bpy.context.object;o.name=n;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 for p in o.data.polygons:p.use_smooth=True
 parts.append(o);return o
def tube(n,points,r,m):
 c=bpy.data.curves.new(n,'CURVE');c.dimensions='3D';c.resolution_u=24;c.bevel_depth=r;c.bevel_resolution=5;c.use_fill_caps=True
 s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
 for p,v in zip(s.bezier_points,points):p.co=v;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
 o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o);o.data.materials.append(m);parts.append(o)
# Tạo khối cánh kín, rãnh giữa là biến dạng bề mặt chứ không phải vạch màu.
def organic(n,start,direction,length,width,depth,m,leaf=False):
 d=Vector(direction).normalized();normal=Vector((0,-1,.12));normal=(normal-d*normal.dot(d)).normalized();side=d.cross(normal).normalized();start=Vector(start)
 vs=[];fs=[];U=48;V=36
 for j in range(V+1):
  t=j/V;s=math.sin(math.pi*t);w=width*s**(.85 if leaf else .62)*(1+.16*t)
  for i in range(U):
   a=math.tau*i/U;cross=math.cos(a);front=math.sin(a)
   bulge=depth*s**.65*front
   if front>0:bulge-=depth*.43*math.exp(-(cross/.13)**2)*s**.6*front**5
   curve=(.035 if leaf else .025)*math.sin(math.pi*t)
   p=start+d*(length*t)+side*(w*cross)+normal*(bulge+curve)
   vs.append(tuple(p))
 for j in range(V):
  for i in range(U):k=j*U+i;nn=j*U+(i+1)%U;fs.append((k,nn,nn+U,k+U))
 o=mesh(n,vs,fs,m)
 mod=o.modifiers.new('Soft organic surface','SUBSURF');mod.levels=1
 # Gộp các đỉnh trùng ở hai đầu để cánh và lá là khối kín.
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.0001);bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
 return o
C=Vector((-.18,0,2.06));vertical=Vector((0,.16,.987));horizontal=Vector((1,0,0))
tube('Curved flower stem',[(.09,.08,.03),(.075,.08,.43),(-.015,.08,.94),(-.15,.055,1.52),tuple(C)],.105,green)
sphere('Rounded stem foot',(.09,.08,.06),(.105,.105,.065),green)
sphere('Green calyx',C+Vector((0,.12,0)),(.29,.16,.28),green)
for k in range(6):
 a=math.radians(15+k*60);d=horizontal*math.cos(a)+vertical*math.sin(a)
 organic('Pink petal %02d'%(k+1),C+d*.22,d,1.00,.41,.245,pink if k%2 else pink2)
sphere('Golden domed center',C+Vector((0,-.19,.025)),(.405,.245,.38),yellow,48,28)
# Répartition en spirale des petits grains sur la face visible du cœur.
for i in range(23):
 r=.91*math.sqrt((i+.5)/23);a=i*2.39996323;x=.405*r*math.cos(a);z=.38*r*math.sin(a);y=-.19-.245*math.sqrt(1-r*r)
 sphere('Pollen grain %02d'%i,C+Vector((x,y-.006,z+.025)),(.034,.026,.034),pollen,16,10)
organic('Left leaf',(.06,.07,.30),(-.79,-.03,.62),1.0,.255,.105,leafmat,True)
organic('Right leaf',(.025,.085,.64),(.76,.005,.62),.85,.22,.10,leaflight,True)
# Xuất riêng phần model, giữ pivot ở chân thân cây.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH');parts=list(bpy.context.selected_objects)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'PinkFlower_Cartoon.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'PinkFlower_Cartoon.glb'),use_selection=True,export_format='GLB')
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
floor=mat('Cream studio',(.83,.69,.46));bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015));bpy.context.object.name='Studio floor';bpy.context.object.data.materials.append(floor)
bpy.ops.object.camera_add(location=(.8,-7,3.3));cam=bpy.context.object;aim(cam,(-.12,0,1.52));cam.data.type='ORTHO';cam.data.ortho_scale=3.8;bpy.context.scene.camera=cam
for loc,power,size in [((-3,-4,6),600,4),((4,-1,4),300,4),((0,4,5),750,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,1.5))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.world.color=(.3,.3,.3)
scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.7
scene.render.filepath=os.path.join(OUT,'PinkFlower_Preview.png')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=5;area.spaces.active.region_3d.view_location=(0,0,1.5)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'PinkFlower_Cartoon.blend'));bpy.ops.render.render(write_still=True)
print('FINISHED',sum(len(o.data.vertices) for o in parts),'vertices',flush=True)

