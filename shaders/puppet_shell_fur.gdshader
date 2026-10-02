shader_type spatial;
render_mode cull_disabled, diffuse_burley;
uniform sampler2D root_pattern : filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec4 base_color : source_color=vec4(.38,.28,.48,1);
uniform float layer_t=0.25;
uniform float normal_pile=.012;
uniform float trim_pile=.0038;
uniform float carve_pile=.0026;
uniform bool mask_pass=false;
varying vec3 root_position;
varying vec3 root_normal;
varying float kind;
void vertex(){
    root_position=VERTEX;root_normal=normalize(NORMAL);kind=COLOR.a;
    float trim=1.0-smoothstep(.05,.22,abs(kind-.5));
    float carve=smoothstep(.70,.95,kind);
    float pile=mix(mix(normal_pile,trim_pile,trim),carve_pile,carve);
    // Preserve R6's short inner cap: extending this hidden surface produced
    // isolated fur pixels through the cheek even with small root offsets.
    if(kind<.2&&dot(root_normal,root_position-vec3(0,.05,0))<-.01) pile=min(pile,.004);
    vec3 flow=vec3(.3+.18*sin(VERTEX.y*5.0),.15-VERTEX.z, .8);
    vec3 tangent=flow-root_normal*dot(flow,root_normal);
    tangent=normalize(tangent+vec3(.00001));
    // Controlled outward nap plus a gentle authored lean; never a SDF query.
    VERTEX+=pile*(root_normal*layer_t*.94+tangent*layer_t*layer_t*.40);
}
void fragment(){
    vec3 q=root_position/.12;
    vec3 dx=dFdx(q)*1.25,dy=dFdy(q)*1.25;
    vec3 w=pow(abs(root_normal),vec3(8));w/=max(dot(w,vec3(1)),.0001);
    vec2 pattern=textureGrad(root_pattern,q.zy,dx.zy,dy.zy).rg*w.x
        +textureGrad(root_pattern,q.xz,dx.xz,dy.xz).rg*w.y
        +textureGrad(root_pattern,q.xy,dx.xy,dy.xy).rg*w.z;
    float height=.70+pattern.g*.40;
    float taper=.10+.80*pow(clamp(layer_t/height,0.0,1.1),1.35);
    float root_tint=.91+.09*smoothstep(-.17,.25,root_position.y);
    // Small follicle normals and a restrained root-to-tip light gradient make
    // the pile readable on a facing surface, not only along the silhouette.
    vec3 dpdx=dFdx(VERTEX),dpdy=dFdy(VERTEX);
    vec3 rx=cross(dpdy,NORMAL),ry=cross(NORMAL,dpdx);
    float determinant=dot(dpdx,rx);
    vec3 gradient=(rx*dFdx(pattern.r)+ry*dFdy(pattern.r))/max(abs(determinant),1e-9)*sign(determinant);
    vec3 bump=gradient*.0007;
    bump*=min(1.0,.45/max(length(bump),.00001));
    // Evaluate derivatives before divergent discard.
    if(pattern.r<taper||layer_t>height)discard;
    NORMAL=normalize(NORMAL-bump);
    ALBEDO=base_color.rgb*root_tint*(.83+.17*layer_t)*(.87+.22*pattern.r);
    ROUGHNESS=.96;SPECULAR=.10;RIM=.17;RIM_TINT=.9;
    BACKLIGHT=ALBEDO*.15;EMISSION=ALBEDO*.22;
    if(mask_pass){ALBEDO=vec3(0);EMISSION=vec3(1);SPECULAR=0.0;}
}
