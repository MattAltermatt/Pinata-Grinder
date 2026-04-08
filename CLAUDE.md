# Pinata Grinder — Claude Code Guide

## Project Overview

A physics-based arcade game where coloured "pinata" objects fall from above the screen
and land on the terrain. Players drag stoppers and their mounted weapons to destroy
incoming pinatas.

Target platforms: **WebGL, Android, iOS**
Unity version: **6000.4.1f1 (Unity 6)**
Render pipeline: **URP 2D** (Universal Render Pipeline, 2D Renderer)
Input: **New Input System** package (`UnityEngine.InputSystem`)
Version control: **Git** (migrated from Plastic SCM)

## Architecture

The game uses a pure-code scene setup: the only objects saved to the scene file are
`Main Camera`, `Global Light 2D`, and `GameManager`. All play-field geometry (walls,
death line, stoppers, weapons) and UI are created at runtime in `GameField.Awake()`.
Composite pinatas are instantiated at runtime by `SquareSpawner`.

**Economy system**: The player starts with $1000 and earns $1 per dead square that reaches
the death line. Money is spent on stoppers and weapons via the shop UI. Prices grow
exponentially using the formula `baseCost × 1.6^purchaseCount` with independent counters
per weapon type. The `Economy` singleton on GameManager tracks money, purchase counts,
and sell refunds, firing `OnMoneyChanged` events for UI. Weapons can be sold for their
previous purchase cost (no penalty).

**Composite pinatas** use a compound collider pattern: a parent GameObject holds a single
`Rigidbody2D` + `Pinata` component, with child GameObjects for each square (each has
`BoxCollider2D`, `SpriteRenderer`, `PinataSquare` — no `Rigidbody2D`). All children move
as one rigid body. When a square dies, it darkens, shrinks to 75% size, switches to the
`DeadSquare` layer, and gets its own `Rigidbody2D`.

**Weapons** mount on stoppers. The `Weapon` abstract base class defines `Init(Vector2, float)`,
`WeaponType Type`, and `string DisplayName`. The `WeaponType` enum (`None, Saw, Laser, Missile`)
identifies weapon types for the UI and economy. Three weapon types exist:
- **SawBlade**: orbits its stopper at the edge using `Rigidbody2D.MovePosition()` in
  `FixedUpdate`; damages on collision; emits sparks on hit
- **Laser**: satellite-dish shaped; locks onto the closest alive `PinataSquare` within
  range, fires a continuous red `LineRenderer` beam dealing 1 DPS; enters 3s cooldown
  (upgradeable to 0.1s) after a kill; sparkles at dish and hit point while firing; visually replaces the
  stopper sprite (hides it on attach, restores on sell)
- **Missile**: launcher tube/pod; acquires targets with lead prediction (solves quadratic
  intercept equation), fires slow AOE projectiles; missiles travel via transform (no
  Rigidbody2D/collider), detonate on proximity to alive squares; explosion damages all
  squares within blast radius; smoke/fire trail particles; visually replaces the stopper
  sprite (hides it on attach, restores on sell)

Weapons are purchased per-stopper via the `StopperMenu` popup (click a stopper to open).
Clicking a stopper with a weapon shows the weapon name and a sell button.

**Stoppers** are draggable (click-and-drag via `Draggable` component) and constrained to
the field bounds. Each stopper has a Kinematic `Rigidbody2D`. Weapons track their
stopper's `Transform` and follow automatically. The player starts with one stopper
(centered at 0, 1) and buys more via the HUD button. `Draggable` distinguishes click from drag using a `_hasDragged` flag
(threshold 0.35 world units), firing `OnClicked` for menu interaction only on tap.

**StopperFactory** is a plain C# class (not MonoBehaviour) that encapsulates stopper,
saw, laser, and missile creation/destruction. It is initialized by `GameField.Awake()` and used
by the shop UI to spawn new stoppers/weapons at runtime. `FindClearSpawnPos()` ensures
new stoppers don't overlap existing ones.

## Key Scripts (`Assets/Scripts/`)

