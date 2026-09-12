"""Build the original Wildflight hummingbird and export an animated-parts FBX.
Run: blender --background --python tools/create_bird.py
Blender coordinates: +X forward, +Z up. Unity FBX conversion handles Y up.
"""
import bpy, math, random, os
from mathutils import Vector
random.seed(41)
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name, color, metal=0, rough=.4):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    return m
emerald=material('Iridescent emerald',(.035,.27,.15),.48,.3)
teal=material('Jade feathers',(.055,.38,.28),.35,.35)
dark=material('Flight feather charcoal',(.035,.052,.039),.15,.48)
gold=material('Bronze feather edges',(.37,.29,.09),.4,.38)
throat=material('Copper ruby throat',(.53,.11,.045),.5,.3)
belly=material('Warm ivory down',(.63,.59,.42),0,.75)
beak=material('Graphite beak',(.025,.024,.018),.2,.3)
eye=material('Obsidian eyes',(.007,.009,.007),.1,.07)
glint=material('Eye reflection',(.9,.95,.85),0,.12)

def uv(name, loc, scale, mat, parent=None, seg=32, rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    for p in o.data.polygons:p.use_smooth=True
    if parent:o.parent=parent
    return o

root=bpy.data.objects.new('Hummingbird',None); bpy.context.collection.objects.link(root)
uv('Body',(0,0,0),(.62,.255,.32),emerald,root)
uv('Breast',(.18,-.005,-.13),(.4,.237,.235),belly,root)
uv('Head',(.43,0,.28),(.28,.215,.265),emerald,root)
uv('Ruby gorget',(.53,0,.105),(.175,.218,.19),throat,root)
for side in [-1,1]:
    uv('Eye surround',(.53,side*.182,.335),(.078,.023,.08),gold,root)
    uv('Eye',(.542,side*.204,.34),(.054,.016,.057),eye,root)
    uv('Catchlight',(.56,side*.218,.363),(.013,.006,.013),glint,root,16,8)
    uv('Cheek',(.43,side*.196,.215),(.115,.022,.048),belly,root)

def spike(name,start,end,r1,r2,mat,parent):
    d=Vector(end)-Vector(start)
    bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=r1,radius2=r2,depth=d.length,location=(Vector(start)+Vector(end))/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(mat);o.parent=parent
    for p in o.data.polygons:p.use_smooth=True
    return o
spike('Needle beak',(.63,0,.29),(1.25,0,.25),.046,.004,beak,root)

def feather(name,start,end,width,mat,parent):
    a=Vector(start); b=Vector(end); d=b-a
    # Curved, lens-section feather with a raised quill and pointed tip.
    side=d.cross(Vector((0,0,1))).normalized()
    verts=[]; faces=[]
    for i in range(9):
        t=i/8; c=a+d*t+Vector((0,0,.055*math.sin(math.pi*t)))
        w=width*math.sin(math.pi*t)**.7
        verts.extend([c-side*w,c+Vector((0,0,.012*math.sin(math.pi*t))),c+side*w])
    for i in range(8):
        for j in range(2):faces.append((i*3+j,i*3+j+1,(i+1)*3+j+1,(i+1)*3+j))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.materials.append(mat);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.parent=parent
    for p in mesh.polygons:p.use_smooth=True
    sol=o.modifiers.new('Feather thickness','SOLIDIFY');sol.thickness=.006
    return o

for s,label in [(-1,'WingNear'),(1,'WingFar')]:
    wing=bpy.data.objects.new(label,None);bpy.context.collection.objects.link(wing);wing.parent=root
    wing.location=(.0,s*.17,.12)
    for i in range(12):
        t=i/11
        feather('Primary %s %02d'%(label,i),(-.04-t*.21,s*(.08+t*.2),0),(-.25-t*.75,s*(1.1-t*.36),-.03-t*.09),.075,dark if i%3 else gold,wing)
    for row in range(3):
        for i in range(11):
            t=i/10
            feather('Wing covert',(.03-t*.42,s*(.06+row*.15),.025+row*.012),(-.17-t*.44,s*(.32+row*.19),.035),.054,teal if (i+row)%3 else emerald,wing)
for i in range(7):
    t=(i-3)/3
    feather('Tail feather',(-.42,t*.08,-.04),(-1.23+abs(t)*.13,t*.23,-.23),.065,dark if i%2 else teal,root)
# Hundreds of individually layered contour feathers, following the torso.
for row in range(13):
    x=-.48+row*.075
    for i in range(18):
        a=2*math.pi*i/18+row*.12
        f=math.sqrt(max(.05,1-(x/.65)**2))
        y=math.cos(a)*.257*f; z=math.sin(a)*.323*f
        if z<-.08: mat=belly
        else: mat=teal if random.random()<.24 else emerald
        o=uv('Contour feather',(x,y,z),(.065,.028,.012),mat,root,8,6)
        o.rotation_euler.x=a-math.pi/2
for s in [-1,1]:
    spike('Tucked foot',(-.17,s*.12,-.24),(-.39,s*.15,-.32),.018,.013,beak,root)
    for j in range(3):spike('Toe',(-.39,s*.15,-.32),(-.49,s*.15+(j-1)*.035,-.3),.009,.003,beak,root)

# Join static parts by material, keeping wing pivots independent.
for parent in [root,bpy.data.objects['WingNear'],bpy.data.objects['WingFar']]:
    for mat in list(bpy.data.materials):
        children=[o for o in list(parent.children) if o.type=='MESH']
        group=[o for o in children if o.data.materials and o.data.materials[0]==mat]
        if not group:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in group:o.select_set(True)
        bpy.context.view_layer.objects.active=group[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
        bpy.context.object.name=parent.name+'_'+mat.name

# Store an authored wingbeat action as well as runtime-friendly pivots.
for name,s in [('WingNear',-1),('WingFar',1)]:
    o=bpy.data.objects[name]
    for frame,angle in [(1,-35),(4,48),(7,-35)]:
        o.rotation_euler.x=math.radians(angle*s);o.keyframe_insert(data_path='rotation_euler',frame=frame)
bpy.context.scene.frame_set(1)
bpy.context.scene.render.fps=30;bpy.context.scene.frame_end=7
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'Art/Blender/Hummingbird.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=os.path.join(ROOT,'Assets/Resources/Models/Hummingbird.fbx'),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
print('WILDFLIGHT_BIRD_EXPORTED',len(bpy.data.objects))
