import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
parts=[]
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.57
 return m
red=mat('Cherry red fabric',(.73,.012,.035));white=mat('Warm white fabric',(.94,.90,.85));stitch=mat('Red visor seams',(.55,.007,.022));lining=mat('Inner red lining',(.24,.008,.018))
def mesh(name,v,f,m):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.data.materials.append(m)
 for p in me.polygons:p.use_smooth=True
 parts.append(o);return o
def tube(name,points,r,m,closed=False):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=3;s=c.splines.new('POLY');s.points.add(len(points)-1)
 for p,v in zip(s.points,points):p.co=(*v,1)
 s.use_cyclic_u=closed;o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.data.materials.append(m);parts.append(o)
def sphere(name,loc,scale,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=20,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 for p in o.data.polygons:p.use_smooth=True
 parts.append(o)
# Tám múi nối liền, có độ phồng nhẹ để đường ráp hiện tự nhiên.
for panel in range(8):
 v=[];f=[];U=20;V=30
 for j in range(V+1):
  t=j/V*math.pi/2;r=max(.001,math.cos(t));z=.16+1.28*math.sin(t)
  for i in range(U+1):
   a=(panel-.5+i/U)*math.tau/8;bulge=.014*math.sin(math.pi*i/U)*math.cos(t)
   v.append(((1.10*r+bulge)*math.sin(a),-(1.05*r+bulge)*math.cos(a),z))
 for j in range(V):
  for i in range(U):k=j*(U+1)+i;f.append((k,k+1,k+U+2,k+U+1))
 o=mesh('Crown panel %02d'%panel,v,f,white if panel%2==0 else red);o.data.materials.append(lining)
 mod=o.modifiers.new('Fabric shell','SOLIDIFY');mod.thickness=.045;mod.material_offset=1
sphere('Red top button',(0,0,1.47),(.16,.15,.085),red)
tube('Inner opening rim',[(1.075*math.sin(i/128*math.tau),-1.025*math.cos(i/128*math.tau),.165) for i in range(128)],.032,red,True)
# Lưỡi trai uốn xuống ở hai bên, kéo dài phía trước; phần gốc lồng vào thân mũ.
def visor(a,t):
 reach=max(0,math.cos(a/1.48*math.pi/2))**.65
 x=(1.07+.32*t*reach)*math.sin(a)
 y=-(1.02+.94*t*reach)*math.cos(a)
 z=.19-.40*t*reach*(x/1.36)**2-.08*t*reach+.045*math.sin(math.pi*t)*reach
 return (x,y,z)
v=[];f=[];U=80;V=22
for j in range(V+1):
 for i in range(U+1):v.append(visor(-1.48+2.96*i/U,j/V))
for j in range(V):
 for i in range(U):k=j*(U+1)+i;f.append((k,k+U+1,k+U+2,k+1))
o=mesh('Curved red visor',v,f,red);o.data.materials.append(lining)
mod=o.modifiers.new('Visor thickness','SOLIDIFY');mod.thickness=.065;mod.material_offset=1
mod=o.modifiers.new('Soft visor edge','BEVEL');mod.width=.027;mod.segments=3
tube('Rounded visor lip',[visor(-1.48+2.96*i/128,1) for i in range(129)],.033,red)
for t in [.40,.60,.80]:
 pts=[]
 for i in range(121):
  x,y,z=visor(-1.40+2.8*i/120,t);pts.append((x,y,z+.006))
 tube('Visor stitched ridge',pts,.009,red)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH');parts=list(bpy.context.selected_objects)
for o in parts:
 bpy.context.view_layer.objects.active=o
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'BaseballCap_Cartoon.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'BaseballCap_Cartoon.glb'),use_selection=True,export_format='GLB')
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
floor=mat('Cream backdrop',(.83,.70,.48));bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.34));bpy.context.object.name='Studio floor';bpy.context.object.data.materials.append(floor)
bpy.ops.object.camera_add(location=(3.5,-6,2.7));cam=bpy.context.object;aim(cam,(0,-.25,.73));cam.data.type='ORTHO';cam.data.ortho_scale=4.0;bpy.context.scene.camera=cam
for loc,power,size in [((-3,-4,6),650,4),((4,-1,4),350,4),((0,4,5),800,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,.7))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.world.color=(.3,.3,.3)
scene.view_settings.view_transform='Standard';scene.view_settings.exposure=-.65
scene.render.filepath=os.path.join(OUT,'BaseballCap_Preview.png')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=5;area.spaces.active.region_3d.view_location=(0,0,.7)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'BaseballCap_Cartoon.blend'));bpy.ops.render.render(write_still=True)
print('FINISHED',sum(len(o.data.vertices) for o in parts),'vertices',flush=True)


