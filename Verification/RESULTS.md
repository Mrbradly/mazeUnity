# Verification results

Date: 2026-10-23 (UTC). Version 3 / The Warden.

## Passed in this environment

| Check | Result |
| --- | --- |
| Runtime C# source compile | 0 warnings, 0 errors; all files under Core and Runtime compiled together |
| Device-independent rules, navigation, discovery, and sentinel decisions | 3,031 assertions passed |
| Complete grid route, key order 012 | Passed; 90 grid steps |
| Complete grid route, key order 021 | Passed; 102 grid steps |
| Complete grid route, key order 102 | Passed; 90 grid steps |
| Complete grid route, key order 120 | Passed; 102 grid steps |
| Complete grid route, key order 201 | Passed; 66 grid steps |
| Complete grid route, key order 210 | Passed; 66 grid steps |
| Serialized scene, component, and asset validation | 20,862 assertions passed |

`RulesCheck` executes the actual `MazeProgress`, `MazeLayout`, `MazeExploration`, and `SentinelBrain` C# sources. It checks reachable keys, no locked-door bypass, route adjacency, all six key orders, duplicate collection, invalid IDs, 0/1/2-key rejection, required unlock before victory, one-shot victory, clean new state, malformed layouts, local-only discovery, ignored invalid/wall visits, repeat visits, and fresh survey state. These simulations traverse grid cells, not Unity colliders.

Sentinel checks cover a 400-step closed-door patrol soak, cardinal-only steps, visiting all four waypoints, range/cone boundaries, alerted sight, loss of sight through a supplied obstruction signal, capture distance and obstruction gates, chase → last-known-position search → patrol, reacquisition, blocked/open door routing, invalid patrol data, and loss/victory mutual exclusion. The obstruction signal is supplied by these pure tests; native Physics.Linecast behavior is unverified here.

`check_scene.py` parses the saved `.unity`, materials, metadata, and settings. It checks internal/external references, GUID uniqueness, component ownership, parent/child links, serialized script assignments, solid wall locations/sizes, unique key IDs and trigger colliders, player/door/exit locations, the kinematic exit trigger, camera/light settings, materials, build scene inclusion, desktop input, and absence of Android/XR choices. These checks do not prove Unity imports or renders the scene.

Additional scene checks cover live door indicator/material arrays, map source assignments, the static batching root, exclusion of movable objects and mutable pickup indicators from that root, and a geometry budget below 10,000 triangles. Sentinel checks cover exactly one enemy, reference assignments, spawn matching the patrol source, a trigger volume, startup grace, range/cone/memory configuration, exclusion from static batching, and speeds below the player's walking speed. Current scene: 565 objects, 544 primitive cubes, 6,528 triangles, thirteen materials. No draw call, FPS, or headset performance result is claimed.

Compilation used .NET SDK 8.0.426 with C# language version 9 and real UnityEngine managed assemblies for Unity 2022.3.62, obtained from `RocketModFix.UnityEngine.Redist` NuGet version `2022.3.62.3`. No Unity API stubs or mocks were used. The external reference DLLs are not shipped with this project. This checks C# API/type correctness against those assemblies; it is not compilation by the Unity Editor, a player build, or native runtime execution. Unity test assembly compilation is pending the Editor's package import.

## Not run / unverified

- Unity Editor import and script compilation.
- The eight supplied Unity Edit Mode tests and six Unity Play Mode tests.
- Native CharacterController wall/door collision, raycast interaction, and exit trigger callbacks.
- Desktop mouse/keyboard, rendered maze/HUD/victory screen, map toggle, focus effects, live indicators, static batching, and replay button behavior.
- Native enemy motion/sight/capture, chase balance, caught/retry screen, startup grace, and pause/win behavior.
- Standalone player build.
- Android build, headset rendering/input, headset compatibility, and device performance.

The cloud workspace initially contained no Unity project, Unity Editor, or C# compiler. A local .NET SDK was installed outside the project for the reference compile and rules checks. No Unity Editor was installed, so an engine run was unavailable. No Unity tests or device checks are claimed as passed.

## Unity tests supplied for the next validation step

Edit Mode: duplicate keys cannot unlock; all keys → door → exit is required; invalid key IDs; local survey knowledge; keys reachable and no locked exit bypass.

Sentinel Edit Mode: last-sighting search and timeout; sight/capture obstruction/cone gates; caught/victory mutual exclusion.

Play Mode: actual character wall and locked-door collision; ray range and wall occlusion; traversal to all pickups, floor socket/door indicator material changes, unlock, exit trigger, victory state, and replay reset including the survey. Tests disable the keyboard driver and feed the real motor/interactor directly, so manual mouse/keyboard and visual checks are still needed even after these pass.

Sentinel Play Mode: active-time grace and pause; native open-corridor sight and wall obstruction; chase/search, contact capture, caught HUD/timer freeze, full retry reset; victory stopping the Warden. Existing maze tests disable the enemy to isolate the key/exit flow. Enemy tests disable its automatic Update and drive the actual Tick method explicitly. Native suites remain unrun.
