#!/usr/bin/env python3
"""Deterministically serialize the primitive scene. No Unity editor or third-party modules needed.

Run from anywhere: python3 Tools/generate_scene.py
This only overwrites generated scene/material/meta/settings files in this new project.
"""
from pathlib import Path
import hashlib
import json
from collections import deque

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/EscapeMaze'
BUILTIN_MESH = '0000000000000000e000000000000000'
BUILTIN_SHADER = '0000000000000000f000000000000000'


def guid(path):
    return hashlib.md5(str(path).encode()).hexdigest()


def meta(path):
    relative = path.relative_to(ROOT).as_posix()
    text = f'fileFormatVersion: 2\nguid: {guid(relative)}\n'
    if path.is_dir():
        text += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.cs':
        text += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {fileID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.mat':
        text += 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.asmdef':
        text += 'AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix == '.txt':
        text += 'TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        text += 'DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    path.with_name(path.name + '.meta').write_text(text)


def vec(values, names='xyz'):
    return '{' + ', '.join(f'{key}: {value:g}' for key, value in zip(names, values)) + '}'


class Scene:
    def __init__(self):
        self.next_id = 1000
        self.objects = []
        self.components = []

    def alloc(self):
        self.next_id += 1
        return self.next_id

    def obj(self, name, parent=None, position=(0, 0, 0), scale=(1, 1, 1), rotation=(0, 0, 0, 1), tag='Untagged'):
        obj = dict(id=self.alloc(), transform=self.alloc(), name=name, parent=parent,
                   position=position, scale=scale, rotation=rotation, tag=tag, components=[])
        obj['components'].append(obj['transform'])
        self.objects.append(obj)
        return obj

    def component(self, obj, class_id, class_name, fields):
        cid = self.alloc()
        obj['components'].append(cid)
        self.components.append((cid, class_id, class_name, obj, fields))
        return cid

    def script(self, obj, name, fields=''):
        script_guid = guid(f'Assets/EscapeMaze/Runtime/{name}.cs')
        return self.component(obj, 114, 'MonoBehaviour', f'''  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
{fields}''')

    def box(self, obj, size=(1, 1, 1), trigger=False):
        return self.component(obj, 65, 'BoxCollider', f'''  m_Material: {{fileID: 0}}
  m_IsTrigger: {int(trigger)}
  m_Enabled: 1
  serializedVersion: 2
  m_Size: {vec(size)}
  m_Center: {{x: 0, y: 0, z: 0}}
''')

    def cube(self, name, parent, position, scale, material, collider=False):
        obj = self.obj(name, parent, position, scale)
        self.component(obj, 33, 'MeshFilter', f'  m_Mesh: {{fileID: 10202, guid: {BUILTIN_MESH}, type: 0}}\n')
        self.component(obj, 23, 'MeshRenderer', f'''  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 0
  m_StaticShadowCaster: 0
  m_MotionVectors: 0
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {guid('Assets/EscapeMaze/Materials/' + material + '.mat')}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 0
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
''')
        if collider:
            self.box(obj)
        return obj

    def renderer(self, obj):
        return next(cid for cid, kind, _, owner, _ in self.components if owner is obj and kind == 23)

    def number_sign(self, parent, name, position, number, color, sideways=False):
        sign = self.obj(name, parent, position, rotation=(0, .7071068, 0, .7071068) if sideways else (0, 0, 0, 1))
        self.cube(name + ' panel', sign, (0, 0, 0), (1.12, .66, .06), 'Trim')
        self.cube(name + ' underline', sign, (0, -.26, -.041), (.9, .025, .025), color)
        # Seven-segment numerals made entirely from little cubes. Visible from either side.
        digit_segments = {'0': 'abcedf', '1': 'bc', '2': 'abged', '3': 'abgcd'}
        segments = {'a': (0, .18, .2, .035), 'b': (.1, .09, .035, .18),
                    'c': (.1, -.09, .035, .18), 'd': (0, -.18, .2, .035),
                    'e': (-.1, -.09, .035, .18), 'f': (-.1, .09, .035, .18), 'g': (0, 0, .2, .035)}
        for place, digit in enumerate(f'{number:02}'):
            for segment in digit_segments[digit]:
                x, y, w, h = segments[segment]
                self.cube(f'{name} digit {place} {segment}', sign, (-.19 + place * .38 + x, y, 0), (w, h, .09), color)
        return sign

    def save(self, path, sun):
        parts = [f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 0
  m_FogColor: {{r: 0.08, g: 0.12, b: 0.15, a: 1}}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {{r: 0.65, g: 0.7, b: 0.75, a: 1}}
  m_AmbientEquatorColor: {{r: 0.65, g: 0.7, b: 0.75, a: 1}}
  m_AmbientGroundColor: {{r: 0.65, g: 0.7, b: 0.75, a: 1}}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {{r: 0.42, g: 0.48, b: 0.63, a: 1}}
  m_SkyboxMaterial: {{fileID: 0}}
  m_HaloStrength: 0
  m_FlareStrength: 0
  m_FlareFadeSpeed: 3
  m_HaloTexture: {{fileID: 0}}
  m_SpotCookie: {{fileID: 0}}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 16
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 0
  m_CustomReflection: {{fileID: 0}}
  m_Sun: {{fileID: {sun}}}
  m_UseRadianceAmbientProbe: 0
''']
        for obj in self.objects:
            parts.append(f'''--- !u!1 &{obj['id']}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
''' + ''.join(f'  - component: {{fileID: {cid}}}\n' for cid in obj['components']) + f'''  m_Layer: 0
  m_Name: {obj['name']}
  m_TagString: {obj['tag']}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{obj['transform']}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj['id']}}}
  serializedVersion: 2
  m_LocalRotation: {vec(obj['rotation'], 'xyzw')}
  m_LocalPosition: {vec(obj['position'])}
  m_LocalScale: {vec(obj['scale'])}
  m_ConstrainProportionsScale: 0
