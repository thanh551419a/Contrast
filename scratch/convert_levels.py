import json
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

html_path = r'y:\Contrast\My project\CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html'
with open(html_path, 'r', encoding='utf-8') as f:
    content = f.read()

match = re.search(r'window\.__CE_LEVELS__\s*=\s*(\[.*?\]);\s*window\.__CE_FULL_GAME__', content, re.DOTALL)
if not match:
    raise ValueError("Could not find window.__CE_LEVELS__ in HTML!")

ce_levels = json.loads(match.group(1))
print(f"Extracted {len(ce_levels)} levels from HTML.")

for idx, lvl in enumerate(ce_levels):
    lvl_id = lvl.get('levelId')
    lvl_name = lvl.get('levelName')
    terrain = lvl.get('terrain', [])
    checkpoints = lvl.get('checkpoints', [])
    newton = lvl.get('newton')
    collectibles = lvl.get('collectibles', [])
    world = lvl.get('world')
    print(f"\n--- Level {idx+1}: {lvl_id} - '{lvl_name}' ---")
    print(f"World: {world}")
    print(f"Terrain count: {len(terrain)}")
    mat_counts = {}
    moving_count = 0
    for t in terrain:
        mat = t.get('material')
        mat_counts[mat] = mat_counts.get(mat, 0) + 1
        if t.get('motion', {}).get('enabled'):
            moving_count += 1
            print(f"  Moving platform: {t['id']} mat={mat} motion={t['motion']}")
    print(f"Material distribution: {mat_counts}")
    print(f"Moving platforms: {moving_count}")
    print(f"Checkpoints: {len(checkpoints)}")
    print(f"Collectibles: {len(collectibles)}")
    print(f"Newton: {newton}")
