import bpy,sys
from pathlib import Path
from mathutils import Vector
P=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckAnimation'); s=bpy.context.scene
s.render.engine='BLENDER_WORKBENCH'; s.render.resolution_x=640; s.render.resolution_y=640; s.render.resolution_percentage=100; s.render.image_settings.file_format='PNG'
s.display.shading.light='STUDIO'; s.display.shading.color_type='MATERIAL'; s.display.shading.show_shadows=True; s.display.shading.show_cavity=True; s.display.shading.background_type='WORLD'
if not s.world: s.world=bpy.data.worlds.new('Preview World')
s.world.color=(.10,.10,.10)
c=bpy.data.objects.new('Preview',bpy.data.cameras.new('Preview')); s.collection.objects.link(c); s.camera=c; c.data.type='ORTHO'; c.data.ortho_scale=4.4
frames=[1,30,66,102,144,158,171,177,183,201,216,231,255]
if '--' in sys.argv: frames=[int(x) for x in sys.argv[sys.argv.index('--')+1:]]
for f in frames:
 s.frame_set(f)
 for view,loc in [('front',(0,-8,2)),('angle',(5,-8,2.8))]:
  c.location=loc; c.rotation_euler=(Vector((0,0,1.8))-c.location).to_track_quat('-Z','Y').to_euler(); s.render.filepath=str(P/f'{view}_{f:03d}.png'); bpy.ops.render.render(write_still=True)
