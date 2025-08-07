# The Forest 💀🌴
> *A collection of mods I've created for The Forest.*

This is a new README. It contains more information about how everything works and is more straightforward. If you want to check out the old README, just click it.

## Content
- [Mods](#mods)
- [Installation](#installation)
- [Comparison](#comparison)
- [How to merge yourself](#how-to-merge-yourself)

## Mods

I've created four mods so far. They are intended to modify the behavior of the flashlight, torch, water, and inventory (without making the game easier). These are just small tweaks to maintain the vanilla difficulty. Below are the changes for each:

### Flashlight
- Increased the **intensity** (*or brightness, if you prefer*) of the light by approximately `222%`. This was done by *multiplying* the original intensity (`0.45f`) by `2f`, resulting in `1.0f`, and then to `1.45f`.
- Increased the **range** (*how far the light goes*) by around `20%`, by multiplying the final range by `1.2f`.
- Doubled the **battery lifespan** by cutting the usage cost in *half* (from `0.1f`), resulting in *20 minutes of light*.

### Torch
- Doubled the **intensity** (*or brightness*) of the light.
- Doubled the **lifespan** of the fire by halving the fuel consumption.

### Inventory
- **Hard-capped** the limit of all countable items to `1000`.
- Tweaked the `BowController` to actually *see* that the inventory can **handle more** than 30 arrows.

### Water
- **Removed** the water blur post-effect.

If you want all mods in *one package*, download the `Merged` version. Otherwise, choose the individual ones you want: `flashlight`, `torch`, `inventory`, or `water`.

## Installation

### Windows:
Go to your **game’s root folder**, then open the correct folder depending on the *version* you're playing.
**For example:**
- 64-bit (x64): `Steam\steamapps\common\The Forest\TheForest_Data\Managed`.
- 32-bit (x86): `Steam\steamapps\common\The Forest\TheForest_Data\Managed`.
- VR version: `C:\Program Files (x86)\Steam\steamapps\common\The Forest\TheForestVR_Data\Managed`.

*Make a backup of the original DLL so that if you want to uninstall the mod, you can restore it easily.*

**Copy** the DLL that you have downloaded (`Assembly-CSharp.dll`) to the `Managed` folder and **replace-it**.

Done! You folder should look like this:

<div align="center">
    <img alt="Windows Folder" src="https://i.imgur.com/DGdEPcH.png" width="800" />
</div>

### Linux
Same process as Windows. The final path should look like: `/home/yourusername/.local/share/Steam/steamapps/common/The Forest/TheForest_Data/Managed/`

*Make a backup of the original DLL so that if you want to uninstall the mod, you can restore it easily.*

**Copy** the DLL that you have downloaded (`Assembly-CSharp.dll`) to the `Managed` folder and **replace-it**.

Done! You folder should look like this:

<div align="center">
    <img alt="Windows Folder" src="https://i.imgur.com/dT2WxF1.jpeg" width="800" />
</div>

<!-- ![Windows Folder](https://i.imgur.com/DGdEPcH.png) -->
<!-- ![Linux Folder](https://i.imgur.com/dT2WxF1.jpeg) -->

## Comparison
If you're **unsure** about what *visually* has actually **changed**, take a look at the comparison section:

<h6 align="center" style="background: #d3d3d3; padding: 4px;">Flashlight<h6>
<div align="center">
    <img alt="Flashlight-vanilla" src="https://i.imgur.com/8dF3Mu9.png" width="800" />
    <img alt="Flashlight-modded" src="https://i.imgur.com/ius5Ohs.png" width="800" />
</div>

<h6 align="center" style="background: #d3d3d3; padding: 4px;">Torch<h6>
<div align="center"">
    <img alt="Torch-vanilla" src="https://i.imgur.com/dyam6L3.png" width="800" />
    <img alt="Torch-modded" src="https://i.imgur.com/lzkn9ii.png" width="800" />
</div>

<h6 align="center" style="background: #d3d3d3; padding: 4px;">Inventory<h6>
<div align="center">
    <img alt="Inventory-vanilla" src="https://i.imgur.com/8XEPydI.jpeg" width="800" />
    <img alt="Inventory-modded" src="https://i.imgur.com/1lh5YE7.jpeg" width="800" />
</div>

<h6 align="center" style="background: #d3d3d3; padding: 4px;">Water<h6>
<div align="center">
    <img alt="Water-vanilla" src="https://i.imgur.com/78lPdiN.png" width="800" />
    <img alt="Water-modded" src="https://i.imgur.com/33wdJrl.png" width="800" />
</div>

## How to merge yourself

To be honest, the only method I found to work is the *old-fashioned cowboy way*: **copy and paste**. I initially tried using the **Merge with Assembly** in dnSpy, but I can't get it to work. So my tip is *real simple*, just more *time demanding* (about 5 minutes): **Copy & Paste**.

You will need the [dnSpy]("https://github.com/dnSpyEx/dnSpy") to edit game assemblie. Just download it, then open the `Assembly-CSharp.dll`. Now is just copy and paste. 

Look at the mod that you want, let's say you want to merge the `flashlight` mod.

Go to the [`BatteryBasedLight.cs`](https://github.com/Ishidawg/TheForest-Modding/blob/main/001_flashlight/003Intensity_Range_BatteryCost/BatteryBasedLight.cs) and copy the changes (battery cost, intensity and range). Then in `dnSpy`, open your `Assembly-CSharp.dll` → `TheForest.items.World` → `BatteryBasedLight`, now with the *mouse right button* click on `Edit Class (C#)...` and just do the changes you want, like this:

<div align="center">
    <img alt="Torch-vanilla" src="https://i.imgur.com/P8E3ZnP.png" width="900" />
</div>
<div align="center">
    <img alt="Torch-vanilla" src="https://i.imgur.com/jEZbOpm.png" width="900" />
</div>

When you are done with editing, click on `File` → `Save All` and then `Ok`. That's it! You've made your **own mod** by merging what you want into your game.
****
Yeep, that's it, contribute to modding, write code and use tools. Be happy ❤️