''')
            children = [o['transform'] for o in self.objects if o['parent'] is obj]
            parts.append('  m_Children:\n' + ''.join(f'  - {{fileID: {child}}}\n' for child in children) if children else '  m_Children: []\n')
            parts.append(f"  m_Father: {{fileID: {obj['parent']['transform'] if obj['parent'] else 0}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n")
        for cid, class_id, name, obj, fields in self.components:
            parts.append(f'''--- !u!{class_id} &{cid}
{name}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj['id']}}}
{fields}''')
        path.write_text(''.join(parts))


def material(name, color, emission=0):
    (ASSETS / f'Materials/{name}.mat').write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 46, guid: {BUILTIN_SHADER}, type: 0}}
  m_ValidKeywords:
  - _SPECULARHIGHLIGHTS_OFF
  - _GLOSSYREFLECTIONS_OFF
{('  - _EMISSION' + chr(10)) if emission else ''}  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Glossiness: 0
    - _Metallic: 0
    - _SpecularHighlights: 0
    - _GlossyReflections: 0
    m_Colors:
    - _Color: {vec((*color, 1), 'rgba')}
    - _EmissionColor: {vec((*(c * emission for c in color), 1), 'rgba')}
''')


def main():
    colors = {'Floor': (.045, .07, .09), 'Wall': (.14, .24, .28),
              'Amber': (1, .69, .16), 'Sky': (.25, .75, 1), 'Coral': (1, .38, .33),
              'Door': (.62, .43, .19), 'Trim': (.035, .065, .08), 'Exit': (.3, .92, .65),
              'Cap': (.3, .43, .45), 'Tile': (.085, .13, .16), 'Guide': (.16, .4, .42),
              'Spent': (.08, .12, .14), 'Danger': (1, .14, .17)}
    for name, color in colors.items():
        material(name, color, .5 if name == 'Danger' else .32 if name in ['Amber', 'Sky', 'Coral', 'Exit'] else .1 if name == 'Guide' else 0)

    layout = (ASSETS / 'Data/MazeLayout.txt').read_text().strip().splitlines()
    assert len({len(row) for row in layout}) == 1
    markers = {token: (x * 2.5, 0, -row * 2.5) for row, line in enumerate(layout)
               for x, token in enumerate(line) if token in 'SabcDE'}
    s = Scene()
    session_obj = s.obj('Maze Session and HUD')
    session = s.script(session_obj, 'MazeSession')
    env = s.obj('Maze - primitive geometry')
    static = s.obj('Architecture - static batching only', env)
    s.script(static, 'MazeStaticGeometry')
    s.cube('Floor', static, (15, -.1, -12.5), (32.5, .2, 27.5), 'Floor', True)
    walls = s.obj('Walls', static)
    key_colors = ['Amber', 'Sky', 'Coral']
    sector = {}
    for token, color in zip('abc', key_colors):
        origin = next((x, row) for row, line in enumerate(layout) for x, cell in enumerate(line) if cell == token)
        queue = deque([(origin, 0)])
        seen = {origin}
        while queue:
            (x, row), distance = queue.popleft()
            if distance > 3:
                continue
            if (x, row) not in sector or distance < sector[(x, row)][0]:
                sector[(x, row)] = (distance, color)
            for nx, nr in [(x + 1, row), (x - 1, row), (x, row + 1), (x, row - 1)]:
                if 0 <= nx < len(layout[0]) and 0 <= nr < len(layout) and layout[nr][nx] not in '#D' and (nx, nr) not in seen:
                    seen.add((nx, nr)); queue.append(((nx, nr), distance + 1))
    for row, line in enumerate(layout):
        for x, cell in enumerate(line):
            if cell == '#':
                s.cube(f'Wall_{x}_{row}', walls, (x * 2.5, 1.3, -row * 2.5), (2.5, 2.6, 2.5), 'Wall', True)
                s.cube(f'Plinth_{x}_{row}', walls, (x * 2.5, .12, -row * 2.5), (2.5, .24, 2.5), 'Trim')
                s.cube(f'Cap_{x}_{row}', walls, (x * 2.5, 2.64, -row * 2.5), (2.5, .08, 2.5), 'Cap')
                for dx, dr in [(1, 0), (-1, 0), (0, 1), (0, -1)]:
                    nx, nr = x + dx, row + dr
                    if not (0 <= nx < len(line) and 0 <= nr < len(layout)) or layout[nr][nx] == '#':
                        continue
                    color = sector.get((nx, nr), (0, 'Guide'))[1]
                    scale = (.024, .036, 2.32) if dx else (2.32, .036, .024)
                    s.cube(f'Guide_{x}_{row}_{dx}_{dr}', walls, (x * 2.5 + dx * 1.258, .29, -row * 2.5 - dr * 1.258), scale, color)
            else:
                s.cube(f'Tile_{x}_{row}', static, (x * 2.5, .007, -row * 2.5), (2.38, .014, 2.38), 'Tile')

    player = s.obj('Desktop Player', position=(markers['S'][0], .04, markers['S'][2]), rotation=(0, 1, 0, 0))
    s.component(player, 143, 'CharacterController', '''  m_Material: {fileID: 0}
  m_IsTrigger: 0
  m_Enabled: 1
  serializedVersion: 2
  m_Height: 1.8
  m_Radius: 0.3
  m_SlopeLimit: 45
  m_StepOffset: 0.2
  m_SkinWidth: 0.03
  m_MinMoveDistance: 0
  m_Center: {x: 0, y: 0.9, z: 0}
''')
    camera = s.obj('Main Camera', player, (0, 1.6, 0), tag='MainCamera')
    s.component(camera, 20, 'Camera', '''  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {r: 0.12, g: 0.21, b: 0.28, a: 1}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_FocalLength: 50
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.05
  far clip plane: 80
  field of view: 72
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: 1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 0
  m_HDR: 0
  m_AllowMSAA: 0
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 0
''')
    input_id = s.script(player, 'DesktopMazeInput', f'  mouseSensitivity: 2\n  session: {{fileID: {session}}}\n')
    motor = s.script(player, 'MazePlayerMotor', '  speed: 3\n')
    look = s.script(player, 'MazeLook', f'  eye: {{fileID: {camera["transform"]}}}\n')
    interactor = s.script(player, 'MazeInteractor', f'  session: {{fileID: {session}}}\n  reach: 2.8\n')
    survey = s.script(session_obj, 'MazeSurvey', f'''  layoutAsset: {{fileID: 4900000, guid: {guid('Assets/EscapeMaze/Data/MazeLayout.txt')}, type: 3}}
  player: {{fileID: {player['transform']}}}
''')
    weapon = s.obj('Desktop stun blaster', camera, (.24, -.23, .42))
    s.cube('Blaster housing', weapon, (0, 0, 0), (.15, .13, .29), 'Trim')
    s.cube('Blaster barrel', weapon, (0, .025, .21), (.09, .07, .18), 'Cap')
    s.cube('Blaster energy strip', weapon, (.079, .02, .02), (.012, .035, .2), 'Sky')
    s.cube('Blaster grip', weapon, (0, -.105, -.06), (.075, .16, .08), 'Door')
    s.cube('Blaster muzzle glow', weapon, (0, .025, .307), (.07, .05, .016), 'Sky')
    muzzle = s.obj('Stun muzzle', weapon, (0, .025, .32))
    tracer = s.cube('Stun beam - dynamic', env, (0, -10, 0), (.025, .025, .1), 'Sky')
    blaster = s.script(player, 'MazeBlaster', f'''  session: {{fileID: {session}}}
  muzzle: {{fileID: {muzzle['transform']}}}
  tracer: {{fileID: {tracer['transform']}}}
  tracerRenderer: {{fileID: {s.renderer(tracer)}}}
  range: 18
  shotInterval: 0.4
  stunSeconds: 4
  tracerSeconds: 0.08
''')
    s.script(player, 'DesktopMazePlayer', f'''  input: {{fileID: {input_id}}}
  motor: {{fileID: {motor}}}
  look: {{fileID: {look}}}
  interactor: {{fileID: {interactor}}}
  blaster: {{fileID: {blaster}}}
  eye: {{fileID: {camera['transform']}}}
  session: {{fileID: {session}}}
''')
    enemy_ids = []
    for number, patrol_path in enumerate(['Data/SentinelPatrol.txt', 'Data/SentinelPatrol2.txt', 'Data/SentinelPatrol3.txt'], 1):
        patrol = [tuple(map(int, line.split(','))) for line in (ASSETS / patrol_path).read_text().strip().splitlines()]
        spawn_x, spawn_row = patrol[0]
        enemy = s.obj(f'Warden {number} - patrol sentinel', env, (spawn_x * 2.5, 0, -spawn_row * 2.5), rotation=(0, 1, 0, 0))
        s.component(enemy, 136, 'CapsuleCollider', '''  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_Enabled: 1
  m_Radius: 0.34
  m_Height: 1.8
  m_Direction: 1
  m_Center: {x: 0, y: 0.9, z: 0}
''')
        robot = s.obj('Warden hovering visual', enemy)
        s.cube('Warden armored torso', robot, (0, .88, 0), (.6, .7, .44), 'Trim')
        s.cube('Warden chest plate', robot, (0, .95, .24), (.48, .42, .07), 'Cap')
        s.cube('Warden head', robot, (0, 1.42, .015), (.76, .36, .53), 'Wall')
        s.cube('Warden visor recess', robot, (0, 1.43, .3), (.66, .16, .06), 'Trim')
        eye_obj = s.cube('Warden status eye', robot, (0, 1.43, .345), (.44, .065, .035), 'Amber')
        for side in [-1, 1]:
            s.cube(f'Warden brass shoulder {side}', robot, (side * .38, 1.12, 0), (.16, .18, .35), 'Door')
            s.cube(f'Warden side armor {side}', robot, (side * .4, .84, 0), (.12, .38, .24), 'Wall')
            s.cube(f'Warden hover skid {side}', robot, (side * .2, .26, 0), (.2, .12, .55), 'Trim')
        for i in range(3):
            s.cube(f'Warden chest vent {i}', robot, (0, .83 - i * .07, .285), (.3, .022, .02), 'Guide')
        s.cube('Warden antenna', robot, (-.24, 1.72, 0), (.035, .22, .035), 'Trim')
        s.cube('Warden antenna cap', robot, (-.24, 1.84, 0), (.1, .05, .1), 'Amber')
        enemy_id = s.script(enemy, 'MazeSentinel', f'''  session: {{fileID: {session}}}
  layoutAsset: {{fileID: 4900000, guid: {guid('Assets/EscapeMaze/Data/MazeLayout.txt')}, type: 3}}
  patrolAsset: {{fileID: 4900000, guid: {guid('Assets/EscapeMaze/' + patrol_path)}, type: 3}}
  player: {{fileID: {player['transform']}}}
  visual: {{fileID: {robot['transform']}}}
  eye: {{fileID: {s.renderer(eye_obj)}}}
  patrolMaterial: {{fileID: 2100000, guid: {guid('Assets/EscapeMaze/Materials/Amber.mat')}, type: 2}}
  alertMaterial: {{fileID: 2100000, guid: {guid('Assets/EscapeMaze/Materials/Danger.mat')}, type: 2}}
  stunnedMaterial: {{fileID: 2100000, guid: {guid('Assets/EscapeMaze/Materials/Sky.mat')}, type: 2}}
  patrolSpeed: 1.2
  chaseSpeed: 2.1
  sightRange: 8
  fieldOfView: 120
  captureRadius: 0.7
  memorySeconds: 3
  startGraceSeconds: 3
''')
        enemy_ids.append(enemy_id)
    s.script(session_obj, 'MazeHud', f'''  session: {{fileID: {session}}}
  input: {{fileID: {input_id}}}
  interactor: {{fileID: {interactor}}}
  survey: {{fileID: {survey}}}
  blaster: {{fileID: {blaster}}}
  enemies:
''' + ''.join(f'  - {{fileID: {enemy_id}}}\n' for enemy_id in enemy_ids))

    keys = s.obj('Three collectible keys', env)
    for i, token in enumerate('abc'):
        x, _, z = markers[token]
        key = s.obj(f'Key {i + 1} - {list(colors)[i + 2]}', keys, (x, 1.15, z))
        s.component(key, 135, 'SphereCollider', '''  m_Material: {fileID: 0}
  m_IsTrigger: 1
  m_Enabled: 1
  serializedVersion: 2
  m_Radius: 0.4
  m_Center: {x: 0, y: 0, z: 0}
''')
        visual = s.obj('Rotating key visual', key)
        key_color = ['Amber', 'Sky', 'Coral'][i]
        pieces = [((0, .43, 0), (.5, .1, .12)), ((0, .03, 0), (.5, .1, .12)),
                  ((-.2, .23, 0), (.1, .4, .12)), ((.2, .23, 0), (.1, .4, .12)),
                  ((0, -.26, 0), (.12, .55, .12)), ((.13, -.4, 0), (.25, .1, .12)),
                  ((.1, -.23, 0), (.2, .1, .12))]
        for n, (pos, scale) in enumerate(pieces):
            s.cube(f'Key piece {n}', visual, pos, scale, key_color)
        marker = s.cube(f'{key_color} floor indicator', keys, (x, .055, z), (.66, .012, .66), key_color)
        s.script(key, 'KeyPickup', f'''  keyId: {i}
  visual: {{fileID: {visual['transform']}}}
  floorIndicator: {{fileID: {s.renderer(marker)}}}
  spentMaterial: {{fileID: 2100000, guid: {guid('Assets/EscapeMaze/Materials/Spent.mat')}, type: 2}}
''')
        s.cube(f'{key_color} floor socket', static, (x, .025, z), (.92, .05, .92), 'Trim')
        for side in [-1, 1]:
            s.cube(f'{key_color} socket edge X {side}', static, (x + side * .44, .06, z), (.035, .025, .92), key_color)
            s.cube(f'{key_color} socket edge Z {side}', static, (x, .06, z + side * .44), (.92, .025, .035), key_color)
        cell_x, cell_row = round(x / 2.5), round(-z / 2.5)
        approach_x, approach_r = next((dx, dr) for dx, dr in [(1, 0), (-1, 0), (0, 1), (0, -1)] if layout[cell_row + dr][cell_x + dx] != '#')
        s.number_sign(static, f'Signal {i + 1:02} alcove', (x - approach_x * 1.205, 1.95, z + approach_r * 1.205), i + 1, key_color, bool(approach_x))

    dx, _, dz = markers['D']
    door = s.obj('Locked Exit Door', env, (dx, 1.3, dz))
    s.box(door, (.25, 2.6, 2.5))
    s.cube('Brass door left panel', door, (0, 0, -.62), (.25, 2.6, 1.2), 'Door')
    s.cube('Brass door right panel', door, (0, 0, .62), (.25, 2.6, 1.2), 'Door')
    s.cube('Door center seam', door, (-.132, 0, 0), (.025, 2.6, .045), 'Trim')
    s.cube('Door seal housing', door, (-.17, .16, 0), (.1, .64, 2.12), 'Trim')
    indicators = []
    for i, color in enumerate(['Amber', 'Sky', 'Coral']):
        s.cube(f'{color} lock rim', door, (-.235, .16, -.6 + .6 * i), (.04, .36, .36), color)
        lens = s.cube(f'{color} lock indicator', door, (-.263, .16, -.6 + .6 * i), (.035, .25, .25), 'Spent')
        indicators.append(s.renderer(lens))
    s.script(door, 'MazeDoor', f'  session: {{fileID: {session}}}\n  indicators:\n' + ''.join(f'  - {{fileID: {r}}}\n' for r in indicators) +
             '  keyMaterials:\n' + ''.join(f'  - {{fileID: 2100000, guid: {guid("Assets/EscapeMaze/Materials/" + color + ".mat")}, type: 2}}\n' for color in key_colors))
    for side in [-1, 1]:
        s.cube('Exit frame', static, (dx, 1.4, dz + side * 1.22), (.4, 2.8, .12), 'Trim')
        s.cube('Exit frame light', static, (dx - .22, 1.4, dz + side * 1.19), (.03, 2.5, .035), 'Exit')
    s.cube('Exit lintel', static, (dx, 2.85, dz), (.4, .2, 2.55), 'Trim')
    s.cube('Exit lintel light', static, (dx - .22, 2.81, dz), (.025, .045, 2.4), 'Exit')
    ex, _, ez = markers['E']
    s.cube('Victory landing', static, (ex, .021, ez), (2.35, .025, 2.35), 'Trim')
    for i in range(4):
        s.cube(f'Exit landing stripe {i}', static, (ex - .8 + i * .52, .04, ez), (.2, .015, 1.8), 'Exit')
    zone = s.obj('Exit Victory Trigger', env, (ex, 1, ez))
    s.box(zone, (1.8, 2, 2), True)
    s.component(zone, 54, 'Rigidbody', '''  serializedVersion: 2
  m_Mass: 1
  m_Drag: 0
  m_AngularDrag: 0.05
  m_UseGravity: 0
  m_IsKinematic: 1
  m_Interpolate: 0
  m_Constraints: 0
  m_CollisionDetection: 0
''')
    s.script(zone, 'MazeExitZone', f'  session: {{fileID: {session}}}\n')
    light = s.obj('Directional Light', rotation=(.4082179, -.2345697, .1093817, .8754261))
    sun = s.component(light, 108, 'Light', '''  m_Enabled: 1
  serializedVersion: 10
  m_Type: 1
  m_Shape: 0
  m_Color: {r: 1, g: 0.95, b: 0.84, a: 1}
  m_Intensity: 0.9
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.8
  m_CookieSize: 10
  m_Shadows:
    m_Type: 0
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
  m_Cookie: {fileID: 0}
  m_DrawHalo: 0
  m_Flare: {fileID: 0}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {x: 1, y: 1}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {x: 0, y: 0, z: 0, w: 0}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ShadowRadius: 0
  m_ShadowAngle: 0
''')
    scene_path = ASSETS / 'Scenes/EscapeMaze.unity'
    s.save(scene_path, sun)
    settings = ROOT / 'ProjectSettings'
    (settings / 'EditorBuildSettings.asset').write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/EscapeMaze/Scenes/EscapeMaze.unity
    guid: {guid('Assets/EscapeMaze/Scenes/EscapeMaze.unity')}
  m_configObjects: {{}}
