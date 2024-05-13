# The Forest 💀🌴
> *A collection of mods I've created for The Forest.*

<p align="justify">Having recently returned to The Forest after all these years, I found certain aspects of the game to be rather bothersome, even though I understand they are intentional design choices by the developers. As a developer myself, I wanted to play the game as I envisioned it after completing it without mods. The particular aspects that bothered me were: Water Blur, Flashlight, and Lighter.</p>

**About Water Blur:** <p align="justify">I understand that it's realistic for my vision to become almost blinded by particles and such when underwater, resulting in a blurry mess. Do I like it? No. Do I understand it? Yes. Therefore, I've removed the underwater blur to make the underwater environment clearer.</p>

**About Flashlight:** <p align="justify">In my initial playthrough (co-op), I hardly used the flashlight. Why? Because the game was too dark, necessitating the use of the built-in color grading feature to brighten things up. However, caves and nights were still too dark (which is realistic and I appreciate), but the flashlight barely emitted more light than the default lighter and drained battery incredibly quickly — approximately 10 minutes according to the wiki ("The flashlight emits a bright source of light when turned on, and it lasts for about 10 minutes."). Therefore, I've increased the radius and range of the flashlight's illumination, as well as extended its battery life to approximately 60 minutes.</p>

**About Lighter:** <p align="justify">This one was a real pain in the ass to tweak. I struggled to figure out how to mod its values; it was quite confusing. After spending countless hours debugging and testing, I couldn't find a solution. So, if adjusting the range of the lighter is a common annoyance for you as well, any help with modding it would be greatly appreciated.</p>


****

## Softwares 🛠️
> *IDK if can I write down the link to all tools, cuz some are in archive repository...*
- *Code Editor:* VS Code ([Download](https://code.visualstudio.com/))
- *Language:* C# ([Learn more](https://learn.microsoft.com/en-us/dotnet/csharp/))
- *Debugger and .NET assembly editor:* dnSpy

## How to install 📑
you **DONT** need a modmanager, just donwload the mod that you want and put in:

```diff
+  The Forest\TheForest_Data\Managed\ **Assembly-CSharp.dll**
```

## IMPORTANT!
I'll merge the mods to create a comprehensive version with all the changes. Also, if you know how to modify the range of the lighter's flame (its duration and the radius it illuminates), please contribute to it.
****

## How I made
<p align="justify">As The Forest is a Unity game, you'll need to modify the file present in the Managed folder (all Unity games have one). Sometimes it's an Assembly-CSharp.dll; other times, for more complex games, it may be different. Once you've identified the file, you'll need a debugger and editor. I use dnSpy. Navigate through the files, for example, for the water blur, the file that contains all the functions and methods is UnderWaterPostEffect.cs.</p>

### Remove blur
<p align="justify">To remove the blur, simply follow the comments I've added in the code. I removed the blur_object from the Start() function and all lines referring to blur in the OnRenderImage() function. You might wonder, "Why does the water still appear blurred after removing the blur?" Well, in fact, the blur doesn't affect the water much; it's the color filter applied over it that causes the clarity issue due to its whitish tint. I couldn't reduce the opacity of the water's tint, but it's better than it being blurrier, as you can see in the image below.</p>

**Default Underwater**
![DefaultWater](https://github.com/Ishidawg/TheForest-Modding/blob/main/images/underwater-blur.png?raw=true)

**Modded Underwater**
![ModdedWater](https://github.com/Ishidawg/TheForest-Modding/blob/main/images/underwater-clear.png?raw=true)

<p align="justify">So, I managed to get rid of the underwater blur, but that annoying white tint is still there. I tried toning it down, but removing it altogether made things look pretty awful tbh. It's not perfect, but it's definitely better than before IMO.</p>

### Better flashlight (IMO)
<p align="justify">By the way, just like I did with the underwater post process, if you take a peek at the code, you'll notice comments to help you navigate through the modifications.</p>
<p align="justify">So, I started by digging into the .cs file responsible for the flashlight functions and attributes, named "BatteryBasedLight". The game's code had me scratching my head a bit when it came to modding. I ended up removing all values from constructors at the bottom of the code:</p>

![Code-contructor](https://github.com/Ishidawg/TheForest-Modding/blob/main/images/flashlight-constructors-values.png?raw=true)

<p align="justify">Then, go on Awake() function, that is the main function of lantern and set the values directly, I added this code:</p>

![Code-main-function](https://github.com/Ishidawg/TheForest-Modding/blob/main/images/awake-function.png?raw=true)

**Default Flashlight**
![DefautFlashlight](https://github.com/Ishidawg/TheForest-Modding/blob/main/images/Flashlight-shitty-one.png?raw=true)

**Modded Flashlight**
![DefautFlashlight](https://github.com/Ishidawg/TheForest-Modding/blob/main/images/Flashlight-good-one.png?raw=true)

****
Yeep, that's it, contribute to modding, write code and use tools. Be happy ❤️



