shader_type spatial;
render_mode unshaded, depth_draw_never, cull_back;
void fragment(){float facing=max(dot(NORMAL,VIEW),0.0);ALBEDO=vec3(.14,.12,.17);ALPHA=.34*pow(facing,1.6);}
