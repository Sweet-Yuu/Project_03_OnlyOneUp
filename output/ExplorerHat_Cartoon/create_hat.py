import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
parts=[]
def mat(name,c):
 m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.44
 return m
sand=mat('Sand canvas',(.66,.365,.135));seam=mat('Raised canvas seams',(.79,.49,.23));brown=mat('Chocolate leather',(.17,.052,.023));gold=mat('Golden buckle',(1,.57,.012));inside=mat('Inner lining',(.26,.115,.035))
def mesh(name,v,f,m):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.data.materials.append(m)
 for p in me.polygons:p.use_smooth=True
 parts.append(o);return o
def tube(name,pts,r,m,closed=False):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=3
 s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,v in zip(s.points,pts):p.co=(*v,1)
 s.use_cyclic_u=closed;o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.data.materials.append(m);parts.append(o);return o
def sphere(name,loc,scale,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=24,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 for p in o.data.polygons:p.use_smooth=True
 parts.append(o);return o
N=96
# Vòm mở đáy để có thể đội lên đầu nhân vật.
v=[];f=[];R=32
for j in range(R):
 t=j/(R-1)*math.pi/2;r=max(.001,math.cos(t))
 for i in range(N):
  a=i/N*math.tau;v.append((1.15*r*math.cos(a),1.02*r*math.sin(a),.22+1.48*math.sin(t)))
for j in range(R-1):
 for i in range(N):k=j*N+i;n=j*N+(i+1)%N;f.append((k,n,n+N,k+N))
o=mesh('Hollow helmet crown',v,f,sand);o.data.materials.append(inside);mod=o.modifiers.new('Canvas shell','SOLIDIFY');mod.thickness=.065;mod.material_offset=1
# Vành có tiết diện kín, bo tròn ở mép ngoài và giữ lỗ đội đầu.
profile=[(.94,.27),(1.12,.2),(1.28,.14),(1.45,.045),(1.55,-.045),(1.57,-.09),(1.55,-.135),(1.50,-.15),(1.35,-.08),(1.15,.015),(.94,.06)]
v=[];f=[]
for r,z in profile:
 for i in range(N):
  a=i/N*math.tau;v.append((1.15*r*math.cos(a),1.02*r*math.sin(a),z+.035*math.cos(2*a)))
for j in range(len(profile)):
 for i in range(N):f.append((j*N+i,j*N+(i+1)%N,((j+1)%len(profile))*N+(i+1)%N,((j+1)%len(profile))*N+i))
o=mesh('Rounded oval brim',v,f,sand);mod=o.modifiers.new('Rounded brim finish','SUBSURF');mod.levels=2
# Các đường nổi chia múi trên vòm.
for a in [i*math.tau/6 for i in range(6)]:
 pts=[]
 for j in range(65):
  t=.20+(math.pi/2-.20)*j/64;r=math.cos(t)
  pts.append(((1.15*r+.008)*math.cos(a),(1.02*r+.008)*math.sin(a),.22+1.48*math.sin(t)+.008))
 tube('Panel seam',pts,.013,seam)
sphere('Top button',(0,0,1.73),(.205,.19,.105),sand)
# Đai theo độ cong vòm, dày và bo cạnh.
v=[];f=[]
for j in range(9):
 t=j/8;z=.245+.29*t;r=math.sqrt(1-((z-.22)/1.48)**2)+.018
 for i in range(N):a=i/N*math.tau;v.append((1.15*r*math.cos(a),1.02*r*math.sin(a),z))
for j in range(8):
 for i in range(N):k=j*N+i;n=j*N+(i+1)%N;f.append((k,n,n+N,k+N))
o=mesh('Leather band',v,f,brown);mod=o.modifiers.new('Leather thickness','SOLIDIFY');mod.thickness=.035
mod=o.modifiers.new('Soft leather edges','BEVEL');mod.width=.025;mod.segments=3
# Khóa tròn phía trước, nền da nâu ở giữa.
sphere('Buckle leather center',(0,-1.074,.47),(.19,.065,.22),brown)
tube('Round gold buckle',[(.238*math.cos(i/96*math.tau),-1.12,.47+.265*math.sin(i/96*math.tau)) for i in range(96)],.058,gold,True)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH');parts=list(bpy.context.selected_objects)
for o in parts:
 bpy.context.view_layer.objects.active=o
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'ExplorerHat_Cartoon.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'ExplorerHat_Cartoon.glb'),use_selection=True,export_format='GLB')
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
floor=mat('Studio cream',(.78,.66,.45));bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.22));bpy.context.object.name='Studio floor';bpy.context.object.data.materials.append(floor)
bpy.ops.object.camera_add(location=(3,-7,2.6));camera=bpy.context.object;aim(camera,(0,0,.73));camera.data.type='ORTHO';camera.data.ortho_scale=4.75;bpy.context.scene.camera=camera
for loc,power,size in [((-3,-4,6),650,5),((4,-1,4),450,4),((0,4,5),850,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,.7))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.world.color=(.3,.3,.3)
scene.render.filepath=os.path.join(OUT,'ExplorerHat_Preview.png')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=6;area.spaces.active.region_3d.view_location=(0,0,.7)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'ExplorerHat_Cartoon.blend'));bpy.ops.render.render(write_still=True)
print('FINISHED',sum(len(o.data.vertices) for o in parts),'vertices',flush=True)