| Script | Purpose |
|---|---|
| `GameField.cs` | Configures camera, creates walls (with visuals), death line, initial stopper; initializes Economy, StopperFactory, GlobalUpgrades, and UI; provides procedural sprite generators; walls rebuildable via `RebuildWalls()` |
| `SquareSpawner.cs` | Spawns composite pinatas as random connected shapes (center-biased growth algorithm); oscillates spawn position horizontally; grid size, interval, oscillation, and field width settable at runtime via public setters |
| `GlobalUpgrades.cs` | Singleton managing 4 global upgrade levels (wall size, pinata size, spawner rate, oscillation rate); applies effects to GameField, SquareSpawner, StopperFactory |
| `GlobalUpgradesUI.cs` | "Upgrades" button below Buy Stopper; full-screen overlay with 4 upgrade rows, cost labels, descriptions, and "SOLD OUT" state for maxed upgrades |
| `Pinata.cs` | Parent controller for composite pinatas; manages child square list and detachment |
| `PinataSquare.cs` | Individual square within a pinata; tracks health, takes damage, darkens/shrinks on death; exposes `IsDead` property; dead squares earn $1 and spawn confetti at death line |
| `Weapon.cs` | Abstract base class for weapon groups; defines `Init`, `Type`, `DisplayName`, `Upgrades`, `UpgradeSlotCount`, `GetSlotInfo`, `TryUpgrade`; `WeaponType` enum: None, Saw, Laser, Missile |
| `WeaponUpgradeData.cs` | Plain C# class tracking per-weapon upgrade levels and total investment for sell price |
| `UpgradeSlotInfo.cs` | Struct with upgrade slot UI info (name, description, icon, cost, level, maxLevel) |
| `SawGroup.cs` | Weapon group managing multiple `SawBlade` instances; 5 upgrade slots (blades, speed, size, torque, damage) |
| `LaserGroup.cs` | Weapon group managing multiple `Laser` instances; 5 upgrade slots (aim speed, range, damage, extra lasers, cooldown) |
| `SawBlade.cs` | Single saw blade instance (MonoBehaviour, not Weapon); orbits stopper, damages on contact; configurable via setters |
| `Laser.cs` | Single laser turret instance (MonoBehaviour, not Weapon); gradual rotation, angular targeting preference; configurable via setters |
| `MissileGroup.cs` | Weapon group managing multiple `MissileLauncher` instances; 6 upgrade slots (fire rate, damage, blast radius, speed, extra launchers, homing) |
| `MissileLauncher.cs` | Single launcher turret instance (MonoBehaviour, not Weapon); lead targeting via quadratic intercept; fires `Missile` projectiles |
| `Missile.cs` | Fire-and-forget AOE projectile; homing, proximity detonation, smoke trail, explosion VFX; self-destructs after 10s |
| `Stopper.cs` | Stopper component; tracks `Weapon` reference (polymorphic), opens `StopperMenu` on click |
| `Draggable.cs` | Click-and-drag via Input System; clamps to configurable bounds; fires `OnClicked` event for taps (tracks `_hasDragged` flag, threshold 0.35 units) |
| `Economy.cs` | Singleton tracking money ($10 start), independent purchase counts per weapon type, exponential pricing (`base × 1.6^n`), sell refunds = TotalInvestment; fires `OnMoneyChanged` event |
| `EconomyUI.cs` | Creates Canvas (Screen Space Overlay) + EventSystem; money label (top-center), buy-stopper button (top-left with icon and cost); uses `FindClearSpawnPos` for placement |
| `StopperMenu.cs` | Popup panel near clicked stopper; no weapon: shows buy-saw, buy-laser, and sell-stopper rows; has weapon: shows full-screen upgrade overlay with per-upgrade buttons + sell |
| `StopperFactory.cs` | Plain C# class (not MonoBehaviour); `SpawnStopper`, `AttachSaw` (creates SawGroup), `AttachLaser` (creates LaserGroup), `DetachWeapon`, `DestroyStopper`, `FindClearSpawnPos` |
| `ConfettiBurst.cs` | Spawns a one-shot `ParticleSystem` burst (15 particles), tinted to match the destroyed square |
| `DeathLine.cs` | Marker `MonoBehaviour` on the red death-line trigger collider |

## Scene Layout (runtime)

