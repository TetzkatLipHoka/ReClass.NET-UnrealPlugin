ReClass.NET Unreal Plugin
=================================

A ReClass.NET plugin which displays type infos of Unreal Engine classes.

## SUPPORTED GAMES
- AtomicHeart
- Dishonored
- Fortnite
- Playerunknown's Battlegrounds™
- Sea of Thieves
- To get it work with other unreal games add a case with the processname of your game 
  and a signature for the GNames Array to the Code.

## Compiling
If you want to compile the ReClass.NET Plugins just fork the repository and create the following folder structure. If you don't use this structure you need to fix the project references.

```
..\ReClass.NET\
..\ReClass.NET\ReClass.NET\ReClass.NET.csproj
..\ReClass.NET-UnrealPlugin
..\ReClass.NET-UnrealPlugin\UnrealEnginePlugin.sln
```