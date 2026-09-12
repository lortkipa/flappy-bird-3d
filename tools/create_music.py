"""Render the original River Run loop. Requires NumPy; no sampled music."""
from pathlib import Path
import wave
import numpy as np

RATE = 44100
BEAT = 60 / 116
LENGTH = round(64 * BEAT * RATE)
mix = np.zeros((LENGTH, 2), dtype=np.float64)
rng = np.random.default_rng(116)


def add(signal, beat, gain, pan=0, echo=False):
    indices = (round(beat * BEAT * RATE) + np.arange(len(signal))) % LENGTH
    stereo = signal[:, None] * np.sqrt([(1 - pan) / 2, (1 + pan) / 2]) * gain
    np.add.at(mix, indices, stereo)
    if echo:
        for delay, level in [(0.75, 0.22), (1.5, 0.09)]:
            np.add.at(mix, (indices + round(delay * BEAT * RATE)) % LENGTH,
                      stereo[:, ::-1] * level)


def note(midi, duration, kind):
    t = np.arange(round(duration * BEAT * RATE)) / RATE
    phase = 2 * np.pi * 440 * 2 ** ((midi - 69) / 12) * t
    attack = np.minimum(t / 0.008, 1)
    release = np.minimum((t[-1] - t) / 0.045, 1)
    if kind == "pluck":
        tone = np.sin(phase) + 0.35 * np.sin(2 * phase) * np.exp(-t * 12)
        envelope = np.exp(-t * 5)
    elif kind == "bass":
        tone = np.sin(phase) + 0.22 * np.sin(2 * phase)
        envelope = np.exp(-t * 2)
    else:
        tone = np.sin(phase) + 0.18 * np.sin(phase * 2)
        envelope = np.minimum(t / 0.1, 1) * np.exp(-t * 1.4)
    return tone * attack * release * envelope


def drum(kind):
    seconds = {"kick": 0.3, "snare": 0.18, "hat": 0.055}[kind]
    t = np.arange(round(seconds * RATE)) / RATE
    noise = rng.uniform(-1, 1, len(t))
    if kind == "kick":
        signal = np.sin(2 * np.pi * (48 * t + 55 * 0.025 * (1 - np.exp(-t / 0.025)))) * np.exp(-t * 18)
    elif kind == "snare":
        signal = (noise * 0.65 + np.sin(2 * np.pi * 185 * t) * 0.35) * np.exp(-t * 27)
    else:
        signal = (noise - np.roll(noise, 1)) * np.exp(-t * 75)
    return signal * np.minimum(t / 0.002, 1) * np.minimum((t[-1] - t) / 0.008, 1)


# D major: Dmaj7, Aadd9, Bm7, Gmaj7. Four phrases with an answering melody.
chords = [(50, [62, 66, 69, 73]), (45, [61, 64, 69, 71]),
          (47, [62, 66, 69, 71]), (43, [62, 66, 67, 71])]
melodies = [[78, 81, 85, 81, 78, 76], [76, 73, 76, 81, 83, 81],
            [78, 81, 83, 85, 83, 78], [78, 74, 78, 81, 78, 76]]
for bar in range(16):
    root, chord = chords[(bar // 2) % 4]
    start = bar * 4
    for i, pitch in enumerate(chord):
        add(note(pitch, 3.8, "pad"), start, 0.055, (i - 1.5) * 0.35)
    for offset, pitch in [(0, root), (1.5, root), (2.5, root + 12), (3.5, root + 7)]:
        add(note(pitch, 0.48, "bass"), start + offset, 0.3)
    for step in range(8):
        add(note(chord[step % 4] + 12, 0.8, "pluck"), start + step * 0.5,
            0.055, -0.4 if step % 2 else 0.4, True)
        add(drum("hat"), start + step * 0.5 + (0.025 if step % 2 else 0),
            0.032 if step % 2 == 0 else 0.05, 0.25)
    for offset in [0, 2, 2.75] if bar % 2 else [0, 2]:
        add(drum("kick"), start + offset, 0.34)
    for offset in [1, 3]:
        add(drum("snare"), start + offset, 0.12, -0.12)
    melody = melodies[(bar // 2) % 4]
    if bar % 2:
        melody = melody[3:] + melody[:3]
    for i, offset in enumerate([0.5, 1, 1.75, 2.5, 3, 3.5]):
        # The final phrase resolves back into the opening chord.
        pitch = 74 if bar == 15 and i == 5 else melody[i]
        add(note(pitch, 1.2, "pluck"), start + offset, 0.13, -0.1, True)

mix = np.tanh(mix * 1.3)
mix *= 0.85 / np.max(np.abs(mix))
destination = Path(__file__).resolve().parents[1] / "Assets/Resources/Audio/RiverRun.wav"
destination.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(destination), "wb") as output:
    output.setparams((2, 2, RATE, LENGTH, "NONE", "not compressed"))
    output.writeframes((mix * 32767).astype("<i2").tobytes())
print(f"Wrote {destination}: {LENGTH / RATE:.2f}s, peak {np.max(np.abs(mix)):.3f}")
