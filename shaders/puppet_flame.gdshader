shader_type spatial;
render_mode unshaded, cull_disabled;
uniform float phase=0.0;
varying float h;
void vertex(){h=clamp(VERTEX.y/.22,0.0,1.0);VERTEX.x+=sin(TIME*12.0+h*6.0+phase)*h*h*.025;VERTEX.z+=sin(TIME*9.0+h*5.0+phase)*h*.008;}
void fragment(){vec3 c=mix(vec3(1.0,.58,.09),vec3(.86,.16,.025),h*h);ALBEDO=c;EMISSION=c*.60;}
