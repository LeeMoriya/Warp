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
    public static AbstractRoom switchRoom;
    public static string newRoom;
    public static string newRegion;
    public static string newRegionPrefix;
    public static List<AbstractRoom> abstractRoomList = new List<AbstractRoom>();
    public static bool updateRoomButtons = false;
    public static bool updateDenText = false;
    public static bool denMode = false;
    public static string denPos = "NONE";
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
        if(denPos != "NONE")
        {
            self.denPosition = denPos;
        }
    }

    private static void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
    {
        orig.Invoke(self);
        Player player = (self.game.Players.Count <= 0) ? null : (self.game.Players[0].realizedCreature as Player);
        if (warpActive)
        {
            if (newRegion != null && newRegion.Length == 2 && newRegion != self.activeWorld.region.name)
            {
                //Debug.Log(newRegion + " | " + self.activeWorld.region.name);
                RegionSwitcher rs = new RegionSwitcher();
                rs.SwitchRegions(self.game, newRegion, newRoom, new IntVector2(0, 0));
                warpActive = false;
            }
            else
            {
                if (switchRoom == null || switchRoom.name != newRoom)
                {
                    //Debug.Log("WARP: Room warp activated.");
                    switchRoom = self.game.world.GetAbstractRoom(newRoom);
                }
                if (switchRoom != null && switchRoom.realizedRoom == null)
                {
                    //Debug.Log("WARP: About to realise destination " + switchRoom.name);
                    switchRoom.RealizeRoom(self.game.world, self.game);
                }
                if (switchRoom.realizedRoom != null && switchRoom.realizedRoom.ReadyForPlayer && player != null && player.room != switchRoom.realizedRoom)
                {
                    if (player.grasps != null)
                    {
                        for (int i = 0; i < player.grasps.Length; i++)
                        {
                            if (player.grasps[i] != null && player.grasps[i].grabbed != null && !player.grasps[i].discontinued && player.grasps[i].grabbed is Creature)
                            {
                                player.ReleaseGrasp(i);
                            }
                        }
                    }
                    //Debug.Log("WARP: Destination room " + switchRoom.name + " fully loaded, about to warp player.");
                    player.PlaceInRoom(switchRoom.realizedRoom);
                    player.abstractCreature.ChangeRooms(player.room.GetWorldCoordinate(player.mainBodyChunk.pos));
                }
                if (player != null && player.room == switchRoom.realizedRoom)
                {
                    //Debug.Log("WARP: Player moved to destination room, moving camera position.");
                    self.game.cameras[0].MoveCamera(player.room, 0);
                    warpActive = false;
                    switchRoom = null;
                    //Debug.Log("WARP: Warp completed.");
                }
            }
        }

    }
    private static void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
    {
        orig.Invoke(self);
        if (Input.GetKey(KeyCode.C) && denPos != "NONE")
        {
            denPos = "NONE";
            updateDenText = true;
        }
        if (updateDenText)
        {
            for(int i = 0; i < self.pages[0].subObjects.Count; i++)
            {
                if(self.pages[0].subObjects[i] is MenuLabel)
                {
                    if((self.pages[0].subObjects[i] as MenuLabel).label.text.StartsWith("Cur"))
                    {
                        (self.pages[0].subObjects[i] as MenuLabel).label.text = "Current den position: " + denPos + " | Press C to clear";
                        break;
                    }
                }
            }
            updateDenText = false;
        }
        if (updateRoomButtons)
        {
            updateRoomButtons = false;
            self.init = false;
            for (int i = 0; i < self.pages[0].subObjects.Count; i++)
            {
                if (self.pages[0].subObjects[i] is SimpleButton)
                {
                    if ((self.pages[0].subObjects[i] as SimpleButton).signalText.EndsWith("warp"))
                    {
                        (self.pages[0].subObjects[i] as SimpleButton).RemoveSprites();
                        self.pages[0].RecursiveRemoveSelectables(self.pages[0].subObjects[i] as SimpleButton);
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
        }
    }
    private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
    {
        orig.Invoke(self, sender, message);
        //Debug.Log("WARP: Button pressed");
        if (message.EndsWith("warp"))
        {
            string room = message.Remove(message.Length - 4, 4);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                denPos = room;
                self.PlaySound(SoundID.MENU_Player_Join_Game);
                updateDenText = true;
                return;
            }
            else
            {
                Debug.Log(room);
                newRoom = room;
                warpActive = true;
                //Debug.Log("WARP: Room warp initiated for: " + room);
                self.Singal(null, "CONTINUE");
            }
        }
        if (message.EndsWith("reg"))
        {
            newRegion = message.Remove(message.Length - 3, 3);
            warpActive = false;
            updateRoomButtons = true;
        }
    }

    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        warpActive = false;
        orig.Invoke(self, manager, game);
        if (game.devToolsActive)
        {
            if (self.controlMap != null)
            {
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
                //Debug.Log("WARP: Grabbing region's abstract rooms list.");
                List<string> roomList = new List<string>();
                foreach (AbstractRoom room in game.world.abstractRooms)
                {
                    roomList.Add(room.name);
                }
                if (roomList.Count > 0)
                {
                    roomList.Sort();
                }
                MenuLabel labelOne = new MenuLabel(self, self.pages[0], "Shift click a room name to set den position", new Vector2(22f + ((hOffset - 25f) * regionShift), game.rainWorld.options.ScreenSize.y - 20f),new Vector2(), false);
                MenuLabel labelTwo = new MenuLabel(self, self.pages[0], "Current den position: " + denPos + " | Press C to clear", new Vector2(22f + ((hOffset - 25f) * regionShift), game.rainWorld.options.ScreenSize.y - 37f), new Vector2(), false);
                labelOne.label.alignment = FLabelAlignment.Left;
                labelTwo.label.alignment = FLabelAlignment.Left;
                self.pages[0].subObjects.Add(labelOne);
                self.pages[0].subObjects.Add(labelTwo);
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
            }
            if (self.game.overWorld.regions != null)
            {
                //Debug.Log("WARP: Grabbing loaded region list");
                for (int r = 0; r < self.game.overWorld.regions.Length; r++)
                {
                    self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], self.game.overWorld.regions[r].name, self.game.overWorld.regions[r].name + "reg", new Vector2(20f + ((hOffset - 25f) * regionShift), game.rainWorld.options.ScreenSize.y - 80f - ((vOffset) * regionVerShift)), new Vector2(45f, 30f)));
                    regionShift++;
                    if (regionShift == 2)
                    {
                        regionShift = 0;
                        regionVerShift++;
                    }
                }
            }
        }
    }
}



