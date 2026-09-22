# CU Trollers

**CU Trollers** is a source mod for the Casualties: Unknown mobile multiplayer port. It adds two intentionally chaotic items: **BlueOK** and the **Metal Pipe**.

## BlueOK

BlueOK is a throwable explosive item. A fast throw arms it, and a hard impact with the ground detonates it.

The explosion:

- plays a dedicated explosion sound and effect,
- triggers explosion haptics when CU Haptics is available,
- launches nearby players and supported physics objects,
- synchronizes the explosion and multiplayer knockback through the C:U mobile multiplayer runtime.

### Source

- [BlueOKBootstrap.cs](BlueOK/BlueOKBootstrap.cs)
- [BlueOKItem.cs](BlueOK/BlueOKItem.cs)
- [TemporaryNoclipFlight.cs](BlueOK/TemporaryNoclipFlight.cs)

## Metal Pipe

The Metal Pipe is a heavy melee item built around strong impact and knockback. Its attack checks for players, builder dummies and world geometry, then applies damage and a powerful launch effect to valid targets.

It also uses its own metallic impact/drop sound and supports multiplayer knockback requests.

### Source

- [MetalPipeBootstrap.cs](MetalPipe/MetalPipeBootstrap.cs)
- [MetalPipeItem.cs](MetalPipe/MetalPipeItem.cs)

## Resources

The original item resources are included in this repository and in the release package:

- [BlueOK sprite](Resources/blueok.png)
- [Metal Pipe sprite](Resources/metalpipe.png)
- [BlueOK explosion sound](Resources/sounds/blueok_boom.ogg)
- [Metal Pipe sound](Resources/sounds/metalpipe.ogg)

The resource layout matches the names used by Unity `Resources.Load` in the source code.

## Integration

CU Trollers is designed for the C:U mobile multiplayer codebase. The source uses mobile-port runtime components such as `HGMultiplayerRuntime`, `HGMultiplayerExtensions`, `HGBuilderDummy` and `HGLanguageRuntime`.

BlueOK also calls `HGHaptics.WorldExplosion`. The haptics module is available in [CU Haptics](https://github.com/doubleduoYT/CU_Haptics).

## Download

The latest release contains a C:U mobile source-mod ZIP with the scripts and resources in a Unity-ready layout. GitHub also provides the repository source code automatically with each release.
