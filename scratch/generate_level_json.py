import json
import math
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

def phase_to_raw_color(phase):
    # I = (1 + cos(phase)) / 2
    intensity = (1.0 + math.cos(phase)) * 0.5
    if intensity >= 0.78:
        return 0.0 # White
    elif intensity <= 0.22:
        return 212.0 # Black
    else:
        return 127.0 # Gray

def convert_level(lvl):
    lvl_id = lvl['levelId']
    lvl_name = lvl['levelName']
    world = lvl['world']
    terrain = lvl['terrain']
    checkpoints = lvl['checkpoints']
    newton = lvl.get('newton')
    collectibles = lvl.get('collectibles', [])

    # World / CameraBoundary
    w_px = float(world['w'])
    h_px = float(world['h'])
    cam_size_x = w_px / 64.0
    cam_size_y = h_px / 64.0
    cam_pos_x = (w_px * 0.5) / 64.0
    cam_pos_y = -(h_px * 0.5) / 64.0

    camera_boundary = {
        "position": {"x": round(cam_pos_x, 6), "y": round(cam_pos_y, 6)},
        "size": {"x": round(cam_size_x, 6), "y": round(cam_size_y, 6)}
    }

    # Start position from CP0
    cp0 = checkpoints[0]
    cp0_x = float(cp0['x'])
    cp0_feet = float(cp0['feet'])
    cp0_phase = float(cp0.get('phase', math.pi * 0.5))
    start_pos_x = (cp0_x + 38.0 * 0.5) / 64.0
    start_pos_y = (-cp0_feet / 64.0) + 0.5 # player feet touches platform top
    start_color = phase_to_raw_color(cp0_phase)

    start_pos = {
        "position": {"x": round(start_pos_x, 6), "y": round(start_pos_y, 6)},
        "size": {"x": 0.59375, "y": 1.0},
        "color": start_color
    }

    # Checkpoints (CP1 through CPn)
    unity_checkpoints = []
    for cp in checkpoints[1:]:
        c_x = float(cp['x'])
        c_feet = float(cp['feet'])
        c_phase = float(cp.get('phase', math.pi * 0.5))
        c_pos_x = (c_x + 38.0 * 0.5) / 64.0
        c_pos_y = (-c_feet / 64.0) + 0.5
        c_color = phase_to_raw_color(c_phase)
        
        # Zone size if specified, else 1.5 x 1.6
        zone = cp.get('zone', {})
        z_w = float(zone.get('w', 100)) / 64.0
        z_h = float(zone.get('h', 105)) / 64.0
        
        unity_checkpoints.append({
            "position": {"x": round(c_pos_x, 6), "y": round(c_pos_y, 6)},
            "size": {"x": round(z_w, 6), "y": round(z_h, 6)},
            "color": c_color
        })

    # Platforms
    unity_platforms = []
    for t in terrain:
        t_x = float(t['x'])
        t_y = float(t['y'])
        t_w = float(t['w'])
        t_h = float(t['h'])
        mat = t['material']

        if mat == 'WHITE':
            raw_c = 0.0
        elif mat == 'GRAY':
            raw_c = 127.0
        elif mat == 'BLACK':
            raw_c = 212.0
        elif mat == 'FIXED':
            raw_c = 255.0
        else:
            raw_c = 255.0

        p_pos_x = (t_x + t_w * 0.5) / 64.0
        p_pos_y = -(t_y + t_h * 0.5) / 64.0
        p_size_x = t_w / 64.0
        p_size_y = t_h / 64.0

        p_dict = {
            "position": {"x": round(p_pos_x, 6), "y": round(p_pos_y, 6)},
            "size": {"x": round(p_size_x, 6), "y": round(p_size_y, 6)},
            "rawGrayscaleColor": raw_c
        }

        # Moving platform motion
        motion = t.get('motion')
        if motion and motion.get('enabled'):
            dx_u = float(motion.get('dx', 0)) / 64.0
            dy_u = -float(motion.get('dy', 0)) / 64.0
            length = math.hypot(dx_u, dy_u)
            rot_deg = math.degrees(math.atan2(dy_u, dx_u)) if length > 1e-6 else 0.0
            period = float(motion.get('period', 4.0))

            p_dict["trajectory"] = {
                "enabled": True,
                "curveType": 0, # Line
                "domainMode": 0, # ParameterRange
                "playbackMode": 1, # PingPong
                "originX": round(p_pos_x, 6),
                "originY": round(p_pos_y, 6),
                "rotation": round(rot_deg, 4),
                "scaleX": round(length, 6),
                "scaleY": 1.0,
                "paramMin": 0.0,
                "paramMax": 1.0,
                "boundsXMin": -10.0,
                "boundsXMax": 10.0,
                "boundsYMin": -10.0,
                "boundsYMax": 10.0,
                "radius": 2.0,
                "duration": round(period, 4),
                "reverseDirection": False
            }

        unity_platforms.append(p_dict)

    # Newton chamber data
    unity_newton = None
    if newton:
        unity_newton = {
            "cx": float(newton.get('cx', 5135)),
            "cy": float(newton.get('cy', 659)),
            "i": int(newton.get('i', 3)),
            "k": int(newton.get('k', 6)),
            "R": 1500.0,
            "lambda": 550.0,
            "orbitStart": newton.get('orbitStart', 'auto')
        }
        rails = newton.get('rails', {})
        if rails:
            unity_newton["rails"] = {}
            if 'K' in rails:
                rK = rails['K']
                unity_newton["rails"]["K"] = {
                    "width": float(rK.get('width', 136)),
                    "period": float(rK.get('period', 10.8)),
                    "angleDeg": float(rK.get('angleDeg', 24.4)),
                    "direction": int(rK.get('direction', -1))
                }
            if 'I' in rails:
                rI = rails['I']
                unity_newton["rails"]["I"] = {
                    "width": float(rI.get('width', 126)),
                    "period": float(rI.get('period', 8.6)),
                    "angleDeg": float(rI.get('angleDeg', 135)),
                    "direction": int(rI.get('direction', 1))
                }

    # Collectibles
    unity_collectibles = []
    for c in collectibles:
        unity_collectibles.append({
            "id": c.get('id', ''),
            "kind": c.get('kind', 'collectible'),
            "x": float(c.get('x', 0)),
            "y": float(c.get('y', 0)),
            "w": float(c.get('w', 26)),
            "h": float(c.get('h', 26))
        })

    level_data = {
        "startPos": start_pos,
        "checkPoint": unity_checkpoints,
        "end": None,
        "spawnPoint": None,
        "cameraBoundary": camera_boundary,
        "platforms": unity_platforms,
        "newton": unity_newton,
        "collectibles": unity_collectibles,
        "levelId": lvl_id,
        "levelName": lvl_name
    }
    return level_data

for lvl in ce_levels:
    lvl_id = lvl['levelId']
    conv = convert_level(lvl)
    print(f"Generated {lvl_id}: platforms={len(conv['platforms'])}, checkpoints={len(conv['checkPoint'])}, newton={conv['newton'] is not None}")
    
    # Save to Assets/Resources/Levels/{lvl_id}.json
    out_path = f"Assets/Resources/Levels/{lvl_id}.json"
    with open(out_path, 'w', encoding='utf-8') as f:
        json.dump(conv, f, indent=2, ensure_ascii=False)
    print(f"Saved to {out_path}")
