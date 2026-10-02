"""Reproducible, Lab-only adaptations of licensed assets. Run with Blender 4.5.
Original downloads stay under artifacts/asset-search-r1/downloads. Never edits them.
"""
import bpy, pathlib, math, json, sys, bmesh
from mathutils import Vector, Matrix

ROOT=pathlib.Path(__file__).resolve().parents[1]
SRC=ROOT/'artifacts/asset-search-r1/downloads'
OUT=ROOT/'assets/third_party/visual_target'
OUT.mkdir(parents=True,exist_ok=True)
REPORT=[]

def material(name,color,rough=.8,texture=None):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough
    p.inputs['Specular IOR Level'].default_value=.25
    if texture:
        img=bpy.data.images.load(str(texture),check_existing=True)
        # The Lab needs clean albedo only, without film-specific shader groups or SSS.
        if max(img.size)>1024:img.scale(1024,1024)
        t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=img;m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    return m

def export(objects,name):
    if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.hide_set(False);o.hide_viewport=False;o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True,export_animations=False,export_cameras=False,export_lights=False,export_extras=True)
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects if o.type=='MESH')
    bounds=[o.matrix_world@v.co for o in objects if o.type=='MESH' for v in o.data.vertices]
    REPORT.append(dict(name=name,triangles=tris,objects=len(objects),min=[min(v[i] for v in bounds) for i in range(3)],max=[max(v[i] for v in bounds) for i in range(3)]))

def pose_asset(r,name,amount=1):
    action=bpy.data.actions.get(name)
    if not action:return
    for f in action.fcurves:
        try:
            arr=r.path_resolve(f.data_path)
            arr[f.array_index]=arr[f.array_index]*(1-amount)+f.evaluate(1)*amount
        except (ValueError,TypeError,IndexError,AttributeError):pass

def aim_fk(r,name,direction):
    b=r.pose.bones[name];bpy.context.view_layer.update()
    current=b.matrix.copy();q=current.to_quaternion();delta=(q@Vector((0,1,0))).rotation_difference(Vector(direction).normalized())
    b.matrix=Matrix.Translation(current.translation)@(delta@q).to_matrix().to_4x4()
    bpy.context.view_layer.update()

