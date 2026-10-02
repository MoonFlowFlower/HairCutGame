shader_type spatial;
render_mode cull_back, diffuse_burley;
uniform vec3 features=vec3(1.0); // flock, rim, micro
varying vec3 p;
void vertex(){p=VERTEX;vec3 grain=vec3(sin(p.y*31.0),sin(p.z*27.0),sin(p.x*29.0));NORMAL=normalize(NORMAL+(grain-NORMAL*dot(grain,NORMAL))*0.15*features.x);}
void fragment(){float fibers=sin(p.x*79.0+p.z*13.0)*sin(p.y*37.0);float filter=1.0-smoothstep(0.4,2.0,length(fwidth(p))*79.0);ALBEDO=COLOR.rgb*(1.0+fibers*0.14*features.x*features.z*filter);ROUGHNESS=0.96;SPECULAR=0.18;RIM=0.17*features.y;RIM_TINT=0.6;}
