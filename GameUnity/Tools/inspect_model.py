import bpy, json, os
from mathutils import Vector
out = os.path.join(os.path.dirname(bpy.data.filepath), 'Logs', 'model-inspection.json')
items=[]
for obj in bpy.context.scene.objects:
    corners=[obj.matrix_world @ Vector(c) for c in obj.bound_box]
    items.append(dict(name=obj.name,type=obj.type,location=list(obj.location),rotation=list(obj.rotation_euler),scale=list(obj.scale),minimum=[min(c[i] for c in corners) for i in range(3)],maximum=[max(c[i] for c in corners) for i in range(3)],materials=[dict(name=m.name,color=list(m.diffuse_color),nodes=[dict(type=n.type, color=list(n.inputs['Base Color'].default_value) if n.type=='BSDF_PRINCIPLED' else None) for n in m.node_tree.nodes] if m.use_nodes else []) for m in obj.data.materials if m] if hasattr(obj.data,'materials') else []))
with open(out,'w',encoding='utf-8') as f: json.dump(items,f,ensure_ascii=False,indent=2)
print('INSPECTION',out,len(items))
for obj in bpy.context.scene.objects:
    print('DETAIL',obj.name,'hideRender',obj.hide_render,'hideView',obj.hide_get(),'display',obj.display_type,'mods',[(m.type,m.show_render) for m in obj.modifiers])
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.context.scene.objects:
    if obj.type in {'MESH','FONT','CURVE'} and not obj.hide_render and obj.name not in {'КриваяБезье','Текст'}:
        obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(os.path.dirname(bpy.data.filepath),'Assets','Models','Macanic.fbx'),use_selection=True,object_types={'MESH','EMPTY','OTHER'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
