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
using Partiality.Modloader;
using Partiality;

public class WarpMenu
{
    public enum SortType
    {
        Type,
        Size,
        Subregion
    }

    public enum ViewType
    {
        Type,
        Size,
        Subregion
    }

    public static SortType sortType = SortType.Type;
    public static ViewType viewType = ViewType.Type;

    //New
    //Master Room Dictionary
    public static Dictionary<string, List<RoomInfo>> masterRoomList = new Dictionary<string, List<RoomInfo>>();
    public static WarpContainer warpContainer;

    //Old
    public static bool warpActive = false;
    public static bool regionWarpActive = false;
    public static AbstractRoom switchRoom;
    public static string newRoom;
    public static string newRegion = "";
    public static string newRegionPrefix;
    public static List<AbstractRoom> abstractRoomList = new List<AbstractRoom>();
    public static bool updateRoomButtons = false;
    public static bool updateDenText = false;
    public static bool ctrlSave = false;
    public static bool ctrlWipe = false;
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
        if (denPos != "NONE")
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
                    for (int i = 0; i < player.abstractCreature.realizedCreature.bodyChunks.Length; i++)
                    {
                        player.abstractCreature.realizedCreature.bodyChunks[i].pos = new Vector2((float)player.room.LocalCoordinateOfNode(0).x * 20f, (float)player.room.LocalCoordinateOfNode(0).y * 20f);
                        player.abstractCreature.realizedCreature.bodyChunks[i].lastPos = new Vector2((float)player.room.LocalCoordinateOfNode(0).x * 20f, (float)player.room.LocalCoordinateOfNode(0).y * 20f);
                        player.abstractCreature.realizedCreature.bodyChunks[i].lastLastPos = new Vector2((float)player.room.LocalCoordinateOfNode(0).x * 20f, (float)player.room.LocalCoordinateOfNode(0).y * 20f);
                    }
                    self.game.cameras[0].virtualMicrophone.AllQuiet();
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
        for (int i = 0; i < self.pages[0].subObjects.Count; i++)
        {
            if (self.pages[0].subObjects[i] is SimpleButton)
            {
                if ((self.pages[0].subObjects[i] as SimpleButton).Selected && (self.pages[0].subObjects[i] as SimpleButton).signalText.EndsWith("warp"))
                {
                    if (RWInput.PlayerInput(0, self.manager.rainWorld.options, self.manager.rainWorld.setup).pckp)
                    {
                        ctrlSave = true;
                        denPos = (self.pages[0].subObjects[i] as SimpleButton).signalText.Remove((self.pages[0].subObjects[i] as SimpleButton).signalText.Length - 4, 4);
                        updateDenText = true;
                    }
                }
            }
        }
        if (RWInput.PlayerInput(0, self.manager.rainWorld.options, self.manager.rainWorld.setup).mp)
        {
            ctrlWipe = true;
            updateDenText = true;
        }
        if (Input.GetKey(KeyCode.C) && denPos != "NONE" || ctrlWipe)
        {
            denPos = "NONE";
            updateDenText = true;
            ctrlWipe = false;
        }
        if (updateDenText)
        {
            for (int i = 0; i < self.pages[0].subObjects.Count; i++)
            {
                if (self.pages[0].subObjects[i] is MenuLabel)
                {
                    if ((self.pages[0].subObjects[i] as MenuLabel).label.text.StartsWith("Cur"))
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
            if (!masterRoomList.ContainsKey(newRegion))
            {
                RoomFinder rf = new RoomFinder();
                List<RoomInfo> roomList = rf.Generate(newRegion, WarpMod.customRegions);
                warpContainer.GenerateRoomButtons(roomList, sortType, viewType);
            }
            else
            {
                warpContainer.GenerateRoomButtons(masterRoomList[newRegion], sortType, viewType);
            }
        }
    }
    private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
    {
        orig.Invoke(self, sender, message);
        if (message.EndsWith("warp"))
        {
            string room = message.Remove(message.Length - 4, 4);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || ctrlSave)
            {
                denPos = room;
                self.PlaySound(SoundID.MENU_Player_Join_Game);
                updateDenText = true;
                ctrlSave = false;
                return;
            }
            else
            {
                Debug.Log(room);
                newRoom = room;
                warpActive = true;
                Debug.Log("WARP: Room warp initiated for: " + room);
                self.Singal(null, "CONTINUE");
                return;
            }
        }
        if (message.EndsWith("reg"))
        {
            newRegion = message.Remove(message.Length - 3, 3);
            warpActive = false;
            Debug.Log("WARP: Loading room list for: " + newRegion);
            self.PlaySound(SoundID.MENU_Add_Level);
            updateRoomButtons = true;
        }
    }