def snow(kind):
    bpy.ops.wm.open_mainfile(filepath=str(SRC/'snow/Snow/snow_v4.2.blend'),load_ui=False,use_scripts=False)
    if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
    r=bpy.data.objects['RIG-Snow'];r.animation_data.action=None
    props=r.pose.bones['Properties']
    for side in ['left','right']:
        props['ik_'+side+'_upperarm']=0.0;props['ik_'+side+'_thigh']=0.0
    pose_asset(r,'Mouth Opensmile' if kind=='customer' else 'Mouth Teethsmile',.55)
    pose_asset(r,'Eyemask Relaxed',.65 if kind=='customer' else .40)
    pose_asset(r,'Hand Relaxed',.8)
    if kind=='customer':
        for side,sign in [('L',1),('R',-1)]:
            aim_fk(r,'FK-Thigh.'+side,(sign*.1,-1,-.10))
            aim_fk(r,'FK-Knee.'+side,(0,.08,-1))
            aim_fk(r,'FK-Foot.'+side,(0,-1,0))
            aim_fk(r,'FK-UpperArm.'+side,(sign*.42,.08,-1))
            aim_fk(r,'FK-Forearm.'+side,(0,-1,-.25))
    elif kind=='teammate':
        aim_fk(r,'FK-UpperArm.L',(.45,-.3,-.7));aim_fk(r,'FK-Forearm.L',(-.15,-.45,.85))
        aim_fk(r,'FK-UpperArm.R',(-.2,0,-1));aim_fk(r,'FK-Forearm.R',(0,-.5,-.8))
        pose_asset(r,'Hand Fist',.55)
    else:pose_asset(r,'Hand Fist',.6)
    bpy.context.view_layer.update()
    tex=SRC/'snow/Snow/textures'
    skin=[material('Snow_Skin_'+str(i),(1,1,1),.87,tex/('skin_diffuse.'+str(i)+'.png')) for i in [1001,1002,1003]]
    cloth=material('Salon_Cream',(.63,.56,.44),.94)
    pants=material('Salon_Teal',(.022,.07,.066),.9)
    shoes=material('Salon_Charcoal',(.025,.028,.026),.8)
    eye=material('Snow_Eyes',(1,1,1),.34,tex/'eyes_diffuse.png')
    brow=material('Snow_Brows',(.045,.022,.013),.93)
    teeth=material('Snow_Teeth',(.8,.76,.63),.6)
    gums=material('Snow_Mouth',(.24,.045,.028),.85)
    generated=[]
    selected=['GEO-snow-body','GEO-snow-head','GEO-snow-eyebrows','GEO-snow-eyes','GEO-snow-teeth_lower','GEO-snow-teeth_upper','GEO-snow-tongue','GEO-snow-gums_lower','GEO-snow-gums_upper','GEO-snow-shirt','GEO-snow-pants','GEO-snow-shoes_base','GEO-snow-shoes_bottom','GEO-snow-shoes_parts']
    if kind=='fps':selected=['GEO-snow-body']
    for name in selected:
        o=bpy.data.objects[name]
        if o.animation_data:
            for driver in o.animation_data.drivers:
                if 'levels' in driver.data_path or 'show_viewport' in driver.data_path:driver.mute=True
        for mod in o.modifiers:
            if mod.type=='SUBSURF':
                level=1 if name.endswith(('body','head','shirt')) else 0
                mod.show_viewport=True;mod.levels=level;mod.render_levels=level
            elif mod.type in ['CORRECTIVE_SMOOTH','LATTICE']:mod.show_viewport=mod.show_render
        bpy.context.view_layer.update()
        deps=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(deps)
        mesh=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=deps)
        print('EVALUATED_MESH',kind,name,len(mesh.vertices),len(mesh.polygons))
        if 'shirt' in name:
            # Preserve the authored cloth; a small shell offset clears the posed shoulders.
            for v in mesh.vertices:v.co+=v.normal*.009
        obj=bpy.data.objects.new(name.replace('GEO-snow','Snow')+'_'+kind,mesh);bpy.context.scene.collection.objects.link(obj)
        # Strip the rig after evaluating the pose. Original rig and face controls remain in the source blend.
        obj['asset_source']='https://studio.blender.org/characters/snow/v4/';obj['license']='CC-BY-4.0';obj['adaptation']='Lab pose bake; no production character controller'
        mesh.materials.clear()
        if name.endswith(('body','head')):
            for m in skin:mesh.materials.append(m)
            uv=mesh.uv_layers.get('UVMap') or mesh.uv_layers.active
            for layer in list(mesh.uv_layers):
                if layer!=uv:mesh.uv_layers.remove(layer)
            if uv:mesh.uv_layers.active=uv;uv.active_render=True
            if uv:
                for p in mesh.polygons:
                    tile=max(0,min(2,int(sum(uv.data[i].uv.x for i in p.loop_indices)/len(p.loop_indices))))
                    p.material_index=tile
                    for i in p.loop_indices:uv.data[i].uv.x-=tile
        else:
            m=eye if name.endswith('eyes') else brow if 'eyebrow' in name else teeth if 'teeth' in name else gums if any(x in name for x in ['gum','tongue']) else cloth if 'shirt' in name else pants if 'pants' in name else shoes
            mesh.materials.append(m)
            for p in mesh.polygons:p.material_index=0
        for p in mesh.polygons:p.use_smooth=True
        if kind=='fps':
            bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.x>-.39],context='VERTS');bm.to_mesh(mesh);bm.free()
            for v in mesh.vertices:
                v.co-=Vector((-.73,0,1.33))
                # Existing forearm continues outside the FPS frame; grip stays unchanged.
                if v.co.x>.08:v.co.x=.08+(v.co.x-.08)*3.7
        else:
            head=any(x in name for x in ['head','eyes','eyebrow','teeth','tongue','gum'])
            for v in mesh.vertices:
                p=v.co.copy();factor=1.25 if kind=='customer' else 1.10
                headfactor=2.25 if kind=='customer' else 1.70
                if head:
                    w=max(0,min(1,(p.z-1.43)/.09));f=factor+(headfactor-factor)*w
                    p=Vector((p.x*f,p.y*f,(p.z-1.45)*f+1.45*factor))
                else:
                    p*=factor
                    p.x*=1.12
                if kind=='customer':p.z-=.30
                v.co=p
        generated.append(obj)
    if kind=='teammate':
        socket=bpy.data.objects.new('ClipperSocket',None);bpy.context.scene.collection.objects.link(socket)
        socket.location=r.pose.bones['Wrist.L'].head*1.1+Vector((0,-.015,.04));socket.location.x*=1.12;generated.append(socket)
    export(generated,'snow_'+kind)

