using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menu;
using UnityEngine;
using HUD;
using RWCustom;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

public class WarpMenu
{
    public static bool warpActive = false;
    public static bool regionWarpActive = false;
    public static bool warpingToNewRegion = false;
    public static AbstractRoom switchRoom;
    public static string newRoom;
    public static string newRegion;
    public static List<AbstractRoom> abstractRoomList = new List<AbstractRoom>();
    public static bool realtimeWarp = false;
    public static SimpleButton switchButton;
    public static bool updateText = false;
    public static bool updateRoomButtons = false;

    public static void MenuHook()
    {
        On.Menu.PauseMenu.ctor += PauseMenu_ctor;
        On.Menu.PauseMenu.Singal += PauseMenu_Singal;
        On.Menu.PauseMenu.Update += PauseMenu_Update;
        On.OverWorld.Update += OverWorld_Update;
        On.SaveState.LoadGame += SaveState_LoadGame;
    }


    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        orig.Invoke(self, str, game);
        if (warpingToNewRegion)
        {
            Debug.Log("WARP: Loading game in new region, adjusting denPosition.");
            self.denPosition = newRegion + "_ZZZ";
            warpingToNewRegion = false;
            Debug.Log("WARP: Region warp completed.");
        }
    }

    private static void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
    {
        orig.Invoke(self);
        Player player = (self.game.Players.Count <= 0) ? null : (self.game.Players[0].realizedCreature as Player);
        if (warpActive && !warpingToNewRegion)
        {
            if (switchRoom == null || switchRoom.name != newRoom)
            {
                Debug.Log("WARP: Room warp activated.");
                switchRoom = self.game.world.GetAbstractRoom(newRoom);
            }
            if (switchRoom != null && switchRoom.realizedRoom == null)
            {
                Debug.Log("WARP: About to realise destination " + switchRoom.name);
                switchRoom.RealizeRoom(self.game.world, self.game);
            }
            if (switchRoom.realizedRoom != null && switchRoom.realizedRoom.ReadyForPlayer && player != null)
            {
                Debug.Log("WARP: Destination room " + switchRoom.name + " fully loaded, about to warp player.");
                player.PlaceInRoom(switchRoom.realizedRoom);
                player.abstractCreature.ChangeRooms(player.room.GetWorldCoordinate(player.mainBodyChunk.pos));
            }
            if (player != null && player.room == switchRoom.realizedRoom)
            {
                Debug.Log("WARP: Player moved to destination room, moving camera position.");
                self.game.cameras[0].MoveCamera(player.room, 0);
                warpActive = false;
                switchRoom = null;
                Debug.Log("WARP: Warp completed.");
            }
        }
        if (regionWarpActive && !warpActive)
        {
            if (realtimeWarp)
            {
                regionWarpActive = false;
                RegionSwitcher rs = new RegionSwitcher();
                rs.SwitchRegions(self.game, newRegion, newRegion + "_S04", new IntVector2(0, 0));
            }
            else
            {
                self.game.RestartGame();
                warpingToNewRegion = true;
            }
            regionWarpActive = false;
        }
    }
    private static void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
    {
        orig.Invoke(self);
        if (updateRoomButtons)
        {
            updateRoomButtons = false;
            for (int i = 0; i < self.pages[0].subObjects.Count; i++)
            {
                if (self.pages[0].subObjects[i] is SimpleButton)
                {
                    if ((self.pages[0].subObjects[i] as SimpleButton).signalText.EndsWith("warp"))
                    {
                        (self.pages[0].subObjects[i] as SimpleButton).RemoveSprites();
                        //(self.pages[0].subObjects[i] as SimpleButton).pos.y += 2000f;
                    }
                }
            }
            RoomFinder rf = new RoomFinder();
            List<string> newRoomList = rf.RoomList(newRegion);
            float hOffset = 80f;
            float vOffset = 35f;
            int vershift = 0;
            int horShift = 0;
            int gateshift = 0;
            if (newRoomList.Count > 0)
            {
                newRoomList.Sort();
            }
            for (int i = 0; i < newRoomList.Count; i++)
            {
                if (vershift < 16 && !newRoomList[i].StartsWith("Off"))
                {
                    if (newRoomList[i].StartsWith("GATE"))
                    {
                        self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], newRoomList[i], newRoomList[i] + "warp", new Vector2(20f, 165f + (vOffset * gateshift)), new Vector2(100f, 30f)));
                        gateshift++;
                    }
                    else
                    {
                        self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], newRoomList[i], newRoomList[i] + "warp", new Vector2((self.game.rainWorld.options.ScreenSize.x - 100f) - (hOffset * horShift), (self.game.rainWorld.options.ScreenSize.y - 80f) - (vOffset * vershift)), new Vector2(70f, 30f)));
                        vershift++;
                        if (vershift == 16)
                        {
                            vershift = 0;
                            horShift++;
                        }
                    }
                }
            }
            updateText = true;
            Debug.Log("WARP: Finished adding room buttons.");
        }
        if (updateText)
        {
            if (realtimeWarp)
            {
                switchButton.menuLabel.text = "ENABLED";
            }
            else
            {
                switchButton.menuLabel.text = "DISABLED";
            }
            updateText = false;
        }
    }
    private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
    {
        orig.Invoke(self, sender, message);
        Debug.Log("WARP: Button pressed");
        if (message.EndsWith("warp"))
        {
            string room = message.Remove(message.Length - 4, 4);
            warpActive = true;
            newRoom = room;
            Debug.Log("WARP: Room warp initiated for: " + room);
            self.Singal(null, "CONTINUE");
        }
        if (message.EndsWith("reg"))
        {
            newRegion = message.Remove(message.Length - 3, 3);
            Debug.Log("WARP: New region selected: " + newRegion);
            warpActive = false;
            updateRoomButtons = true;
            if(self.game.world.name != newRegion)
            {
                regionWarpActive = true;
            }
            else
            {
                regionWarpActive = true;
            }
        }
        if (message == "SWITCH")
        {
            if (realtimeWarp)
            {
                realtimeWarp = false;
            }
            else
            {
                realtimeWarp = true;
            }
            updateText = true;
            self.PlaySound(SoundID.MENU_Player_Join_Game);
        }
    }

    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        warpActive = false;
        Debug.Log("WARP: Pause screen opened.");
        orig.Invoke(self, manager, game);
        if (game.devToolsActive)
        {
            if (self.controlMap != null)
            {
                Debug.Log("WARP: Moving control map position.");
                self.controlMap.pos = new Vector2(0f, 3000f);
            }
            float hOffset = 80f;
            float vOffset = 35f;
            int vershift = 0;
            int horShift = 0;
            int gateshift = 0;
            int regionShift = 0;
            int regionVerShift = 0;
            if (game.world.abstractRooms != null)
            {
                Debug.Log("WARP: Grabbing region's abstract rooms list.");
                List<string> roomList = new List<string>();
                foreach (AbstractRoom room in game.world.abstractRooms)
                {
                    roomList.Add(room.name);
                }
                if (roomList.Count > 0)
                {
                    roomList.Sort();
                }
                for (int i = 0; i < roomList.Count; i++)
                {
                    if (vershift < 16 && !roomList[i].StartsWith("Off"))
                    {
                        if (roomList[i].StartsWith("GATE"))
                        {
                            self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], roomList[i], roomList[i] + "warp", new Vector2(20f, 165f + (vOffset * gateshift)), new Vector2(100f, 30f)));
                            gateshift++;
                        }
                        else
                        {
                            self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], roomList[i], roomList[i] + "warp", new Vector2((game.rainWorld.options.ScreenSize.x - 100f) - (hOffset * horShift), (game.rainWorld.options.ScreenSize.y - 80f) - (vOffset * vershift)), new Vector2(70f, 30f)));
                            vershift++;
                            if (vershift == 16)
                            {
                                vershift = 0;
                                horShift++;
                            }
                        }
                    }
                }
                self.pages[0].subObjects.Add(new MenuLabel(self, self.pages[0], "Real-time Warp", new Vector2(71f, game.rainWorld.options.ScreenSize.y - 38f), new Vector2(), false));
                switchButton = new SimpleButton(self, self.pages[0], "", "SWITCH", new Vector2(20f, game.rainWorld.options.ScreenSize.y - 80f), new Vector2(100f, 30f));
                self.pages[0].subObjects.Add(switchButton);
                updateText = true;
                Debug.Log("WARP: Finished adding room buttons.");
            }
            if (self.game.overWorld.regions != null)
            {
                Debug.Log("WARP: Grabbing loaded region list");
                for (int r = 0; r < self.game.overWorld.regions.Length; r++)
                {
                    self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], self.game.overWorld.regions[r].name, self.game.overWorld.regions[r].name + "reg", new Vector2(20f + ((hOffset - 25f) * regionShift), game.rainWorld.options.ScreenSize.y - 120f - ((vOffset + 6f) * regionVerShift)), new Vector2(45f, 30f)));
                    regionShift++;
                    if (regionShift == 2)
                    {
                        regionShift = 0;
                        regionVerShift++;
                    }
                }
                Debug.Log("WARP: Finished adding region buttons.");
            }
        }
    }
}



