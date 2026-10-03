"""Offline level authoring and exhaustive validation. No runtime dependency.

The solver enumerates simple source/target paths and assigns one orientation to
each visited tile. Unlike an edge-only search, it cannot reuse a tile in two
incompatible orientations. Clockwise click costs include straight/Cross symmetry.
"""
from dataclasses import dataclass
from pathlib import Path
import argparse
import csv
import json
import random
import re

ROOT = Path(__file__).resolve().parents[1]
DIRS = ((0, 1), (1, 0), (0, -1), (-1, 0))
BASE = (0, 5, 3, 7, 15)


@dataclass(frozen=True)
class Tile:
    shape: int
    role: int = 0
    rotation: int = 0
    locked: bool = False


def analyze(width, height, tiles):
    active = {pos: tile for pos, tile in tiles.items() if tile.shape}
    source = next((p for p, t in active.items() if t.role == 1), None)
    target = next((p for p, t in active.items() if t.role == 2), None)
    result = {'minimum_moves': None, 'route_count': 0, 'shortest_path': 0,
              'path': [], 'rotations': {}, 'reachable': set(), 'complete': True}
    if source is None or target is None:
        return result
    options = {}
    for pos, tile in active.items():
        for entry in range(4):
            options[pos, entry] = []
            for exit_dir in range(4):
                if exit_dir == entry:
                    continue
                matching = [(cost, rotation) for cost, rotation, mask in orientations(tile)
                            if mask & (1 << entry) and mask & (1 << exit_dir)]
                if matching:
                    cost, rotation = min(matching)
                    options[pos, entry].append((exit_dir, cost, rotation))
    route, visited, rotations = [source], {source}, {source: active[source].rotation}
    result['reachable'].add(source)
    calls = 0

    def visit(pos, entry, cost):
        nonlocal calls
        calls += 1
        if calls > 200000:
            result['complete'] = False
            return
        tile = active[pos]
        # Reject an incompatible locked entry before counting it as reachable.
        entry_options = [(c, r) for c, r, mask in orientations(tile) if mask & (1 << entry)]
        if not entry_options:
            return
        result['reachable'].add(pos)
        if pos == target:
            end_cost, end_rotation = min(entry_options)
            result['route_count'] += 1
            result['shortest_path'] = min(result['shortest_path'] or len(route), len(route))
            total = cost + end_cost
            if result['minimum_moves'] is None or total < result['minimum_moves']:
                result['minimum_moves'] = total
                result['path'] = list(route)
                result['rotations'] = {p: rotations[p] for p in route[:-1]}
                result['rotations'][pos] = end_rotation
            return
        for exit_dir, tile_cost, rotation in options[pos, entry]:
            dx, dy = DIRS[exit_dir]
            neighbor = (pos[0] + dx, pos[1] + dy)
            if neighbor not in active or neighbor in visited:
                continue
            rotations[pos] = rotation
            visited.add(neighbor)
            route.append(neighbor)
            visit(neighbor, (exit_dir + 2) % 4, cost + tile_cost)
            route.pop()
            visited.remove(neighbor)
            if not result['complete']:
                break
    for direction, (dx, dy) in enumerate(DIRS):
        if not mask_at(active[source].shape, active[source].rotation) & (1 << direction):
            continue
        neighbor = (source[0] + dx, source[1] + dy)
        if neighbor not in active:
            continue
        visited.add(neighbor)
        route.append(neighbor)
        visit(neighbor, (direction + 2) % 4, 0)
        route.pop()
        visited.remove(neighbor)
    return result


def mask_at(shape, rotation):
    mask = BASE[shape]
    for _ in range(rotation):
        mask = ((mask << 1) | (mask >> 3)) & 15
    return mask


def orientations(tile):
    for rotation in range(4):
        if not tile.locked or rotation == tile.rotation:
            yield ((rotation - tile.rotation) % 4, rotation, mask_at(tile.shape, rotation))


def load_level(path):
    text = path.read_text(encoding='utf-8-sig')
    width = int(re.search(r'  width: (\d+)', text)[1])
    height = int(re.search(r'  height: (\d+)', text)[1])
    values = re.findall(r'- x: (\d+)\s+y: (\d+)\s+shape: (\d+)\s+role: (\d+)\s+startRotation: (\d+)\s+isLocked: (\d+)', text)
    tiles = {(int(x), int(y)): Tile(int(s), int(role), int(r), bool(int(lock)))
             for x, y, s, role, r, lock in values}
    if len(tiles) != len(values):
        raise ValueError(f'Duplicate tile in {path}')
    return width, height, tiles


