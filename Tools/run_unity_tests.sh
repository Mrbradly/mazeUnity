#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
editor_path="${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity 2022.3.62f1 executable}"
result_dir="$project_dir/Verification/UnityResults"
mkdir -p "$result_dir"

for test_platform in EditMode PlayMode; do
  "$editor_path" -batchmode -nographics -projectPath "$project_dir" \
    -runTests -testPlatform "$test_platform" -testFilter EscapeMaze.Tests \
    -testResults "$result_dir/$test_platform.xml" \
    -logFile "$result_dir/$test_platform.log"
  python3 - "$result_dir/$test_platform.xml" <<'PY'
import pathlib, sys, xml.etree.ElementTree as ET
path = pathlib.Path(sys.argv[1])
if not path.exists():
    raise SystemExit('FAIL: Unity did not produce test results: ' + str(path))
root = ET.parse(path).getroot()
total = int(root.attrib.get('total', '0'))
failed = int(root.attrib.get('failed', '0'))
result = root.attrib.get('result', '')
if total < 1 or failed or result not in ('Passed', 'Success'):
    raise SystemExit('FAIL: ' + str(root.attrib))
print('PASS ' + path.stem + ': ' + str(total) + ' Unity tests')
PY
done
