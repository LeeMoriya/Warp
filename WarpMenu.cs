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

public class WarpModMenu
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

    public enum Mode
    {
        Warp,
        Stats
    }

    //Persistant Settings
    public static bool showMenu = true;
    public static bool showStats = false;
    public static SortType sortType = SortType.Type;
    public static ViewType viewType = ViewType.Type;
    public static Mode mode = Mode.Warp;

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
        On.RoomSpecificScript.SU_C04StartUp.Update += SU_C04StartUp_Update;
    }

    private static void SU_C04StartUp_Update(On.RoomSpecificScript.SU_C04StartUp.orig_Update orig, RoomSpecificScript.SU_C04StartUp self, bool eu)
    {
        self.showedControls = true;
        orig.Invoke(self, eu);
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
            //New region selected, initiate Region Switcher
            if ((newRegion != null && newRegion.Length == 2 && newRegion != self.activeWorld.region.name))
            {
                RegionSwitcher rs = new RegionSwitcher();
                try
                {
                    rs.SwitchRegions(self.game, newRegion, newRoom, new IntVector2(0, 0));
                }
                catch
                {
                    Debug.Log("Something broke!!");
                }
                warpActive = false;
            }
            //New room in same region selected
            else
            {
                //Get the abstract room
                if (switchRoom == null || switchRoom.name != newRoom)
                {
                    switchRoom = self.game.world.GetAbstractRoom(newRoom);
                }
                //Realise it if its still abstract
                if (switchRoom != null && switchRoom.realizedRoom == null)
                {
                    switchRoom.RealizeRoom(self.game.world, self.game);
                }
                //Move each player to the new room
                if (switchRoom.realizedRoom != null && switchRoom.realizedRoom.ReadyForPlayer && player != null && player.room != switchRoom.realizedRoom)
                {
                    for (int i = 0; i < self.game.Players.Count; i++)
                    {
                        if (self.game.Players[i].realizedCreature.grasps != null)
                        {
                            for (int g = 0; g < self.game.Players[i].realizedCreature.grasps.Length; g++)
                            {
                                if (self.game.Players[i].realizedCreature.grasps[g] != null && self.game.Players[i].realizedCreature.grasps[g].grabbed != null && !self.game.Players[i].realizedCreature.grasps[g].discontinued && self.game.Players[i].realizedCreature.grasps[g].grabbed is Creature)
                                {
                                    self.game.Players[i].realizedCreature.ReleaseGrasp(g);
                                }
                            }
                        }
                        self.game.Players[i].realizedCreature.abstractCreature.Move(switchRoom.realizedRoom.LocalCoordinateOfNode(0));
                        self.game.Players[i].realizedCreature.PlaceInRoom(switchRoom.realizedRoom);
                        self.game.Players[i].realizedCreature.abstractCreature.ChangeRooms(player.room.GetWorldCoordinate(player.mainBodyChunk.pos));
                    }
                }
                //Move player backspears to new room and release creature grasps
                for (int i = 0; i < self.game.Players.Count; i++)
                {
                    if (self.game.Players[i].realizedCreature != null && (self.game.Players[i].realizedCreature as Player).spearOnBack != null)
                    {
                        if ((self.game.Players[i].realizedCreature as Player).spearOnBack.spear != null)
                        {
                            (self.game.Players[i].realizedCreature as Player).spearOnBack.spear.PlaceInRoom(switchRoom.realizedRoom);
                            (self.game.Players[i].realizedCreature as Player).spearOnBack.spear.room = (self.game.Players[i].realizedCreature as Player).room;
                        }
                    }
                }
                //Move players to the first entrance, move the room camera
                if (player != null && player.room == switchRoom.realizedRoom)
                {
                    self.game.cameras[0].virtualMicrophone.AllQuiet();
                    self.game.cameras[0].MoveCamera(player.room, 0);
                    warpActive = false;
                    switchRoom = null;
                }
            }
        }
    }
    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        WarpMod.CheckForMSC();
        if (WarpMod.msc)
        {
            WarpMod.DisableMSCWarp(game.rainWorld);
        }
        WarpSettings.Load();
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
                if (mod.ModID == "Jolly Co-op Mod")
                {
                    WarpMod.jollyCoop = true;
                }
            }
            if (self.controlMap != null && showMenu)
            {
                self.controlMap.pos.y = -2000f;
                self.controlMap.lastPos.y = -2000f;
            }
            if (game.Players[0] != null && game.Players[0].realizedCreature != null)
            {
                Vector2 offset = new Vector2();
                if (!showMenu)
                {
                    offset.y = -2000f;
                }
                warpContainer = new WarpContainer(self, self.pages[0], self.pages[0].pos + offset, new Vector2());
                self.pages[0].subObjects.Add(warpContainer);
                WarpButton toggle = new WarpButton(self, self.pages[0], "HIDE", "toggleWarp", new Vector2(21f, game.rainWorld.options.ScreenSize.y - 72f), new Vector2(45f, 20), new Color(1f, 0f, 0f));
                if (showMenu)
                {
                    toggle.menuLabel.text = "HIDE";
                    toggle.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                }
                else
                {
                    toggle.menuLabel.text = "WARP";
                    toggle.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                }
                self.pages[0].subObjects.Add(toggle);
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
        if(message == "toggleWarp")
        {
            if (showMenu)
            {
                showMenu = false;
                (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                (sender as WarpButton).menuLabel.text = "WARP";
                if(self.controlMap != null)
                {
                    self.controlMap.pos.y = 380f;
                    self.controlMap.lastPos.y = -380f;
                }
            }
            else
            {
                showMenu = true;
                (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                (sender as WarpButton).menuLabel.text = "HIDE";
                if (self.controlMap != null)
                {
                    self.controlMap.pos.y = -2000f;
                    self.controlMap.lastPos.y = -2000f;
                }
            }
        }
        WarpSettings.Save();
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
        public MenuLabel denLabel;
        public WarpStats warpStats;
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
            string title = "Warp Menu - ";
            if (WarpMod.msc) { title = "Warp MSC - "; }
            MenuLabel labelOne = new MenuLabel(menu, this, title + WarpMod.mod.Version, new Vector2(22f, game.rainWorld.options.ScreenSize.y - 20f), new Vector2(), false);
            labelOne.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(labelOne);
            //Den pos
            denLabel = new MenuLabel(menu, this, "Den Position: NONE", new Vector2(game.rainWorld.options.ScreenSize.x- 22f, game.rainWorld.options.ScreenSize.y - 20f), new Vector2(), false);
            denLabel.label.alignment = FLabelAlignment.Right;
            this.subObjects.Add(denLabel);
            //Region Title
            MenuLabel regionLabel = new MenuLabel(menu, this, "REGION LIST", new Vector2(70f, game.rainWorld.options.ScreenSize.y - 40f), new Vector2(), false);
            this.subObjects.Add(regionLabel);
            //Region Buttons
            regionButtons = new List<WarpButton>();
            regOffset = new IntVector2();
            if (game.overWorld.regions != null)
            {
                Color statColor;
                string statText;
                if(mode == Mode.Stats)
                {
                    statColor = new Color(1f, 0.85f, 0f);
                    statText = "STATS";
                }
                else
                {
                    statColor = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    statText = "WARP";
                }
                WarpButton statButton = new WarpButton(menu, this, statText, "STATS", new Vector2(20f + ((hOffset - 25f) * 1), game.rainWorld.options.ScreenSize.y - 72f), new Vector2(45f, 20f), statColor);
                this.subObjects.Add(statButton);
                for (int r = 0; r < this.game.overWorld.regions.Length; r++)
                {
                    WarpButton region = new WarpButton(menu, this, this.game.overWorld.regions[r].name, this.game.overWorld.regions[r].name + "reg", new Vector2(20f + ((hOffset - 45f) * regOffset.x), game.rainWorld.options.ScreenSize.y - 105f - ((vOffset) * regOffset.y)), new Vector2(30f, 23f), new Color(0.8f, 0.8f, 0.8f));
                    regionButtons.Add(region);
                    this.subObjects.Add(region);
                    regOffset.x++;
                    if (regOffset.x == 3)
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
            warpStats = new WarpStats(menu, this, new Vector2(), new Vector2());
            this.subObjects.Add(warpStats);
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
            if (showMenu)
            {
                this.pos.y = 0f;
                this.lastPos.y = 0f;
            }
            else
            {
                this.pos.y = -2000f;
                this.lastPos.y = -2000f;
            }
            if (regionButtons != null)
            {
                if (Input.GetKey(KeyCode.F5))
                {
                    loadAll = true;
                }
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
            if (message == "STATS")
            {
                if(WarpModMenu.mode == Mode.Warp)
                {
                    //Enable Stats mode
                    WarpModMenu.mode = Mode.Stats;
                    (sender as WarpButton).color = new Color(1f, 0.85f, 0f);
                    (sender as WarpButton).menuLabel.text = "STATS";
                    warpStats = new WarpStats(menu, this, new Vector2(), new Vector2());
                    this.subObjects.Add(warpStats);
                    warpStats.GenerateStats(newRegion);
                    menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);
                }
                else
                {
                    //Disable Stats mode
                    WarpModMenu.mode = Mode.Warp;
                    (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    (sender as WarpButton).menuLabel.text = "WARP";
                    if (warpStats != null)
                    {
                        warpStats.RemoveSprites();
                        warpStats = null;
                    }
                    menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);
                }
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
            //Don't generate room buttons if the color customiser is open
            if (warpColor != null)
            {
                return;
            }
            //Remove existing buttons and labels
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
                            if (ColorInfo.customSubregionColors.ContainsKey(WarpModMenu.newRegion))
                            {
                                color = ColorInfo.customSubregionColors[WarpModMenu.newRegion][info.subregion].rgb;
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
                name = Regex.Split(info.name, "_(.+)")[1];
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
                        for (int i = 0; i < WarpModMenu.subregionNames[newRegion].Count; i++)
                        {
                            MenuLabel label = new MenuLabel(menu, this, WarpModMenu.subregionNames[newRegion][i], new Vector2(), new Vector2(), false);
                            label.label.alignment = FLabelAlignment.Left;
                            while(WarpModMenu.subregionNames[newRegion].Count > ColorInfo.customSubregionColors[WarpModMenu.newRegion].Count)
                            {
                                ColorInfo.customSubregionColors[WarpModMenu.newRegion].Add(new HSLColor(1f, 1f, 1f));
                            }
                            label.label.color = ColorInfo.customSubregionColors[WarpModMenu.newRegion][i].rgb;
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
                for (int i = 0; i < WarpModMenu.subregionNames[newRegion].Count; i++)
                {
                    MenuLabel label = new MenuLabel(menu, this, i + " - " + WarpModMenu.subregionNames[newRegion][i], new Vector2(), new Vector2(), false);
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
            if (showStats)
            {
                warpStats.GenerateStats(newRegion);
            }
            else
            {
                warpStats.stats.label.text = "";
                warpStats.stats2.label.text = "";
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
            if (denLabel != null)
            {
                if (denPos == "NONE")
                {
                    denLabel.label.text = "Den Position: " + denPos + " | Hold shift + click a room to assign";
                }
                else
                {
                    denLabel.label.text = "Den Position: " + denPos + " | Press C to clear";
                }
            }
        }
    }
}





