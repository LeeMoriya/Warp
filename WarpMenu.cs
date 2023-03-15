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
using Menu.Remix.MixedUI;
using Menu.Remix;

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

    //Error checking
    public static string warpError = "";

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
        //On.RoomSpecificScript.SU_C04StartUp.Update += SU_C04StartUp_Update;
    }

    private static void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
    {
        orig.Invoke(self);
        WarpOverworldUpdate(self, self.game);
    }

    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        orig.Invoke(self, str, game);
        if (game != null && WarpModMenu.WarpEnabled(game) && WarpModMenu.denPos != "NONE")
        {
            self.denPosition = WarpModMenu.denPos;
        }
    }

    private static void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
    {
        orig.Invoke(self);
        WarpUpdate(self.game, self);
    }

    private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
    {
        WarpSignal(self, self.game, sender, message);
        orig.Invoke(self, sender, message);
    }

    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        WarpPreInit(game);
        orig.Invoke(self, manager, game);
        WarpInit(game, self);
    }

    public static void WarpPreInit(RainWorldGame game)
    {
        if (WarpModMenu.WarpEnabled(game))
        {
            WarpSettings.Load();
            WarpModMenu.warpActive = false;
        }
    }

    public static void WarpInit(RainWorldGame game, PauseMenu self)
    {
        if (WarpModMenu.WarpEnabled(game) && game.IsStorySession)
        {
            if (self.controlMap != null && WarpModMenu.showMenu)
            {
                self.controlMap.pos.y = -2000f;
                self.controlMap.lastPos.y = -2000f;
            }

            if (game.Players.Count > 0 && game.Players[0] != null && game.Players[0].realizedCreature != null && !game.Players[0].realizedCreature.inShortcut)
            {
                Vector2 offset = new Vector2(20f,0f);
                if (!WarpModMenu.showMenu)
                {
                    offset.y = -2000f;
                }
                WarpModMenu.warpContainer = new WarpModMenu.WarpContainer(self, self.pages[0], self.pages[0].pos + offset, new Vector2());
                self.pages[0].subObjects.Add(WarpModMenu.warpContainer);

                WarpButton toggle = new WarpButton(self, self.pages[0], "HIDE", "toggleWarp", new Vector2(41f, game.rainWorld.options.ScreenSize.y - 72f), new Vector2(45f, 20), new Color(1f, 0f, 0f));
                if (WarpModMenu.showMenu)
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

    public static void WarpUpdate(RainWorldGame game, PauseMenu self)
    {
        if (WarpModMenu.WarpEnabled(game))
        {
            self.pages[0].pos.x = 0f;
            if (Input.GetKey(KeyCode.C) && WarpModMenu.denPos != "NONE")
            {
                WarpModMenu.denPos = "NONE";
                WarpModMenu.updateDenText = true;
            }
            if (WarpModMenu.updateRoomButtons)
            {
                WarpModMenu.updateRoomButtons = false;
                self.init = false;
                if (!WarpModMenu.masterRoomList.ContainsKey(WarpModMenu.newRegion))
                {
                    RoomFinder rf = new RoomFinder();
                    List<RoomInfo> roomList = rf.GetRegionInfo(WarpModMenu.newRegion);
                    rf.GetRegionInfo(newRegion);
                    WarpModMenu.warpContainer.GenerateRoomButtons(roomList, WarpModMenu.sortType, WarpModMenu.viewType);
                }
                else
                {
                    WarpModMenu.warpContainer.GenerateRoomButtons(WarpModMenu.masterRoomList[WarpModMenu.newRegion], WarpModMenu.sortType, WarpModMenu.viewType);
                }
                //Stats
                if (WarpModMenu.warpContainer != null && WarpModMenu.warpContainer.warpStats != null)
                {
                    WarpModMenu.warpContainer.warpStats.GenerateStats(WarpModMenu.newRegion, "");
                }
            }
        }
    }

    public static void WarpSignal(PauseMenu self, RainWorldGame game, MenuObject sender, string message)
    {
        if (WarpModMenu.WarpEnabled(game))
        {
            if (message.EndsWith("warp"))
            {
                string room = message.Remove(message.Length - 4, 4);
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    WarpModMenu.denPos = room;
                    self.PlaySound(SoundID.MENU_Player_Join_Game);
                    WarpModMenu.updateDenText = true;
                }
                else
                {
                    //if (WarpModMenu.mode == Mode.Warp)
                    //{
                    WarpModMenu.newRoom = room;
                    WarpModMenu.warpActive = true;
                    Debug.Log("WARP: Room warp initiated for: " + room);
                    self.Singal(null, "CONTINUE");
                    //}
                    //else
                    //{
                    //    if(WarpModMenu.warpContainer.warpStats != null)
                    //    {
                    //        WarpModMenu.warpContainer.warpStats.RemoveSprites();
                    //        WarpModMenu.warpContainer.RemoveSubObject(WarpModMenu.warpContainer.warpStats);
                    //    }
                    //    WarpModMenu.warpContainer.warpStats = new WarpStats(self, WarpModMenu.warpContainer, new Vector2(), new Vector2());
                    //    WarpModMenu.warpContainer.warpStats.GenerateStats(newRegion, room);
                    //    WarpModMenu.warpContainer.subObjects.Add(WarpModMenu.warpContainer.warpStats);
                    //}
                }
                WarpSettings.Save();
            }
            else if (message.EndsWith("reg"))
            {
                WarpModMenu.newRegion = message.Remove(message.Length - 3, 3);
                WarpModMenu.warpActive = false;
                Debug.Log("WARP: Loading room list for: " + WarpModMenu.newRegion);
                self.PlaySound(SoundID.MENU_Add_Level);
                WarpModMenu.updateRoomButtons = true;
                WarpSettings.Save();
            }
            else if (message == "toggleWarp")
            {
                if (WarpModMenu.showMenu)
                {
                    WarpModMenu.showMenu = false;
                    (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                    (sender as WarpButton).menuLabel.text = "WARP";
                    if (self.controlMap != null)
                    {
                        self.controlMap.pos.y = 380f;
                        self.controlMap.lastPos.y = -380f;
                    }
                }
                else
                {
                    WarpModMenu.showMenu = true;
                    (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    (sender as WarpButton).menuLabel.text = "HIDE";
                    if (self.controlMap != null)
                    {
                        self.controlMap.pos.y = -2000f;
                        self.controlMap.lastPos.y = -2000f;
                    }
                }
                WarpSettings.Save();
            }
        }
    }

    public static void WarpOverworldUpdate(OverWorld self, RainWorldGame game)
    {
        if (WarpModMenu.WarpEnabled(game))
        {
            Player player = (self.game.Players.Count <= 0) ? null : (self.game.Players[0].realizedCreature as Player);
            if (WarpModMenu.warpActive)
            {
                if (player == null || player.inShortcut)
                {
                    return;
                }
                //New region selected, initiate Region Switcher
                if ((WarpModMenu.newRegion != null && WarpModMenu.newRegion.Length == 2 && WarpModMenu.newRegion != self.activeWorld.region.name))
                {
                    RegionSwitcher rs = new RegionSwitcher();
                    try
                    {
                        WarpModMenu.warpError = "";
                        rs.SwitchRegions(self.game, WarpModMenu.newRegion, WarpModMenu.newRoom, new IntVector2(0, 0));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.Log("WARP ERROR: " + rs.GetErrorText(rs.error));
                        WarpModMenu.warpError = rs.GetErrorText(rs.error);
                        self.game.pauseMenu = new Menu.PauseMenu(self.game.manager, self.game);

                    }
                    WarpModMenu.warpActive = false;
                }
                //New room in same region selected
                else
                {
                    //Get the abstract room
                    if (WarpModMenu.switchRoom == null || WarpModMenu.switchRoom.name != WarpModMenu.newRoom)
                    {
                        WarpModMenu.switchRoom = self.game.world.GetAbstractRoom(WarpModMenu.newRoom);
                    }
                    //Realise it if its still abstract
                    if (WarpModMenu.switchRoom != null && WarpModMenu.switchRoom.realizedRoom == null)
                    {
                        WarpModMenu.switchRoom.RealizeRoom(self.game.world, self.game);
                    }
                    //Move each player to the new room
                    if (WarpModMenu.switchRoom.realizedRoom != null && WarpModMenu.switchRoom.realizedRoom.ReadyForPlayer && player != null && player.room != WarpModMenu.switchRoom.realizedRoom)
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
                            self.game.Players[i].realizedCreature.abstractCreature.Move(WarpModMenu.switchRoom.realizedRoom.LocalCoordinateOfNode(0));
                            self.game.Players[i].realizedCreature.PlaceInRoom(WarpModMenu.switchRoom.realizedRoom);
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
                                (self.game.Players[i].realizedCreature as Player).spearOnBack.spear.PlaceInRoom(WarpModMenu.switchRoom.realizedRoom);
                                (self.game.Players[i].realizedCreature as Player).spearOnBack.spear.room = (self.game.Players[i].realizedCreature as Player).room;
                            }
                        }
                    }
                    //Move players to the first entrance, move the room camera
                    if (player != null && player.room == WarpModMenu.switchRoom.realizedRoom)
                    {
                        self.game.cameras[0].virtualMicrophone.AllQuiet();
                        self.game.cameras[0].MoveCamera(player.room, 0);
                        WarpModMenu.warpActive = false;
                        WarpModMenu.switchRoom = null;
                    }
                }
            }
        }
    }

    public static bool WarpEnabled(RainWorldGame game)
    {
        return game.IsStorySession && (!ModManager.MSC || !game.rainWorld.safariMode);
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
        public WarpButton pageLeft;
        public WarpButton pageRight;
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
        public WarpButton statButton;
        public WarpStats warpStats;
        public MenuLabel errorMessage;
        public bool debugMode = false;

        public MenuTabWrapper tabWrapper;
        public OpComboBox regionDropdown;
        public Configurable<string> currentRegion;

        public WarpContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            game = (menu as PauseMenu).game;
            newRegion = game.world.region.name;
            //try
            //{
            //    ColorInfo.Load();
            //}
            //catch
            //{
            //    Debug.Log("WARP: Error loading color info!");
            //}
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
            string title = "Warp Menu";// + " - v1.7";
            MenuLabel labelOne = new MenuLabel(menu, this, title, new Vector2(35f, game.rainWorld.options.ScreenSize.y - 20f), new Vector2(), false);
            labelOne.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(labelOne);
            //Den pos
            denLabel = new MenuLabel(menu, this, "Den Position: NONE", new Vector2(game.rainWorld.options.ScreenSize.x - 22f, game.rainWorld.options.ScreenSize.y - 20f), new Vector2(), false);
            denLabel.label.alignment = FLabelAlignment.Right;
            this.subObjects.Add(denLabel);
            //Region Title
            MenuLabel regionLabel = new MenuLabel(menu, this, "REGION LIST", new Vector2(70f, game.rainWorld.options.ScreenSize.y - 40f), new Vector2(), false);
            this.subObjects.Add(regionLabel);

            currentRegion = new Configurable<string>(newRegion);
            List<ListItem> regs = new List<ListItem>();
            foreach (Region reg in game.overWorld.regions)
            {
                regs.Add(new ListItem(reg.name, Region.GetRegionFullName(reg.name, game.StoryCharacter)));
            };

            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            regionDropdown = new OpComboBox(currentRegion, new Vector2(0f, regionLabel.pos.y - 65f), 150f, regs);
            UIelementWrapper wrapper = new UIelementWrapper(tabWrapper, regionDropdown);

            //TODO - Events for dropdown

            //Region Buttons
            if (game.overWorld.regions != null)
            {
                Color statColor;
                string statText;
                if (mode == Mode.Stats)
                {
                    statColor = new Color(1f, 0.85f, 0f);
                    statText = "STATS";
                }
                else
                {
                    statColor = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    //statText = "WARP";
                    statText = "STATS";
                }
                this.statButton = new WarpButton(menu, this, statText, "STATS", new Vector2(20f + ((hOffset - 25f) * 1), game.rainWorld.options.ScreenSize.y - 72f), new Vector2(45f, 20f), statColor);
                this.subObjects.Add(statButton);
                float regionHeight = regionDropdown.PosY;
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
                List<RoomInfo> roomList = rf.GetRegionInfo(game.world.region.name);
                GenerateRoomButtons(roomList, sortType, viewType);
            }
            else
            {
                GenerateRoomButtons(masterRoomList[game.world.region.name], sortType, viewType);
            }
            if (mode == Mode.Stats)
            {
                warpStats = new WarpStats(menu, this, new Vector2(), new Vector2());
                warpStats.GenerateStats(newRegion, "");
                this.subObjects.Add(warpStats);
            }
            if (warpError != "")
            {
                this.errorMessage = new MenuLabel(menu, this, warpError, new Vector2(682f, 72f), new Vector2(), true);
                this.errorMessage.label.color = new Color(1f, 0f, 0f);
                this.subObjects.Add(errorMessage);
            }
        }

        public override void Update()
        {
            base.Update();
            if (warpError != "")
            {
                this.menu.PlaySound(SoundID.HUD_Game_Over_Prompt);
                warpError = "";
            }
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
            if (message == "PAGELEFT")
            {
                for (int i = 0; i < this.roomButtons.Count; i++)
                {
                    roomButtons[i].pos.x -= 3042f;
                }
                for (int i = 0; i < categoryLabels.Count; i++)
                {
                    categoryLabels[i].label.SetPosition(categoryLabels[i].label.x - 2000f, categoryLabels[i].label.y);
                }
                pageLeft.buttonBehav.greyedOut = true;
                pageLeft.color = new Color(0.3f, 0.3f, 0.3f);
                for (int i = 0; i < this.subObjects.Count; i++)
                {
                    if (this.subObjects[i] is WarpButton && (this.subObjects[i] as WarpButton).signalText == "PAGERIGHT")
                    {
                        pageRight.buttonBehav.greyedOut = false;
                        pageRight.color = new Color(1f, 0.9f, 0.3f);
                    }
                }
            }
            if (message == "PAGERIGHT")
            {
                for (int i = 0; i < this.roomButtons.Count; i++)
                {
                    roomButtons[i].pos.x += 3042f;
                }
                for (int i = 0; i < categoryLabels.Count; i++)
                {
                    categoryLabels[i].label.SetPosition(categoryLabels[i].label.x + 2000f, categoryLabels[i].label.y);
                }
                pageRight.buttonBehav.greyedOut = true;
                pageRight.color = new Color(0.3f, 0.3f, 0.3f);
                for (int i = 0; i < this.subObjects.Count; i++)
                {
                    if (this.subObjects[i] is WarpButton && (this.subObjects[i] as WarpButton).signalText == "PAGELEFT")
                    {
                        pageLeft.buttonBehav.greyedOut = false;
                        pageLeft.color = new Color(1f, 0.9f, 0.3f);
                    }
                }
            }
            if (message == "LOADALL")
            {
                loadAll = true;
            }
            if (message == "STATS")
            {
                if (WarpModMenu.mode == Mode.Warp)
                {
                    //Enable Stats mode
                    if (warpStats != null)
                    {
                        this.RemoveSubObject(warpStats);
                        warpStats.RemoveSprites();
                        warpStats = null;
                    }
                    WarpModMenu.mode = Mode.Stats;
                    (sender as WarpButton).color = new Color(1f, 0.85f, 0f);
                    warpStats = new WarpStats(menu, this, new Vector2(), new Vector2());
                    warpStats.GenerateStats(newRegion, "");
                    this.subObjects.Add(warpStats);
                    //(sender as WarpButton).menuLabel.text = "STATS";
                    menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);
                }
                else
                {
                    //Disable Stats mode
                    WarpModMenu.mode = Mode.Warp;
                    (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    //(sender as WarpButton).menuLabel.text = "WARP";
                    if (warpStats != null)
                    {
                        this.RemoveSubObject(warpStats);
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
                    if (warpStats != null)
                    {
                        this.RemoveSubObject(warpStats);
                        warpStats.RemoveSprites();
                        warpStats = null;
                    }
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
                    List<RoomInfo> roomList = rf.GetRegionInfo(newRegion);
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
                    List<RoomInfo> roomList = rf.GetRegionInfo(game.world.region.name);
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
            try
            {
                //Don't generate room buttons if the color customiser is open
                if (warpColor != null)
                {
                    return;
                }
                if (pageLeft != null || pageRight != null)
                {
                    this.pageLeft.RemoveSprites();
                    this.RemoveSubObject(pageLeft);
                    pageLeft = null;
                    this.pageRight.RemoveSprites();
                    this.RemoveSubObject(pageRight);
                    pageRight = null;
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
                bool largeRegion = false;
                for (int i = 0; i < roomButtons.Count; i++)
                {
                    if (roomButtons[i].pos.x < 250f)
                    {
                        largeRegion = true;
                        roomButtons[i].pos.x -= 2000f;
                    }
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
                                while (WarpModMenu.subregionNames[newRegion].Count > ColorInfo.customSubregionColors[WarpModMenu.newRegion].Count)
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
                //TODO - this doesn't work
                for (int i = 0; i < categoryLabels.Count; i++)
                {
                    if (categoryLabels[i].label.x < 250f)
                    {
                        categoryLabels[i].label.SetPosition(categoryLabels[i].label.x - 2000f, categoryLabels[i].label.y);
                    }
                    this.subObjects.Add(categoryLabels[i]);
                }
                if (largeRegion && (pageLeft == null || pageRight == null))
                {
                    pageLeft = new WarpButton(menu, this, "LEFT", "PAGELEFT", new Vector2(screenWidth - 135f, 55f), new Vector2(50f, 25f), new Color(0.3f, 0.3f, 0.3f));
                    pageRight = new WarpButton(menu, this, "RIGHT", "PAGERIGHT", new Vector2(screenWidth - 80f, 55f), new Vector2(50f, 25f), new Color(1f, 0.9f, 0f));
                    pageLeft.buttonBehav.greyedOut = true;
                    this.subObjects.Add(pageLeft);
                    this.subObjects.Add(pageRight);
                }
            }
            catch
            {
                Debug.Log("There was an error while generating the room buttons for this region.");
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





