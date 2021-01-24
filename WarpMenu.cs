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
    public static Dictionary<string, List<string>> subregionNames = new Dictionary<string, List<string>>();
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
        self.pages[0].pos.x = 0f;
        if (Input.GetKey(KeyCode.C) && denPos != "NONE")
        {
            denPos = "NONE";
            updateDenText = true;
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
        public FSprite bg;
        public RainWorldGame game;
        public IntVector2 regOffset;
        public IntVector2 gateOffset;
        public List<WarpButton> roomButtons;
        public List<MenuLabel> categoryLabels;
        public List<WarpButton> regionButtons;
        public List<WarpButton> sortButtons;
        public List<WarpButton> viewButtons;
        public WarpButton colorConfig;
        public MenuLabel keyLabel;
        public List<MenuLabel> colorKey;
        public MenuLabel subLabel;
        public List<MenuLabel> subregionLabels;
        public bool loadAll = false;
        public int loadCount = 0;
        public WarpColor warpColor;
        public WarpContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            game = (menu as PauseMenu).game;
            newRegion = game.world.region.name;
            ColorInfo.Load();
            bg = new FSprite("LinearGradient200", true);
            bg.color = new Color(0.01f, 0.01f, 0.01f);
            bg.SetAnchor(1f, 0f);
            bg.rotation = 90f;
            bg.scaleY = 2f;
            bg.scaleX = 1250f;
            bg.alpha = 0.5f;
            this.Container.AddChild(bg);
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
                this.subObjects.Add(new SimpleButton(menu, this, "LOAD ALL", "LOADALL", new Vector2(21f, game.rainWorld.options.ScreenSize.y - 72f), new Vector2(100f, 20f)));
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
                colorConfig = new WarpButton(menu, this, "COLORS", "COLORS", new Vector2(21f, regionHeight - 137f), new Vector2(100f, 20f), new Color(1f, 0.4f, 0.4f));
                this.subObjects.Add(colorConfig);
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
                    if (newRegion == but.menuLabel.text)
                    {
                        but.color = new Color(0.2f, 1f, 0.2f);
                    }
                    else
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
                    if (i == 0 && sortType == SortType.Type)
                    {
                        sortButtons[i].color = new Color(0.2f, 1f, 0.2f);
                    }
                    else if (i == 1 && sortType == SortType.Size)
                    {
                        sortButtons[i].color = new Color(0.2f, 1f, 0.2f);
                    }
                    else if (i == 2 && sortType == SortType.Subregion)
                    {
                        sortButtons[i].color = new Color(0.2f, 1f, 0.2f);
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
                        viewButtons[i].color = new Color(0.2f, 1f, 0.2f);
                    }
                    else if (i == 1 && viewType == ViewType.Size)
                    {
                        viewButtons[i].color = new Color(0.2f, 1f, 0.2f);
                    }
                    else if (i == 2 && viewType == ViewType.Subregion)
                    {
                        viewButtons[i].color = new Color(0.2f, 1f, 0.2f);
                    }
                    else
                    {
                        viewButtons[i].color = new Color(0.4f, 0.4f, 0.4f);
                    }
                }
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            if (bg != null)
            {
                bg.RemoveFromContainer();
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
            if (message == "COLORS")
            {
                if (warpColor == null)
                {
                    ObliterateRoomButtons();
                    ObliterateColorKeyLabels();
                    ObliterateCategoryLabels();
                    warpColor = new WarpColor(menu, this, new Vector2(0f, 0f), new Vector2());
                    this.subObjects.Add(warpColor);
                    menu.PlaySound(SoundID.MENU_Player_Join_Game);
                }
                else
                {
                    menu.PlaySound(SoundID.MENU_Error_Ping);
                }
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
            if (warpColor != null)
            {
                return;
            }
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
                if (info.cameras == 0)
                {
                    info.cameras = 1;
                }
                switch (view)
                {
                    case ViewType.Type:
                        {
                            color = ColorInfo.typeColors[(int)info.type].rgb;
                            break;
                        }
                    case ViewType.Size:
                        {
                            if (info.cameras <= 9)
                            {
                                color = ColorInfo.sizeColors[info.cameras - 1].rgb;
                            }
                            else
                            {
                                color = ColorInfo.sizeColors[8].rgb;
                            }
                            break;
                        }
                    case ViewType.Subregion:
                        {
                            if (ColorInfo.customSubregionColors.ContainsKey(WarpMenu.newRegion))
                            {
                                color = ColorInfo.customSubregionColors[WarpMenu.newRegion][info.subregion].rgb;
                            }
                            else
                            {
                                color = ColorInfo.subregionColors[info.subregion].rgb;
                            }
                            break;
                        }
                }
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
                                if (offset.y > 0)
                                {
                                    offset.x++;
                                    categoryOffset -= 5f;
                                }
                                else
                                {
                                    categoryOffset += 5f;
                                }
                                offset.y = 0;
                                num = info.subregion;
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
                                if (offset.y > 0)
                                {
                                    offset.x++;
                                    categoryOffset -= 5f;
                                }
                                offset.y = 0;
                                num = info.cameras;
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
                                if (offset.y > 0)
                                {
                                    offset.x++;
                                    categoryOffset -= 5f;
                                }
                                offset.y = 0;
                                num = (int)info.type;
                                MenuLabel label = new MenuLabel(menu, this, CategoryName(sort, num), new Vector2(screenWidth - (50f + categoryOffset) - (75f * offset.x), screenHeight - 45f), new Vector2(), false);
                                categoryLabels.Add(label);
                            }
                            break;
                        }

                }
                string name = "";
                if (info.type == RoomInfo.RoomType.Gate)
                {
                    name = Regex.Split(info.name, "GATE_")[1];
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
            }
            //Add buttons
            for (int i = 0; i < roomButtons.Count; i++)
            {
                this.subObjects.Add(roomButtons[i]);
            }


            float sortHeight = regionHeight - 135f;
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
                            label.label.color = ColorInfo.typeColors[i].rgb;
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
                            string name = (i + 1).ToString();
                            if (i >= 8)
                            {
                                name = "9+";
                            }
                            MenuLabel label = new MenuLabel(menu, this, name, new Vector2(), new Vector2(), false);
                            label.label.alignment = FLabelAlignment.Left;
                            label.label.color = ColorInfo.sizeColors[i].rgb;
                            colorKey.Add(label);
                        }
                        break;
                    }
                case ViewType.Subregion:
                    {
                        //Sort list by Subregion so values can match correct labels
                        colorKey = new List<MenuLabel>();
                        keyLabel = new MenuLabel(menu, this, "SUBREGION", new Vector2(22f, sortHeight - 15f), new Vector2(), false);
                        keyLabel.label.alignment = FLabelAlignment.Left;
                        this.subObjects.Add(keyLabel);
                        for (int i = 0; i < WarpMenu.subregionNames[newRegion].Count; i++)
                        {
                            MenuLabel label = new MenuLabel(menu, this, WarpMenu.subregionNames[newRegion][i], new Vector2(), new Vector2(), false);
                            label.label.alignment = FLabelAlignment.Left;
                            label.label.color = ColorInfo.customSubregionColors[WarpMenu.newRegion][i].rgb;
                            colorKey.Add(label);
                        }
                        break;
                    }
            }
            //Add color key labels
            for (int i = 0; i < colorKey.Count; i++)
            {
                if (viewType == ViewType.Size)
                {
                    colorKey[i].pos = new Vector2(25f + (10f * i), sortHeight - 35f);
                }
                else
                {
                    colorKey[i].pos = new Vector2(25f, sortHeight - 35f - (15f * i));
                }
                this.subObjects.Add(colorKey[i]);
            }
            if (sortType == SortType.Subregion && viewType != ViewType.Subregion)
            {
                subregionLabels = new List<MenuLabel>();
                roomList.Sort(RoomInfo.SortBySubregionAndName);
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
                for (int i = 0; i < WarpMenu.subregionNames[newRegion].Count; i++)
                {
                    MenuLabel label = new MenuLabel(menu, this, i + " - " + WarpMenu.subregionNames[newRegion][i], new Vector2(), new Vector2(), false);
                    label.label.alignment = FLabelAlignment.Left;
                    label.label.color = ColorInfo.subregionColors[i].rgb;
                    subregionLabels.Add(label);
                }
                for (int i = 0; i < subregionLabels.Count; i++)
                {
                    subregionLabels[i].pos = new Vector2(25f, subregionHeight - 35f - (15f * i));
                    this.subObjects.Add(subregionLabels[i]);
                }
            }
            for (int i = 0; i < categoryLabels.Count; i++)
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
                        string text = Enum.GetNames(typeof(RoomInfo.RoomType))[num].ToUpper();
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
            if (subregionLabels != null)
            {
                if (subLabel != null)
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

public class WarpColor : RectangularMenuObject, Slider.ISliderOwner
{
    public WarpMenu.WarpContainer warpContainer;
    public MenuLabel infoLabel;
    public List<WarpButton> colButtons;
    public WarpButton doneButton;
    public Vector2 anchor;
    public HorizontalSlider hueSlider;
    public HorizontalSlider satSlider;
    public HorizontalSlider litSlider;
    public HSLColor currentCol;
    public WarpButton typeButton;
    public WarpButton sizeButton;
    public WarpButton subButton;
    public WarpButton saveButton;
    public Category category;
    public MenuLabel categoryLabel;
    public MenuLabel mouseOverLabel;
    public int selectedColor = -1;
    public List<string> subregionNames;
    public string currentRegion;
    public enum Category
    {
        Type,
        Size,
        Subregion
    }
    public float hue = 0.2f;
    public float sat = 1f;
    public float lit = 0.5f;
    public WarpColor(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        warpContainer = (owner as WarpMenu.WarpContainer);
        currentCol = new HSLColor(hue, 1f, 0.5f);
        anchor = new Vector2(160f, menu.manager.rainWorld.screenSize.y - 350f);
        currentRegion = WarpMenu.newRegion;

        //Reset and Save buttons
        saveButton = new WarpButton(menu, this, "SAVE", "SAVE", new Vector2(anchor.x + 20f + 95f, anchor.y - 20f), new Vector2(80f, 30f), new Color(0.4f, 1f, 0.4f));
        this.subObjects.Add(saveButton);
        doneButton = new WarpButton(menu, this, "DONE", "DONE", new Vector2(saveButton.pos.x + 95f, anchor.y - 20f), new Vector2(80f, 30f), new Color(0.8f, 0.8f, 0.8f));
        this.subObjects.Add(doneButton);

        //Category switch buttons
        typeButton = new WarpButton(menu, this, "TYPE", "CTYPE", new Vector2(anchor.x + 20f, anchor.y + 200f), new Vector2(80f, 30f), new Color(0.8f, 0.8f, 0.8f));
        this.subObjects.Add(typeButton);
        sizeButton = new WarpButton(menu, this, "SIZE", "CSIZE", new Vector2(typeButton.pos.x + 95f, anchor.y + 200f), new Vector2(80f, 30f), new Color(0.8f, 0.8f, 0.8f));
        this.subObjects.Add(sizeButton);
        subButton = new WarpButton(menu, this, "SUB", "CSUB", new Vector2(sizeButton.pos.x + 95f, anchor.y + 200f), new Vector2(80f, 30f), new Color(0.8f, 0.8f, 0.8f));
        this.subObjects.Add(subButton);

        //Title label
        infoLabel = new MenuLabel(menu, this, "COLOR CUSTOMISER" + Environment.NewLine + "Adjust global room Type and Size button colors" + Environment.NewLine + "and individual subregion colors per region", new Vector2(sizeButton.pos.x + 40f, anchor.y + 265f), new Vector2(), false);
        this.subObjects.Add(infoLabel);

        //Category label
        categoryLabel = new MenuLabel(menu, this, "Select a button above to configure colors", new Vector2(sizeButton.pos.x + 40f, anchor.y + 147f), new Vector2(), false);
        this.subObjects.Add(categoryLabel);

        //Mouse Over label
        mouseOverLabel = new MenuLabel(menu, this, "", new Vector2(sizeButton.pos.x + 40f, anchor.y - 45f), new Vector2(), false);
        this.subObjects.Add(mouseOverLabel);

        //Color controls
        hueSlider = new HorizontalSlider(menu, this, "HUE", new Vector2(anchor.x + 30f, anchor.y + 100f), new Vector2(185f, 0f), Slider.SliderID.LevelsListScroll, false);
        this.subObjects.Add(hueSlider);
        satSlider = new HorizontalSlider(menu, this, "SAT", new Vector2(anchor.x + 30f, anchor.y + 60f), new Vector2(185f, 0f), Slider.SliderID.LevelsListScroll, false);
        this.subObjects.Add(satSlider);
        litSlider = new HorizontalSlider(menu, this, "LIT", new Vector2(anchor.x + 30f, anchor.y + 20f), new Vector2(185f, 0f), Slider.SliderID.LevelsListScroll, false);
        this.subObjects.Add(litSlider);

        switch (WarpMenu.viewType)
        {
            case WarpMenu.ViewType.Type:
                {
                    category = Category.Type;
                    CreateTypeButtons();
                    break;
                }
            case WarpMenu.ViewType.Size:
                {
                    category = Category.Size;
                    CreateSizeButtons();
                    break;
                }
            case WarpMenu.ViewType.Subregion:
                {
                    category = Category.Subregion;
                    CreateSubregionButtons();
                    break;
                }
        }
    }

    public override void Update()
    {
        base.Update();
        //Region has changed
        if (currentRegion != WarpMenu.newRegion && WarpMenu.masterRoomList.ContainsKey(WarpMenu.newRegion))
        {
            currentRegion = WarpMenu.newRegion;
            selectedColor = -1;
            switch (category)
            {
                case Category.Type:
                    {
                        CreateTypeButtons();
                        break;
                    }
                case Category.Size:
                    {
                        CreateSizeButtons();
                        break;
                    }
                case Category.Subregion:
                    {
                        CreateSubregionButtons();
                        break;
                    }
            }
        }
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message == "DONE")
        {
            this.RemoveSprites();
            warpContainer.RemoveSubObject(this);
            warpContainer.warpColor = null;
            if (!WarpMenu.masterRoomList.ContainsKey(currentRegion))
            {
                RoomFinder rf = new RoomFinder();
                List<RoomInfo> roomList = rf.Generate(currentRegion, WarpMod.customRegions);
                warpContainer.GenerateRoomButtons(roomList, WarpMenu.sortType, WarpMenu.viewType);
            }
            else
            {
                warpContainer.GenerateRoomButtons(WarpMenu.masterRoomList[currentRegion], WarpMenu.sortType, WarpMenu.viewType);
            }
        }
        //Category
        if (message == "CTYPE")
        {
            menu.PlaySound(SoundID.MENU_Add_Level);
            category = Category.Type;
            selectedColor = -1;
            CreateTypeButtons();
        }
        if (message == "CSIZE")
        {
            menu.PlaySound(SoundID.MENU_Add_Level);
            category = Category.Size;
            selectedColor = -1;
            CreateSizeButtons();
        }
        if (message == "CSUB")
        {
            menu.PlaySound(SoundID.MENU_Add_Level);
            category = Category.Subregion;
            selectedColor = -1;
            CreateSubregionButtons();
        }
        //Color buttons
        if (message.StartsWith("X"))
        {
            menu.PlaySound(SoundID.MENU_Add_Level);
            int num = int.Parse(message.Substring(1));
            switch (category)
            {
                case Category.Type:
                    {
                        selectedColor = num;
                        currentCol = ColorInfo.typeColors[selectedColor];
                        break;
                    }
                case Category.Size:
                    {
                        selectedColor = num;
                        currentCol = ColorInfo.sizeColors[selectedColor];
                        break;
                    }
                case Category.Subregion:
                    {
                        selectedColor = num;
                        currentCol = ColorInfo.customSubregionColors[currentRegion][selectedColor];
                        break;
                    }
            }
            Debug.Log("H:" + hue + "S:" + sat + "L:" + lit);
            hue = currentCol.hue;
            sat = currentCol.saturation;
            lit = currentCol.lightness;
        }
        if (message == "SAVE")
        {
            menu.PlaySound(SoundID.MENU_Player_Join_Game);
            ColorInfo.Save();
        }
    }

    public void ObliterateColorButtons()
    {
        if (colButtons != null)
        {
            for (int i = 0; i < colButtons.Count; i++)
            {
                colButtons[i].RemoveSprites();
                this.RemoveSubObject(colButtons[i]);
            }
        }
    }

    public void CreateTypeButtons()
    {
        ObliterateColorButtons();
        float offset = 30f;
        colButtons = new List<WarpButton>();
        for (int i = 0; i < Enum.GetNames(typeof(RoomInfo.RoomType)).Length; i++)
        {
            WarpButton but = new WarpButton(menu, this, (i + 1).ToString(), "X" + i.ToString(), new Vector2(typeButton.pos.x + 3f + (offset * i), typeButton.pos.y - 35f), new Vector2(25f, 25f), ColorInfo.typeColors[i].rgb);
            colButtons.Add(but);
        }
        for (int i = 0; i < colButtons.Count; i++)
        {
            this.subObjects.Add(colButtons[i]);
        }
    }

    public void CreateSizeButtons()
    {
        ObliterateColorButtons();
        float offset = 30f;
        colButtons = new List<WarpButton>();
        for (int i = 0; i < 9; i++)
        {
            WarpButton but = new WarpButton(menu, this, (i + 1).ToString(), "X" + i.ToString(), new Vector2(typeButton.pos.x + 3f + (offset * i), typeButton.pos.y - 35f), new Vector2(25f, 25f), ColorInfo.sizeColors[i].rgb);
            colButtons.Add(but);
        }
        for (int i = 0; i < colButtons.Count; i++)
        {
            this.subObjects.Add(colButtons[i]);
        }
    }

    public void CreateSubregionButtons()
    {
        Debug.Log("Creating subregion buttons");
        subregionNames = new List<string>();
        if (WarpMenu.masterRoomList.ContainsKey(currentRegion))
        {
            //If no custom colors are defined for this region's subregions, create a new entry with default colors
            if (!ColorInfo.customSubregionColors.ContainsKey(currentRegion))
            {
                ColorInfo.customSubregionColors.Add(currentRegion, new List<HSLColor>());
                for (int i = 0; i < WarpMenu.subregionNames[currentRegion].Count; i++)
                {
                    ColorInfo.customSubregionColors[currentRegion].Add(ColorInfo.subregionColors[i]);
                }
            }
        }
        ObliterateColorButtons();
        float offset = 30f;
        colButtons = new List<WarpButton>();
        for (int i = 0; i < ColorInfo.customSubregionColors[currentRegion].Count; i++)
        {
            WarpButton but = new WarpButton(menu, this, (i + 1).ToString(), "X" + i.ToString(), new Vector2(typeButton.pos.x + 3f + (offset * i), typeButton.pos.y - 35f), new Vector2(25f, 25f), ColorInfo.customSubregionColors[currentRegion][i].rgb);
            colButtons.Add(but);
        }
        for (int i = 0; i < colButtons.Count; i++)
        {
            this.subObjects.Add(colButtons[i]);
        }
    }


    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (typeButton != null && sizeButton != null && subButton != null)
        {
            switch (category)
            {
                case Category.Type:
                    {
                        typeButton.color = new Color(0.8f, 0.8f, 0.8f);
                        sizeButton.color = new Color(0.4f, 0.4f, 0.4f);
                        subButton.color = new Color(0.4f, 0.4f, 0.4f);
                        for (int i = 0; i < colButtons.Count; i++)
                        {
                            colButtons[i].color = ColorInfo.typeColors[i].rgb;
                        }
                        break;
                    }
                case Category.Size:
                    {
                        sizeButton.color = new Color(0.8f, 0.8f, 0.8f);
                        typeButton.color = new Color(0.4f, 0.4f, 0.4f);
                        subButton.color = new Color(0.4f, 0.4f, 0.4f);
                        for (int i = 0; i < colButtons.Count; i++)
                        {
                            colButtons[i].color = ColorInfo.sizeColors[i].rgb;
                        }
                        break;
                    }
                case Category.Subregion:
                    {
                        subButton.color = new Color(0.8f, 0.8f, 0.8f);
                        typeButton.color = new Color(0.4f, 0.4f, 0.4f);
                        sizeButton.color = new Color(0.4f, 0.4f, 0.4f);
                        for (int i = 0; i < colButtons.Count; i++)
                        {
                            colButtons[i].color = ColorInfo.customSubregionColors[currentRegion][i].rgb;
                        }
                        break;
                    }
            }
        }
        if (selectedColor != -1)
        {
            switch (category)
            {
                case Category.Type:
                    {
                        ColorInfo.typeColors[selectedColor] = currentCol;
                        break;
                    }
                case Category.Size:
                    {
                        ColorInfo.sizeColors[selectedColor] = currentCol;
                        break;
                    }
                case Category.Subregion:
                    {
                        ColorInfo.customSubregionColors[currentRegion][selectedColor] = currentCol;
                        break;
                    }
            }
        }
        currentCol.hue = hue;
        currentCol.saturation = sat;
        currentCol.lightness = lit;
        if (mouseOverLabel != null)
        {
            string text = "";
            //Category buttons
            if (typeButton.IsMouseOverMe)
            {
                text = "Configure button colors when viewing by room type";
            }
            else if (sizeButton.IsMouseOverMe)
            {
                text = "Configure button colors when viewing by room size";
            }
            else if (subButton.IsMouseOverMe)
            {
                text = "Configure button colors when viewing by subregion";
            }
            //Config buttons
            else if (saveButton.IsMouseOverMe)
            {
                text = "Save the configured colors to a file";
            }
            else if (doneButton.IsMouseOverMe)
            {
                text = "Return to the main Warp menu";
            }
            else
            {
                text = "";
            }
            mouseOverLabel.label.text = text;
        }
        if (categoryLabel != null)
        {
            if (selectedColor != -1)
            {
                if (category == Category.Subregion && subregionNames != null)
                {
                    if (WarpMenu.subregionNames[currentRegion].Count >= selectedColor)
                    {
                        categoryLabel.label.text = WarpMenu.subregionNames[currentRegion][selectedColor];
                        categoryLabel.label.color = currentCol.rgb;
                    }
                }
                else
                {
                    if (category == Category.Size)
                    {
                        categoryLabel.label.text = "Cameras: " + (selectedColor + 1).ToString();
                    }
                    if (category == Category.Type)
                    {
                        switch (selectedColor)
                        {
                            case 0:
                                categoryLabel.label.text = "Room";
                                break;
                            case 1:
                                categoryLabel.label.text = "Gate";
                                break;
                            case 2:
                                categoryLabel.label.text = "Shelter";
                                break;
                            case 3:
                                categoryLabel.label.text = "Swarmroom";
                                break;
                            case 4:
                                categoryLabel.label.text = "Scavenger Trader";
                                break;
                            case 5:
                                categoryLabel.label.text = "Scavenger Outpost";
                                break;
                        }
                    }
                    categoryLabel.label.color = currentCol.rgb;
                }
            }
            else
            {
                categoryLabel.label.text = "Select a button above to configure colors";
                categoryLabel.label.color = Color.white;
            }
        }
    }

    public float ValueOfSlider(Slider slider)
    {
        if (slider == hueSlider)
        {
            return hue;
        }
        else if (slider == satSlider)
        {
            return sat;
        }
        else if (slider == litSlider)
        {
            return lit;
        }
        else
        {
            return 0f;
        }
    }

    public void SliderSetValue(Slider slider, float setValue)
    {
        if (selectedColor != -1)
        {
            if (slider == hueSlider)
            {
                hue = Mathf.Lerp(0f, 0.99f, setValue);
            }
            if (slider == satSlider)
            {
                sat = Mathf.Lerp(0f, 0.99f, setValue);
            }
            if (slider == litSlider)
            {
                lit = Mathf.Lerp(0f, 0.99f, setValue);
            }
        }
    }
}



