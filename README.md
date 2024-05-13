# The Forest 💀🌴
> *A collection of mods I've created for The Forest.*

<p align="justify">Having recently returned to The Forest after all these years, I found certain aspects of the game to be rather bothersome, even though I understand they are intentional design choices by the developers. As a developer myself, I wanted to play the game as I envisioned it after completing it without mods. The particular aspects that bothered me were: Water Blur, Flashlight, and Lighter.</p>

<p align="justify">**About Water Blur:** I understand that it's realistic for my vision to become almost blinded by particles and such when underwater, resulting in a blurry mess. Do I like it? No. Do I understand it? Yes. Therefore, I've removed the underwater blur to make the underwater environment clearer.</p>

<p align="justify">**About Flashlight:** In my initial playthrough (co-op), I hardly used the flashlight. Why? Because the game was too dark, necessitating the use of the built-in color grading feature to brighten things up. However, caves and nights were still too dark (which is realistic and I appreciate), but the flashlight barely emitted more light than the default lighter and drained battery incredibly quickly — approximately 10 minutes according to the wiki ("The flashlight emits a bright source of light when turned on, and it lasts for about 10 minutes."). Therefore, I've increased the radius and range of the flashlight's illumination, as well as extended its battery life to approximately 60 minutes.</p>

<p align="justify">**About Lighter:** This one was a real pain in the ass to tweak. I struggled to figure out how to mod its values; it was quite confusing. After spending countless hours debugging and testing, I couldn't find a solution. So, if adjusting the range of the lighter is a common annoyance for you as well, any help with modding it would be greatly appreciated.</p>


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

<p align="justify">To remove the blur, simply follow the comments I've added in the code. I removed the blur_object from the Start() function and all lines referring to blur in the OnRenderImage() function. You might wonder, "Why does the water still appear blurred after removing the blur?" Well, in fact, the blur doesn't affect the water much; it's the color filter applied over it that causes the clarity issue due to its whitish tint. I couldn't reduce the opacity of the water's tint, but it's better than it being blurrier, as you can see in the image below.</p>

**Default Underwater**
![DefaultWater](https://cdn.discordapp.com/attachments/524370625167491073/1237985600087523339/TheForest_2024-05-09_01-32-32.png?ex=6642e96f&is=664197ef&hm=2c8f94a780b852e6fc4b699c724906a9dde268cbd79001fa608f538fedd6e6a8&)

**Modded Underwater**
![ModdedWater](https://cdn.discordapp.com/attachments/524370625167491073/1237987002150944809/TheForest_2024-05-09_01-37-09.png?ex=6642eabd&is=6641993d&hm=f9cb23fe5c6e4e658207e23ce0c11ce81faea24fdb7e24d7cf1efe44241ba560&)

DONT LOOK AT IT ;-;
