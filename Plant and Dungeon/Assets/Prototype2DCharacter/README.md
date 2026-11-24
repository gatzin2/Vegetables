# 2D Battle Character Prototype

This prototype targets the battle layer, not the 3D exploration map.

## Scene

Open `Assets/Prototype2DCharacter/Scenes/BattleAnimationPrototype.unity` and select `BattleAnimationTestBench` in the Hierarchy.

## Inspector workflow

1. Assign player and enemy character Prefabs (with `BattleCharacterView` on their root).
2. Assign the two anchor transforms and the battle stage. Player faces right; enemy faces left.
3. Click **装配／替换双方角色** in Edit Mode to apply the Prefab choices. Save the scene to retain the setup.
4. Assign a `BattleThemeDefinition` and click **应用背景主题** if desired. Create themes via Assets > Create > Plant and Dungeon > Battle Theme.
5. Enter Play Mode. Select the preview target and action, then click **播放所选动作**. Use the reset button after Death.
6. For a class-specific skill, fill in its full Animator state name (for example `Base Layer.Cast`). Clear it to return to the standard action selector.

The prefab field selects imported Unity character prefabs, not raw PSD/PSB images. Build the PSB rig and Animator inside the character Prefab first.

## Controls

- `1`: Idle/reset
- `Space`: play the selected Inspector action on the selected target

## Art replacement path

The battle stage keeps allies on the left and enemies on the right at fixed positions. Walking belongs to the separate 3D exploration presentation and is intentionally absent from this battle Animator.

Characters are authored in right-facing profile (nose, eye, and boots point toward +X). The enemy mirrors the entire rig to face left. Attack extends the weapon toward the opponent while the character root stays at its assigned position. Arrow keys no longer change facing.

## Resource ownership

`Characters/Placeholder/PlaceholderFighter.prefab` and its `Animations/` folder are independent authored resources. Put future classes under `Characters/<ClassName>/` and duplicate the placeholder resources when starting a new character.

The builder creates missing placeholder resources once and preserves existing Prefabs, animation clips, and controllers. When a configured test bench already exists, rebuilding preserves its scene configuration too. The old `Generated/Animations/` directory is legacy material and is no longer referenced by this test bench.

The generated characters use layered placeholder sprites under `VisualRoot/Rig`. Replace those layers with a PSB imported through Unity 2D PSD Importer, bind the bones with Sprite Skin, and preserve the animation state names. Combat code only talks to `BattleCharacterView`.

Regional battle art is represented by `BattleThemeDefinition`. A 3D exploration area selects a theme id such as `wasteland-frontier-town`; the battle stage applies its far, mid, and foreground sprites.
