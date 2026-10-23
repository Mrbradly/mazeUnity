#!/usr/bin/env python3
"""Validate actual serialized assets; needs PyYAML. This is not a Unity import or physics test."""
from pathlib import Path
import json
import re
import yaml

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets'
checks = 0


def check(condition, description):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(description)


def load_unity(path):
    text = path.read_text()
    text = '\n'.join(line for line in text.splitlines() if not line.startswith('%'))
    text = re.sub(r'^--- !u!(\d+) &(\d+)\s*$', r'---\n__class: \1\n__id: \2', text, flags=re.M)
    return list(yaml.safe_load_all(text))


def main():
    scene_path = ASSETS / 'EscapeMaze/Scenes/EscapeMaze.unity'
    docs = load_unity(scene_path)
    records = {doc['__id']: doc for doc in docs}
    check(len(records) == len(docs), 'Scene file IDs are unique')
    assets = {}
    for meta in ASSETS.rglob('*.meta'):
        data = yaml.safe_load(meta.read_text())
        guid = data['guid']
        check(guid not in assets, 'Unique asset GUID: ' + str(meta))
        source = meta.with_name(meta.name[:-5])
        check(source.exists(), 'Meta has source asset: ' + str(meta))
        assets[guid] = source
    for path in ASSETS.rglob('*'):
        if path.suffix != '.meta':
            check(path.with_name(path.name + '.meta').exists(), 'Asset has meta: ' + str(path))

    def references(value):
        if isinstance(value, dict):
            if 'fileID' in value:
                if 'guid' in value:
                    check(value['guid'] in assets or value['guid'].startswith('0000000000000000'),
                          'External reference resolves: ' + str(value))
                else:
                    check(value['fileID'] == 0 or value['fileID'] in records, 'Local reference resolves: ' + str(value))
            for child in value.values():
                references(child)
        elif isinstance(value, list):
            for child in value:
                references(child)
    for doc in docs:
        references(doc)

    objects = [doc for doc in docs if 'GameObject' in doc]
    transforms = [doc for doc in docs if 'Transform' in doc]
    by_name = {doc['GameObject']['m_Name']: doc for doc in objects}
    transform_by_object = {doc['Transform']['m_GameObject']['fileID']: doc for doc in transforms}
    for obj in objects:
        go = obj['GameObject']
        check(go['m_IsActive'] == 1, 'Game object starts active')
        for ref in go['m_Component']:
            doc = records[ref['component']['fileID']]
            body = next(v for k, v in doc.items() if not k.startswith('__'))
            check(body['m_GameObject']['fileID'] == obj['__id'], 'Component owner is correct')
        transform = transform_by_object[obj['__id']]
        check(go['m_Component'][0]['component']['fileID'] == transform['__id'], 'Transform is first component')
    for doc in transforms:
        t = doc['Transform']
        for child in t['m_Children']:
            check(records[child['fileID']]['Transform']['m_Father']['fileID'] == doc['__id'], 'Parent/child relation consistent')
        if t['m_Father']['fileID']:
            parent = records[t['m_Father']['fileID']]['Transform']
            check({'fileID': doc['__id']} in parent['m_Children'], 'Child/parent relation consistent')

    scripts = {}
    for doc in docs:
        if 'MonoBehaviour' not in doc:
            continue
        body = doc['MonoBehaviour']
        source = assets[body['m_Script']['guid']]
        check(source.suffix == '.cs', 'Behaviour references C# source')
        check(re.search(r'class\s+' + source.stem + r'\b', source.read_text()) is not None, 'Script class matches filename')
        for field in re.findall(r'\[SerializeField\]\s+private\s+\w+(?:\[\])?\s+(\w+)', source.read_text()):
            check(field in body, source.stem + ' serialized field exists: ' + field)
            if isinstance(body[field], dict):
                check(body[field].get('fileID', 0) != 0, source.stem + ' reference assigned: ' + field)
            if isinstance(body[field], list):
                check(len(body[field]) > 0, source.stem + ' array assigned: ' + field)
                check(all(ref.get('fileID', 0) != 0 for ref in body[field]), source.stem + ' array has no empty references: ' + field)
        scripts.setdefault(source.stem, []).append(body)
    check(len(scripts['KeyPickup']) == 3, 'Exactly three keys')
    check({b['keyId'] for b in scripts['KeyPickup']} == {0, 1, 2}, 'Keys have distinct IDs')
    check(len(scripts['MazeDoor']) == 1, 'Exactly one door')
    check(len(scripts['MazeDoor'][0]['indicators']) == 3, 'Door has three live indicators')
    check(len(scripts['MazeDoor'][0]['keyMaterials']) == 3, 'Door has three signal materials')
    check(len(scripts['MazeSurvey']) == 1, 'Discovered map has one survey source')
    check(len(scripts['MazeStaticGeometry']) == 1, 'Static decor has one batching root')
    check(len(scripts['MazeSentinel']) == 3, 'Exactly three Wardens')
    check(len({b['patrolAsset']['guid'] for b in scripts['MazeSentinel']}) == 3, 'Wardens use independent routes')
    for sentinel in scripts['MazeSentinel']:
        check(0 < sentinel['patrolSpeed'] < sentinel['chaseSpeed'] < scripts['MazePlayerMotor'][0]['speed'], 'Player can outrun patrol and chase')
        check(0 < sentinel['captureRadius'] < sentinel['sightRange'], 'Capture range smaller than detection range')
        check(sentinel['startGraceSeconds'] >= 3, 'Enemy gives startup grace')
        check(0 < sentinel['fieldOfView'] <= 180 and sentinel['memorySeconds'] > 0, 'Finite directional sight and search memory')
        enemy_transform = transform_by_object[sentinel['m_GameObject']['fileID']]['Transform']
        route_path = assets[sentinel['patrolAsset']['guid']]
        route = [tuple(map(int, line.split(','))) for line in route_path.read_text().strip().splitlines()]
        check(enemy_transform['m_LocalPosition'] == {'x': route[0][0] * 2.5, 'y': 0, 'z': -route[0][1] * 2.5}, 'Warden starts at first patrol waypoint')
        check(any(d.get('CapsuleCollider', {}).get('m_GameObject', {}).get('fileID') == sentinel['m_GameObject']['fileID'] and
                  d['CapsuleCollider']['m_IsTrigger'] == 1 for d in docs), 'Enemy uses a shootable trigger volume')
    check(len(scripts['MazeHud'][0]['enemies']) == 3, 'HUD tracks all Wardens')
    check(len(scripts['MazeBlaster']) == 1, 'One modular blaster')
    blaster = scripts['MazeBlaster'][0]
    check(blaster['stunSeconds'] == 4 and blaster['range'] == 18 and blaster['shotInterval'] == .4, 'Stun/range/cooldown configured')
    check(scripts['DesktopMazePlayer'][0]['blaster'] == scripts['MazeHud'][0]['blaster'], 'Input driver and HUD share the blaster')
    check(len(scripts['MazeExitZone']) == 1, 'Exactly one exit')
    check(len(scripts['MazePlayerMotor']) == 1, 'Exactly one player')
    key_objects = {b['m_GameObject']['fileID'] for b in scripts['KeyPickup']}
    static_root = scripts['MazeStaticGeometry'][0]['m_GameObject']['fileID']
    static_transform = transform_by_object[static_root]['__id']
    def is_static(object_id):
        transform = transform_by_object[object_id]
        while transform['Transform']['m_Father']['fileID']:
            parent_id = transform['Transform']['m_Father']['fileID']
            if parent_id == static_transform:
                return True
            transform = records[parent_id]
        return False
    for body in scripts['KeyPickup'] + scripts['MazeDoor'] + scripts['MazePlayerMotor'] + scripts['MazeSentinel'] + scripts['MazeBlaster']:
        check(not is_static(body['m_GameObject']['fileID']), 'Moving gameplay objects are outside static batch root')
    for body in scripts['KeyPickup']:
        renderer = records[body['floorIndicator']['fileID']]['MeshRenderer']
        check(not is_static(renderer['m_GameObject']['fileID']), 'Mutable pickup indicator is outside static batch root')
    meshes = [d for d in docs if 'MeshFilter' in d]
    check(len(meshes) * 12 <= 10000, 'Primitive geometry stays under 10,000 triangles')
    tracer_object = records[blaster['tracerRenderer']['fileID']]['MeshRenderer']['m_GameObject']['fileID']
    check(not is_static(tracer_object), 'Shot visual is outside static batching')
    check(not any(d.get('BoxCollider', {}).get('m_GameObject', {}).get('fileID') == tracer_object for d in docs), 'Tracer cannot block rays')
    for doc in docs:
        if 'SphereCollider' in doc and doc['SphereCollider']['m_GameObject']['fileID'] in key_objects:
            check(doc['SphereCollider']['m_IsTrigger'] == 1, 'Keys do not block walking')
            check(doc['SphereCollider']['m_Radius'] < .45, 'Eye at key center is outside raycast sphere')
    zone = scripts['MazeExitZone'][0]['m_GameObject']['fileID']
    check(any(d.get('Rigidbody', {}).get('m_GameObject', {}).get('fileID') == zone and
              d['Rigidbody']['m_IsKinematic'] == 1 for d in docs), 'Exit has kinematic trigger body')
    check(any(d.get('BoxCollider', {}).get('m_GameObject', {}).get('fileID') == zone and
              d['BoxCollider']['m_IsTrigger'] == 1 for d in docs), 'Exit is a trigger')

    layout = (ASSETS / 'EscapeMaze/Data/MazeLayout.txt').read_text().strip().splitlines()
    walls = 0
    for row, line in enumerate(layout):
        for x, token in enumerate(line):
            if token != '#':
                continue
            walls += 1
            obj = by_name[f'Wall_{x}_{row}']
            t = transform_by_object[obj['__id']]['Transform']
            check(t['m_LocalPosition'] == {'x': x * 2.5, 'y': 1.3, 'z': -row * 2.5}, 'Wall position matches layout')
            check(t['m_LocalScale'] == {'x': 2.5, 'y': 2.6, 'z': 2.5}, 'Walls fill cells without gaps')
            check(any(d.get('BoxCollider', {}).get('m_GameObject', {}).get('fileID') == obj['__id'] and
                      d['BoxCollider']['m_IsTrigger'] == 0 for d in docs), 'Wall has solid collider')
    check(sum(name.startswith('Wall_') for name in by_name) == walls, 'No extra walls block corridors')
    for token, script in [('S', 'MazePlayerMotor'), ('D', 'MazeDoor'), ('E', 'MazeExitZone')]:
        body = scripts[script][0]
        t = transform_by_object[body['m_GameObject']['fileID']]['Transform']
        row = next(i for i, line in enumerate(layout) if token in line)
        x = layout[row].index(token)
        check(t['m_LocalPosition']['x'] == x * 2.5 and t['m_LocalPosition']['z'] == -row * 2.5, token + ' matches layout')
    for body in scripts['KeyPickup']:
        token = chr(ord('a') + body['keyId'])
        row = next(i for i, line in enumerate(layout) if token in line)
        x = layout[row].index(token)
        t = transform_by_object[body['m_GameObject']['fileID']]['Transform']
        check(t['m_LocalPosition'] == {'x': x * 2.5, 'y': 1.15, 'z': -row * 2.5}, 'Key matches layout')

    cameras = [d['Camera'] for d in docs if 'Camera' in d]
    check(len(cameras) == 1, 'One camera')
    check(cameras[0]['m_HDR'] == 0 and cameras[0]['m_AllowMSAA'] == 0, 'Lightweight camera settings')
    check(all(d['Light']['m_Shadows']['m_Type'] == 0 for d in docs if 'Light' in d), 'No realtime shadows')
    for path in (ASSETS / 'EscapeMaze/Materials').glob('*.mat'):
        mat = load_unity(path)[0]['Material']
        check(mat['m_Shader']['fileID'] == 46, 'Built-in Standard material')
        check(mat['m_SavedProperties']['m_TexEnvs'] == [], 'No texture dependencies')
    build_settings = load_unity(ROOT / 'ProjectSettings/EditorBuildSettings.asset')[0]['EditorBuildSettings']
    check(build_settings['m_Scenes'][0]['path'] == scene_path.relative_to(ROOT).as_posix(), 'Scene included in build')
    manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())
    check(not any('xr' in k.lower() or 'oculus' in k.lower() for k in manifest['dependencies']), 'No XR SDK dependencies')
    player_settings = load_unity(ROOT / 'ProjectSettings/ProjectSettings.asset')[0]['PlayerSettings']
    check(player_settings['activeInputHandler'] == 0, 'Legacy desktop input enabled')
    check(not any('android' in k.lower() for k in player_settings), 'No Android settings selected')
    print(f'PASS {checks} serialized scene / asset assertions (not a Unity import test)')


if __name__ == '__main__':
    main()
