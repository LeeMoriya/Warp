using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DevConsole;
using DevConsole.Commands;
using Partiality;
using Partiality.Modloader;
using UnityEngine;

public static class WarpConsole
{
    public static List<string> warpAutoComplete = new List<string>
    {
        "all"
    };
    public static void RegisterCommands()
    {
        new CommandBuilder("warp").RunGame(delegate (global::RainWorldGame game, string[] args)
        {
            CompatibilityCheck();
            WarpMod.CheckForMSC();
            //Case-sensitivity
            if (args.Length == 0)
            {
                GameConsole.WriteLine("No room name given!");
            }
            if (args.Length >= 1)
            {
                string room = args[0];
                string region = "";
                //Auto Complete Load
                if (args[0].Length == 2)
                {
                    bool regionExists = false;
                    for (int i = 0; i < game.overWorld.regions.Length; i++)
                    {
                        if (game.overWorld.regions[i].name == args[0])
                        {
                            regionExists = true;
                        }
                    }
                    if (regionExists)
                    {
                        //Load room names for region
                        new RoomFinder().Generate(args[0], WarpMod.customRegions);
                        int rooms = WarpModMenu.masterRoomList[args[0]].Count;
                        for (int i = 0; i < WarpModMenu.masterRoomList[args[0]].Count; i++)
                        {
                            if (!warpAutoComplete.Contains(WarpModMenu.masterRoomList[args[0]][i].name))
                            {
                                warpAutoComplete.Add(WarpModMenu.masterRoomList[args[0]][i].name);
                            }
                        }
                        GameConsole.WriteLine(rooms.ToString() + " rooms from " + args[0] + " added to auto-complete");
                    }
                    else
                    {
                        GameConsole.WriteLine("The region " + args[0] + " was not found.");
                    }
                }
                else
                {
                    if(args[0] == "help")
                    {
                        GameConsole.WriteLine("warp all - Load all room names from every installed region and add them to auto-complete", new Color(0.7f, 0.7f, 0.7f));
                        GameConsole.WriteLine("warp XX - Load all room names from the specified region and add them to auto-complete", new Color(0.7f,0.7f,0.7f));
                        GameConsole.WriteLine("warp XX_A01 - Warp the player to the specified room", new Color(0.7f, 0.7f, 0.7f));
                    }
                    if(args[0] == "all")
                    {
                        for (int i = 0; i < game.overWorld.regions.Length; i++)
                        {
                            if (!WarpModMenu.masterRoomList.ContainsKey(game.overWorld.regions[i].name))
                            {
                                new RoomFinder().Generate(game.overWorld.regions[i].name, WarpMod.customRegions);
                                int rooms = WarpModMenu.masterRoomList[game.overWorld.regions[i].name].Count;
                                GameConsole.WriteLine(rooms.ToString() + " rooms from " + game.overWorld.regions[i].name + " added to auto-complete");
                                for (int b = 0; b < WarpModMenu.masterRoomList[game.overWorld.regions[i].name].Count; b++)
                                {
                                    if (!warpAutoComplete.Contains(WarpModMenu.masterRoomList[game.overWorld.regions[i].name][b].name))
                                    {
                                        warpAutoComplete.Add(WarpModMenu.masterRoomList[game.overWorld.regions[i].name][b].name);
                                    }
                                }
                            }
                        }
                        GameConsole.WriteLine("Done!");
                        return;
                    }
                    //Determine if the room is a gate
                    if (room.StartsWith("GATE"))
                    {
                        region = Regex.Split(room, "_")[1];
                    }
                    else
                    {
                        region = Regex.Split(room, "_")[0];
                    }
                    //Warp to room
                    new RoomFinder().Generate(region, WarpMod.customRegions);
                    if (WarpModMenu.masterRoomList.ContainsKey(region))
                    {
                        bool exists = false;
                        for (int i = 0; i < WarpModMenu.masterRoomList[region].Count; i++)
                        {
                            if (!warpAutoComplete.Contains(WarpModMenu.masterRoomList[region][i].name))
                            {
                                warpAutoComplete.Add(WarpModMenu.masterRoomList[region][i].name);
                            }
                            if (WarpModMenu.masterRoomList[region][i].name == room)
                            {
                                WarpModMenu.newRegion = region;
                                WarpModMenu.newRoom = room;
                                WarpModMenu.warpActive = true;
                                GameConsole.WriteLine("Warping to " + args[0]);
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            GameConsole.WriteLine("The room " + room + " wasn't found, names are case-sensitive, ensure you've typed it correctly or use auto complete");
                        }
                    }
                }
            }
        }).AutoComplete(delegate(string[] args)
        {
            return warpAutoComplete.ToArray();
        }).Register();
    }

    public static void CompatibilityCheck()
    {
        foreach (PartialityMod mod in PartialityManager.Instance.modManager.loadedMods)
        {
            if (mod.ModID == "Custom Regions Mod")
            {
                WarpMod.customRegions = true;
            }
            if (mod.ModID == "Jolly Co-op Mod")
            {
                WarpMod.jollyCoop = true;
            }
        }
    }
}

