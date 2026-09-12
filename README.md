# Wildflight

A playable 3D Flappy Bird interpretation for **Unity 6**, with an original **Blender** hummingbird. Fly through oxidized copper passages above a reflective river, against layered cedar forests and a misty mountain skyline.

## Play

Launch `Builds/Linux/Wildflight.x86_64`, then click **Take flight** or press **Space**.

| Input | Action |
| --- | --- |
| Space, left click, or touch | Flap |
| P or Escape | Pause / continue |
| R or Space after a collision | Retry |
| M | Toggle sound |

The top-right controls toggle sound and graphics quality. Passages increase your score and gradually increase flight speed. Your personal best and sound preference are saved locally. Losing focus pauses the game.

## Open the source

1. Add this folder to Unity Hub. The project was built with **6000.6.0f1**.
2. Open `Assets/Scenes/Riverlands.unity` and press Play. The scene creates its environment on startup.
3. Open `Art/Blender/Hummingbird.blend` in Blender to edit the bird. It contains independent wing pivots and an authored seven-frame wingbeat.

The project uses Unity's built-in render pipeline and built-in modules, with no Asset Store packages or external services. The environment is generated from meshes, layered noise materials, directional lighting, soft shadows, planar water reflections, a procedural sky, and a filmic image effect. The bird uses individually modeled contour and flight feathers with metallic emerald and copper materials. Sound is synthesized locally.

## Rebuild

```bash
# Recreate the original Blender source and FBX export.
bash tools/blender.sh
python3 tools/create_foliage.py

# Set UNITY_EDITOR if your editor lives elsewhere.
bash tools/unity.sh -batchmode -nographics -quit \
  -executeMethod Wildflight.Editor.WildflightBuild.BuildLinux \
  -logFile /tmp/wildflight-build.log

# Run the rendered integration test; requires a graphical desktop.
Builds/Linux/Wildflight.x86_64 --smoke-test \
  --capture-dir "$PWD/Captures" -screen-width 1600 -screen-height 900 \
  -logFile /tmp/wildflight-player.log
```

The build runs nine deterministic simulation checks. The integration test pilots the bird through three passages, checks pause, waits for a collision, checks restart, and writes screenshots plus `Captures/smoke-test.json`.

## Build for the web

```bash
bash tools/unity.sh -batchmode -nographics -quit \
  -executeMethod Wildflight.Editor.WildflightBuild.BuildWebGL \
  -logFile /tmp/wildflight-webgl-build.log
```

This writes a static WebGL player to `Builds/WebGL/`. Serve that directory through a local or hosted web server; opening `index.html` directly from the filesystem is not supported by browsers.

## Project map

- `Assets/Scripts/FlightModel.cs`: deterministic flight and collision rules.
- `Assets/Scripts/WildflightGame.cs`: state transitions, input, obstacle pooling, wing animation, interface, and rendered verification.
- `Assets/Scripts/WorldBuilder.cs`: river valley, trees, banks, pipes, and materials.
- `Assets/Scripts/Cinema.cs`: water reflection camera and filmic rendering.
- `Assets/Scripts/FlightAudio.cs`: synthesized wingbeats, scoring, impact, and ambience.
- `Assets/Editor/WildflightBuild.cs`: scene setup, checks, and Linux build.
- `tools/create_bird.py`: reproducible Blender modeling and FBX export.
- `tools/create_foliage.py`: original cedar alpha texture, generated with Python's standard library.

Original game art and code were created for this project. Bundled fonts are Liberation Sans (SIL Open Font License) and GNU FreeSerif (GPL with the font embedding exception); see `Assets/Resources/Fonts/` for license notices.
