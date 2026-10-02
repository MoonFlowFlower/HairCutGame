shader_type spatial;
render_mode cull_disabled, diffuse_burley;
// Core casts the silhouette shadow; opaque fibers receive it, including under the fringe.
#include "puppet_hair_shared.gdshaderinc"