    public class WarpContainer : RectangularMenuObject
    {
        public RainWorldGame game;
        public IntVector2 regOffset;
        public IntVector2 gateOffset;
        public List<WarpButton> roomButtons;
        public List<MenuLabel> categoryLabels;
        public List<WarpButton> regionButtons;
        public List<WarpButton> sortButtons;
        public List<WarpButton> viewButtons;
        public MenuLabel keyLabel;
        public List<MenuLabel> colorKey;
        public MenuLabel subLabel;
        public List<MenuLabel> subregionLabels;
        public bool loadAll = false;
        public int loadCount = 0;
        public WarpContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            game = (menu as PauseMenu).game;
            float hOffset = 80f;
            float vOffset = 28f;
            //Menu Text
            MenuLabel labelOne = new MenuLabel(menu, this, "Warp Menu - " + WarpMod.mod.Version, new Vector2(22f, game.rainWorld.options.ScreenSize.y - 20f), new Vector2(), false);
            labelOne.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(labelOne);
            //Region Title
            MenuLabel regionLabel = new MenuLabel(menu, this, "REGION LIST", new Vector2(70f, game.rainWorld.options.ScreenSize.y - 40f), new Vector2(), false);
            this.subObjects.Add(regionLabel);
            //Region Buttons
            regionButtons = new List<WarpButton>();
            regOffset = new IntVector2();
            if (game.overWorld.regions != null)
            {
                this.subObjects.Add(new SimpleButton(menu, this, "LOAD ALL", "LOADALL", new Vector2(20f, game.rainWorld.options.ScreenSize.y - 72f), new Vector2(100f, 20f)));
                for (int r = 0; r < this.game.overWorld.regions.Length; r++)
                {
                    WarpButton region = new WarpButton(menu, this, this.game.overWorld.regions[r].name, this.game.overWorld.regions[r].name + "reg", new Vector2(20f + ((hOffset - 25f) * regOffset.x), game.rainWorld.options.ScreenSize.y - 100f - ((vOffset) * regOffset.y)), new Vector2(45f, 23f), new Color(0.8f, 0.8f, 0.8f));
                    regionButtons.Add(region);
                    this.subObjects.Add(region);
                    regOffset.x++;
                    if (regOffset.x == 2)
                    {
                        regOffset.x = 0;
                        regOffset.y++;
                    }
                }
                float regionHeight = regionButtons.Last().pos.y;
                MenuLabel filterLabel = new MenuLabel(menu, this, "SORT     |     VIEW", new Vector2(71f, regionHeight - 15f), new Vector2(), false);
                this.subObjects.Add(filterLabel);
                //Sort By Buttons
                sortButtons = new List<WarpButton>();
                sortButtons.Add(new WarpButton(menu, this, "TYPE", "STYPE", new Vector2(22f, regionHeight - 50f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)));
                sortButtons.Add(new WarpButton(menu, this, "SIZE", "SSIZE", new Vector2(22f, regionHeight - 80f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)));
                sortButtons.Add(new WarpButton(menu, this, "SUB", "SSUB", new Vector2(22f, regionHeight - 110f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)));
                for (int i = 0; i < sortButtons.Count; i++)
                {
                    this.subObjects.Add(sortButtons[i]);
                }
                //View By Buttons
                viewButtons = new List<WarpButton>();
                viewButtons.Add(new WarpButton(menu, this, "TYPE", "VTYPE", new Vector2(77f, regionHeight - 50f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)));
                viewButtons.Add(new WarpButton(menu, this, "SIZE", "VSIZE", new Vector2(77f, regionHeight - 80f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)));
                viewButtons.Add(new WarpButton(menu, this, "SUB", "VSUB", new Vector2(77f, regionHeight - 110f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)));
                for (int i = 0; i < viewButtons.Count; i++)
                {
                    this.subObjects.Add(viewButtons[i]);
                }
            }
            if (!masterRoomList.ContainsKey(game.world.region.name))
            {
                RoomFinder rf = new RoomFinder();
                List<RoomInfo> roomList = rf.Generate(game.world.region.name, WarpMod.customRegions);
                GenerateRoomButtons(roomList, sortType, viewType);
            }
            else
            {
                GenerateRoomButtons(masterRoomList[game.world.region.name], sortType, viewType);
            }
        }

