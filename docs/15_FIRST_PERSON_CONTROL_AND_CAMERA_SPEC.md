# 15 — First-Person Control & Camera Spec

## Main perspective
Normal gameplay is first-person.

The customer head should often occupy a large portion of the view during precision work, while camera movement still allows awareness of nearby teammates/tools.

## Inputs
| Input | Action |
|---|---|
| WASD | Move |
| Mouse | Look |
| E | Context interaction / pick up / place prop |
| G | Drop held tool |
| LMB | Primary tool action |
| RMB | Secondary tool action when applicable |
| R | Reload/reset when applicable |

No individual submit control in shared-head mode.

## Body
The player has a physical body/collider. Avoid ghost-camera interaction.

Need:
- teammate crowding,
- external impulse from blower,
- meaningful collision with large held tools,
- suction tug or equivalent.

Movement should remain controllable; physical comedy should not make basic navigation miserable.

## Held tool presentation
- readable first-person tool model,
- do not obscure the exact work area excessively,
- large tools can legitimately occupy more screen/space,
- world model for remote players should match collision extent.

## Own hair feedback
Because player hair can be modified:
- remote players see it directly,
- mirrors provide primary self-view,
- excessive hair may intrude slightly into first-person view,
- smoke/fire/spray feedback may appear at view edges.

Do not cover the screen so often that play becomes unreadable.

## Customer framing
Customer head should be large enough to target, but players need room to circle all sides. Avoid forcing all four cameras into one exact interaction point.
