vec3 fur_unit(vec3 value){
#ifdef SOFT_VOLUME_SAMPLING
    return value*inversesqrt(max(dot(value,value),1e-10));
#else
    return normalize(value);
#endif
}

uniform sampler3D density_grid : filter_linear, repeat_disable;
uniform float density_scale=3.5;
uniform vec3 density_offset=vec3(0.0);
uniform float root_packing=1.0;
uniform bool production_states=false;
uniform bool cartesian_fur=false;
uniform sampler3D material_states : filter_linear, repeat_disable;
uniform sampler2D root_pattern : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2DArray coverage_pattern : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2DArray bundle_pattern : filter_linear_mipmap_anisotropic, repeat_enable;
uniform bool use_bundles=false;
uniform bool use_tuft_volume=false;
uniform bool curve_reference=false;
uniform bool curve_ribbons=false;
uniform bool surface_samples=false;
uniform float surface_sample_depth=.030;
uniform float surface_sample_spacing=.00375;
uniform sampler3D tuft_volume : filter_linear_mipmap, repeat_enable;
uniform sampler3D tuft_roots : filter_linear_mipmap, repeat_enable;
uniform sampler2D original_extent : filter_linear, repeat_enable;
uniform sampler2D proxy_extent : filter_linear, repeat_disable;
uniform sampler3D original_density : filter_linear, repeat_disable;
uniform sampler2D motion_field : filter_linear, repeat_disable;
uniform vec2 motion_grid=vec2(32.0,16.0);
uniform bool motion_enabled=false;
uniform bool motion_contact_enabled=false;
uniform vec4 motion_contact_sphere=vec4(0.0);
uniform float motion_contact_max_push=.070;
uniform bool contact_pixels=false;
uniform int contact_limit_pixels=0;
uniform int displacement_pixels=0;
uniform vec3 contact_diagnostic_center=vec3(0.0);
uniform float contact_diagnostic_radius=0.0;
uniform vec3 fur_color : source_color = vec3(0.44,0.32,0.53);
uniform bool mask_pass=false;
uniform bool coverage_pass=false;
uniform bool nonfinite_debug=false;
uniform bool clump_depth=false;
uniform bool volume_undercoat=false;
varying vec3 local_point;
varying float height_t;
varying float sample_spacing;
varying vec3 radial_normal;
varying vec3 tangent_u;
varying vec3 tangent_v;
varying mat3 normal_to_world;
varying vec3 curve_root;
varying vec3 curve_normal;
varying float ribbon_across;
varying vec3 ribbon_side;
varying vec3 ribbon_view;
varying vec2 contact_unresolved;
varying vec3 rest_world_point;
varying vec3 local_key_light;

// Shading only: filtered fibre coverage along the light through surviving
// material. Occupancy gates extinction; it is never used as a distance field.
// Both explicit original curves and interior samples use these same probes.
vec3 tuft_coordinate(vec3 point){
    if(cartesian_fur)return point/.24;
    vec3 radial=point-vec3(0,-.24,.015);
    vec3 q=radial/vec3(.445,.45,.322);
    float r=max(length(q),.00001);
    return vec3(fract(atan(q.y,q.x)/6.283185+1.0)*16.0,
        acos(clamp(q.z/r,-1.0,1.0))/3.141593*8.0,(r-.98)*length(radial)/r/.24);
}
float fibre_extinction(vec3 point,float level){
    vec3 grid_uv=((point*density_scale+density_offset-vec3(-2.38,-.7,-1.82))/.14+vec3(.5))/vec3(35.0,31.0,27.0);
    float surviving=smoothstep(.47,.53,texture(density_grid,grid_uv).a);
    return textureLod(tuft_volume,tuft_coordinate(point),level).a*surviving;
}
float clump_visibility(vec3 point,vec3 light_local){
    // The cone has a material-space width, but must never resolve finer
    // than a screen pixel. Wrap the longitude derivative at the head seam.
    vec3 coord=tuft_coordinate(point),gx=dFdx(coord),gy=dFdy(coord);
    gx.x-=round(gx.x/16.0)*16.0;gy.x-=round(gy.x/16.0)*16.0;
    float footprint=log2(max(max(length(gx),length(gy))*float(textureSize(tuft_volume,0).x),1.0));
    float depth=fibre_extinction(point+light_local*.003,max(2.0,footprint))*2.0
        +fibre_extinction(point+light_local*.009,max(2.3,footprint))*4.0
        +fibre_extinction(point+light_local*.018,max(2.7,footprint))*7.0
        +fibre_extinction(point+light_local*.035,max(3.0,footprint))*11.0;
    return exp(-depth);
}

