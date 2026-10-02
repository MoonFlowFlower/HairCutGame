shader_type spatial;
render_mode diffuse_burley;
uniform vec4 base_color:source_color=vec4(.5,.4,.3,1);
uniform float roughness=.8;
uniform float metal=0.0;
uniform int kind=0;
uniform bool transport_material=false;
varying vec3 p;
float hash(vec3 q){q=fract(q*.3183099+vec3(.17,.41,.73));q*=17.0;return fract(q.x*q.y*q.z*(q.x+q.y+q.z));}
float noise(vec3 q){vec3 i=floor(q),f=fract(q);f=f*f*(3.0-2.0*f);return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);}
void vertex(){p=(MODEL_MATRIX*vec4(VERTEX,1)).xyz;}
void fragment(){
 float coarse=noise(p*8.0),fine=noise(p*220.0),bump=fine;
 vec3 c=base_color.rgb*(.91+coarse*.12);
 if(kind==1){float wave=sin(p.x*166.0+noise(p*vec3(13,2,1.5))*18.0+p.z*3.0);c*=.96+wave*.027+noise(p*vec3(70,4,2))*.055;bump=wave*.08+fine*.15;}
 if(kind==2){c*=.94+noise(p*53.0)*.11;bump=fine*.42;}
 if(kind==3){c*=.96+fine*.08;bump=fine*.15;}
 ALBEDO=c;ROUGHNESS=clamp(roughness+(fine-.5)*.10,.1,1.0);METALLIC=metal;SPECULAR=.25;
 if(transport_material){SPECULAR=.5;ROUGHNESS=clamp(roughness-(kind==1?.10:kind==3?.08:0.0)+(fine-.5)*.08,.15,.95);}
 NORMAL=normalize(NORMAL+vec3(dFdx(bump),dFdy(bump),0)*.20);
}
