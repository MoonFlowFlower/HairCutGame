shader_type spatial;
render_mode unshaded, cull_disabled, alpha_to_coverage;
#define SOFT_VOLUME_SAMPLING
#include "puppet_volume_fur_shared.gdshaderinc"
