# LUMEN / The Way Out — desktop prototype v3: The Warden

A new Unity **2022.3.62f1** project using the built-in render pipeline. No existing project was present in the workspace, so this version is a new desktop baseline, not a claim about ClassVR compatibility.

Version 2 adds a cohesive signal-vault visual style: slate architecture, wall caps and skirting, inset floor tiles, coloured guide strips, numbered alcoves, a detailed brass exit seal, and a new desktop HUD. Optional local mapping supports retracing your route without exposing the whole maze.

Version 3 adds **the Warden**, one patrolling mechanical sentinel. It spots the player, pursues, searches the last known position, and returns to patrol when its search expires. Getting caught ends the run and opens a retry screen. The original three-key / exit objective remains.

`Previews/` contains AI-generated illustrations of this design, clearly distinguished from Unity runtime captures.

## Open and play

1. Extract the project ZIP. In Unity Hub, choose **Add project from disk** and select the folder containing `Assets`, `Packages`, and `ProjectSettings`.
2. Open with Unity **2022.3.62f1**. Allow Unity to import assets and resolve its Test Framework package. Unity generates the normal `Library` and any missing default settings on first import.
3. Open **Assets/EscapeMaze/Scenes/EscapeMaze.unity** and press **Play**.
4. Click inside the Game view and choose **Begin Exploration**, or press **Enter** / **numpad Enter**, to capture the mouse.

### Enter or clicks do not start the game

This desktop adapter uses Unity's legacy Input Manager. If you imported the assets into a Unity 6 project configured for the new Input System only, Unity can throw an error before the start controls are read. Stop Play mode, then open **Edit → Project Settings → Player → Other Settings → Active Input Handling** and choose **Both** (or **Input Manager (Old)**). Restart Unity if requested, press Play, click the Game view, and choose **Begin Exploration**. If Unity has paused itself because Console **Error Pause** is enabled, fix the error first and unpause before testing again.

The updated adapter detects the new-Input-System-only configuration before calling legacy input APIs and displays an **Enable Controls** screen with these steps. It also accepts numpad Enter and exposes a clickable **Begin/Resume Exploration** button. It does not change your project settings or install an input package. The original supplied project already selects the old Input Manager; importing only its Assets into another project keeps that other project's input setting.

### Pink or magenta materials, especially after importing into a Unity 6 URP project

The saved materials use the Built-in renderer's Standard shader. A URP project cannot render that shader and normally displays it magenta. Pink can also indicate a missing shader or a shader compilation error, so check the Console if the repair below does not resolve it.

This project now includes an Editor-only repair at **LUMEN → Fix Pink Materials**. It checks the active renderer and uses **Standard** for Built-in or **Universal Render Pipeline/Simple Lit** for URP. It copies each maze material's colour and emission, keeps shared material references intact, and saves only the repaired maze materials. On import, it automatically repairs maze materials whose shader differs from the active renderer's supported choice. It does not install URP or change project renderer, VR, Android, or player settings. Other render pipelines receive an explanatory message instead of a conversion.

To add the fix to an existing downloaded/imported copy without replacing your scene or settings, download the current repository ZIP and copy **Assets/EscapeMaze/Editor** (and `Editor.meta`) into the same location in your project. Allow Unity to compile, then choose **LUMEN → Fix Pink Materials**. The Editor folder includes its assembly definition. Avoid copying the whole project over a copy you have edited.

Alternatively, Unity 6 provides **Window → Rendering → Render Pipeline Converter**. For a URP project, select **Built-in to URP**, enable **Material Upgrade**, click **Initialize Converters**, select the maze materials, then **Convert Assets**. Back up edited materials before using Unity's converter. Convert only when your active project renderer is URP. For the original desktop baseline, open the supplied project with Unity 2022.3.62f1 and its Built-in renderer instead.

The repair's native Editor execution and final on-screen appearance still need verification in your Unity installation. Supporting desktop material conversion does not establish Unity 6 or ClassVR device compatibility.

The repair uses per-material saving on Unity versions that expose it (including the intended Unity 2022.3 baseline and Unity 6). In an older Editor lacking that API it leaves the repaired materials dirty and asks you to use **File → Save Project**, rather than automatically saving unrelated assets.

| Control | Action |
| --- | --- |
| WASD or arrow keys | Walk |
| Mouse | Look |
| E or left mouse button | Use the key or door under the crosshair, within 2.8 metres |
| Escape | Release mouse and pause movement / active timer |
| Begin/Resume button, click, or Enter / numpad Enter | Begin / resume |
| M | Toggle the map of discovered areas |
| Explore again button | Reload the maze and reset all progress and discovery |
| Try again button after capture | Reset the maze, keys, map, Warden, and startup grace |

