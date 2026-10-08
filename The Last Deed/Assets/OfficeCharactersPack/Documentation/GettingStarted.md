# Office Characters Pack — Getting Started

Low-poly rigged office characters for Unity. This guide covers everything you need to start
using them.

## What's included

- **Office Worker** — shirt, tie, lanyard with badge
- **Receptionist** — blouse with a neck scarf, pencil skirt, hair in a bun, headset
- **Manager** — shirt with rolled sleeves, waistcoat, glasses
- **Janitor** — coveralls, cap, rubber gloves; carries a mop

## Render pipeline

Office Characters Pack is **URP only**. All prefabs live in `Prefabs/URP/` and their
materials use Universal Render Pipeline shaders. Make sure your project is set to URP
(Project Settings → Graphics / Quality) — under the Built-in or HD pipelines the materials
render magenta.

## Using the characters

Drag any prefab from `Prefabs/URP/` into your scene. Every character stands in T-pose at
metric scale (about 2 m tall), with its pivot on the ground between the feet and facing +Z,
so it drops onto a floor at Y 0 with no manual offset.

### Animating

Every character shares **one 25-bone skeleton**, built for this pack, whose bones carry
Unity's Humanoid names (`Hips`, `Chest`, `LeftUpperArm`, …). Each FBX is imported as a Unity
**Humanoid** with that skeleton already mapped to Unity's Humanoid avatar, so the rig is
**compatible with Mixamo animations** — and with Starter Assets, Asset Store packs or any
other Humanoid clip — with no retargeting setup:

1. Import your clip with **Rig → Animation Type: Humanoid**.
2. Create an Animator Controller and add the clip as a state.
3. Assign the controller to the prefab's `Animator` (the avatar is already set).

Because the skeleton is identical across the cast, one controller drives every character.
For in-place loops, tick **Bake Into Pose** for Root Transform Rotation / Y / XZ on the clip,
or turn off **Apply Root Motion** on the `Animator`.

No animation clips are included.

### Recolouring

Materials are flat colours from one shared palette (`Materials/URP/Mat_<Swatch>`), with no
texture maps. Every character's body has one material slot per colour, so you can retint a
single character by assigning a duplicated material to its slot, or retint every character
using a colour by changing the shared `Mat_<Swatch>` itself.

## Sample scene

`Samples/URP/URPScene.unity` — the characters lined up in a plain daylight studio, lit for
URP, each already wired to `Samples/URP/Animations/CharacterAnimator.controller`.

That controller has one empty state, `Default`, ready for any Humanoid clip: open it in the
**Animator** window, select **Default**, and drag a clip into its **Motion** field. Press
**Play** and every character in the scene plays it, because they all share one skeleton.

Until a clip is in the state, characters in Play mode hold Unity's neutral Humanoid pose (arms
half raised, knees slightly bent) — that's expected, not a rig problem.

## Troubleshooting

**Materials render magenta / pink** — the project isn't on the Universal Render Pipeline, or
no URP asset is assigned in Project Settings → Graphics. This pack ships URP materials only.

**A clip won't play, or the character stays in T-pose** — the clip was imported as Generic or
Legacy. Set its **Rig → Animation Type** to **Humanoid** and reassign it in the controller.

**A character slides or turns away while animating** — the clip carries root motion. Tick
**Bake Into Pose** for the root transform on the clip, or turn off **Apply Root Motion** on
the `Animator`.