def home():
    folder=SRC/'home/Ultimate House Interior Pack - June 2020/Blends'
    names=['Couch_Small2','Chair_2','Chair_3','Kitchen_3Drawers','Shelf_1','Bathroom_Mirror2','Light_Ceiling6','Houseplant_3','Stool','Carpet_Round']
    for name in names:
        bpy.ops.wm.open_mainfile(filepath=str(folder/(name+'.blend')),load_ui=False,use_scripts=False)
        objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
        # Existing asset topology only: bevel treatment unifies the imported pack.
        for o in objects:
            bpy.context.view_layer.objects.active=o;o.select_set(True)
            bevel=o.modifiers.new('Salon soft edges','BEVEL');bevel.width=.025;bevel.segments=3
            try:bpy.ops.object.modifier_apply(modifier=bevel.name)
            except RuntimeError:pass
            for m in o.data.materials:
                if m:
                    m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
                    if p:p.inputs['Roughness'].default_value=.82
        export(objects,'home_'+name.lower())

def alpaca():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SRC/'Alpaca.gltf'))
    objects=[o for o in bpy.context.scene.objects if o.type=='ARMATURE' or (o.type=='MESH' and len(o.data.materials)>0)]
    for o in objects:
        if o.type=='MESH':
            # glTF splits hard-normal/material boundaries. Weld before subdivision,
            # otherwise each original face shrinks into a disconnected tile.
            bm=bmesh.new();bm.from_mesh(o.data)
            bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001)
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
            for v in o.data.vertices:
                w=max(0,min(1,(v.co.z-3.8)/.4))
                v.co.x*=1+w
                v.co.y=-2.05+(v.co.y+2.05)*(1+w) if w else v.co.y
                if v.co.z>4.8:v.co.z=4.8+(v.co.z-4.8)*4
            eye_ids=set()
            for p in o.data.polygons:
                if o.data.materials[p.material_index].name.startswith('Eyes_'):eye_ids.update(p.vertices)
            for side in [-1,1]:
                ids=[i for i in eye_ids if o.data.vertices[i].co.x*side>0]
                if not ids:continue
                center=sum((o.data.vertices[i].co for i in ids),Vector())/len(ids)
                for i in ids:
                    p=o.data.vertices[i].co
                    p.x=center.x+(p.x-center.x)*2.5;p.z=center.z+(p.z-center.z)*2.5;p.y-=.015
            # Smooth the licensed body, retaining its rig.
            for p in o.data.polygons:p.use_smooth=True
            sub=o.modifiers.new('Soft body','SUBSURF');sub.levels=1
            bpy.context.view_layer.objects.active=o
            bpy.ops.object.modifier_apply(modifier=sub.name)
    export(objects,'quaternius_alpaca')

def chair():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SRC/'polyhaven-chair/BarberShopChair_01_1k.gltf'))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    rose=material('Chair_Upholstery',(.24,.065,.07),.8)
    wood=material('Chair_WarmWood',(.28,.14,.065),.77)
    brass=material('Chair_Brass',(.45,.30,.12),.52)
    brass.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.65
    for o in objects:
        original=o.data.materials[0]
        diff=bpy.data.images.load(str(SRC/'polyhaven-chair/textures/BarberShopChair_01_diff_1k.jpg'))
        arm=bpy.data.images.load(str(SRC/'polyhaven-chair/textures/BarberShopChair_01_arm_1k.jpg'))
        dp=list(diff.pixels);ap=list(arm.pixels);w,h=diff.size;aw,ah=arm.size
        uv=o.data.uv_layers.active
        o.data.materials.clear()
        for m in [rose,wood,brass]:o.data.materials.append(m)
        for p in o.data.polygons:
            u=sum(uv.data[i].uv.x for i in p.loop_indices)/len(p.loop_indices)%1
            v=sum(uv.data[i].uv.y for i in p.loop_indices)/len(p.loop_indices)%1
            off=4*(min(h-1,int(v*h))*w+min(w-1,int(u*w)));r,g,b=dp[off:off+3]
            metal=ap[4*(min(ah-1,int(v*ah))*aw+min(aw-1,int(u*aw)))+2]
            p.material_index=2 if metal>.35 else 1 if r>g*1.17 and r>b*1.4 else 0
            p.use_smooth=True
        # Keep the original sculpted padding and mechanical structure, remove the aged finish.
        o['asset_source']='https://polyhaven.com/a/BarberShopChair_01';o['license']='CC0-1.0'
    export(objects,'polyhaven_barber_chair')

steps=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['customer','teammate','fps','home','alpaca']
for step in steps:
    if step=='home':home()
    elif step=='alpaca':alpaca()
    elif step=='chair':chair()
    else:snow(step)
(ROOT/'artifacts/asset-search-r1'/('converted-'+ '-'.join(steps)+'.json')).write_text(json.dumps(REPORT,indent=2))
print('ASSET_CONVERSION_OK',REPORT)
