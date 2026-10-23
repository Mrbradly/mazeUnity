# Version 4 — Moving Wardens + Stun Blaster

- Added two Wardens (three total), separate four-waypoint routes, and aggregate HUD alerts/status.
- Replaced transform-target movement with pure, continuously tested cell-edge navigation. Spawn uses the route's connected first cell; model height cannot stall horizontal movement. Capture/release now sets active simulation explicitly. Startup grace is three active seconds. The user's stationary behavior could not be reproduced in a native Editor here, so this is not a confirmed diagnosis of their previous copy.
- Added a primitive first-person blaster, an 18 m occluded hitscan shot, a reusable blue shot beam, four-second Warden stun/recovery, and a 0.4 s cooldown with unlimited shots. Stunned eyes turn blue; the HUD shows recharge and hit feedback.
- Left click now fires; E remains the interaction control. Firing and movement remain separate from input. Pause freezes movement/stun/cooldown; terminal states disable firing and captures. Retry resets all Wardens and the blaster.
- Expanded continuous navigation, stun/cooldown, serialized scene, and supplied Unity Play Mode coverage. Desktop only; no XR/Android configuration.

# Desktop start/input compatibility fix

- Added a visible Begin/Resume Exploration button and support for both Return and numpad Enter.
- Detects a new-Input-System-only project before polling legacy Input APIs; shows a clear Enable Controls screen instead of throwing before Enter/click can start the game.
- Documents Active Input Handling = Both / Input Manager (Old), Game view focus, and Console Error Pause when importing into another Unity project. Does not change project settings or add input/VR packages.

# Desktop material compatibility fix

- Added an Editor-only **LUMEN → Fix Pink Materials** command and import-time repair for Built-in/URP shader mismatches.
- Preserves maze material colours, emission, shared references, and opaque matte settings while choosing the current renderer's shader. Limits writes to the maze materials and leaves render-pipeline/VR/Android settings unchanged.
- Added instructions for importing only the repair folder into an existing project, plus Unity 6's built-in material-converter route. Native Editor execution remains to be verified locally.

# Version 3 — The Warden

- Added a single mechanical sentinel assembled from sixteen primitive cubes, with an amber/red state eye. Kept it outside static batching.
- Added four-point patrol, directional native line-of-sight detection, slower-than-player pursuit, last-sighting search, and return to patrol. Uses the existing maze grid without NavMesh packages or baking.
- Added active-time startup grace, shared pause handling, clear pursuit/search warnings, capture rules, and a caught/retry screen.
- Made caught/victory states mutually exclusive and blocked further movement, interaction, key collection, unlocks, and timer progress after either ending. Retry resets the entire scene and enemy.
- Expanded pure C# decision tests, serialized enemy validation, and separate Unity Play Mode enemy scenarios. Native Unity execution remains pending.
- Preserved desktop-only configuration and modular controls; no VR or Android changes.

# Version 2 — LUMEN / The Way Out

- Replaced the plain wall/floor presentation with slate walls, dark plinths, pale caps, inset tiles, and thin emissive guide strips. Paths near key alcoves use their key's colour.
- Built 01/02/03 alcove signs from primitive seven-segment geometry, with framed floor sockets and hovering keys.
- Reworked the exit into a two-panel brass seal with a center seam, lock housing, three live signal indicators, lit frame, and striped landing. Door movement uses an eased lift.
- Replaced default UI boxes with a coordinated desktop HUD: key status cards, compass, active timer, coloured focus reticle, contextual activation prompt, timed notifications, welcome screen, and victory/replay screen.
- Added optional local discovery mapping on M, with isolated C# discovery rules and a Unity position adapter. The map does not reveal remote objectives and resets on replay.
- Added focus feedback to the interaction boundary and spent socket feedback on collection. Kept keyboard/mouse reads confined to the desktop input adapter.
- Added static batching for immovable decoration; excluded the door, pickups, player, and mutable socket indicators.
- Expanded non-Unity and Unity test coverage. Native Unity runs remain pending; no VR/Android configuration or headset assumptions were added.