def asset_paths():
    for world, folder, prefix in [('Sakura', '', ''), ('Bamboo', 'BambooLevels/', 'Bamboo_'), ('Moon', 'MoonLevels/', 'Moon_')]:
        for number in range(1, 13):
            yield world, number, ROOT / f'Assets/Scripts/Data/{folder}{prefix}Level_{number:03}.asset'


def metrics(world, number, width, height, tiles, result):
    active = sum(t.shape != 0 for t in tiles.values())
    return dict(world=world, level=number, board=f'{width}x{height}', pipes=active,
                density=round(active/(width*height), 3), route=result['shortest_path'],
                distractors=active-result['shortest_path'], minimum_moves=result['minimum_moves'],
                routes=result['route_count'], corners=sum(t.shape == 2 and t.role == 0 for t in tiles.values()),
                threeways=sum(t.shape == 3 for t in tiles.values()), crosses=sum(t.shape == 4 for t in tiles.values()),
                search_complete=result['complete'])


def audit():
    rows = []
    for world, number, path in asset_paths():
        width, height, tiles = load_level(path)
        rows.append(metrics(world, number, width, height, tiles, analyze(width, height, tiles)))
    return rows


def inside_mask(pos, mask, width, height):
    return all(not mask & (1 << d) or 0 <= pos[0]+dx < width and 0 <= pos[1]+dy < height
               for d, (dx, dy) in enumerate(DIRS))


def direction(a, b):
    return DIRS.index((b[0]-a[0], b[1]-a[1]))


# width, height, route tiles, distractors, T-junctions, crosses, exact clockwise moves.
# These are content budgets, never a runtime level generator.
PROFILES = {
    'Sakura': [(3,3,3,0,0,0,1), (4,3,5,1,0,0,3), (4,3,7,1,0,0,4),
               (4,4,8,2,0,0,6), (4,4,9,3,0,0,7), (4,4,10,3,1,0,8),
               (5,4,11,4,1,0,10), (5,4,12,4,2,0,11), (5,4,13,4,2,0,12),
               (5,5,14,5,2,0,14), (5,5,15,5,3,0,15), (5,5,16,6,3,0,17)],
    'Bamboo': [(5,4,12,4,2,0,14), (5,4,13,4,2,0,16), (5,4,14,4,3,0,17),
               (5,5,15,5,3,0,19), (5,5,16,5,3,0,20), (5,5,17,5,4,0,22),
               (6,5,18,6,4,0,24), (6,5,19,6,4,0,25), (6,5,20,6,4,0,27),
               (6,6,21,7,5,0,29), (6,6,22,8,5,0,31), (6,6,24,8,5,0,33)],
    'Moon': [(6,5,21,6,4,1,29), (6,6,22,7,4,1,31), (6,6,23,7,4,1,33),
             (6,6,24,7,4,2,35), (6,6,25,7,4,2,36), (6,6,26,7,4,2,38),
             (7,6,27,7,5,2,39), (7,6,28,8,5,2,41), (7,6,29,8,5,2,43),
             (7,7,30,9,5,3,45), (7,7,31,9,5,3,47), (7,7,32,10,6,3,50)]
}


def walk(width, height, length, rng):
    start = (rng.randrange(width), rng.randrange(height))
    path, used = [start], {start}
    while len(path) < length:
        pos = path[-1]
        neighbors = [(pos[0]+dx, pos[1]+dy) for dx, dy in DIRS]
        neighbors = [p for p in neighbors if 0 <= p[0] < width and 0 <= p[1] < height and p not in used]
        if not neighbors:
            return None
        # Prefer bends, with enough straight sections to keep the network readable.
        rng.shuffle(neighbors)
        if len(path) > 1 and rng.random() < .7:
            previous = direction(path[-2], pos)
            neighbors.sort(key=lambda p: direction(pos, p) == previous)
        pos = neighbors[0]
        path.append(pos)
        used.add(pos)
    return path


def route_tiles(path, width, height, rng):
    tiles = {}
    for i, pos in enumerate(path):
        if i in (0, len(path)-1):
            port = direction(pos, path[1] if i == 0 else path[-2])
            candidates = [r for r in range(4) if mask_at(2, r) & (1 << port)
                          and inside_mask(pos, mask_at(2, r), width, height)]
            if not candidates:
                return None
            tiles[pos] = Tile(2, 1 if i == 0 else 2, rng.choice(candidates), True)
        else:
            mask = (1 << direction(pos, path[i-1])) | (1 << direction(pos, path[i+1]))
            shape = 1 if mask in (5, 10) else 2
            rotation = next(r for r in range(4) if mask_at(shape, r) == mask)
            tiles[pos] = Tile(shape, 0, rotation)
    return tiles


