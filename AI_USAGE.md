# Generative AI usage

## Models used

For this project I used the free `ChatGPT` version without an account to brainstorm ideas for game mechanics.

`Claude Haiku 4.5` was used to fix a crash bug in the game loop when returning to the main menu after a run, resolve a score persistence issue in `HighScoreManager`, and figure out how to procedurally generate the dungeon maps.

## Fully / mostly generated regions

1. `World/Map.cs` — procedural dungeon generation, room placement, corridor carving, enemy/item spawning

## Partially assisted

- `Entities/` — class hierarchy and stats level-up
- `DungeonGame.cs` game loop structure — SDL threading issue
- `Engine/GameRenderer.cs` and `Engine/BitmapFont.cs` — SDL2 rendering pipeline and bitmap font
