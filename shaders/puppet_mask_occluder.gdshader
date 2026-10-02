shader_type spatial;
render_mode unshaded, cull_disabled;
uniform bool white=false;
void fragment(){ALBEDO=vec3(white?1.0:0.0);}
