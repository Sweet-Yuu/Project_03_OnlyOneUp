import bpy,math
from mathutils import Quaternion,Vector
area=bpy.context.area
def finish():
 sp=area.spaces.active; sp.shading.type='SOLID'; sp.shading.color_type='MATERIAL'; sp.overlay.show_overlays=False
 sp.region_3d.view_location=Vector((0,0,1.8)); sp.region_3d.view_distance=5.5; sp.region_3d.view_rotation=Quaternion((1,0,0),math.radians(90)); sp.region_3d.view_perspective='ORTHO'
 bpy.context.scene.frame_set(1); bpy.context.scene.sync_mode='FRAME_DROP'
 for a in bpy.context.screen.areas:
  if a.type=='DOPESHEET_EDITOR':
   with bpy.context.temp_override(area=a,region=next(r for r in a.regions if r.type=='WINDOW')):
    bpy.ops.action.view_all()
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
 return None
area.type='VIEW_3D'; bpy.app.timers.register(finish,first_interval=.5)