// Same six tetrahedra and 127.5 isovalue as HairShell.Build. Trilinear
// interpolation would clip a different surface. No sphere tracing / SDF use.
float sample_occupancy(sampler3D grid,vec3 p) {
    vec3 q=(p*density_scale+density_offset-vec3(-2.38,-.7,-1.82))/.14;
    ivec3 c=ivec3(floor(q));vec3 f=fract(q);
    if(any(lessThan(c,ivec3(0)))||any(greaterThanEqual(c,ivec3(34,30,26))))return 0.0;
    ivec3 a;ivec3 b;float hi;float mid;float lo;
    if(f.x>=f.y){
        if(f.y>=f.z){a=ivec3(1,0,0);b=ivec3(1,1,0);hi=f.x;mid=f.y;lo=f.z;}
        else if(f.x>=f.z){a=ivec3(1,0,0);b=ivec3(1,0,1);hi=f.x;mid=f.z;lo=f.y;}
        else{a=ivec3(0,0,1);b=ivec3(1,0,1);hi=f.z;mid=f.x;lo=f.y;}
    }else{
        if(f.x>=f.z){a=ivec3(0,1,0);b=ivec3(1,1,0);hi=f.y;mid=f.x;lo=f.z;}
        else if(f.y>=f.z){a=ivec3(0,1,0);b=ivec3(0,1,1);hi=f.y;mid=f.z;lo=f.x;}
        else{a=ivec3(0,0,1);b=ivec3(0,1,1);hi=f.z;mid=f.y;lo=f.x;}
    }
    float v0=texelFetch(grid,c,0).a,v1=texelFetch(grid,c+a,0).a;
    float v2=texelFetch(grid,c+b,0).a,v3=texelFetch(grid,c+ivec3(1),0).a;
    return v0+(v1-v0)*hi+(v2-v1)*mid+(v3-v2)*lo;
}
void vertex(){
    contact_unresolved=vec2(0.0);
    local_key_light=fur_unit((inverse(MODEL_MATRIX)*vec4(-.5,.8,.7,0.0)).xyz);
    height_t=INSTANCE_CUSTOM.r;
    sample_spacing=INSTANCE_CUSTOM.g;
    vec3 center=vec3(0,-.24,.015);
    curve_normal=fur_unit(MODEL_NORMAL_MATRIX*NORMAL);
    if(curve_reference||surface_samples){
        curve_root=(COLOR.rgb*2.0-1.0)*root_packing;
        if(curve_reference&&curve_ribbons){
            vec3 view_local=fur_unit((inverse(MODEL_MATRIX)*vec4(CAMERA_POSITION_WORLD,1.0)).xyz-VERTEX);
            vec3 side=fur_unit(cross(NORMAL,view_local));
            ribbon_across=UV.x;ribbon_side=fur_unit(MODEL_NORMAL_MATRIX*side);ribbon_view=fur_unit(MODEL_NORMAL_MATRIX*view_local);
            VERTEX+=side*UV.x*UV2.x;
        }
        if(!curve_reference){
            // Only sample positions follow the current boundary. Material
            // coordinates below remain the original continuous tuft volume.
            // Inward offsets expose its existing cross sections after a cut;
            // they never extrude new hair from the cut surface.
            VERTEX-=NORMAL*height_t*surface_sample_depth;
        }
        vec3 q=(VERTEX-center)/vec3(.445,.45,.322);float r=length(q);
        UV=vec2(fract(atan(q.y,q.x)/6.283185+1.0),acos(clamp(q.z/r,-1.0,1.0))/3.141593);
        float end=texture(proxy_extent,(UV*vec2(96.0,48.0)+vec2(.5))/vec2(97.0,49.0)).r;
        height_t=clamp((r-.98)/max(.02,end-.98),0.0,1.0);
        if(cartesian_fur)height_t=1.0;
        else NORMAL=fur_unit((VERTEX-center)/vec3(.445*.445,.45*.45,.322*.322));
    }else VERTEX=center+(VERTEX-center)*mix(.98,UV2.x,height_t);
    local_point=VERTEX;
    rest_world_point=vec3(0.0);
    if(displacement_pixels>0)rest_world_point=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    if(motion_enabled){
        vec4 guide=texture(motion_field,(UV*motion_grid+vec2(.5))/(motion_grid+vec2(1.0)));
        float along=clamp(height_t/max(guide.a,.025),0.0,1.0);
        VERTEX+=guide.rgb*along*along;
        if(motion_contact_enabled){
            // Resolve the explicit engine sphere on the rendered geometry,
            // after guide interpolation. Guides alone left whole tuft patches
            // inside the collider. Preserve the fixed scalp end continuously.
            vec3 world_point=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
            vec3 delta=world_point-motion_contact_sphere.xyz;
            float distance_to_center=length(delta);
            vec3 direction=delta/max(distance_to_center,.00001);
            // A shared plane keeps the whole ribbon segment outside the
            // convex sphere; separately projected endpoints can make a chord
            // through it. Root coordinates precede camera-facing expansion.
            vec3 contact_anchor=center+(COLOR.rgb*2.0-1.0)*mix(.98,COLOR.a*4.0,height_t);
            if(curve_reference||surface_samples)contact_anchor=curve_root;
            vec3 anchor=(MODEL_MATRIX*vec4(contact_anchor,1.0)).xyz-motion_contact_sphere.xyz;
            direction=anchor/max(length(anchor),.00001);
            float required=max(motion_contact_sphere.w+.003-dot(delta,direction),0.0);
            float push=min(required,motion_contact_max_push);
            float root_fade=smoothstep(0.0,.25,along);
            contact_unresolved=vec2(max(required-motion_contact_max_push,0.0),push*(1.0-root_fade));
            if(push>0.0){
                world_point+=direction*push*root_fade;
                VERTEX=(inverse(MODEL_MATRIX)*vec4(world_point,1.0)).xyz;
            }
        }
    }
    radial_normal=fur_unit(MODEL_NORMAL_MATRIX*NORMAL);
    normal_to_world=MODEL_NORMAL_MATRIX;
    tangent_u=fur_unit(MODEL_NORMAL_MATRIX*vec3(-sin(UV.x*6.283185),cos(UV.x*6.283185),0.0));
    tangent_v=fur_unit(cross(radial_normal,tangent_u));
}
void fragment(){
    vec2 field_uv=UV;
#ifdef SOFT_VOLUME_SAMPLING
    // Interpolating longitude across 0/1 sampled the other side of the head.
    // Derive material coordinates from the original, unbent fragment point.
    vec3 field_q=(local_point-vec3(0,-.24,.015))/vec3(.445,.45,.322);
    field_uv=vec2(fract(atan(field_q.y,field_q.x)/6.283185+1.0),acos(clamp(field_q.z/max(length(field_q),.00001),-1.0,1.0))/3.141593);
#endif
    float current=sample_occupancy(density_grid,local_point);
    float tip_extension=0.0;
    if(use_bundles){
        vec3 q=(local_point-vec3(0,-.24,.015))/vec3(.445,.45,.322);
        float r=length(q),end=texture(original_extent,field_uv).r;
        tip_extension=(r-end)*length((local_point-vec3(0,-.24,.015))/r);
    }
    if(current<.5){
        if(cartesian_fur)discard;
        if(!use_bundles)discard;
        vec3 q=(local_point-vec3(0,-.24,.015))/vec3(.445,.45,.322);
        float r=length(q),end=texture(original_extent,field_uv).r;
        vec3 ray=(local_point-vec3(0,-.24,.015))/r;
        tip_extension=(r-end)*length(ray);
        if(end<1.01||tip_extension<0.0||tip_extension>.040)discard;
        // Only original outer tips may extend. A new cut never receives a
        // new coat; its original supporting segment must still exist.
        float before=sample_occupancy(original_density,local_point);
        vec3 anchor=vec3(0,-.24,.015)+ray*(end-.018);
        if(before>=.5||before-current>.006||sample_occupancy(density_grid,anchor)<.5)discard;
    }
    if(curve_reference){
#ifdef SOFT_VOLUME_SAMPLING
        // Fade only the existing finite curve's end, never a fresh cut face.
        ALPHA=curve_ribbons?(1.0-smoothstep(.40,1.0,abs(ribbon_across)))*(1.0-smoothstep(.65,1.0,COLOR.a)):1.0;
#else
        ALPHA=curve_ribbons?1.0-smoothstep(.80,1.0,abs(ribbon_across)):1.0;ALPHA_HASH_SCALE=1.0;
#endif
        if(mask_pass)ALBEDO=vec3(1.0);
        else{
            vec3 n=fur_unit(radial_normal*.35+curve_normal*.90);
            if(curve_ribbons)n=fur_unit(radial_normal*.55+ribbon_view*.55+ribbon_side*ribbon_across*.40);
            vec3 l=fur_unit(vec3(-.5,.8,.7));
            float wrapped=pow(clamp(dot(n,l)*.5+.5,0.0,1.0),1.8);
            float ao=mix(.58,1.0,pow(height_t,.7));
            vec3 view_dir=fur_unit((INV_VIEW_MATRIX*vec4(VIEW,0.0)).xyz);
            float rim=pow(1.0-abs(dot(n,view_dir)),3.0);
            ALBEDO=fur_color*(curve_ribbons?(.62+.48*clamp(COLOR.a,0.0,1.0)):(.80+.20*COLOR.a))*ao*(.38+.96*wrapped)+fur_color*.12*rim;
#ifdef SOFT_VOLUME_SAMPLING
            // A cylinder's diffuse lobe depends on its tangent and light,
            // not the facing card normal that rotates with the camera.
            // GLSL pow(x, 2.0) is undefined for negative x. Back-facing
            // strand tangents are normal here, so square by multiplication.
            float tangent_light=clamp(dot(fur_unit(curve_normal),l),-1.0,1.0);
            float tube_light=sqrt(max(0.0,1.0-tangent_light*tangent_light));
            vec3 grid_uv=((local_point*density_scale+density_offset-vec3(-2.38,-.7,-1.82))/.14+vec3(.5))/vec3(35.0,31.0,27.0);
            vec3 shape=normal_to_world*(texture(density_grid,grid_uv).rgb*2.0-1.0);
            vec3 broad_normal=fur_unit(shape*.55+radial_normal*.45);
            float broad_light=clamp(dot(broad_normal,l)*.5+.5,0.0,1.0);
            ALBEDO=fur_color*(.78+.24*clamp(COLOR.a,0.0,1.0))*mix(.72,1.0,pow(clamp(height_t,0.0,1.0),.7))*(.42+.42*broad_light+.30*tube_light);
            if(clump_depth){
                float visibility=clump_visibility(local_point,local_key_light);
                float root_tone=mix(.78,1.08,clamp(COLOR.a,0.0,1.0));
                ALBEDO=fur_color*root_tone*(.42+(.32*broad_light+.42*tube_light)*visibility);
            }
#endif
        }
    }else{
    // One continuous root coordinate is shared by EVERY shell, before and
    // after cutting. The slow height-dependent warp bends the same follicles.
    float phase=sin(field_uv.x*75.0+field_uv.y*49.0)*2.0;
    vec2 groom=vec2(sin(field_uv.y*71.0+field_uv.x*36.0+height_t*8.0),cos(field_uv.x*67.0-field_uv.y*32.0+height_t*7.0+phase));
    vec2 root_uv=field_uv*vec2(12.0,6.0)+groom*.44*height_t*height_t;
    vec4 root=vec4(.5);
    if(!use_tuft_volume||!volume_undercoat)root=texture(root_pattern,root_uv);
    float tip=mix(.97,1.07,root.g);
    float taper=smoothstep(.75,tip,height_t);
    float band=taper*7.0;
    float lower=floor(band);
    float coverage=0.0;
    if(!use_tuft_volume||!volume_undercoat)coverage=mix(texture(coverage_pattern,vec3(root_uv,lower)).r,
        texture(coverage_pattern,vec3(root_uv,min(lower+1.0,7.0))).r,fract(band));
    vec2 across=root.ba*2.0-1.0;
    vec3 bundle_normal=vec3(0.0);
    float tone=mix(.92,1.06,root.g);
    vec3 tuft_normal=vec3(0.0);
    if(use_bundles&&!use_tuft_volume){
        float slice=height_t*127.0;
        vec4 bundle=mix(texture(bundle_pattern,vec3(UV*vec2(8.0,4.0),floor(slice))),
            texture(bundle_pattern,vec3(UV*vec2(8.0,4.0),min(floor(slice)+1.0,127.0))),fract(slice));
        vec2 fine_uv=UV*vec2(20.0,10.0)+groom*.06*height_t*height_t;
        float fine=texture(coverage_pattern,vec3(fine_uv,band)).r*.40*(1.0-smoothstep(.0,.006,tip_extension));
        float endpoint=mix(-.010,.040,clamp(bundle.b/max(bundle.a,.001),0.0,1.0));
        float outer=1.0-smoothstep(endpoint-.009,endpoint,tip_extension);
        coverage=1.0-(1.0-bundle.a*outer)*(1.0-fine);
        across=(bundle.rg*2.0-1.0)*.42;
        bundle_normal=vec3(bundle.rg*2.0-1.0,0.0)*bundle.a;
        tone=1.0;
    }
    if(use_tuft_volume){
        vec3 radial=local_point-vec3(0,-.24,.015);
        float r=length(radial/vec3(.445,.45,.322));
        vec3 coord=cartesian_fur?local_point/.24:vec3(field_uv*vec2(16.0,8.0),(r-.98)*length(radial)/max(r,.00001)/.24);
#ifdef SOFT_VOLUME_SAMPLING
        vec2 dx=dFdx(field_uv),dy=dFdy(field_uv);
        dx.x-=round(dx.x);dy.x-=round(dy.x);
        vec3 gx=cartesian_fur?dFdx(coord):vec3(dx*vec2(16.0,8.0),dFdx(coord.z)),gy=cartesian_fur?dFdy(coord):vec3(dy*vec2(16.0,8.0),dFdy(coord.z));
        vec4 tuft=textureGrad(tuft_volume,coord,gx,gy);
        vec4 anchor_data=textureGrad(tuft_roots,coord,gx,gy);
#else
        vec4 tuft=texture(tuft_volume,coord);
        vec4 anchor_data=texture(tuft_roots,coord);
#endif
        vec3 root_delta=clamp((anchor_data.rgb*2.0-1.0)/max(tuft.a,.03),vec3(-1.0),vec3(1.0))*.10;
        vec3 anchor_coord=coord*.24+root_delta;
        float a=anchor_coord.x/(.24*16.0)*6.283185,b=anchor_coord.y/(.24*8.0)*3.141593;
        vec3 anchor_ray=vec3(.445*cos(a)*sin(b),.45*sin(a)*sin(b),.322*cos(b));
        vec3 anchor=cartesian_fur?anchor_coord:vec3(0,-.24,.015)+anchor_ray*(.98+anchor_coord.z/length(anchor_ray));
        // Root membership is fixed in the ORIGINAL material. The current
        // occupancy clips points above, rather than deleting intact portions
        // of a curve elsewhere when its root happens to be in the cut.
        // B can grow into any lattice cell. Its fixed finite-curve template
        // covers that bounded material domain from the start; current voxels
        // only reveal/clip it. This also makes late joins history-independent.
        float root_alive=production_states?1.0:step(.5,sample_occupancy(original_density,anchor));
        float fine=0.0;vec3 fine_normal=vec3(0.0);
        if(!volume_undercoat){
            vec2 fine_uv=field_uv*vec2(20.0,10.0)+groom*.06*height_t*height_t;
            fine=texture(coverage_pattern,vec3(fine_uv,band)).r*.35*(1.0-smoothstep(-.030,-.016,tip_extension));
            vec4 fine_root=texture(root_pattern,fine_uv);
            fine_normal=tangent_u*(fine_root.b*2.0-1.0)+tangent_v*(fine_root.a*2.0-1.0);
        }
#ifdef SOFT_VOLUME_SAMPLING
        if(volume_undercoat){
            // The old 2D undercoat repeated the same marks through depth,
            // making cut interiors look combed. This is a second, half-size
            // population of the SAME finite curved-fibre template, fixed in
            // original material space. A cut only clips it; it adds no roots.
            vec3 micro_coord=coord*2.0+vec3(.317,.619,.127);
            float micro_lod=max(1.0,log2(max(max(length(gx),length(gy))*2.0*float(textureSize(tuft_volume,0).x),1.0)));
            vec4 micro=textureLod(tuft_volume,micro_coord,micro_lod);
            vec4 micro_root=textureLod(tuft_roots,micro_coord,micro_lod);
            vec3 micro_delta=clamp((micro_root.rgb*2.0-1.0)/max(micro.a,.03),vec3(-1.0),vec3(1.0))*.05;
            vec3 micro_anchor_coord=coord*.24+micro_delta;
            float ma=micro_anchor_coord.x/(.24*16.0)*6.283185,mb=micro_anchor_coord.y/(.24*8.0)*3.141593;
            vec3 micro_ray=vec3(.445*cos(ma)*sin(mb),.45*sin(ma)*sin(mb),.322*cos(mb));
            vec3 micro_anchor=cartesian_fur?micro_anchor_coord:vec3(0,-.24,.015)+micro_ray*(.98+micro_anchor_coord.z/max(length(micro_ray),.00001));
            float micro_alive=production_states?1.0:step(.5,sample_occupancy(original_density,micro_anchor));
            fine=(1.0-pow(max(1.0-micro.a,0.0),3.0))*micro_alive*(production_states?1.0:1.0-smoothstep(-.030,-.016,tip_extension));
            vec3 mn=micro.rgb*2.0-1.0;
            fine_normal=tangent_u*mn.x-tangent_v*mn.y+radial_normal*mn.z;
        }
#endif
        coverage=1.0-(1.0-tuft.a*root_alive)*(1.0-fine);
        float visible_tuft=tuft.a*root_alive;
        vec3 field_normal=tuft.rgb*2.0-1.0;
        vec3 mapped_normal=tangent_u*field_normal.x-tangent_v*field_normal.y+radial_normal*field_normal.z;
        tuft_normal=(mapped_normal*visible_tuft+fine_normal*fine*(1.0-visible_tuft))/max(visible_tuft+fine*(1.0-visible_tuft),.01);
        tone=.80+.20*clamp(anchor_data.a/max(tuft.a,.03),0.0,1.0);
#ifdef SOFT_VOLUME_SAMPLING
        // Integrate the same filtered material over a sampling interval.
        // This adds opacity within surviving follicles, not an opaque core.
        if(surface_samples){
            float interval=clamp(surface_sample_spacing/.0015,1.0,4.0);
            if(volume_undercoat){
                // Grazing rays travel farther between boundary-parallel
                // samples. Integrate that bounded path instead of preserving
                // front-facing opacity at every angle. Density still clips
                // every fragment; zero fibre coverage stays zero.
                vec3 sample_normal=fur_unit(cross(dFdx(VERTEX),dFdy(VERTEX)));
                interval=min(8.0,max(sample_spacing/.0015,.25)/max(abs(dot(sample_normal,fur_unit(VIEW))),.30));
            }
            coverage=1.0-pow(max(1.0-coverage,0.0),interval);
        }
        tone=.92+.08*clamp(anchor_data.a/max(tuft.a,.03),0.0,1.0);
#endif
    }
    // Stable spatial alpha hashing integrates minified coverage across shells.
    // Resolved roots still have solid interiors; no frame/time randomization.
    ALPHA=coverage_pass?1.0:coverage;
#ifndef SOFT_VOLUME_SAMPLING
    ALPHA_HASH_SCALE=1.0;
#endif
    if(mask_pass){ALBEDO=vec3(1.0);}
    else{
        vec3 grid_uv=((local_point*density_scale+density_offset-vec3(-2.38,-.7,-1.82))/.14+vec3(.5))/vec3(35.0,31.0,27.0);
        vec3 shape=normal_to_world*(texture(density_grid,grid_uv).rgb*2.0-1.0);
        if(length(shape)<.1)shape=radial_normal;
        vec3 n=fur_unit(fur_unit(shape)*.70+radial_normal*.20+tangent_u*across.x*1.5+tangent_v*across.y*1.5);
        if(use_bundles)n=fur_unit(fur_unit(shape)*.45+radial_normal*.20+tangent_u*bundle_normal.x*.95+tangent_v*bundle_normal.y*.95);
        if(use_tuft_volume)n=fur_unit(radial_normal*.35+tuft_normal*.90+fur_unit(shape)*.15);
        vec3 l=fur_unit(vec3(-.5,.8,.7));
        float wrapped=pow(clamp(dot(n,l)*.5+.5,0.0,1.0),1.8);
        float ao=mix(.58,1.0,pow(height_t,.7));
        vec3 view_dir=fur_unit((INV_VIEW_MATRIX*vec4(VIEW,0.0)).xyz);
        float rim=pow(1.0-abs(dot(n,view_dir)),3.0);
        float back=pow(max(dot(-l,view_dir),0.0),3.0)*(1.0-max(dot(n,l),0.0));
        ALBEDO=fur_color*tone*ao*(.38+.96*wrapped)+fur_color*(.12*rim+.12*back);
#ifdef SOFT_VOLUME_SAMPLING
        n=fur_unit(radial_normal*.35+tuft_normal*.25+fur_unit(shape)*.40);
        wrapped=clamp(dot(n,l)*.5+.5,0.0,1.0);
        ALBEDO=fur_color*tone*mix(.72,1.0,pow(clamp(height_t,0.0,1.0),.7))*(.48+.72*wrapped);
        if(clump_depth){
            float visibility=clump_visibility(local_point,local_key_light);
            ALBEDO=fur_color*tone*(.42+.80*wrapped*visibility);
        }
#endif
    }
    }
#ifdef SOFT_VOLUME_SAMPLING
    ALPHA_SCISSOR_THRESHOLD=.18;
    ALPHA_ANTIALIASING_EDGE=.04;
    if(production_states&&!mask_pass&&!coverage_pass){
        vec3 state_uv=(local_point*density_scale+density_offset-vec3(-2.38,-.7,-1.82))/vec3(4.76,4.2,3.64);
        vec4 state=texture(material_states,state_uv);
        ALBEDO*=mix(1.0,.62,state.r);
        ALBEDO=mix(ALBEDO,vec3(.55,.72,.82),state.g*.78);
        ALBEDO=mix(ALBEDO,vec3(.032,.023,.026),state.b*.92);
        ALBEDO=mix(ALBEDO,ALBEDO*vec3(1.13,1.08,.86),state.a*.45);
    }
    // Keep a diagnostic pixel at the actual failing fragment, before bloom
    // can spread a non-finite value into an apparent white flash.
    if(any(isnan(ALBEDO))||any(isinf(ALBEDO))||isnan(ALPHA)||isinf(ALPHA)){
        ALBEDO=nonfinite_debug?vec3(1.0,0.0,0.0):fur_color;ALPHA=1.0;
    }
#endif
    if(contact_pixels){
        // Actual post-WPO fragment position, including interpolation between
        // guide/mesh vertices. Only this explicit sphere has a distance field;
        // the editable hair lattice remains an occupancy clip above.
        vec3 world_point=(INV_VIEW_MATRIX*vec4(VERTEX,1.0)).xyz;
        if(distance(world_point,contact_diagnostic_center)>=contact_diagnostic_radius-.0005)discard;
        if(contact_limit_pixels==1&&contact_unresolved.x<.0001)discard;
        if(contact_limit_pixels==2&&contact_unresolved.y<.0001)discard;
        ALBEDO=vec3(1.0,0.0,0.0);
    }
    if(displacement_pixels>0){
        if(displacement_pixels==2&&(!curve_reference||COLOR.a<.85))discard;
        vec3 world_point=(INV_VIEW_MATRIX*vec4(VERTEX,1.0)).xyz;
        // Green distinguishes actual raster coverage from the black scene.
        // Red is total post-WPO displacement relative to this point's rest
        // position, including both guides and the extra sphere correction.
        float distance_m=displacement_pixels==3?.050:distance(world_point,rest_world_point);
        ALBEDO=vec3(clamp(distance_m/.16,0.0,1.0),1.0,0.0);
    }
}
