# Pinata Grinder

A physics-based arcade/idle game where colorful pinata blocks fall from the sky and you must destroy them before they reach the bottom. Drag stoppers into position, equip them with weapons, and upgrade your arsenal to keep up with increasingly tough waves.

**[Play Now](https://mattaltermatt.github.io/Pinata-Grinder/)** (WebGL — runs in your browser)

## How to Play

### The Basics

Colored pinata shapes fall from above and land on the terrain. Your job is to destroy them before they pile up and reach the death line (the red line near the bottom of the screen). You start with **$15** and one **stopper** — a draggable grey circle that blocks falling pinatas.

### Controls

- **Drag** a stopper to reposition it anywhere on the field
- **Tap** a stopper to open its menu (buy or upgrade weapons, sell)
- All input works with mouse or touch

### Stoppers & Weapons

Tap a stopper to open its menu. From there you can buy one of three weapon types:

| Weapon | Cost | Description |
|---|---|---|
| **Saw Blade** | $8+ | Spinning blades that orbit the stopper and slice through pinatas on contact |
| **Laser** | $40+ | A turret that locks onto the nearest pinata square and fires a continuous beam |
| **Missile** | $60+ | A launcher that fires homing AOE projectiles with lead targeting |

Each stopper holds one weapon. Buy more stoppers (top-left button) to field more weapons. Prices increase with each purchase.

### Upgrades

**Weapon upgrades** — Tap a stopper that already has a weapon to open the upgrade menu. Each weapon has 4-5 upgrade slots (blades, damage, range, fire rate, etc.). Upgrades cost money and have individual max levels.

**Global upgrades** — The "Upgrades" button (top-left, below Buy Stopper) opens a panel with 6 upgrades that affect the entire game:

| Upgrade | Effect |
|---|---|
| Wall Size | Widens the playing field |
| Pinata Size | Increases the number of squares per pinata |
| Spawner Rate | Pinatas spawn faster |
| Oscillation | Spawn point moves faster across the field |
| Pinata HP | Pinatas have more health |
| Death Line Damage | The death line deals more damage per second |

Global upgrades make the game harder but also more rewarding — tougher pinatas are worth more money.

### Pinata Variants

Not all pinatas are created equal. As you progress, you'll encounter different types:

| Variant | Appearance | Behavior |
|---|---|---|
| **Basic** | Random pastel colors | Standard pinata, no special properties |
| **Armored** | Grey, metallic | Resistant to saw blade damage (physical) |
| **Shielded** | Blue glow | Energy shield absorbs laser damage until broken |
| **Swift** | Yellow-green, streaky | Falls faster, harder to hit with missiles |
| **Heavy** | Dark purple | High mass, hard to push, but slow-moving |

Tougher variants are worth more money when destroyed. Diversifying your weapon types helps deal with all variants effectively.

### Economy

- **Weapon kills** earn `max(1, round(maxHealth x 2))` dollars per dead square that reaches the death line (with confetti!)
- **Death line kills** earn only **$1** per square (no confetti) — the death line is a safety net, not a money farm
- Weapons can be **sold** for their full investment (base cost + all upgrade costs)
- Stoppers without a weapon can also be sold (you must keep at least one)

### Saving

The game **auto-saves every 5 minutes**. You can also manually save or restart from the Options menu (gear icon, bottom-left).

## Tech Stack

- **Engine**: Unity 6 (6000.4.1f1)
- **Render Pipeline**: URP 2D
- **Language**: C# 10+
- **Input**: Unity New Input System
- **Platforms**: WebGL, Android, iOS

## Building

1. Open the project in **Unity 6** (version 6000.4.1f1 or compatible)
2. Open the main scene
3. **File > Build Settings > WebGL > Build** — set output to `Build/WebGL`

### Deploying to GitHub Pages

After building for WebGL:

```bash
./deploy.sh
```

This pushes the `Build/WebGL` output to the `gh-pages` branch. The game will be live at the Play Now link above within a couple of minutes.

## License

[MIT](LICENSE) — Copyright (c) 2026 Matthew Altermatt