Find the amber, sky, and coral keys in any order. Their alcoves are marked 01, 02, and 03; guide strips take on the key colour near each alcove. Aim at each key and use it. A focused key grows slightly and the reticle takes on its colour. The floor socket dims when the key is recovered, and its matching door indicator lights up. The brass exit seal requires all three keys; use it, then walk onto the green-striped landing behind it. Unlocking the door alone does not win. Victory freezes desktop movement and releases the cursor.

## Avoid the Warden

- The Warden starts away from the player, at grid cell (7,5), and gives **five seconds of active play** before moving. Menus, Escape pause, and lost application focus freeze it, including this grace period.
- It patrols four waypoints at **1.2 m/s**. Its initial sight cone is **120 degrees** with an **8 m** range. Walls and the locked door block sight; keys do not.
- When it sees you, its eye turns red and the HUD shows a pursuit warning. It chases at **2.1 m/s**; your normal walking speed is **3 m/s**. There is no sprint or crouch requirement.
- Break sight around corners. It searches toward the last cell where it saw you for up to **three seconds**, then resumes its patrol. It does not track your hidden position. While alerted, it can reacquire you anywhere around it if sight is clear and within range.
- It catches you within **0.7 m**, with clear sight. A caught screen stops movement, interaction, the Warden, and the timer; the cursor is released. **Try again** resets the entire run, including all three keys. There are no attacks, combat, health meters, or jump-scare camera effects.
- Once you win, the Warden stops and cannot turn victory into a loss.

The Inspector exposes speeds, range, cone, capture radius, search memory, and startup grace. `Data/SentinelPatrol.txt` defines the four patrol cells. If changing the route, keep its first cell consistent with the scene spawn by regenerating the scene, and use only cells connected while the exit is locked.

The saved scene already contains the complete maze, camera/player, keys, door, exit trigger, and lighting. It does not depend on an editor generator or runtime scene bootstrap to create the geometry.

## Contents

- A 13 × 11 grid with enclosed corridors, three dead-end key locations, and a single gated exit.
- 92 solid wall cubes, a floor, a sliding door, key meshes assembled from cubes, and an exit landing.
- 544 primitive cubes / 6,528 triangles, thirteen shared texture-free materials, and one directional light. No realtime shadows, HDR, bloom, post-processing, external art, or audio. Emissive materials colour the guide strips without extra lights.
- Static decoration has a dedicated runtime batching root; moving objects and mutable pickup indicators are excluded. Actual draw calls and frame times still need measurement in Unity and on the eventual device.
- A CharacterController player with grounded movement, diagonal speed limiting, and clamped desktop look.
- Designed desktop overlays, colour-aware focus reticle and prompts, three key status cards, compass, active play timer, toast feedback, victory screen, and replay.
- An optional discovered-area map (M). It reveals the current cell and its four neighbors, with no full-map preview. Replay resets discovery.
- One primitive-built Warden with patrol/chase/search, a changing eye indicator, pursuit warnings, catch/retry, and pause/win handling. No NavMesh bake or AI package is required.
- Edit Mode and Play Mode tests plus standalone rule / scene validation tools.

## Input and gameplay boundaries

`MazeProgress` holds the device-independent key, unlock, and win rules. `MazeSession` owns each play session. `MazePlayerMotor.Tick()` accepts movement values, without reading a keyboard. `MazeLook.Apply()` handles desktop yaw/pitch. `MazeInteractor.Tick(Ray, bool)` accepts an aim ray and activation signal; it handles reach and wall occlusion. Collectibles and the door derive from `MazeInteractable`.

`MazeExploration` keeps device-independent map discovery. `MazeSurvey` converts the player's world position into the canonical grid and records that discovery; the HUD only draws it. `MazePalette` supplies the UI's shared colours.

`SentinelBrain` is pure C#: it handles patrol decisions, last-sighting memory, detection/capture rules, and grid routing. `MazeSentinel` supplies native line-of-sight queries and advances the model from cell center to cell center, completing each edge before turning to prevent corner cutting. It reads no keyboard, mouse, or headset input. `MazeSession.IsRunning` is the common simulation gate; `DesktopMazePlayer` updates it, and a future controller should call `SetRunning()` when entering/leaving active play. The Warden's collider is a trigger and does not shove or block the player. Capture uses distance plus an unobstructed sight ray.

`DesktopMazeInput` is the only class that reads Unity's legacy keyboard/mouse input. It implements `MazeInputSource`; `DesktopMazePlayer` connects that source to movement, look, and interaction. A later gaze/dwell adapter can supply a ray and activation directly to `MazeInteractor`, while a separately confirmed locomotion adapter drives the motor. Replace the desktop look driver if a headset runtime controls camera orientation. The desktop IMGUI HUD is also a replaceable view and is not a headset UI implementation.

