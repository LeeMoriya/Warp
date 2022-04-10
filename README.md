# Rain World Warp Menu ![GitHub all releases](https://img.shields.io/github/downloads/LeeMoriya/Warp/total?style=for-the-badge)

Warp is a utility mod for Rain World that allows you to teleport the player to any room in the game, most common uses are to help with the creation of Custom Regions or testing in-development mods.

The Warp menu is accessed via the Pause screen. Rooms in your current region are displayed on the right while a list of vanilla and modded regions are displayed on the left. Clicking on a room button will warp the player to that room. Clicking on a region button will change the list of room buttons to that of the selected region so that you can warp to rooms in different regions without the use of gates.

![Warp](https://i.imgur.com/sse46qV.png)

Warp supports three sorting options for rooms:
- Type: Room, Shelter, Gate, Scavenger Trader, etc.
- Size: The number of screens a room has.
- Subregion: Which subregion the room belongs to.

Room buttons can be colored independently of the current sorting option, for example you can sort rooms by their subregion, but color them by their type.

Warp also features a color customiser so that you can adjust the colors used for showing room types and sizes as well as the colors of different subregions for each region. These colors are saved in your Rain World\UserData\Warp folder.

![Colors](https://i.imgur.com/BxFdGyq.png)

### MapWarp
Warp v1.6 and up features MapWarp, created by [Henpemaz](https://github.com/henpemaz). This allows you to warp to rooms at the exact coordinates you click on via the Dev Tools Map tab.

### DevConsole Support
Warp v1.65 and up features support for Slime_Cubed's [DevConsole](https://github.com/SlimeCubed/DevConsole/releases) mod, allowing you to use the following commands:
- `warp help` - Show the following commands and their usage:
- `warp all` - Parses all installed regions for room names and adds them to auto-complete.
- `warp XX` - Where XX is a region acronym, adds that region's rooms to auto-complete.
- `warp` - Followed by the name of the room you wish to warp to (case-sensitive!).

## Requirements:
Extended DevTools is not required but is recommended [Link](https://drive.google.com/file/d/1X9EQbZ__lla36YLKYijvwsshyEwy7QA7/view)

## Download:

Get the latest release [Here](https://github.com/LeeMoriya/Warp/releases/tag/v1.7)

This mod also supports AutoUpdate.

## Special Thanks:
Slime_Cubed - For contributing the initial seamless region switching code.

Henpemaz - For creating MapWarp