```
Main Camera           — orthographic, size 5 (set by GameField)
Global Light 2D       — ambient 2D lighting
GameManager           — GameField + SquareSpawner + Economy components
EconomyCanvas         — Canvas (Screen Space Overlay) + CanvasScaler + GraphicRaycaster
  MoneyLabel          — UI.Text, top-center, green, shows "$10"
  BuyStopperButton    — UI.Button + Image, top-left, stopper icon + cost label
  StopperMenu         — popup panel (hidden by default), buy-saw row or "Saw equipped"
EventSystem           — EventSystem + InputSystemUIInputModule (required for UI clicks)
WallLeft              — BoxCollider2D (Wall layer), off-screen
WallLeftVisual        — SpriteRenderer, 2px white line at field edge
WallRight             — BoxCollider2D (Wall layer), off-screen
WallRightVisual       — SpriteRenderer, 2px white line at field edge
WallBottom            — BoxCollider2D (Wall layer), off-screen
WallBottomVisual      — SpriteRenderer, 2px white line at screen bottom
RedLine               — SpriteRenderer (red) + BoxCollider2D (trigger) + DeathLine, just above bottom wall
Stopper (×1 start)    — CircleCollider2D + Kinematic Rigidbody2D + Draggable + Stopper, dark grey, centered at (0, 1)
SawBlade (×0 start)   — purchased per-stopper; CircleCollider2D + Dynamic Rigidbody2D, silver saw-tooth sprite (Weapon layer, sortingOrder 4)
Laser (×0 start)      — purchased per-stopper; satellite dish sprite, LineRenderer beam, dish/hit sparkle ParticleSystems (Weapon layer, sortingOrder 4)
MissileLauncher (×0)  — purchased per-stopper; tube/pod sprite (Weapon layer, sortingOrder 4); fires Missile projectiles
Missile(s)            — fire-and-forget projectiles (Weapon layer, sortingOrder 3); smoke trail ParticleSystem; explode on proximity
Pinata(s)             — spawned periodically, parent with Rigidbody2D + Pinata component
  Square (×25)        — SpriteRenderer + BoxCollider2D + PinataSquare (compound collider children)
ConfettiBurst(s)      — ParticleSystem, self-destructs after 3 s
```

## Physics

- **Engine**: Physics 2D (all colliders are 2D types)
- **Gravity scale on pinatas**: 0.4 (configurable on `SquareSpawner`)
- **Stoppers**: `CircleCollider2D` + Kinematic `Rigidbody2D` (draggable, block pinatas, no damage)
- **Saw blades**: Dynamic `Rigidbody2D`, mass 50, gravityScale 0; orbit driven by `MovePosition()` in `FixedUpdate`
- **Compound colliders**: Pinata children have no `Rigidbody2D`; they inherit physics from the parent
- **Spawn gating**: `Physics2D.OverlapBox` at spawn point prevents spawning on top of existing pinatas

### Physics Layers

| Layer | ID | Purpose |
|---|---|---|
| Weapon | 8 | Saw blades and future weapons |
| DeadSquare | 9 | Detached dead pinata squares |
| Wall | 10 | Side and bottom wall colliders |
| Stopper | 11 | Stopper circles (draggable blockers) |