## Verification status

See **Verification/RESULTS.md** for exact results and limits. Runtime source reference-compilation, all six key-order simulations, and serialized asset checks passed here. A Unity Editor is not installed, so Unity import, native collision/trigger behavior, mouse interaction, rendering, and the included Unity Test Runner suites have **not** been executed. A reference compile is not a Unity build.

### Run the Unity suites

In Unity, open **Window → General → Test Runner**. Run the **EscapeMaze.Tests** tests in both Edit Mode and Play Mode. Play Mode tests load `EscapeMaze` and exercise character wall/door collision, ray range/occlusion, actual pickups, door unlocking, exit trigger victory, and scene reload/reset. Run Play Mode tests in the Editor; they read the layout asset from the project directory.

The original maze tests disable the Warden to isolate the key/exit flow. Separate sentinel Play Mode tests drive its real runtime adapter and check active-time grace/pause, native sight rays and wall obstruction, chase/search, capture/retry reset, and victory stopping the enemy. These tests have been supplied but not executed in this environment.

Alternatively, with a licensed Unity Editor installed:

```bash
UNITY_EDITOR=/path/to/Unity ./Tools/run_unity_tests.sh
```

Results and logs go to `Verification/UnityResults/`. The script stops on failures. It does not configure Android or VR.

### Manual acceptance check

1. Play the scene, capture the mouse, walk and look; verify diagonal movement is not faster and walls stop the player.
2. Release/resume the cursor with Escape/click; movement and the timer pause while released.
3. Try the door with fewer than three keys; verify the message and blocking collider remain.
4. Aim at keys from nearby; verify focus scale/reticle colour, then press E/click. Each must disappear, dim its floor socket, light the matching door indicator, and increment inventory once. A distant or wall-obscured key must not collect.
5. Collect all three and use the gold door. Verify it rises; victory should wait until you walk through it.
6. Walk onto the green landing; verify the victory screen, stopped movement/timer, and free cursor.
7. Toggle the map with M while exploring; distant areas must stay hidden and the player marker must follow movement.
8. Click Explore again after winning; verify three restored keys, a closed door with inactive indicators, zero inventory/timer, fresh map discovery, and the starting position.
9. Begin a fresh run and verify the Warden waits five active seconds, then patrols. Pause while near it; it must freeze and resume with you.
10. Enter its viewing cone in an open corridor; verify the red eye and pursuit warning. Move behind a wall; it must search the last sighting, then return to patrol. Verify you can outrun it.
11. Let it reach you; verify the caught screen, frozen simulation/timer, released cursor, and Try again reset. Repeat after a win to check that victory stays final.

For a desktop executable, use **File → Build Settings**, select a desktop platform supported by your installed Editor, and build. The scene is already in the build scene list. No desktop executable has been built in this environment.

### Reproduce non-Unity checks

With .NET SDK 8 and Python 3 + PyYAML installed:

```bash
dotnet run --project Verification/RulesCheck.csproj -- Assets/EscapeMaze/Data/MazeLayout.txt
python3 Verification/check_scene.py
dotnet build Verification/CompileRuntime.csproj -p:UnityReferencePath=/path/to/Unity/Editor/Data/Managed/UnityEngine
```

The last command compiles runtime C# against actual Unity managed reference assemblies. It does not launch Unity or its native physics engine. Verification helpers stay outside `Assets` and do not ship in a player.

The primitive scene is deterministic. If intentionally changing the layout, run `python3 Tools/generate_scene.py`; this overwrites the generated scene, materials, and small project settings files. Preserve hand edits first. The current generator's floor dimensions assume this 13 × 11 layout.

## ClassVR handoff: deliberately deferred

No Android platform switch, target/minimum Android API, graphics API selection, XR provider, OpenXR loader, Meta package, stereo camera rig, 6DoF assumption, or headset input mapping has been configured. The project is a desktop prototype and does not yet establish that an APK will work on the CVR-255-64.

Before the next phase, confirm from ClassVR/Avantis documentation or the actual device:

- Exact CVR-255-64 firmware, Android version, and supported application launch / deployment format.
- Headset runtime and vendor-supported Unity version, rendering SDK, and stereo rendering path.
- Required Android target/minimum API, ABI, graphics API, and signing/deployment requirements.
- Available orientation/tracking information and input mappings: gaze, dwell, select/back buttons, controller or touchpad, and recenter behavior.

Then choose the compatible Unity/toolchain baseline, add the confirmed rendering/input integration, replace desktop HUD/locomotion as needed, and test comfort and performance on the actual headset.
