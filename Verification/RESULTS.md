# Verification results

Date: 2026-10-23 (UTC). Version 4 / Moving Wardens + Stun Blaster.

## Passed in this environment

| Check | Result |
| --- | --- |
| Runtime C# reference compile, `ENABLE_LEGACY_INPUT_MANAGER` | 0 warnings, 0 errors |
| Runtime C# reference compile, new-system-only `ENABLE_INPUT_SYSTEM` | 0 warnings, 0 errors |
| Pure gameplay/navigation/stun/cooldown checks | 46,488 assertions passed |
| Saved scene, component and asset validation | 22,474 assertions passed |
| All six key collection orders, unlock, exit rules | Passed |
| Three independent continuous patrol simulations | Each ran 3 minutes at 20 simulated steps/s, visited every waypoint, stayed on corridor edges, and did not stall |
| Mid-edge stun and recovery, refreshed stun | Passed for all three routes |
| Fire cooldown, pause/end firing gates, unlimited shots, fresh state | Passed |

`RulesCheck` executes the actual `MazeProgress`, `MazeLayout`, `MazeExploration`, `SentinelBrain`, `SentinelNavigator`, and `BlasterState` C# sources. It checks required keys/unlock/exit order, no locked-door bypass, all six key orders, duplicate/invalid key handling, terminal states, local map discovery, patrol routing, directional detection/capture gates, last-sighting search and expiry, continuous movement, four-second stun, and cooldown behavior. Visibility/obstruction are supplied signals in pure tests. These simulations do not run Unity physics or supply keyboard/mouse events.

Continuous navigation checks advance each of the three routes for 3,600 frames at 0.05 seconds/frame. They check actual world coordinates, a speed bound, absence of consecutive stalled frames, cardinal grid edges, walkable cells, and reaching all waypoints. At a waypoint U-turn the net frame displacement can be less than the distance travelled; the test allows that reversal. Each simulated Warden covers over 210 metres in three active minutes. Shots stop motion mid-edge; expiry resumes movement from that edge. Search does not obtain the hidden player's position.

`check_scene.py` parses the saved scene/materials/settings and checks file IDs/GUIDs, hierarchy, all serialized assignments, three distinct patrol assets and correct spawns, shootable enemy trigger volumes, the blaster's muzzle/beam references and settings, all-three HUD references, solid walls, keys, door/exit setup, mutable objects excluded from static batching, desktop input configuration, and no XR/Android choices. It does not prove native import or rendering.

The saved scene contains **609 GameObjects, 582 primitive cubes, 6,984 triangles, 13 shared texture-free materials, three Wardens, three keys, and one blaster**. It stays under the existing 10,000-triangle geometry guard. No draw-call, frame-time, or device-performance result is claimed.

Runtime compilation used .NET SDK 8.0.426 / C# 9 and actual UnityEngine managed reference assemblies for Unity 2022.3.62 (`RocketModFix.UnityEngine.Redist` 2022.3.62.3). No Unity API stubs/mocks were used. Reference DLLs and tooling are outside the project and are not distributed. A reference compile checks managed API/types; it is not Unity Editor compilation or a player build.

## Unity verification still required

No Unity Editor is installed in this environment. The supplied Unity Edit Mode and Play Mode tests have **not run**. Native physics, rendering, cursor capture, keyboard/mouse, material repair, HUD buttons, wall/door collisions, actual raycasts, exit triggers, enemy capture/stun, and replay require Unity testing. No desktop executable or Android build was produced.

Existing Edit Mode suites cover key/exit rules, discovery, sentinel perception/search, and terminal states. Existing Play Mode suites isolate wall/door collision, pickups, inventory/indicator feedback, exit victory/reload, sight occlusion, chase/capture, pause, and retry. Maze tests disable all enemies to isolate the objective. Enemy tests disable automatic Update and drive the real adapters directly.

New `BlasterGameplayTests` cover all three Wardens moving/pausing, a native ray hitting/stunning a Warden, mid-stun pause, recovery, blocked/recharged firing, walls/range blocking shots, and victory preventing further shots. These are test cases supplied for Unity, not passed results. Run them using **Window → General → Test Runner**, Edit Mode and Play Mode, or `Tools/run_unity_tests.sh` with a licensed Editor.

The user reported stationary Wardens in their prior copy. Without their native runtime/Console a cause was not established. Version 4 replaces the transform-target loop with tested continuous cell-edge navigation, uses connected route spawns independently of model height, and explicitly activates the session on desktop capture/resume. This is not a claim to have reproduced the original issue in Unity.

The user reports playing the previous desktop version successfully. That does not establish native correctness of these new features. No VR/Android configuration was added; ClassVR runtime/render SDK, Android target API, and input mappings still need confirmation before device work.
