import unittest
import copy
import json
from level_design import ROOT, Tile, analyze, validate_designs


class SolverTests(unittest.TestCase):
    def test_current_manifest_keeps_unique_routes_and_small_declared_cosmetic_groups(self):
        designs = json.loads((ROOT / 'tools/level_design_manifest.json').read_text(encoding='utf-8'))
        validate_designs(designs)
        self.assertEqual(sum(len(d.get('cosmetic_fillers', [])) for d in designs), 10)

    def test_clockwise_cost_respects_straight_symmetry(self):
        tiles = {(1, 1): Tile(2, 1, 0, True),
                 (2, 1): Tile(1, 0, 0, False),
                 (3, 1): Tile(2, 2, 3, True)}
        result = analyze(5, 3, tiles)
        self.assertEqual(result['minimum_moves'], 1)
        self.assertEqual(result['route_count'], 1)
        self.assertEqual(result['shortest_path'], 3)

    def test_dead_end_does_not_need_rotation_or_join_success_path(self):
        tiles = {(1, 1): Tile(3, 1, 0, True),
                 (2, 1): Tile(1, 0, 0, False),
                 (3, 1): Tile(2, 2, 3, True),
                 (1, 2): Tile(2, 0, 1, False),
                 (2, 2): Tile(1, 0, 0, False)}
        result = analyze(5, 4, tiles)
        self.assertEqual(result['minimum_moves'], 1)
        self.assertEqual(result['route_count'], 1)
        self.assertNotIn((1, 2), result['path'])
        self.assertIn((1, 2), result['reachable'])
        self.assertEqual(set(result['rotations']), set(result['path']))

    def test_locked_wrong_port_makes_board_unsolvable(self):
        tiles = {(1, 1): Tile(2, 1, 0, True),
                 (2, 1): Tile(1, 0, 0, False),
                 (3, 1): Tile(2, 2, 0, True)}
        result = analyze(5, 3, tiles)
        self.assertIsNone(result['minimum_moves'])
        self.assertEqual(result['route_count'], 0)

    def test_manifest_rejects_solved_spawn_even_with_matching_zero_budget(self):
        designs = copy.deepcopy(json.loads((ROOT / 'tools/level_design_manifest.json').read_text(encoding='utf-8')))
        level = designs[1]
        tiles = {(t['x'], t['y']): Tile(t['shape'], t['role'], t['startRotation'], bool(t['isLocked']))
                 for t in level['tiles']}
        solution = analyze(level['width'], level['height'], tiles)
        for tile in level['tiles']:
            pos = tile['x'], tile['y']
            if pos in solution['rotations']:
                tile['startRotation'] = solution['rotations'][pos]
        level['metrics']['minimum_moves'] = 0
        with self.assertRaisesRegex(ValueError, 'starts solved'):
            validate_designs(designs)


if __name__ == '__main__':
    unittest.main()
