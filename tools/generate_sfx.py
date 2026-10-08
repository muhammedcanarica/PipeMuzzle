"""Rebuild original Ruilay SFX using only Python's standard library.

Deterministic mono 44.1 kHz PCM; no recordings, downloads or dependencies.
"""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 44100
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/Resources/Audio/SFX"


def envelope(t, duration, attack=.008, release=.04, decay=0):
    a = min(1., max(0., t / attack))
    r = min(1., max(0., (duration - t) / release))
    return a*a*(3-2*a) * r*r*(3-2*r) * math.exp(-decay*t)


def bell(t, frequency):
    return (math.sin(math.tau*frequency*t) + .18*math.sin(math.tau*frequency*2.01*t)
            + .04*math.sin(math.tau*frequency*3.97*t)) / 1.22


def render(name, duration, sample, attack=.008, release=.04):
    data = [sample(i/RATE) * envelope(i/RATE, duration, attack, release) for i in range(round(duration*RATE))]
    peak = max(abs(x) for x in data)
    gain = min(1., .42 / max(peak, .0001))
    data[0] = data[-1] = 0.
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as out:
        out.setparams((1, 2, RATE, len(data), "NONE", "not compressed"))
        out.writeframes(b"".join(struct.pack("<h", round(x*gain*32767)) for x in data))
    print(f"{name}: {duration:.3f}s, peak {peak*gain:.3f}")


def melody(notes, duration):
    def sample(t):
        return sum(.24*bell(t-start, frequency)*envelope(t-start, duration-start, .014, .13, 5)
                   for start, frequency in notes if t >= start)
    return sample


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    rng = random.Random(20261006)
    filtered = softened = rumble = 0.
    def water(t):
        nonlocal filtered, softened, rumble
        # Smooth both the scratchy high end and low rumble. No sustained pitched tones.
        filtered += .28 * (rng.uniform(-1, 1) - filtered)
        softened += .16 * (filtered - softened)
        rumble += .006 * (softened - rumble)
        swell = .75 + .25 * math.sin(math.pi * t / .8)
        return .18 * (softened - rumble) * swell
    render("PipeRotate", .095, lambda t: .38*(math.sin(math.tau*420*t)*math.exp(-55*t) + .25*math.sin(math.tau*1150*t)*math.exp(-85*t)))
    render("UiClick", .065, lambda t: .30*math.sin(math.tau*(720*t-2600*t*t))*math.exp(-48*t))
    render("Hint", .32, melody([(0, 1046.5), (.075, 1568)], .32))
    render("WaterFlow", .8, water, attack=.11, release=.22)
    render("TargetReached", .085, lambda t: .22*bell(t, 880)*math.exp(-25*t))
    render("LevelComplete", .94, melody([(0, 523.25), (.16, 659.25), (.34, 783.99)], .94))
    render("WorldUnlock", 1.35, melody([(0, 659.25), (.22, 783.99), (.46, 1046.5)], 1.35))


if __name__ == "__main__":
    main()
