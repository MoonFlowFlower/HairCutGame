shader_type spatial;
render_mode unshaded, cull_back;
uniform sampler3D tuft_volume : filter_linear_mipmap, repeat_enable;
uniform vec3 fur_color : source_color=vec3(.44,.32,.53);
uniform vec3 wool_color : source_color=vec3(.839,.773,.659);
uniform vec3 features=vec3(1.0);
varying vec3 p;
varying vec3 n;
void vertex(){p=VERTEX;n=normalize(MODEL_NORMAL_MATRIX*NORMAL);}
void fragment(){
    vec4 fibre=texture(tuft_volume,p*1.1);
    vec3 color=mix(fur_color,wool_color,COLOR.a);
    color*=mix(1.0,.62,COLOR.r);
    color=mix(color,vec3(.55,.72,.82),COLOR.g*.78);
    color=mix(color,vec3(.032,.023,.026),COLOR.b*.92);
    float light=clamp(dot(normalize(n),normalize(vec3(-.5,.8,.7)))*.5+.5,0.0,1.0);
    ALBEDO=color*(.56+.56*light)*(.84+.20*fibre.a);
}
