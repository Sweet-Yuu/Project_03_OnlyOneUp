exec(compile(open('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckNeckIK/test_rig.py',encoding='utf-8').read(),'test_rig.py','exec'))
scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH'
scene.render.resolution_x=760; scene.render.resolution_y=760; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.display.shading.light='STUDIO'; scene.display.shading.color_type='MATERIAL'
scene.display.shading.show_shadows=True; scene.display.shading.show_cavity=True
scene.display.shading.background_type='WORLD'; scene.world.color=(.12,.12,.12)
for o in bpy.data.objects:
    if o.name=='Ground' or o.name.startswith('WGT_'): o.hide_render=True
cam=bpy.data.objects.new('QA_Camera',bpy.data.cameras.new('QA_Camera')); scene.collection.objects.link(cam)
cam.location=(6,-3.4,3.1); cam.rotation_euler=(Vector((0,-.3,1.9))-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO'; cam.data.ortho_scale=4.4; scene.camera=cam
for label,t,r,b in cases:
    pose(t,r,b)
    scene.render.filepath=str(OUT/(label+'.png')); bpy.ops.render.render(write_still=True)
reset()
