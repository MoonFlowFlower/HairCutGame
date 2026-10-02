uniform vec4 base_color : source_color=vec4(0.38,0.28,0.48,1.0);
uniform bool strands=false;
uniform bool hair_optimization=false;
uniform bool stable_sampling=false;
uniform bool soft_hair_lighting=false;
uniform bool edge_only=false;
uniform bool plush_geometry=false;
uniform bool mask_pass=false;
uniform bool depth_pass=false;
uniform bool microfiber_core=true;
uniform bool short_trim=true;
uniform bool short_carve=true;
uniform int optimization_round=0;
uniform int shell_index=0;
uniform int shell_count=0;
uniform bool transport_material=false;
uniform sampler2D nap_height:filter_linear_mipmap_anisotropic,repeat_enable;
uniform sampler2D nap_normal:hint_normal,filter_linear_mipmap_anisotropic,repeat_enable;
uniform bool states_enabled=false;
uniform int state=0;
uniform bool cut_pending=false;
uniform vec3 cut_center=vec3(0);
uniform vec3 cut_normal=vec3(0,1,0);
varying vec3 p;
varying vec3 local_normal;
varying mat3 hair_frame;
float hash(vec3 p0){vec3 q=fract(p0*0.3183099+vec3(.17,.41,.73));q*=17.0;return fract(q.x*q.y*q.z*(q.x+q.y+q.z));}
float region(vec3 q){if(state==2)return smoothstep(.24,.42,q.x+.20*q.y)*smoothstep(-.03,.13,q.y);if(state==3||state==4)return smoothstep(-.025,.025,q.x);return 0.0;}
void vertex(){
    p=VERTEX;local_normal=NORMAL;hair_frame=mat3(MODEL_MATRIX);
    if(optimization_round>=3){
        // <= 8 mm local motion, encoded only on a proven articulation subtree.
        VERTEX+=UV2.x*vec3(sin(TIME*8.0)*.003,-.0035-.0025*sin(TIME*5.0),0);
        if(shell_index>0){float pile=COLOR.a>.7?.002:COLOR.a>.2?.003:.012;VERTEX+=NORMAL*pile*float(shell_index)/float(max(shell_count,1));}
    }
}
void fragment(){
    if(edge_only&&abs(dot(normalize(NORMAL),VIEW))>.42)discard;
    if(shell_index>0&&hash(floor(p*260.0)+vec3(float(shell_index)*7.13))>.20)discard;
    if(strands&&cut_pending&&distance(p,cut_center)<.245&&dot(p-cut_center,cut_normal)>-.006)discard;
    float aa=1.0-smoothstep(.4,1.7,length(fwidth(p))*380.0);
    // The scanned nap supplies nonperiodic detail. Discontinuous voxel noise
    // and phase-locked sine bands generated visible interference on top of it.
    float grain=stable_sampling?.5:hash(floor(p*430.0));
    float flow=stable_sampling?0.0:sin(p.x*430.0+p.y*91.0+sin(p.z*37.0)*3.0);
    float root=smoothstep(-.17,.25,p.y);
    float root_tint=soft_hair_lighting?(.91+.09*root):(.77+.23*root);
    vec3 strand_tint=soft_hair_lighting?mix(COLOR.rgb,vec3(1),.35):COLOR.rgb;
    vec3 color=base_color.rgb*root_tint;
    float bundles=stable_sampling?0.0:sin((p.x+.16*sin(p.z*5.0))*116.0+p.y*11.0);
    if(strands){color*=strand_tint*(.96+.07*bundles);}
    else {color*=.95+aa*(bundles*.10+flow*.028+(grain-.5)*.10);}
    float mask=states_enabled?region(p):0.0;
    float rough=strands?.79:.94;
    if(state==2){color=mix(color,vec3(.020,.015,.026)*(1.0+.4*grain),mask);rough=.99;}
    if(state==3){color=mix(color,color*vec3(.40,.43,.52)*(0.90+0.10*sin(p.x*125.0+p.y*19.0)),mask);rough=mix(.91,.64,mask);}
    if(state==4){color=mix(color,vec3(.33,.47,.65)*(1.0+.11*flow*aa),mask);rough=mix(.91,.57,mask);}
    float cut=strands?0.0:COLOR.a;
    bool old_surface=hair_optimization&&((COLOR.a>.7&&!short_carve)||(COLOR.a>.2&&COLOR.a<.7&&!short_trim));
    if(hair_optimization)cut=old_surface?1.0:0.0;
    ALBEDO=color*(1.0+cut*aa*(grain-.5)*.16);ROUGHNESS=mix(rough,.98,cut);SPECULAR=mix(state==3?.20:strands?.22:.045,.035,cut);RIM=mix(strands?.28:.12,0.0,cut);RIM_TINT=.95;BACKLIGHT=color*.09;
    if(!strands&&!stable_sampling)NORMAL=normalize(NORMAL+vec3(dFdx(grain),dFdy(grain),0.0)*.14*aa);
    if(transport_material){
        ROUGHNESS=mix(strands?.77:.92,rough,mask);
        ROUGHNESS=mix(ROUGHNESS,.97,cut);
        SPECULAR=mix(strands?.36:.22,.08,cut);RIM=strands?.16:.09;RIM_TINT=.8;
        BACKLIGHT=color*(strands?.24:.035);
        if(!strands&&!stable_sampling){
            vec3 weights=pow(abs(local_normal),vec3(4));weights/=max(dot(weights,vec3(1)),.0001);
            vec3 q=p*5.0;
            float height=texture(nap_height,q.zy).r*weights.x+texture(nap_height,q.xz).r*weights.y+texture(nap_height,q.xy).r*weights.z;
            vec2 nx=texture(nap_normal,q.zy).xy*2.0-1.0,ny=texture(nap_normal,q.xz).xy*2.0-1.0,nz=texture(nap_normal,q.xy).xy*2.0-1.0;
            vec3 gradient=vec3(0,nx.y,nx.x)*weights.x+vec3(ny.x,0,ny.y)*weights.y+vec3(nz,0)*weights.z;
            float dry=1.0-(state==3?mask*.70:0.0);
            ALBEDO*=mix(1.0,.59+height*.95,dry*(1.0-cut));
            NORMAL=normalize(NORMAL+mat3(VIEW_MATRIX)*hair_frame*gradient*.72*dry*(1.0-cut));
            AO=.70+height*.30;AO_LIGHT_AFFECT=.10;
        }
        // Wet nap uses its roughness/specular response; a varnish layer produced blown white islands.
        CLEARCOAT=0.0;CLEARCOAT_ROUGHNESS=.70;
    }
    if(hair_optimization){
        float trimmed=short_trim?1.0-smoothstep(.05,.22,abs(COLOR.a-.5)):0.0;
        float interior=short_carve?smoothstep(.70,.95,COLOR.a):0.0;
        float short_pile=max(trimmed,interior);
        // One continuous material through the volume: cut planes do not erase normals.
        if(!strands&&microfiber_core&&!old_surface){
            vec3 w=pow(abs(local_normal),vec3(4));w/=max(dot(w,vec3(1)),.0001);
            // Preserve visible fiber scale after mip filtering at the real FPS distance.
            vec3 q=p*(optimization_round>=2?mix(1.15,1.75,short_pile):mix(12.0,22.0,short_pile));
            float h=texture(nap_height,q.zy).r*w.x+texture(nap_height,q.xz).r*w.y+texture(nap_height,q.xy).r*w.z;
            vec2 nx=texture(nap_normal,q.zy).xy*2.0-1.0,ny=texture(nap_normal,q.xz).xy*2.0-1.0,nz=texture(nap_normal,q.xy).xy*2.0-1.0;
            if(stable_sampling){
                // Filter each triplanar projection using its real pixel footprint.
                // A small wider footprint avoids detail near the Nyquist limit;
                // there is no whole-screen blur or render-resolution change.
                vec3 dx=dFdx(q)*1.6,dy=dFdy(q)*1.6;
                h=textureGrad(nap_height,q.zy,dx.zy,dy.zy).r*w.x+textureGrad(nap_height,q.xz,dx.xz,dy.xz).r*w.y+textureGrad(nap_height,q.xy,dx.xy,dy.xy).r*w.z;
                nx=textureGrad(nap_normal,q.zy,dx.zy,dy.zy).xy*2.0-1.0;
                ny=textureGrad(nap_normal,q.xz,dx.xz,dy.xz).xy*2.0-1.0;
                nz=textureGrad(nap_normal,q.xy,dx.xy,dy.xy).xy*2.0-1.0;
            }
            vec3 g=vec3(0,nx.y,nx.x)*w.x+vec3(ny.x,0,ny.y)*w.y+vec3(nz,0)*w.z;
            NORMAL=normalize(NORMAL+mat3(VIEW_MATRIX)*hair_frame*g*(stable_sampling?.30:optimization_round>=2?.62:.95));
            float comb=stable_sampling?0.0:sin((p.x+p.z*.22)*790.0+p.y*24.0)*aa;
            ALBEDO=color*((stable_sampling?.79:optimization_round>=2?.64:.83)+(stable_sampling?.46:optimization_round>=2?.78:.35)*h)*(1.0+trimmed*(.025+comb*.023)-interior*.035);
            AO=.88+h*.12;AO_LIGHT_AFFECT=.05;
        }
        ROUGHNESS=mix(.94,.98,short_pile);SPECULAR=.10;RIM=strands?.19:.13;RIM_TINT=.9;
        BACKLIGHT=ALBEDO*(strands?.16:.04);
        if(optimization_round>=2){
            if(state==3){
                // Wetness gets only a modest color shift; clustered nap and broader
                // reflections distinguish it from the near-black burnt state.
                vec3 dry_color=base_color.rgb*root_tint;
                if(strands)dry_color*=strand_tint;
                ALBEDO=mix(ALBEDO,dry_color*.86,mask);
                ROUGHNESS=mix(ROUGHNESS,optimization_round>=3?.69:.48,mask);SPECULAR=mix(.10,optimization_round>=3?.19:.32,mask);
            }
            if(state==4){ROUGHNESS=mix(ROUGHNESS,.94,mask);SPECULAR=.13;}
        }
        if(soft_hair_lighting){
            // Bounded local fill approximates unresolved scattering between
            // fibers. SDFGI replaces IRRADIANCE later in the Forward+ pass;
            // this term survives it without changing room/direct shadows.
            // These meshes do not inject light into GI (GIMode.Disabled).
            EMISSION=ALBEDO*.22;
            BACKLIGHT=ALBEDO*(strands?.22:.07);
        }
    }
    if(plush_geometry){ROUGHNESS=.98;SPECULAR=.07;EMISSION=ALBEDO*.30;BACKLIGHT=ALBEDO*.26;RIM=.22;}
    if(mask_pass){ALBEDO=vec3(0);EMISSION=vec3(1);SPECULAR=0.0;ROUGHNESS=1.0;}
    if(depth_pass){ALBEDO=vec3(0);EMISSION=vec3(clamp(-VERTEX.z/8.0,0.0,1.0));SPECULAR=0.0;BACKLIGHT=vec3(0);RIM=0.0;}
}
