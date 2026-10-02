shader_type spatial;
render_mode unshaded, cull_disabled;
varying vec3 p;
void vertex(){p=VERTEX;}
float cloud(vec2 uv,vec2 center,vec2 size){return 1.0-smoothstep(.76,1.0,length((uv-center)/size));}
void fragment(){
    vec2 uv=p.xy/vec2(1.4,1.89)+.5;
    vec3 sky=mix(vec3(.77,.72,.54),vec3(.22,.45,.64),smoothstep(.0,1.0,uv.y));
    float clouds=max(cloud(uv,vec2(.73,.71),vec2(.39,.07)),cloud(uv,vec2(.22,.9),vec2(.31,.055)));
    sky=mix(sky,vec3(.86,.84,.76),clouds*.7);
    float sun=1.0-smoothstep(.03,.12,length(uv-vec2(.92,.81)));
    sky+=vec3(.7,.42,.15)*sun;
    float leaves=1.0-smoothstep(.02,.08,uv.y-(.12+.07*sin(uv.x*13.0)+.04*sin(uv.x*31.0)));
    sky=mix(sky,vec3(.18,.31,.24),leaves*.65);
    ALBEDO=sky;EMISSION=sky*.38;
}
