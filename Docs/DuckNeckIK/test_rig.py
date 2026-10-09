import bpy, json, math
from pathlib import Path
from mathutils import Vector, Matrix, Euler
OUT=Path('D:/AGU_Project/Project_03_OnlyOneUp/Docs/DuckNeckIK')
rig=bpy.data.objects['Duck_Rig']; p=rig.pose.bones
names=['Neck_01','Neck_02','Neck_03']; beak=bpy.data.objects['Beak']
contact=Vector(rig['BeakContactRest']); bases={b.name:b.matrix_basis.copy() for b in p}
resthead=rig.data.bones['Head'].matrix_local.inverted() @ contact
def reset():
    for n,m in bases.items(): p[n].matrix_basis=m
    bpy.context.view_layer.update()
def pose(target,rotation=(0,0,0),body=0):
    reset(); mat=Euler(rotation,'XYZ').to_matrix().to_4x4() @ rig.data.bones['CTRL_Beak'].matrix_local
    mat.translation=Vector(target); p['CTRL_Beak'].matrix=mat
    bm=p['CTRL_Body'].matrix.copy(); bm.translation.z+=body; p['CTRL_Body'].matrix=bm
    bpy.context.view_layer.update()
def record(label):
    dg=bpy.context.evaluated_depsgraph_get(); er=rig.evaluated_get(dg); ep=er.pose.bones
    actual=ep['Head'].matrix @ resthead; target=ep['CTRL_Beak'].head
    ev=beak.evaluated_get(dg); mesh=ev.to_mesh(); v=rig.matrix_world.inverted() @ ev.matrix_world @ mesh.vertices[rig['BeakContactVertex']].co; ev.to_mesh_clear()
    return {'name':label,'error':(actual-target).length,'mesh_error':(v-target).length,'target':list(target),'actual':list(actual),'lengths':[(ep[n].tail-ep[n].head).length for n in names],'joints':[list(ep[n].head) for n in names]+[list(ep['Neck_03'].tail)],'root':list(ep['Neck_01'].head),'chest':list(ep['Chest'].head)}
cases=[('rest',contact,(0,0,0),0),('forward',(0,-1.48,2.55),(0,0,0),0),('low',(0,-1.42,1.75),(0,0,0),0),('high',(0,-.94,2.89),(0,0,0),0),('left',(-.45,-1.25,2.55),(0,0,0),0),('right',(.45,-1.25,2.55),(0,0,0),0),('body_up',(0,-1.3,2.5),(0,0,0),.35),('body_down',(0,-1.3,2.5),(0,0,0),-.2),('rotate',(0,-1.3,2.5),(.2,.15,.25),0),('unreachable',(0,-4,5),(0,0,0),0)]
report=[]
for label,t,r,b in cases: pose(t,r,b); report.append(record(label))
reset(); (OUT/'test_report.json').write_text(json.dumps(report,indent=2))
print([(r['name'],round(r['error'],6)) for r in report])
