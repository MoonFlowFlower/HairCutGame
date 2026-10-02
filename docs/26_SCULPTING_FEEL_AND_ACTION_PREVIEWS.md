# Sculpting Feel & Action Previews

## Design rule

**Player action should be accurate; material consequences should be challenging.**

The game should not ask the player to fight ambiguous brush depth or hidden volume edits.

## Three-stage feedback

### Before action — show what will happen

#### Precision clipper
Show a subtle semi-transparent removal preview matching the actual shallow layer/plane that will be removed on activation.

#### Growth spray
Show a short-horizon projected extrusion volume/direction in translucent green or an equally readable in-world preview. It represents a brief spray duration, not infinite future growth.

#### Hedge trimmer
Show the actual blade/cut plane and affected side of the plane.

#### Blower / vacuum
Show a small set of directional surface arrows/flow indicators on the local affected region. Avoid screen-filling vector fields.

#### Glue
Highlight the contact surfaces/join that would be bonded.

Do not use previews to auto-solve the target; they communicate tool consequences only.

### During action — stable mapping

- Effect orientation follows visible tool orientation.
- Precision mode may apply light stroke stabilization and gentle near-horizontal/near-vertical alignment assistance.
- Do not auto-snap to the target model.
- Surface editing should prefer the visible/front surface and avoid accidental back-face edits, except explicitly penetrating tools.
- Held strokes should not unexpectedly deepen beyond their advertised layer without a clear deliberate re-engagement rule.

### After action — readable result

Distinct feedback for:
- no contact,
- shallow contact,
- thick/heavy contact,
- severing a connected part,
- striking frozen/glued/burnt material.

Use sound, debris amount, tool resistance, particles and visual state changes. Camera shake must remain subtle enough for precision work.

## Spatial target ghost

The existing view-dependent target outline remains valuable as an accessibility/precision aid, but should no longer dominate ordinary play.

Recommended presentation:
- off by default in normal play or shown only briefly during onboarding,
- toggle/hold via a dedicated assist setting/input,
- never required to understand the target,
- board + miniature must be sufficient for ordinary play.

## Resolution caution

Do not immediately halve the existing volume grid spacing solely because precision feels poor. First fix:
- action previews,
- surface protection,
- stroke mapping,
- move/pull tools,
- large-plane vs fine-cut identities.

Then use fixed torture tests (thin lip, clean hole, sloped plane, flat helipad) to determine whether local or global resolution must change.
