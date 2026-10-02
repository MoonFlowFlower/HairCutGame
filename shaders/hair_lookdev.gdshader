shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;
// Opaque one-pass presentation. COLOR changes to local state only at phase 5.
instance uniform vec4 base_color : source_color = vec4(0.47,0.32,0.23,1.0);
instance uniform vec4 average_state = vec4(0.0);
instance uniform vec4 features = vec4(1.0); // flock, rim, micro, proximity detail
instance uniform int species = 0; // human, animal, alien: visual profiles only
instance uniform bool local_states = false;
instance uniform vec3 visual_offset = vec3(0.0);
varying vec3 local_pos;
void vertex(){local_pos=VERTEX;
    VERTEX+=visual_offset*smoothstep(-0.05,0.65,local_pos.y)*clamp(UV.x/0.10,0.0,1.0);
    if(species==1){vec3 grain=vec3(sin(VERTEX.y*25.0+VERTEX.z*19.0),sin(VERTEX.x*27.0+VERTEX.z*21.0),sin(VERTEX.x*23.0+VERTEX.y*26.0));float fleece=features.x*(1.0-average_state.r)*(1.0-average_state.a);float plane=1.0-smoothstep(0.93,0.995,NORMAL.y);NORMAL=normalize(NORMAL+(grain-NORMAL*dot(grain,NORMAL))*0.13*fleece*(0.2+0.8*plane));}
}
void fragment(){
    vec4 state=local_states?COLOR:average_state;
    float wet=state.r;float frost=state.g;float burn=state.b;float glue=state.a;
    float dry=features.x*(1.0-wet)*(1.0-glue*0.65)*(1.0-burn*0.8);
    // Broad curved flow + fine fiber modulation, analytic filtering, no clock noise.
    float flow=local_pos.x*48.0+local_pos.y*14.0+sin(local_pos.z*3.0+local_pos.y)*5.0;
    float fibers=sin(flow)*sin(flow*0.47+1.2);
    float filtered=1.0-smoothstep(0.35,2.0,fwidth(flow));
    float micro=sin(local_pos.x*173.0)*sin(local_pos.y*157.0)*sin(local_pos.z*191.0);
    float micro_filter=1.0-smoothstep(0.04,0.12,length(fwidth(local_pos)));
    vec3 col=base_color.rgb*(1.0+fibers*0.075*dry*filtered*features.w+micro*0.035*features.z*dry*micro_filter*features.w);
    if(species==1){
        // Rounded fleece clusters: filtered tonal relief, no transparent fur shells.
        float tufts=sin(local_pos.x*24.0)*sin(local_pos.y*21.0+local_pos.z*12.0)*sin(local_pos.z*23.0);
        float tuft_filter=1.0-smoothstep(0.25,1.2,length(fwidth(local_pos))*24.0);
        col=base_color.rgb*(1.0+tufts*0.10*dry*tuft_filter+micro*0.035*features.z*dry*micro_filter);
    }
    if(species==2){col=base_color.rgb*(1.0+micro*0.055*features.z*dry*micro_filter);}
    col*=1.0-wet*0.18;
    col=mix(col,vec3(0.78,0.63,0.35),glue*0.12);
    col=mix(col,vec3(0.63,0.76,0.85),frost*0.75);
    col=mix(col,vec3(0.065,0.05,0.043),burn*0.88);
    ALBEDO=col;METALLIC=0.0;
    ROUGHNESS=clamp(0.84-wet*0.25-glue*0.16+burn*0.08,0.45,0.98);
    SPECULAR=0.25+wet*0.14+glue*0.08;
    RIM=0.15*features.y*(0.5+dry*0.5);RIM_TINT=0.55;
    if(species==1){ROUGHNESS=clamp(0.94-wet*0.20-glue*0.15,0.6,0.98);SPECULAR=0.18+wet*0.14;RIM=0.28*features.y*(0.5+dry*0.5);RIM_TINT=0.8;}
    if(species==2){ROUGHNESS=clamp(0.65-wet*0.16-glue*0.10+burn*0.25,0.4,0.96);SPECULAR=0.38;RIM=0.20*features.y;RIM_TINT=0.7;BACKLIGHT=col*0.035*(1.0-burn);}
    CLEARCOAT=max(wet*0.12,glue*0.10);CLEARCOAT_ROUGHNESS=0.5;
}
