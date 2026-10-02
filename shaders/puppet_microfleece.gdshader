shader_type spatial;
render_mode cull_disabled, diffuse_burley;
uniform vec4 base_color : source_color=vec4(0.8,0.6,0.4,1.0);
uniform float grain_scale=210.0;
uniform float grain_strength=0.24;
uniform bool woven=false;
uniform bool transport_material=false;
uniform bool clean_fabric=false;
uniform bool soft_fabric=false;
instance uniform bool puppet_face=false;
instance uniform float puppet_mouth=0.0;
uniform sampler2D scan_height:filter_linear_mipmap_anisotropic,repeat_enable;
uniform sampler2D scan_normal:hint_normal,filter_linear_mipmap_anisotropic,repeat_enable;
varying vec3 p;
varying vec3 wn;
varying vec3 tx;
varying vec3 ty;
varying vec3 tz;
float hash(vec3 q){q=fract(q*0.3183099+vec3(0.17,0.41,0.73));q*=17.0;return fract(q.x*q.y*q.z*(q.x+q.y+q.z));}
float noise(vec3 q){vec3 i=floor(q),f=fract(q);f=f*f*(3.0-2.0*f);return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);}
void vertex(){if(puppet_face){vec2 q=(VERTEX.xy-vec2(0.0,-.185))/vec2(.35,.19);VERTEX.y+=sign(VERTEX.y+.185)*puppet_mouth*.03*exp(-dot(q,q));}vec3 scale=vec3(length(MODEL_MATRIX[0].xyz),length(MODEL_MATRIX[1].xyz),length(MODEL_MATRIX[2].xyz));p=VERTEX*scale;wn=normalize(NORMAL);tx=normalize(MODEL_MATRIX[0].xyz);ty=normalize(MODEL_MATRIX[1].xyz);tz=normalize(MODEL_MATRIX[2].xyz);}
void fragment(){
    float aa=1.0-smoothstep(0.65,2.6,length(fwidth(p))*grain_scale);
    float grain=noise(p*vec3(grain_scale,grain_scale*.78,grain_scale));
    float broad=noise(p*38.0);
    float thread=sin(p.x*1200.0+sin(p.y*160.0)*.8)*sin(p.y*960.0);
    float nap=pow(max(grain,0.0),2.8);
    vec3 w=pow(abs(wn),vec3(4));w/=max(w.x+w.y+w.z,.0001);
    float tex_scale=clean_fabric?(woven?1.8:2.6):(woven?1.4:2.1);
    if(soft_fabric)tex_scale=woven?2.4:4.0;
    float h;
    vec2 nx,ny,nz;
    if(clean_fabric){
        // The scan describes fine nap, not stains or deep dents in the puppet.
        // Filter at the actual pixel footprint before reducing its contrast.
        vec3 q=p*tex_scale,dx=dFdx(q)*1.5,dy=dFdy(q)*1.5;
        h=textureGrad(scan_height,q.zy,dx.zy,dy.zy).r*w.x+textureGrad(scan_height,q.xz,dx.xz,dy.xz).r*w.y+textureGrad(scan_height,q.xy,dx.xy,dy.xy).r*w.z;
        nx=textureGrad(scan_normal,q.zy,dx.zy,dy.zy).xy*2.0-1.0;
        ny=textureGrad(scan_normal,q.xz,dx.xz,dy.xz).xy*2.0-1.0;
        nz=textureGrad(scan_normal,q.xy,dx.xy,dy.xy).xy*2.0-1.0;
    }else{
        h=texture(scan_height,p.zy*tex_scale).r*w.x+texture(scan_height,p.xz*tex_scale).r*w.y+texture(scan_height,p.xy*tex_scale).r*w.z;
        nx=texture(scan_normal,p.zy*tex_scale).xy*2.0-1.0;
        ny=texture(scan_normal,p.xz*tex_scale).xy*2.0-1.0;
        nz=texture(scan_normal,p.xy*tex_scale).xy*2.0-1.0;
    }
    vec3 grad=vec3(0,nx.y,nx.x)*w.x+vec3(ny.x,0,ny.y)*w.y+vec3(nz,0)*w.z;
    vec3 color=base_color.rgb*(.66+h*.78+(broad-.5)*.06);
    if(woven){color*=.96+thread*.06*aa;float stripe=smoothstep(.78,.82,sin(p.x*57.0+.25*sin(p.y*9.0)));color=mix(color,color*vec3(.46,.64,.64),stripe*.27);}
    if(clean_fabric)color=base_color.rgb*(woven?(.86+.30*h):(.90+.22*h));
    if(soft_fabric)color=base_color.rgb*(woven?(.94+.12*h):(.96+.10*h));
    ALBEDO=color;
    float normal_strength=soft_fabric?(woven?.22:.14):(clean_fabric?(woven?.38:.30):(woven?.75:.65));
    NORMAL=normalize(NORMAL+(VIEW_MATRIX*vec4(tx*grad.x+ty*grad.y+tz*grad.z,0)).xyz*normal_strength);
    ROUGHNESS=.95;SPECULAR=.08;RIM=.24;RIM_TINT=.90;BACKLIGHT=base_color.rgb*.065;
    if(transport_material){
        ROUGHNESS=clamp((woven?.86:.87)+(h-.5)*.10,.77,.97);SPECULAR=.45;
        RIM=woven?.18:.10;RIM_TINT=.75;BACKLIGHT=base_color.rgb*(woven?.13:.08);
    }
    if(clean_fabric){
        ROUGHNESS=.94;SPECULAR=.16;RIM=.16;RIM_TINT=.90;
        BACKLIGHT=base_color.rgb*.10;
    }
}