        public override void Update()
        {
            base.Update();
            if (regionButtons != null)
            {
                foreach (WarpButton but in regionButtons)
                {
                    if (!masterRoomList.ContainsKey(but.menuLabel.text))
                    {
                        but.color = new Color(0.45f, 0.45f, 0.45f);
                    }
                    else
                    {
                        but.color = new Color(0.8f, 0.8f, 0.8f);
                    }
                }
                if (loadAll)
                {
                    if (loadCount < game.overWorld.regions.Length)
                    {
                        if (!masterRoomList.ContainsKey(game.overWorld.regions[loadCount].name))
                        {
                            RoomFinder rf = new RoomFinder();
                            List<RoomInfo> temp = rf.Generate(game.overWorld.regions[loadCount].name, WarpMod.customRegions);
                            menu.PlaySound(SoundID.MENU_Add_Level);
                        }
                        else
                        {
                            loadCount++;
                        }
                    }
                    else
                    {
                        loadAll = false;
                        menu.PlaySound(SoundID.MENU_Start_New_Game);
                    }
                }
            }
            if (sortButtons != null)
            {
                for (int i = 0; i < sortButtons.Count; i++)
                {
                    if(i == 0 && sortType == SortType.Type)
                    {
                        sortButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else if(i == 1 && sortType == SortType.Size)
                    {
                        sortButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else if (i == 2 && sortType == SortType.Subregion)
                    {
                        sortButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else
                    {
                        sortButtons[i].color = new Color(0.4f, 0.4f, 0.4f);
                    }
                }
            }
            if (viewButtons != null)
            {
                for (int i = 0; i < viewButtons.Count; i++)
                {
                    if (i == 0 && viewType == ViewType.Type)
                    {
                        viewButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else if (i == 1 && viewType == ViewType.Size)
                    {
                        viewButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else if (i == 2 && viewType == ViewType.Subregion)
                    {
                        viewButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else
                    {
                        viewButtons[i].color = new Color(0.4f, 0.4f, 0.4f);
                    }
                }
            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "LOADALL")
            {
                loadAll = true;
            }
            if (message == "STYPE")
            {
                sortType = SortType.Type;
                viewType = ViewType.Type;
                RefreshRoomButtons();
            }
            if (message == "SSIZE")
            {
                sortType = SortType.Size;
                viewType = ViewType.Size;
                RefreshRoomButtons();
            }
            if (message == "SSUB")
            {
                sortType = SortType.Subregion;
                viewType = ViewType.Subregion;
                RefreshRoomButtons();
            }
            if (message == "VTYPE")
            {
                viewType = ViewType.Type;
                RefreshRoomButtons();
            }
            if (message == "VSIZE")
            {
                viewType = ViewType.Size;
                RefreshRoomButtons();
            }
            if (message == "VSUB")
            {
                viewType = ViewType.Subregion;
                RefreshRoomButtons();
            }
        }

        public void RefreshRoomButtons()
        {
            if (newRegion != "")
            {
                if (!masterRoomList.ContainsKey(newRegion))
                {
                    RoomFinder rf = new RoomFinder();
                    List<RoomInfo> roomList = rf.Generate(newRegion, WarpMod.customRegions);
                    GenerateRoomButtons(roomList, sortType, viewType);
                }
                else
                {
                    GenerateRoomButtons(masterRoomList[newRegion], sortType, viewType);
                }
            }
            else
            {
                if (!masterRoomList.ContainsKey(game.world.region.name))
                {
                    RoomFinder rf = new RoomFinder();
                    List<RoomInfo> roomList = rf.Generate(game.world.region.name, WarpMod.customRegions);
                    GenerateRoomButtons(roomList, sortType, viewType);
                }
                else
                {
                    GenerateRoomButtons(masterRoomList[game.world.region.name], sortType, viewType);
                }
            }
        }

        public void GenerateRoomButtons(List<RoomInfo> roomList, SortType sort, ViewType view)
        {
            if (roomButtons != null)
            {
                ObliterateRoomButtons();
            }
            if (categoryLabels != null)
            {
                ObliterateCategoryLabels();
            }
            if (colorKey != null)
            {
                ObliterateColorKeyLabels();
            }
            IntVector2 offset = new IntVector2();
            gateOffset = new IntVector2();
            float regionHeight = regionButtons.Last().pos.y;
            float categoryOffset = 0f;
            float screenWidth = this.game.rainWorld.options.ScreenSize.x;
            float screenHeight = this.game.rainWorld.options.ScreenSize.y;
            int num = -1;
            roomButtons = new List<WarpButton>();
            categoryLabels = new List<MenuLabel>();
            colorKey = new List<MenuLabel>();
            subregionLabels = new List<MenuLabel>();
            List<string> subregionNames = new List<string>();
            //Generate buttons
            switch (sort)
            {
                case SortType.Type:
                    {
                        roomList.Sort(RoomInfo.SortByTypeAndName);
                        break;
                    }
                case SortType.Size:
                    {
                        roomList.Sort(RoomInfo.SortBySizeAndName);
                        break;
                    }
                case SortType.Subregion:
                    {
                        roomList.Sort(RoomInfo.SortBySubregionAndName);
                        break;
                    }
            }
            foreach (RoomInfo info in roomList)
            {
                Color color = new Color(1f, 1f, 1f);
                switch (view)
                {
                    case ViewType.Type:
                        {
                            color = ColorInfo.typeColors[(int)info.type];
                            break;
                        }
                    case ViewType.Size:
                        {
                            if (info.cameras <= 9)
                            {
                                color = ColorInfo.sizeColors[info.cameras];
                            }
                            else
                            {
                                color = new Color(0.3f, 0.3f, 0.3f);
                            }
                            break;
                        }
                    case ViewType.Subregion:
                        {
                            color = ColorInfo.subregionColors[info.subregion];
                            break;
                        }
                }
                //if (info.type == RoomInfo.RoomType.Gate)
                //{
                //    float hOffset = 80f;
                //    float vOffset = 28f;
                //    string name = Regex.Split(info.name, "GATE_")[1];
                //    WarpButton gate = new WarpButton(menu, this, name, info.name + "warp", new Vector2(20f + ((hOffset - 25f) * gateOffset.x), regionHeight - 50f - ((vOffset) * gateOffset.y)), new Vector2(45f, 23f), color);
                //    gateHeight = gate.pos.y;
                //    roomButtons.Add(gate);
                //    gateOffset.x++;
                //    if (gateOffset.x == 2)
                //    {
                //        gateOffset.x = 0;
                //        gateOffset.y++;
                //    }
                //}
                //else
                //{
                    switch (sort)
                    {
                        case SortType.Subregion:
                            {
                                if (num == -1)
                                {
                                    num = info.subregion;
                                    MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                    categoryLabels.Add(label);
                                }
                                if (info.subregion != num)
                                {
                                    offset.x++;
                                    offset.y = 0;
                                    num = info.subregion;
                                    categoryOffset -= 5f;
                                    MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                    categoryLabels.Add(label);
                                }
                                break;
                            }
                        case SortType.Size:
                            {
                                if (num == -1)
                                {
                                    num = info.cameras;
                                    MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                    categoryLabels.Add(label);
                                }
                                if (info.cameras != num)
                                {
                                    offset.x++;
                                    offset.y = 0;
                                    num = info.cameras;
                                    categoryOffset -= 5f;
                                    MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                    categoryLabels.Add(label);
                                }
                                break;
                            }
                        case SortType.Type:
                            {
                                if (num == -1)
                                {
                                    num = (int)info.type;
                                    MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                    categoryLabels.Add(label);
                                }
                                if ((int)info.type != num)
                                {
                                    offset.x++;
                                    offset.y = 0;
                                    num = (int)info.type;
                                    categoryOffset -= 5f;
                                    MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                    categoryLabels.Add(label);
                                }
                                break;
                            }

                    }
                string name = "";
                if(info.type == RoomInfo.RoomType.Gate)
                {
                    name = Regex.Split(info.name, "GATE_")[1];
                    if (info.cameras == 0)
                    {
                        info.cameras = 1;
                    }
                }
                else
                {
                    name = Regex.Split(info.name, "_")[1];
                }
                roomButtons.Add(new WarpButton(menu, this, name, info.name + "warp", new Vector2(screenWidth - (80f + categoryOffset) - (75f * offset.x), screenHeight - 80f - (30f * offset.y)), new Vector2(60f, 25f), color));
                    if (offset.y < 20)
                    {
                        offset.y++;
                    }
                    else
                    {
                        offset.x++;
                        offset.y = 0;
                        categoryOffset -= 10f;
                    }
                //}
            }
            //Add buttons
            for (int i = 0; i < roomButtons.Count; i++)
            {
                this.subObjects.Add(roomButtons[i]);
            }


            float sortHeight = regionHeight - 110f;
            //Add Color Key
            switch (view)
            {
                case ViewType.Type:
                    {
                        colorKey = new List<MenuLabel>();
                        keyLabel = new MenuLabel(menu, this, "ROOM TYPE", new Vector2(22f, sortHeight - 15f), new Vector2(), false);
                        keyLabel.label.alignment = FLabelAlignment.Left;
                        this.subObjects.Add(keyLabel);
                        for (int i = 0; i < ColorInfo.typeColors.Length; i++)
                        {
                            MenuLabel label = new MenuLabel(menu, this, Enum.GetNames(typeof(RoomInfo.RoomType))[i], new Vector2(), new Vector2(), false);
                            label.label.color = ColorInfo.typeColors[i];
                            label.label.alignment = FLabelAlignment.Left;
                            colorKey.Add(label);
                        }
                        break;
                    }
                case ViewType.Size:
                    {
                        colorKey = new List<MenuLabel>();
                        keyLabel = new MenuLabel(menu, this, "ROOM SIZE", new Vector2(22f, sortHeight - 15f), new Vector2(), false);
                        keyLabel.label.alignment = FLabelAlignment.Left;
                        this.subObjects.Add(keyLabel);
                        for (int i = 0; i < ColorInfo.sizeColors.Length; i++)
                        {
                            if (i > 0)
                            {
                                MenuLabel label = new MenuLabel(menu, this, i.ToString(), new Vector2(), new Vector2(), false);
                                label.label.alignment = FLabelAlignment.Left;
                                label.label.color = ColorInfo.sizeColors[i];
                                colorKey.Add(label);
                            }
                        }
                        break;
                    }
                case ViewType.Subregion:
                    {
                        //Sort list by Subregion so values can match correct labels
                        roomList.Sort(RoomInfo.SortBySubregionAndName);
                        subregionNames = new List<string>();
                        foreach(RoomInfo info in roomList)
                        {
                            if(info.subregionName == null)
                            {
                                info.subregionName = "Default";
                            }
                            //Add subregion names to list
                            if (!subregionNames.Contains(info.subregionName))
                            {
                                subregionNames.Add(info.subregionName);
                            }
                        }
                        colorKey = new List<MenuLabel>();
                        keyLabel = new MenuLabel(menu, this, "SUBREGION", new Vector2(22f, sortHeight - 15f), new Vector2(), false);
                        keyLabel.label.alignment = FLabelAlignment.Left;
                        this.subObjects.Add(keyLabel);
                        for (int i = 0; i < subregionNames.Count; i++)
                        {
                            MenuLabel label = new MenuLabel(menu, this, subregionNames[i], new Vector2(), new Vector2(), false);
                            label.label.alignment = FLabelAlignment.Left;
                            label.label.color = ColorInfo.subregionColors[i];
                            colorKey.Add(label);
                        }
                        break;
                    }
            }
            //Add color key labels
            for (int i = 0; i < colorKey.Count; i++)
            {
                if(viewType == ViewType.Size)
                {
                    colorKey[i].pos = new Vector2(25f + (10f * i), sortHeight - 35f);
                }
                else
                {
                    colorKey[i].pos = new Vector2(25f, sortHeight - 35f - (15f * i));
                }
                this.subObjects.Add(colorKey[i]);
            }
            if(sortType == SortType.Subregion && viewType != ViewType.Subregion)
            {
                subregionLabels = new List<MenuLabel>();
                roomList.Sort(RoomInfo.SortBySubregionAndName);
                subregionNames = new List<string>();
                foreach (RoomInfo info in roomList)
                {
                    if (info.subregionName == null)
                    {
                        info.subregionName = "Default";
                    }
                    //Add subregion names to list
                    if (!subregionNames.Contains(info.subregionName))
                    {
                        subregionNames.Add(info.subregionName);
                    }
                }
                float subregionHeight = 0f;
                if (viewType == ViewType.Size)
                {
                    subregionHeight = sortHeight - 40f;
                }
                else
                {
                    subregionHeight = sortHeight - 25f - (15f * colorKey.Count);
                }
                subLabel = new MenuLabel(menu, this, "SUBREGION", new Vector2(22f, subregionHeight - 15f), new Vector2(), false);
                subLabel.label.alignment = FLabelAlignment.Left;
                this.subObjects.Add(subLabel);
                for (int i = 0; i < subregionNames.Count; i++)
                {
                    MenuLabel label = new MenuLabel(menu, this, i + " - " + subregionNames[i], new Vector2(), new Vector2(), false);
                    label.label.alignment = FLabelAlignment.Left;
                    label.label.color = ColorInfo.subregionColors[i];
                    subregionLabels.Add(label);
                }
                for (int i = 0; i < subregionLabels.Count; i++)
                {
                    subregionLabels[i].pos = new Vector2(25f, subregionHeight - 35f - (15f * i));
                    this.subObjects.Add(subregionLabels[i]);
                }
            }
            for(int i = 0; i < categoryLabels.Count; i++)
            {
                this.subObjects.Add(categoryLabels[i]);
            }
        }

        public string CategoryName(SortType sort, int num)
        {
            switch (sort)
            {
                case SortType.Subregion:
                    {
                        return "SUB " + num.ToString();
                    }
                case SortType.Size:
                    {
                        return "CAMS " + num.ToString();
                    }
                case SortType.Type:
                    {
                        string text = Enum.GetNames(typeof (RoomInfo.RoomType))[num].ToUpper();
                        switch (text)
                        {
                            case "SCAVTRADER":
                                {
                                    text = "TRADER";
                                    break;
                                }
                            case "SWARMROOM":
                                {
                                    text = "SWARM";
                                    break;
                                }
                            case "SCAVOUTPOST":
                                {
                                    text = "OUTPOST";
                                    break;
                                }
                        }
                        return text;
                    }

            }
            return "Error";
        }

        public void ObliterateRoomButtons()
        {
            for (int i = 0; i < roomButtons.Count; i++)
            {
                roomButtons[i].RemoveSprites();
                this.RemoveSubObject(roomButtons[i]);
            }
        }
        public void ObliterateCategoryLabels()
        {
            for (int i = 0; i < categoryLabels.Count; i++)
            {
                categoryLabels[i].RemoveSprites();
                this.RemoveSubObject(categoryLabels[i]);
            }
        }
        public void ObliterateSortButtons()
        {
            for (int i = 0; i < sortButtons.Count; i++)
            {
                sortButtons[i].RemoveSprites();
                this.RemoveSubObject(sortButtons[i]);
            }
            for (int i = 0; i < viewButtons.Count; i++)
            {
                viewButtons[i].RemoveSprites();
                this.RemoveSubObject(viewButtons[i]);
            }
        }
        public void ObliterateColorKeyLabels()
        {
            if (keyLabel != null)
            {
                keyLabel.RemoveSprites();
                this.RemoveSubObject(keyLabel);
            }
            for (int i = 0; i < colorKey.Count; i++)
            {
                colorKey[i].RemoveSprites();
                this.RemoveSubObject(colorKey[i]);
            }
            if(subregionLabels != null)
            {
                if(subLabel != null)
                {
                    subLabel.RemoveSprites();
                    this.RemoveSubObject(subLabel);
                }
                for (int i = 0; i < subregionLabels.Count; i++)
                {
                    subregionLabels[i].RemoveSprites();
                    this.RemoveSubObject(subregionLabels[i]);
                }
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }
    }

    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        warpActive = false;
        orig.Invoke(self, manager, game);
        if (game.IsStorySession)
        {
            foreach (PartialityMod mod in PartialityManager.Instance.modManager.loadedMods)
            {
                if (mod.ModID == "Custom Regions Mod")
                {
                    WarpMod.customRegions = true;
                }
            }
            if (self.controlMap != null)
            {
                self.controlMap.pos = new Vector2(0f, 3000f);
            }
            warpContainer = new WarpContainer(self, self.pages[0], self.pages[0].pos, new Vector2());
            self.pages[0].subObjects.Add(warpContainer);
        }
    }
}



