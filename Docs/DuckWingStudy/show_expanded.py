import bpy,math
from mathutils import Quaternion,Vector
area=bpy.context.area
def finish():
 space=area.spaces.active
 space.shading.type='SOLID'; space.shading.color_type='MATERIAL'; space.overlay.show_overlays=False
 space.region_3d.view_location=Vector((0,0,1.8)); space.region_3d.view_distance=5.0; space.region_3d.view_rotation=Quaternion((1,0,0),math.radians(90)); space.region_3d.view_perspective='ORTHO'
 bpy.context.scene.frame_set(1)
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
 return None
area.type='VIEW_3D'
bpy.app.timers.register(finish,first_interval=.4)