**Collision ignores** (set in `GameField.Awake()`):
- Weapon ↔ DeadSquare: dead squares pass through weapons
- Weapon ↔ Wall: weapons pass through walls
- Weapon ↔ Weapon: weapons pass through each other (blades don't collide with other blades/lasers)
- Weapon ↔ Stopper: weapons pass through all stoppers (not just their own)

### Sorting Orders

| Object | Order |
|---|---|
| Pinata squares | 1 |
| Saw blades | 3 |
| Laser beam (LineRenderer) | 3 |
| Laser dish | 4 |
| Stoppers | 5 |
| Walls / Death line | 10 |

## Stopper Layout

The player starts with a single stopper centered at **(0, 1)** with no weapon.
Additional stoppers are purchased via the HUD button (top-left) and spawn near (0, 1),
offset to avoid overlapping existing stoppers (`StopperFactory.FindClearSpawnPos`).
The player drags them into position.

Radius is driven by `stopperRadius` on `GameField` (default **0.25** world units).
Each stopper has a Kinematic `Rigidbody2D`, `Draggable` (clamped to field bounds),
and `Stopper` component (tracks `Weapon` reference, opens shop menu on click).

## Weapon System

Weapons mount on stoppers. The `Weapon` abstract base class defines `Init(Vector2, float)`,
`WeaponType Type`, and `string DisplayName`. The `WeaponType` enum (`None, Saw, Laser, Missile`)
identifies weapon types for the UI and economy. Three weapon types exist:
- **SawBlade**: orbits its stopper at the edge using `Rigidbody2D.MovePosition()` in
  `FixedUpdate`; damages on collision; emits sparks on hit
- **Laser**: satellite-dish shaped; locks onto the closest alive `PinataSquare` within
  range, fires a continuous red `LineRenderer` beam dealing 1 DPS; enters 3s cooldown
  (upgradeable to 0.1s) after a kill; sparkles at dish and hit point while firing; visually replaces the
  stopper sprite (hides it on attach, restores on sell)
- **Missile**: launcher tube/pod; acquires targets with lead prediction (solves quadratic
  intercept equation), fires slow AOE projectiles; missiles travel via transform (no
  Rigidbody2D/collider), detonate on proximity to alive squares; explosion damages all
  squares within blast radius; smoke/fire trail particles; visually replaces the stopper
  sprite (hides it on attach, restores on sell)

Weapons are purchased per-stopper via the `StopperMenu` popup (click a stopper to open).
Clicking a stopper with a weapon shows the weapon name and a sell button.

**Stoppers** are draggable (click-and-drag via `Draggable` component) and constrained to
the field bounds. Each stopper has a Kinematic `Rigidbody2D`. Weapons track their
stopper's `Transform` and follow automatically. The player starts with one stopper
(centered at 0, 1) and buys more via the HUD button. `Draggable` distinguishes click from drag using a `_hasDragged` flag
(threshold 0.35 world units), firing `OnClicked` for menu interaction only on tap.

**StopperFactory** is a plain C# class (not MonoBehaviour) that encapsulates stopper,
saw, laser, and missile creation/destruction. It is initialized by `GameField.Awake()` and used
by the shop UI to spawn new stoppers/weapons at runtime. `FindClearSpawnPos()` ensures
new stoppers don't overlap existing ones.

### Weapon Groups

`SawGroup` and `LaserGroup` extend `Weapon` and manage lists of child instances.
Individual `SawBlade` and `Laser` are plain MonoBehaviours (not `Weapon` subclasses).
Each group owns a `WeaponUpgradeData` that tracks per-instance upgrade levels and total
money invested (base cost + all upgrade costs). Sell price = `TotalInvestment`.

### SawBlade (via SawGroup)

- Starts with 1 blade at reduced stats (speed 30, radius 0.10, mass 5, damage 1)
- **5 upgrade slots**: Extra Blades ($15, max 19→20 blades), Orbit Speed ($5, max 20,
  30→360 deg/s), Blade Size ($8, max 20, 0.10→0.25), Torque ($5, max 20, mass 5→100),
  Damage ($10, unlimited, 1+0.5×level)
- Adding blades: `Physics2D.IgnoreCollision` with stopper + all existing blades;
  angles redistributed equidistantly via `SetAngle(i * 360/count)`
- **Direction toggle**: free button in upgrade overlay; flips `_directionMultiplier`
  (1 or -1) which multiplies orbit speed, reversing CW↔CCW. Virtual methods on
  `Weapon` base: `HasDirectionToggle`, `IsClockwise`, `ToggleDirection()`

### Laser (via LaserGroup)

- Starts with 1 laser at reduced stats (aim speed 30 deg/s, range 1, DPS 1)
- **Gradual rotation**: uses `Quaternion.RotateTowards` instead of instant snap;
  only fires when within 15° of target (aim error threshold)
- **Angular targeting**: `AcquireTarget()` scores by distance + angular proximity
  to current aim direction, preferring targets it can reach faster
- **5 upgrade slots**: Aim Speed ($5, max 20, 30→360 deg/s), Range ($8, max 20,
  1→fieldWidth), Damage ($10, unlimited, 1+0.5×level), Extra Lasers ($20, max 19→20),
  Cooldown ($8, max 20, 3s→0.1s)
- Each laser targets independently with its own beam and particles

### Missile (via MissileGroup)

- Starts with 1 launcher at reduced stats (fire rate 5s, speed 1.5, damage 5, blast 0.4, no homing)
- **Lead targeting**: `MissileLauncher.ComputeLeadDirection()` solves quadratic intercept
  equation using target's `Rigidbody2D.linearVelocity` to predict where the pinata will be
- **Homing**: upgradeable; missiles curve toward target via `Vector2.Lerp(_direction, toTarget, strength * dt)`
- **AOE detonation**: contact trigger (AABB overlap with any alive square) or wall
  boundary hit; on detonation, damages all alive `PinataSquare`s within blast radius
  via `FindObjectsByType` + distance check
- **Projectile**: `Missile.cs` — fire-and-forget, no Rigidbody2D/collider, moves via
  `transform.position +=`, self-destructs after 10s or 20 units offscreen
- **VFX**: smoke/fire trail (ParticleSystem, detached on detonation to fade),
  orange/red explosion burst (25 particles, 2s self-destruct)
- **6 upgrade slots**: Fire Rate ($8, max 20, 5s→0.5s), Damage ($10, unlimited,
  5+2.5×level), Blast Radius ($8, max 20, 0.4→1.5), Missile Speed ($5, max 20,
  1.5→6 u/s), Extra Launchers ($25, max 19→20), Homing ($12, max 10, 0→5 rad/s)
- Each launcher targets and fires independently
- **Economy**: $60 base cost (premium tier), exponential pricing ($60→$96→$154...)

### Radial Upgrade Menu

When clicking a stopper with a weapon, StopperMenu displays a radial dial:
- Top: weapon name; Bottom: sell button with total investment refund
- N upgrade buttons arranged in a ring (radius 130px, each 85×85px)
- Each button shows: icon, name, level indicator, cost
- Colors: affordable=dark blue, can't afford=red, maxed=gold
- In editor: all upgrade buttons always interactable, free, and ignore max level caps
  (`Weapon.IsDebugMode` static property, `#if UNITY_EDITOR`)

**Important**: Private fields on runtime-created `MonoBehaviour`s are lost during domain
reload. Use `[SerializeField, HideInInspector]` for fields that must survive recompilation
in play mode, or use `GetComponent<>()` directly in callbacks instead of caching.

## Pinata System

Pinatas are composite random-shaped groups of squares that fall as one rigid body.
Shape is generated via a center-biased growth algorithm (`GenerateShape` / `WeightedPick`
in SquareSpawner): starts at (0,0), expands outward preferring cells closer to center,
guaranteed connected. Total square count = `gridWidth × gridHeight` from the upgrade level.

- **Spawning**: `SquareSpawner` oscillates horizontally above the screen; creates a parent
  `Pinata` GO with `Rigidbody2D`, then child squares in the generated shape
- **Spawn bounds**: Oscillation amplitude clamped to keep pinatas within field width
- **Square sprite**: Procedural 32×32 white texture with 1-pixel darker border (grey 0.6)
  for visible outlines between adjacent squares; pastel colors (HSV: S 0.35–0.55, V 0.95–1.0)
- **Health**: Each square has independent health (default 1 HP, will become variable)
- **Death**: When health ≤ 0, the square darkens (color *= 0.5), shrinks to 75% size,
  switches to `DeadSquare` layer, and detaches from the parent
- **Split on disconnect**: After each square death, `Pinata` runs a BFS flood-fill on the
  remaining squares using 4-connected grid adjacency (up/down/left/right). If the squares
  form multiple disconnected groups, the largest group stays on the original `Pinata` and
  each smaller group is split into a new `Pinata` with its own `Rigidbody2D`, inheriting
  velocity (via `GetPointVelocity` at the group centroid), angular velocity, and gravity.
  Each `PinataSquare` stores its grid coordinates (`GridCol`, `GridRow`) for adjacency lookup.
- **Confetti**: Only **dead** squares spawn confetti when hitting the death line; alive
  squares are silently destroyed
- **Cleanup**: When all squares detach, the empty parent `Pinata` destroys itself

## Global Upgrades System

`GlobalUpgrades` singleton on GameManager manages 6 upgrade types, each with independent
level counters and exponential pricing (`$10 × 1.6^level`). Money is deducted via
`Economy.Instance.Earn(-cost)`. Upgrade effects are applied immediately and affect
subsequent gameplay.

### Starting State (Level 0 for all upgrades)

| Parameter | Starting Value | Was |
|---|---|---|
| Field width | 1.5 units (3× stopper diameter) | 6 |
| Pinata grid | 1×1 (single square) | 5×5 |
| Spawn interval | 10 seconds | 1 |
| Oscillation period | 30 seconds | ~6.28s |
| Square health | 1 HP | 1 |
| Death line damage | 1 | N/A (was instant destroy) |

### Upgrade Details

| Upgrade | Formula | Range | Max Level |
|---|---|---|---|
| Wall Size | `1.5 + level × 0.5`, clamped at screen width − 1.5 | 1.5 → screen edge | When screen fills |
| Pinata Size | `n = level/2 + 1; w = even ? n : n+1; h = n` | 1×1 → 2×1 → 2×2 → ... | Unlimited |
| Spawner Rate | `10 × (0.1/10)^(level/20)` | 10s → 0.1s | 20 |
| Oscillation | `period = 30 × (1/30)^(level/20)` | 30s → 1s | 20 |
| Pinata HP | `1 + 0.3 × level^1.5` (accelerating) | 1 → ~28 at Lv 20 | Unlimited |
| Death Line Dmg | `1 + level × 0.5` (linear) | 1 → 11 at Lv 20 | Unlimited |

### Health & Death Line Economy
- **Weapon kills**: Dead squares earn `max(1, round(maxHealth × 2))` dollars at the death line
- **Death line kills**: Live squares take death line damage; if killed there, earn only **$1**
  regardless of HP. This makes the death line a safety net, not a money farm.
- `PinataSquare` tracks `_maxHealth` (set at spawn) for money calculation.

### Apply Chain (Wall Size)
1. `GameField.Instance.RebuildWalls(width)` — destroys/recreates walls + death line
2. `StopperFactory.Instance.UpdateFieldWidth(width)` — re-bounds all existing stoppers
3. `spawner.SetFieldWidth(width)` — updates oscillation bounds

### UI
- **Trigger button**: Top-left, below Buy Stopper (140×140, same style)
- **Overlay**: Semi-transparent full-screen backdrop + centered 700×1050 panel, 6 rows
- **Rows**: Icon (tinted) + name + description + buy button (or "SOLD OUT" when maxed)
- Game continues running underneath the overlay

## Tunable Inspector Fields

**GameField:**
- `fieldWidth` (6) — play area width in world units
- `cameraHalfHeight` (5) — orthographic camera size
- `stopperRadius` (0.25) — world radius of each stopper circle

**SquareSpawner** (serialized defaults overridden by GlobalUpgrades at runtime):
- `spawnInterval` (1) — seconds between drops (overridden to 10 at start)
- `squareSize` (0.175) — world-unit side length of each square
- `spawnY` (7) — Y position of the spawn point (above camera top at Y=5)
- `gravityScale` (0.4) — Rigidbody2D gravity multiplier
- `maxHorizSpeed` (0.5) — max initial horizontal velocity (m/s)
- `maxAngularSpeed` (60) — max initial spin rate (deg/s)
- `maxSpawnAngle` (35) — max initial rotation offset (degrees)
- `gridWidth` (5) — overridden to 1 at start by GlobalUpgrades
- `gridHeight` (5) — overridden to 1 at start by GlobalUpgrades
- `squareHealth` (1) — HP per individual square (will become variable)
- `fieldWidth` (6) — overridden to 1.5 at start by GlobalUpgrades
- `oscillateSpeed` (1) — overridden to ~0.209 at start by GlobalUpgrades

**SawBlade** (defaults set by SawGroup upgrades):
- `orbitSpeed` — starts 30, upgradeable to 360 deg/s
- `bladeRadius` — starts 0.10, upgradeable to 0.25
- `bladeMass` — starts 5, upgradeable to 100
- `damage` — starts 1, upgradeable via `1 + level × 0.5`
- `selfSpinSpeed` (360) — visual spin speed in degrees per second

**Laser** (defaults set by LaserGroup upgrades):
- `damagePerSecond` — starts 1, upgradeable via `1 + level × 0.5`
- `cooldownDuration` (3) — seconds of downtime after killing a target (upgradeable to 0.1s)
- `maxRange` — starts 1, upgradeable to full field width over 20 levels
- `rotationSpeed` — starts 30 deg/s, upgradeable to 360 deg/s

## Confetti System

`ConfettiBurst` creates a fully procedural `ParticleSystem` at runtime:

- **Color**: `main.startColor = squareColor` (matches the destroyed square exactly)
- **Fade**: `colorOverLifetime` uses white color keys + alpha 1→0 — multiplies with
  `startColor` without shifting the hue (white × color = color)
- **Material**: must explicitly use `Universal Render Pipeline/Particles/Unlit` —
  the built-in "Default-Particle" material is **not URP-compatible** and renders as
  magenta. Always set `psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))`.
- 15 particles per burst, sphere emitter radius 0.3, self-destructs after 3 s
- Only triggered by **weapon-killed** dead squares hitting the death line (not death-line kills)

## Economy System

`Economy` is a singleton on GameManager that tracks player money and purchase history.

- **Starting money**: $10
- **Income**: `max(1, round(maxHealth × 2))` per weapon-killed dead square reaching the
  death line; $1 for death-line-killed squares (`PinataSquare.OnTriggerEnter2D`)
- **Pricing formula**: `baseCost × 1.6^purchaseCount` (rounded to int)
- **Saw base cost**: $8 → $13 → $20 → $33 → $52 ... (independent counter)
- **Laser base cost**: $40 → $64 → $102 → $164 ... (independent counter)
- **Missile base cost**: $60 → $96 → $154 → $246 ... (independent counter)
- **Stopper base cost**: $20 → $32 → $51 → $82 → $131 ...
- **Events**: `OnMoneyChanged(int)` fires on earn/spend/sell; UI subscribes to update labels
- **Buy methods**: `TryBuySaw()` / `TryBuyLaser()` / `TryBuyMissile()` / `TryBuyStopper()` return bool, deduct money, increment count
- **Sell methods**: `SellWeapon(Weapon)` refunds `weapon.Upgrades.TotalInvestment` (base
  purchase cost + all upgrade costs) and decrements the per-type purchase counter;
  `TrySellStopper()` refunds previous stopper cost and decrements `_stoppersPurchased`
- **Stopper selling**: Only available when stopper has no weapon and more than one stopper
  exists. Sell price = `Cost(base, purchaseCount - 1)`. Cannot sell the free starter stopper.

## UI System

All UI is built procedurally in `EconomyUI.Awake()` — no prefabs or editor-placed Canvas.

- **Canvas**: Screen Space Overlay, sortingOrder 100, CanvasScaler (Scale With Screen Size,
  1080×1920 reference, matchWidthOrHeight 0.5)
- **EventSystem**: Created by `EconomyUI` if none exists; uses `InputSystemUIInputModule`
  (required for New Input System compatibility)
- **Money label**: Top-center, green, bold, font size 52, with drop shadow (Outline component)
- **Buy Stopper button**: Top-left (20px inset), 140×140, dark panel with stopper circle
  icon + cost label. Grays out and becomes non-interactable when unaffordable.
- **StopperMenu**: Two modes: (1) no weapon — small popup panel near stopper with
  buy-saw, buy-laser, and sell-stopper rows; (2) has weapon — full-screen upgrade overlay
  (matching GlobalUpgrades style) with weapon name, per-upgrade rows (icon + name +
  description + buy button or SOLD OUT), and sell button showing total investment refund.
  Buy panel hides on click outside; upgrade overlay hides via close button (X).
- **Font**: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` (Unity built-in)

## Coding Standards

- **Unity 6 / C# 10+**: use `linearVelocity` (not deprecated `velocity`) on `Rigidbody2D`
- **Input**: Use `UnityEngine.InputSystem` (new Input System), not `UnityEngine.Input`
- All MonoBehaviours use private fields with `[SerializeField]` — no public fields
- Cache component references in `Awake`, not in per-frame callbacks
- **Exception**: Runtime-created objects may need `GetComponent<>()` in callbacks or
  `[SerializeField, HideInInspector]` to survive domain reload during play mode
- No namespaces (single-developer project, kept flat for simplicity)
- Follow URP 2D conventions: `SpriteRenderer` for 2D visuals, `Light2D` for lighting
- Run In Background is enabled (Player Settings) so the game runs without editor focus
- **[SerializeField] gotcha**: changing default values in code does NOT update already-
  serialized component values on scene objects. Use `manage_components set_property`
  (or the Inspector) to update live component values after changing field defaults.

## Git Workflow

Standard git workflow. Always save the scene (`manage_scene save` or Ctrl+S) before
committing. The `.gitignore` excludes Library/, Temp/, build artifacts, IDE files, and
the old `.plastic/` directory.

## MCP / Unity Tool Notes

- `create_script` tool may report false-positive validation errors (e.g. "Duplicate
  method signature"). Workaround: write the file directly with the Write tool, then
  call `refresh_unity`.
- After any script change, call `read_console` to confirm compilation succeeded before
  attempting to use new types or components.
- Adding physics layers via MCP `manage_editor add_layer` may not persist. Use
  `SerializedObject` on `TagManager.asset` via `execute_code` instead.