def unique(width, height, tiles, path, all_reachable=False):
    result = analyze(width, height, tiles)
    return result['complete'] and result['route_count'] == 1 and result['path'] == path and (
        not all_reachable or len(result['reachable']) == len(tiles))


def add_junctions(tiles, path, width, height, count, shape, rng):
    if count == 0:
        return True
    positions = [p for p in path[1:-1] if tiles[p].shape < 3
                 and 0 < p[0] < width-1 and 0 < p[1] < height-1]
    rng.shuffle(positions)
    added = 0
    for pos in positions:
        original = tiles[pos]
        ports = mask_at(original.shape, original.rotation)
        candidates = [r for r in range(4) if mask_at(shape, r) & ports == ports]
        rng.shuffle(candidates)
        for rotation in candidates:
            tiles[pos] = Tile(shape, 0, rotation)
            if unique(width, height, tiles, path):
                added += 1
                break
            tiles[pos] = original
        if added == count:
            return True
    return added == count


def add_decoys(tiles, path, width, height, count, rng):
    for _ in range(count):
        candidates = [(x,y) for y in range(height) for x in range(width) if (x,y) not in tiles]
        rng.shuffle(candidates)
        accepted = False
        for pos in candidates:
            if not any((pos[0]+dx, pos[1]+dy) in tiles for dx,dy in DIRS):
                continue
            shapes = [1, 2]
            rng.shuffle(shapes)
            for shape in shapes:
                valid = [r for r in range(4) if inside_mask(pos, mask_at(shape,r), width,height)]
                if not valid:
                    continue
                tiles[pos] = Tile(shape, 0, rng.choice(valid))
                if unique(width, height, tiles, path, all_reachable=True):
                    accepted = True
                    break
                del tiles[pos]
            if accepted:
                break
        if not accepted:
            return False
    return True


def scramble(tiles, path, width, height, budget, rng):
    # Dynamic programming distributes an authored click budget across the route.
    # Prefer rotating more distinct tiles over padding a few tiles with three clicks.
    choices = {0: (0, {})}
    for i, pos in enumerate(path[1:-1], 1):
        tile = tiles[pos]
        required = (1 << direction(pos, path[i-1])) | (1 << direction(pos, path[i+1]))
        valid = [r for r in range(4) if inside_mask(pos, mask_at(tile.shape,r), width,height)]
        rng.shuffle(valid)
        next_choices = {}
        for total, (score, rotations) in choices.items():
            for initial in valid:
                cost = min((r-initial)%4 for r in range(4) if mask_at(tile.shape,r) & required == required)
                if total + cost > budget:
                    continue
                quality = score + (10 if cost else 0) - (2 if cost == 3 else 0)
                if total+cost not in next_choices or quality > next_choices[total+cost][0]:
                    updated = dict(rotations)
                    updated[pos] = initial
                    next_choices[total+cost] = (quality, updated)
        choices = next_choices
    if budget not in choices:
        return False
    for pos, rotation in choices[budget][1].items():
        tile = tiles[pos]
        tiles[pos] = Tile(tile.shape, tile.role, rotation, tile.locked)
    return True


def generate():
    designs = []
    for wi, (world, profiles) in enumerate(PROFILES.items()):
        for number, profile in enumerate(profiles, 1):
            width,height,length,decoys,tees,crosses,budget = profile
            rng = random.Random(20261003 + wi*1000 + number)
            for attempt in range(50000):
                path = [(1,0), (1,1), (1,2)] if wi == 0 and number == 1 else walk(width,height,length,rng)
                if not path:
                    continue
                if length > 7 and sum(abs(a-b) for a,b in zip(path[0],path[-1])) > length*.65:
                    continue
                tiles = route_tiles(path,width,height,rng)
                if tiles is None or not unique(width,height,tiles,path):
                    continue
                if not add_junctions(tiles,path,width,height,crosses,4,rng):
                    continue
                if not add_junctions(tiles,path,width,height,tees,3,rng):
                    continue
                if not scramble(tiles,path,width,height,budget,rng):
                    continue
                if not add_decoys(tiles,path,width,height,decoys,rng):
                    continue
                result = analyze(width,height,tiles)
                if result['minimum_moves'] != budget or not unique(width,height,tiles,path,True):
                    continue
                design = dict(world=world, level=number, width=width, height=height,
                              route=path, tiles=[dict(x=p[0],y=p[1],shape=t.shape,role=t.role,
                                  startRotation=t.rotation,isLocked=int(t.locked)) for p,t in sorted(tiles.items())],
                              metrics=metrics(world,number,width,height,tiles,result), candidate=attempt)
                designs.append(design)
                print(json.dumps(design['metrics']), flush=True)
                break
            else:
                raise RuntimeError(f'No acceptable authored candidate: {world} {number} {profile}')
    return designs