''')
    (settings / 'InputManager.asset').write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!13 &1
InputManager:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Axes:
''' + ''.join(f'''  - serializedVersion: 3
    m_Name: Mouse {name}
    descriptiveName: 
    descriptiveNegativeName: 
    negativeButton: 
    positiveButton: 
    altNegativeButton: 
    altPositiveButton: 
    gravity: 0
    dead: 0
    sensitivity: 0.1
    snap: 0
    invert: 0
    type: 1
    axis: {axis}
    joyNum: 0
''' for axis, name in enumerate(['X', 'Y'])))
    (settings / 'ProjectSettings.asset').write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 26
  productGUID: e3ef0718b5e14cdb908321153e9f85d6
  companyName: EscapeMazePrototype
  productName: Escape Maze
  defaultScreenWidth: 1280
  defaultScreenHeight: 720
  defaultIsNativeResolution: 0
  resizableWindow: 1
  fullscreenMode: 3
  runInBackground: 0
  m_ActiveColorSpace: 0
  activeInputHandler: 0
''')
    for path in sorted((ROOT / 'Assets').rglob('*')):
        if path.suffix != '.meta':
            meta(path)
    stats = {'gameObjects': len(s.objects), 'primitiveCubes': sum(c[1] == 33 for c in s.components),
             'walls': sum(row.count('#') for row in layout), 'keys': 3, 'enemies': len(enemy_ids), 'materials': len(colors),
             'triangles': sum(c[1] == 33 for c in s.components) * 12, 'layoutWidth': len(layout[0]), 'layoutHeight': len(layout)}
    (ROOT / 'Verification/scene-stats.json').write_text(json.dumps(stats, indent=2) + '\n')
    print(json.dumps(stats, indent=2))


if __name__ == '__main__':
    main()
