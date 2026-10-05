"""Endpoint artwork contracts; no Unity or external image service required."""
import importlib.util
import unittest
from pathlib import Path


class EndpointArtworkTests(unittest.TestCase):
    def generator(self):
        path = Path(__file__).with_name("generate_endpoint_art.py")
        self.assertTrue(path.exists(), "Local endpoint artwork generator is missing")
        spec = importlib.util.spec_from_file_location("endpoint_art", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module

    def test_worlds_have_different_object_silhouettes(self):
        art = self.generator()
        for source in (True, False):
            silhouettes = [art.endpoint(world, source).getchannel("A").tobytes() for world in art.WORLDS]
            self.assertEqual(len(set(silhouettes)), 3)

    def test_transparency_size_and_deterministic_paint(self):
        art = self.generator()
        for world in art.WORLDS:
            for source in (True, False):
                image = art.endpoint(world, source)
                self.assertEqual(image.size, (627, 627))
                self.assertEqual(image.mode, "RGBA")
                self.assertEqual(image.getpixel((0, 0))[3], 0)
                self.assertEqual(image.tobytes(), art.endpoint(world, source).tobytes())
                self.assertGreater(sum(a > 128 for a in image.getchannel("A").get_flattened_data()), 35000)

    def test_arrival_water_stays_inside_basin_and_contains_no_body(self):
        art = self.generator()
        for world in art.WORLDS:
            water = art.target_water(world)
            self.assertGreater(water.getpixel((313, 313))[3], 200)
            self.assertEqual(water.getpixel((313, 100))[3], 0)
            self.assertEqual(water.getpixel((100, 313))[3], 0)
            box = water.getchannel("A").getbbox()
            self.assertGreater(box[0], 200)
            self.assertGreater(box[1], 200)
            self.assertLess(box[2], 427)
            self.assertLess(box[3], 427)


if __name__ == "__main__":
    unittest.main()