def validate_designs(designs):
    expected = {(w,n) for w,n,_ in asset_paths()}
    if len(designs) != 36 or {(d['world'],d['level']) for d in designs} != expected:
        raise ValueError('Manifest must contain exactly the existing 36 level identities.')
    for d in designs:
        w, h = d['width'], d['height']
        tiles = {(t['x'],t['y']): Tile(t['shape'],t['role'],t['startRotation'],bool(t['isLocked'])) for t in d['tiles']}
        if len(tiles) != len(d['tiles']) or any(not (0 <= x < w and 0 <= y < h) for x,y in tiles):
            raise ValueError('Duplicate or out-of-bounds tile.')
        if any(not (1 <= t.shape <= 4 and 0 <= t.role <= 2 and 0 <= t.rotation <= 3) or
               not inside_mask(p,mask_at(t.shape,t.rotation),w,h) for p,t in tiles.items()):
            raise ValueError('Invalid tile or outward initial port.')
        if any(sum(t.role == role for t in tiles.values()) != 1 for role in (1,2)) or any(
                not t.locked for t in tiles.values() if t.role):
            raise ValueError('Expected one locked source and target.')
        result = analyze(w,h,tiles)
        if result['minimum_moves'] == 0:
            raise ValueError('Level starts solved.')
        route = [tuple(p) for p in d['route']]
        if not unique(w,h,tiles,route,True) or result['minimum_moves'] != d['metrics']['minimum_moves']:
            raise ValueError('Incomplete/ambiguous solution or changed click budget.')


def write_manifest(designs, path):
    # One tile per line keeps the authored content easy to review.
    blocks = []
    for d in designs:
        fields = [f'    "{key}": {json.dumps(d[key])}' for key in ('world','level','width','height','route','metrics','candidate')]
        fields.append('    "tiles": [\n' + ',\n'.join('      '+json.dumps(t) for t in d['tiles']) + '\n    ]')
        blocks.append('  {\n' + ',\n'.join(fields) + '\n  }')
    path.write_text('[\n'+',\n'.join(blocks)+'\n]\n', encoding='utf-8')


def apply(designs):
    # Validate all input before changing any asset.
    validate_designs(designs)
    paths = {(w,n): p for w,n,p in asset_paths()}
    for design in designs:
        path = paths[design['world'], design['level']]
        # Preserve the ScriptableObject header and asset/meta identity.
        header = path.read_text(encoding='utf-8-sig').split('  width:')[0]
        width, height = design['width'], design['height']
        tiles = {(t['x'],t['y']): t for t in design['tiles']}
        body = f'  width: {width}\n  height: {height}\n  tiles:\n'
        for y in reversed(range(height)):
            for x in range(width):
                tile = tiles.get((x,y), dict(shape=0,role=0,startRotation=0,isLocked=0))
                body += f'  - x: {x}\n    y: {y}\n'
                for field in ('shape','role','startRotation','isLocked'):
                    body += f'    {field}: {tile[field]}\n'
        path.write_text(header+body, encoding='utf-8', newline='\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--generate', action='store_true', help='Select fixed offline candidates into a reviewable manifest.')
    parser.add_argument('--apply', action='store_true', help='Bake the reviewed manifest into the existing 36 assets.')
    parser.add_argument('--csv', type=Path, help='Export exact current-asset metrics.')
    args = parser.parse_args()
    manifest = ROOT / 'tools/level_design_manifest.json'
    if args.generate:
        write_manifest(generate(), manifest)
    elif args.apply:
        apply(json.loads(manifest.read_text(encoding='utf-8')))
    else:
        rows = audit()
        for row in rows:
            print(json.dumps(row))
        if args.csv:
            with args.csv.open('w', newline='', encoding='utf-8') as stream:
                writer = csv.DictWriter(stream, fieldnames=rows[0].keys())
                writer.writeheader()
                writer.writerows(rows)
