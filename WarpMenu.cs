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
    public static bool dropdownMode = true;
    public static bool alphabetical = true;
    public static HashSet<string> favourites = new HashSet<string>();
    public static SortType sortType = SortType.Type;
    public static ViewType viewType = ViewType.Type;
    public static Mode mode = Mode.Warp;

    //Master Room Dictionary
    public static Dictionary<string, List<RoomInfo>> masterRoomList = new Dictionary<string, List<RoomInfo>>();
    public static Dictionary<string, List<string>> subregionNames = new Dictionary<string, List<string>>();
    public static WarpContainer warpContainer;
    public static RoomPreview roomPreview;

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
    public static IntVector2 coords = new IntVector2(-1, -1);

    public static void MenuHook()
    {
        On.Menu.PauseMenu.ctor += PauseMenu_ctor;
        On.Menu.PauseMenu.Singal += PauseMenu_Singal;
        On.Menu.PauseMenu.Update += PauseMenu_Update;
        On.OverWorld.Update += OverWorld_Update;
        On.SaveState.LoadGame += SaveState_LoadGame;
        On.Menu.TutorialControlsPage.GrafUpdate += TutorialControlsPage_GrafUpdate;
        On.RainWorldGame.ContinuePaused += RainWorldGame_ContinuePaused;
    }

    private static void RainWorldGame_ContinuePaused(On.RainWorldGame.orig_ContinuePaused orig, RainWorldGame self)
    {
        if (roomPreview != null)
        {
            if (self.pauseMenu != null)
            {
                self.pauseMenu.wantToContinue = false;
            }
            return;
        }
        orig.Invoke(self);
    }

    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        orig.Invoke(self, str, game);
        if (denPos != "NONE")
        {
            self.denPosition = denPos;
            self.lastVanillaDen = self.denPosition;
        }
    }

    private static void TutorialControlsPage_GrafUpdate(On.Menu.TutorialControlsPage.orig_GrafUpdate orig, TutorialControlsPage self, float timeStacker)
    {
        orig.Invoke(self, timeStacker);
        if (warpActive)
        {
            self.ShutDownProcess();
        }
    }

    private static void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
    {
        orig.Invoke(self);
        WarpOverworldUpdate(self, self.game);
    }

    private static void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
    {
        if (roomPreview == null)
        {
            orig.Invoke(self);
        }
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
        if (WarpEnabled(game))
        {
            WarpSettings.Load();
            warpActive = false;
        }
    }

    public static void WarpInit(RainWorldGame game, PauseMenu self)
    {
        if (WarpEnabled(game) && game.IsStorySession)
        {
            if (self.controlMap != null && showMenu)
            {
                self.controlMap.pos.y = -2000f;
                self.controlMap.lastPos.y = -2000f;
            }

            if (game.Players.Count > 0 && game.Players[0] != null && game.Players[0].realizedCreature != null && !game.Players[0].realizedCreature.inShortcut)
            {
                Vector2 offset = new Vector2(20f, 0f);
                if (!showMenu)
                {
                    offset.y = -2000f;
                }
                warpContainer = new WarpContainer(self, self.pages[0], self.pages[0].pos + offset, new Vector2());
                self.pages[0].subObjects.Add(warpContainer);

                WarpButton toggle = new WarpButton(self, self.pages[0], "HIDE", "toggleWarp", new Vector2(41f, game.rainWorld.options.ScreenSize.y - 72f), new Vector2(45f, 20), new Color(1f, 0f, 0f));
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

    public static void WarpUpdate(RainWorldGame game, PauseMenu self)
    {
        if (WarpEnabled(game))
        {
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
                    List<RoomInfo> roomList = rf.GetRegionInfo(newRegion);
                    rf.GetRegionInfo(newRegion);
                    warpContainer.GenerateRoomButtons(roomList, sortType, viewType);
                }
                else
                {
                    warpContainer.GenerateRoomButtons(masterRoomList[newRegion], sortType, viewType);
                }
                //Stats
                if (warpContainer != null && warpContainer.warpStats != null)
                {
                    warpContainer.warpStats.GenerateStats(newRegion, "");
                }
            }
        }
    }

    public static void WarpSignal(PauseMenu self, RainWorldGame game, MenuObject sender, string message)
    {
        if (message == "CONTINUE")
        {
            if (roomPreview != null)
            {
                roomPreview.bg?.RemoveFromContainer();
                roomPreview.previewSprite?.RemoveFromContainer();
                roomPreview.instructions?.RemoveFromContainer();
            }
        }
        if (warpActive)
        {
            return;
        }
        if (WarpEnabled(game))
        {
            if (message.EndsWith("warp"))
            {
                string room = message.Remove(message.Length - 4, 4);
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    denPos = room;
                    //RainWorldGame.ForceSaveNewDenLocation(game, denPos, false);
                    self.PlaySound(SoundID.MENU_Player_Join_Game);
                    updateDenText = true;
                }
                else
                {
                    newRoom = room;
                    warpActive = true;
                    Debug.Log("WARP: Room warp initiated for: " + room);
                    self.Singal(null, "CONTINUE");
                }
            }
            else if (message.EndsWith("reg"))
            {
                newRegion = message.Remove(message.Length - 3, 3);
                warpActive = false;
                Debug.Log("WARP: Loading room list for: " + newRegion);
                self.PlaySound(SoundID.MENU_Add_Level);
                updateRoomButtons = true;
            }
            else if (message == "toggleWarp")
            {
                if (showMenu)
                {
                    showMenu = false;
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
    }

    public static void WarpOverworldUpdate(OverWorld self, RainWorldGame game)
    {
        if (WarpEnabled(game))
        {
            Player player = (self.game.AlivePlayers.Count <= 0) ? null : (self.game.AlivePlayers[0].realizedCreature as Player);
            if (warpActive)
            {
                if (player == null || player.inShortcut)
                {
                    return;
                }
                //New region selected, initiate Region Switcher
                if ((newRegion != null && newRegion != self.activeWorld.region.name))
                {
                    RegionSwitcher rs = new RegionSwitcher();
                    try
                    {
                        warpError = "";
                        rs.SwitchRegions(self.game, newRegion, newRoom, new IntVector2(0, 0));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.Log("WARP ERROR: " + rs.GetErrorText(rs.error));
                        warpError = rs.GetErrorText(rs.error);
                        self.game.pauseMenu = new Menu.PauseMenu(self.game.manager, self.game);
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
                    if (switchRoom.realizedRoom != null && switchRoom.realizedRoom.ReadyForPlayer && player != null)
                    {
                        if (player.room != switchRoom.realizedRoom)
                        {
                            for (int i = 0; i < self.game.AlivePlayers.Count; i++)
                            {
                                if (self.game.AlivePlayers[i].realizedCreature.grasps != null)
                                {
                                    //Checking all grasps
                                    for (int g = 0; g < self.game.AlivePlayers[i].realizedCreature.grasps.Length; g++)
                                    {
                                        //If it's a creature, let it go
                                        if (self.game.AlivePlayers[i].realizedCreature.grasps[g] != null && self.game.AlivePlayers[i].realizedCreature.grasps[g].grabbed != null && !self.game.AlivePlayers[i].realizedCreature.grasps[g].discontinued && (self.game.AlivePlayers[i].realizedCreature.grasps[g].grabbed is Creature))
                                        {
                                            //But only if its not another Player/Slugpup
                                            if (!(self.game.AlivePlayers[i].realizedCreature.grasps[g].grabbed is Player && (self.game.AlivePlayers[i].realizedCreature.grasps[g].grabbed as Player).isSlugpup))
                                            {
                                                self.game.AlivePlayers[i].realizedCreature.ReleaseGrasp(g);
                                            }
                                        }
                                    }
                                }
                                self.game.AlivePlayers[i].realizedCreature.abstractCreature.Move(coords != new IntVector2(-1, -1) ? new WorldCoordinate(switchRoom.index, coords.x, coords.y, -1) : switchRoom.realizedRoom.LocalCoordinateOfNode(0));
                                self.game.AlivePlayers[i].realizedCreature.PlaceInRoom(switchRoom.realizedRoom);
                                self.game.AlivePlayers[i].realizedCreature.abstractCreature.ChangeRooms(player.room.GetWorldCoordinate(player.mainBodyChunk.pos));
                                coords = new IntVector2(-1, -1);
                            }
                        }
                        else if (coords != new IntVector2(-1, -1))
                        {
                            for (int i = 0; i < self.game.AlivePlayers.Count; i++)
                            {
                                for (int s = 0; s < self.game.AlivePlayers[i].realizedCreature.bodyChunks.Length; s++)
                                {
                                    self.game.AlivePlayers[i].realizedCreature.bodyChunks[s].pos = self.game.AlivePlayers[i].Room.realizedRoom.MiddleOfTile(coords.x, coords.y);
                                    self.game.AlivePlayers[i].realizedCreature.bodyChunks[s].lastPos = self.game.AlivePlayers[i].realizedCreature.bodyChunks[s].pos;
                                }
                            }
                            warpActive = false;
                            switchRoom = null;
                            return;
                        }
                    }
                    //Move player backspears to new room
                    for (int i = 0; i < self.game.AlivePlayers.Count; i++)
                    {
                        if (self.game.AlivePlayers[i].realizedCreature != null && (self.game.AlivePlayers[i].realizedCreature as Player).spearOnBack != null)
                        {
                            if ((self.game.AlivePlayers[i].realizedCreature as Player).spearOnBack.spear != null)
                            {
                                (self.game.AlivePlayers[i].realizedCreature as Player).spearOnBack.spear.PlaceInRoom(switchRoom.realizedRoom);
                                (self.game.AlivePlayers[i].realizedCreature as Player).spearOnBack.spear.room = (self.game.AlivePlayers[i].realizedCreature as Player).room;
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
        public WarpButton pageLeft;
        public WarpButton pageRight;
        public List<WarpButton> sortButtons;
        public List<WarpButton> viewButtons;
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
        public MenuLabel filterLabel;
        public List<WarpButton> regionButtons;
        public bool debugMode = false;

        public MenuTabWrapper tabWrapper;
        public OpComboBox regionDropdown;
        public Configurable<string> currentRegion;
        public float dropOffset;
        public int counter = 0;

        public WarpSymbolButton listToggle;
        public WarpSymbolButton colorToggle;
        public WarpSymbolButton favToggle;

        public string roomPreview;
        public int previewCounter;
        public bool previewVisible;

        public WarpContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            WarpSettings.LoadFavourites();
            game = (menu as PauseMenu).game;
            newRegion = game.world.region.name;
            try
            {
                ColorInfo.Load();
            }
            catch
            {
                ColorInfo.Wipe();
                Debug.Log("WARP: Error loading color info!");
                (menu as PauseMenu).wantToContinue = true;
            }
            bg = new FSprite("LinearGradient200", true);
            bg.color = new Color(0.01f, 0.01f, 0.01f);
            bg.SetAnchor(1f, 0f);
            bg.rotation = 90f;
            bg.scaleY = 2f;
            bg.scaleX = 1250f;
            bg.alpha = 0.5f;
            Container.AddChild(bg);
            float hOffset = 80f;
            //Menu Text
            string title = "Warp Menu";// + " - v1.7";
            MenuLabel labelOne = new MenuLabel(menu, this, title, new Vector2(70f, game.rainWorld.options.ScreenSize.y - 39f), new Vector2(), false);
            labelOne.label.alignment = FLabelAlignment.Center;
            labelOne.label.color = new Color(0.33f, 0.33f, 0.33f);
            subObjects.Add(labelOne);
            //Den pos
            denLabel = new MenuLabel(menu, this, "Den Position: NONE", new Vector2(game.rainWorld.options.ScreenSize.x - 42f, game.rainWorld.options.ScreenSize.y - 20f), new Vector2(), false);
            denLabel.label.alignment = FLabelAlignment.Right;
            subObjects.Add(denLabel);

            currentRegion = new Configurable<string>(newRegion);
            List<ListItem> regs = GetRegionListItems(game.overWorld.regions);

            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            regionDropdown = new OpComboBox(currentRegion, new Vector2(0f, labelOne.pos.y - 65f), 150f, regs);
            regionDropdown.listHeight = 29;
            regionDropdown.OnListOpen += RegionDropdown_OnListOpen;
            regionDropdown.OnListClose += RegionDropdown_OnListClose;
            regionDropdown.OnValueChanged += RegionDropdown_OnValueChanged;
            UIelementWrapper wrapper = new UIelementWrapper(tabWrapper, regionDropdown);

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
                statButton = new WarpButton(menu, this, statText, "STATS", new Vector2(20f + ((hOffset - 25f) * 1), game.rainWorld.options.ScreenSize.y - 72f), new Vector2(45f, 20f), statColor);
                subObjects.Add(statButton);

                if (!dropdownMode)
                {
                    regionDropdown.Hide();
                    GenerateRegionButtons();
                }

                float regionHeight = (dropdownMode ? regionDropdown.PosY : regionButtons.Last().pos.y) - 60f;

                listToggle = new WarpSymbolButton(menu, this, "warpList", "TOGGLE", new Vector2(22f, regionHeight + 20f));
                listToggle.roundedRect.size = new Vector2(30f, 30f);
                listToggle.size = listToggle.roundedRect.size;
                subObjects.Add(listToggle);

                colorToggle = new WarpSymbolButton(menu, this, "warpColor", "COLORS", new Vector2(57f, regionHeight + 20f));
                colorToggle.roundedRect.size = new Vector2(30f, 30f);
                colorToggle.size = colorToggle.roundedRect.size;
                subObjects.Add(colorToggle);

                favToggle = new WarpSymbolButton(menu, this, "warpFav", "FAV", new Vector2(92f, regionHeight + 20f));
                favToggle.roundedRect.size = new Vector2(30f, 30f);
                favToggle.size = favToggle.roundedRect.size;
                subObjects.Add(favToggle);


                filterLabel = new MenuLabel(menu, this, "SORT     |     VIEW", new Vector2(72f, regionHeight - 15f), new Vector2(), false);
                subObjects.Add(filterLabel);

                //Sort By Buttons
                sortButtons = new List<WarpButton>
                {
                    new WarpButton(menu, this, "TYPE", "STYPE", new Vector2(22f, regionHeight - 50f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)),
                    new WarpButton(menu, this, "SIZE", "SSIZE", new Vector2(22f, regionHeight - 80f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)),
                    new WarpButton(menu, this, "SUB", "SSUB", new Vector2(22f, regionHeight - 110f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f))
                };
                for (int i = 0; i < sortButtons.Count; i++)
                {
                    subObjects.Add(sortButtons[i]);
                }
                //View By Buttons
                viewButtons = new List<WarpButton>
                {
                    new WarpButton(menu, this, "TYPE", "VTYPE", new Vector2(77f, regionHeight - 50f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)),
                    new WarpButton(menu, this, "SIZE", "VSIZE", new Vector2(77f, regionHeight - 80f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f)),
                    new WarpButton(menu, this, "SUB", "VSUB", new Vector2(77f, regionHeight - 110f), new Vector2(45f, 23f), new Color(0.9f, 0.9f, 0.9f))
                };
                for (int i = 0; i < viewButtons.Count; i++)
                {
                    subObjects.Add(viewButtons[i]);
                }
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
                warpStats = new WarpStats(menu, this);
                warpStats.GenerateStats(game.world.region.name, "");
                subObjects.Add(warpStats);
            }
            if (warpError != "")
            {
                errorMessage = new MenuLabel(menu, this, warpError, new Vector2(682f, 72f), new Vector2(), true);
                errorMessage.label.color = new Color(1f, 0f, 0f);
                subObjects.Add(errorMessage);
            }
            UpdateSettingPositions();
        }

        private List<ListItem> GetRegionListItems(Region[] regs)
        {
            Dictionary<string, string> regions = new Dictionary<string, string>();
            foreach (Region r in regs)
            {
                string name = favourites.Contains(r.name) ? $"> {Region.GetRegionFullName(r.name, game.StoryCharacter)}" : Region.GetRegionFullName(r.name, game.StoryCharacter);
                if (name == "Unknown Region") { name = r.name; }
                regions.Add(r.name, name);
            }
            var regList = regions.OrderBy(x => x.Value).ToList();
            List<ListItem> regionsList = new List<ListItem>();
            for (int i = 0; i < regList.Count; i++)
            {
                ListItem item = new ListItem(regList[i].Key, regList[i].Value, i);
                regionsList.Add(item);
            }
            return regionsList;
        }

        private void RegionDropdown_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            newRegion = value;
            if (!masterRoomList.ContainsKey(value))
            {
                RoomFinder rf = new RoomFinder();
                List<RoomInfo> roomList = rf.GetRegionInfo(value);
                GenerateRoomButtons(roomList, sortType, viewType);
            }
            else
            {
                GenerateRoomButtons(masterRoomList[value], sortType, viewType);
            }
            warpStats?.GenerateStats(value, "");
        }

        private void RegionDropdown_OnListClose(UIfocusable trigger)
        {
            dropOffset = 0f;
            UpdateSettingPositions();
        }

        private void RegionDropdown_OnListOpen(UIfocusable trigger)
        {
            dropOffset = 1500f;
            UpdateSettingPositions();
        }

        public void UpdateSettingPositions()
        {
            if (previewVisible)
            {
                return;
            }
            if (filterLabel != null)
            {
                filterLabel.pos.y = dropdownMode ? regionDropdown.PosY - (55f + dropOffset) : regionDropdown.PosY - (65f + dropOffset);
                filterLabel.lastPos = filterLabel.pos;

                listToggle.pos.y = filterLabel.pos.y + 15f;
                listToggle.lastPos.y = listToggle.pos.y;
                colorToggle.pos.y = listToggle.pos.y;
                colorToggle.lastPos.y = colorToggle.pos.y;
                favToggle.pos.y = colorToggle.pos.y;
                favToggle.lastPos.y = favToggle.pos.y;
            }

            if (sortButtons != null)
            {
                for (int i = 0; i < sortButtons.Count; i++)
                {
                    sortButtons[i].pos.y = filterLabel.pos.y - 35f - (30f * i);
                    sortButtons[i].lastPos = sortButtons[i].pos;
                }
            }

            if (viewButtons != null)
            {
                for (int i = 0; i < viewButtons.Count; i++)
                {
                    viewButtons[i].pos.y = filterLabel.pos.y - 35f - (30f * i);
                    viewButtons[i].lastPos = viewButtons[i].pos;
                }
            }

            if (keyLabel != null)
            {
                keyLabel.pos.y = viewButtons.Last().pos.y - 15f;
                keyLabel.lastPos = keyLabel.pos;
            }
            if (colorKey != null)
            {
                for (int i = 0; i < colorKey.Count; i++)
                {
                    colorKey[i].pos.y = keyLabel.pos.y - (viewType == ViewType.Size ? 15 : (15f + (15f * i)));
                    colorKey[i].lastPos = colorKey[i].pos;
                }
            }
            if (sortType == SortType.Subregion && viewType != ViewType.Subregion)
            {
                if (subLabel != null && subregionLabels != null && subregionLabels.Count > 0)
                {
                    subLabel.pos.y = colorKey.Last().pos.y - 25f;
                    subLabel.lastPos.y = subLabel.pos.y;
                    for (int i = 0; i < subregionLabels.Count; i++)
                    {
                        subregionLabels[i].pos.y = subLabel.pos.y - 20f - (15f * i);
                        subregionLabels[i].lastPos.y = subregionLabels[i].pos.y;
                    }
                }
            }

            if (warpStats != null)
            {
                if (regionButtons != null || (subregionLabels != null && subregionLabels.Count > 0 && subregionLabels.Last().pos.y < 320f))
                {
                    if (regionButtons != null)
                    {
                        warpStats.pos.x = 110f;
                    }
                    else
                    {
                        warpStats.pos.x = 160f;
                    }
                    warpStats.pos.y = 0f;
                    warpStats.lastPos = warpStats.pos;
                    warpStats.stats.pos.y = statButton.pos.y + 20f;
                    warpStats.stats.lastPos.y = warpStats.stats.pos.y;
                }
                else
                {
                    warpStats.pos.x = 0f;
                    warpStats.pos.y = -dropOffset;
                    warpStats.lastPos = warpStats.pos;
                    if (subLabel != null && subregionLabels != null && subregionLabels.Count > 0)
                    {
                        warpStats.stats.pos.y = subregionLabels.Last().pos.y - 20f;
                    }
                    else
                    {
                        warpStats.stats.pos.y = colorKey.Last().pos.y - 20f;
                    }
                    warpStats.stats.lastPos.y = warpStats.stats.pos.y;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (warpError != "")
            {
                menu.PlaySound(SoundID.HUD_Game_Over_Prompt);
                warpError = "";
            }
            if (showMenu)
            {
                pos.y = 0f;
                lastPos.y = 0f;
            }
            else
            {
                pos.y = -2000f;
                lastPos.y = -2000f;
            }
            counter++;

            if (roomButtons != null)
            {
                if (newRegion == game.cameras[0].room.world.name)
                {
                    for (int i = 0; i < roomButtons.Count; i++)
                    {
                        if (roomButtons[i].menuLabel.text == game.cameras[0].room.abstractRoom.name.Split(new char[] { '_' }, 2)[1])
                        {
                            roomButtons[i].color = new HSLColor(1f, 0f, 0.5f + Mathf.Sin(counter / 10f) * 0.4f).rgb;
                        }
                        else
                        {
                            roomButtons[i].color = roomButtons[i].defaultColor;
                        }
                    }
                }
            }
            if (regionButtons != null && regionButtons.Count > 0)
            {
                for (int i = 0; i < regionButtons.Count; i++)
                {
                    string text = regionButtons[i].menuLabel.text;
                    if (masterRoomList.ContainsKey(text))
                    {
                        if (favourites.Contains(text))
                        {
                            regionButtons[i].color = new Color(1f, 0.3f, 0.3f);
                        }
                        else
                        {
                            regionButtons[i].color = new Color(0.8f, 0.8f, 0.8f);
                        }
                    }
                    else
                    {
                        if (favourites.Contains(text))
                        {
                            regionButtons[i].color = new Color(0.45f, 0.35f, 0.35f);
                        }
                        else
                        {
                            regionButtons[i].color = new Color(0.35f, 0.35f, 0.35f);
                        }
                    }
                }
            }
            if (favourites != null)
            {
                if (favourites.Contains(newRegion))
                {
                    if (favToggle.color != new Color(1f, 0.3f, 0.3f))
                    {
                        favToggle.color = new Color(1f, 0.3f, 0.3f);
                    }
                }
                else
                {
                    if (favToggle.color != new Color(0.7f, 0.7f, 0.7f))
                    {
                        favToggle.color = new Color(0.7f, 0.7f, 0.7f);
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
            for (int i = 0; i < roomButtons.Count; i++)
            {
                if (roomButtons[i].Selected || roomButtons[i].IsMouseOverMe)
                {
                    if (menu is PauseMenu)
                    {
                        (menu as PauseMenu).infoLabel.text = "Left click: Warp    -    Right click: Preview";
                        (menu as PauseMenu).infoLabel.alpha = 1f;
                    }
                }
            }
            //Room Preview
            if (!previewVisible && Input.GetMouseButton(1))
            {
                for (int i = 0; i < roomButtons.Count; i++)
                {
                    if (roomButtons[i].IsMouseOverMe)
                    {
                        previewVisible = true;
                        (menu as PauseMenu).infoLabel.alpha = 0f;
                        menu.PlaySound(SoundID.MENU_MultipleChoice_Clicked);
                        UpdateSettingPositions();
                        roomPreview = roomButtons[i].signalText.Substring(0, roomButtons[i].signalText.Length - 4);
                        WarpModMenu.roomPreview = new RoomPreview(menu.manager, this, roomPreview, newRegion, menu as PauseMenu);
                        menu.manager.ShowDialog(WarpModMenu.roomPreview);
                    }
                }
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            bg?.RemoveFromContainer();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message.StartsWith("reg-"))
            {
                string regionText = (sender as WarpButton).menuLabel.text;
                Debug.Log($"WARP: {regionText}");
                newRegion = regionText;
                if (!masterRoomList.ContainsKey(regionText))
                {
                    RoomFinder rf = new RoomFinder();
                    List<RoomInfo> roomList = rf.GetRegionInfo(regionText);
                    GenerateRoomButtons(roomList, sortType, viewType);
                }
                else
                {
                    GenerateRoomButtons(masterRoomList[regionText], sortType, viewType);
                }
                warpStats?.GenerateStats(regionText, "");
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
            if (message == "PAGELEFT")
            {
                for (int i = 0; i < roomButtons.Count; i++)
                {
                    roomButtons[i].pos.x -= 3042f;
                }
                for (int i = 0; i < categoryLabels.Count; i++)
                {
                    categoryLabels[i].label.SetPosition(categoryLabels[i].label.x - 2000f, categoryLabels[i].label.y);
                }
                pageLeft.buttonBehav.greyedOut = true;
                pageLeft.color = new Color(0.3f, 0.3f, 0.3f);
                for (int i = 0; i < subObjects.Count; i++)
                {
                    if (subObjects[i] is WarpButton && (subObjects[i] as WarpButton).signalText == "PAGERIGHT")
                    {
                        pageRight.buttonBehav.greyedOut = false;
                        pageRight.color = new Color(1f, 0.9f, 0.3f);
                    }
                }
            }
            if (message == "PAGERIGHT")
            {
                for (int i = 0; i < roomButtons.Count; i++)
                {
                    roomButtons[i].pos.x += 3042f;
                }
                for (int i = 0; i < categoryLabels.Count; i++)
                {
                    categoryLabels[i].label.SetPosition(categoryLabels[i].label.x + 2000f, categoryLabels[i].label.y);
                }
                pageRight.buttonBehav.greyedOut = true;
                pageRight.color = new Color(0.3f, 0.3f, 0.3f);
                for (int i = 0; i < subObjects.Count; i++)
                {
                    if (subObjects[i] is WarpButton && (subObjects[i] as WarpButton).signalText == "PAGELEFT")
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
            if (message == "TOGGLE")
            {
                if (WarpModMenu.dropdownMode)
                {
                    WarpModMenu.dropdownMode = false;
                    regionDropdown.Hide();
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        if (!alphabetical)
                        {
                            alphabetical = true;
                        }
                        else
                        {
                            alphabetical = false;
                        }
                    }
                    GenerateRegionButtons();
                    WarpSettings.Save();
                }
                else if (regionButtons != null)
                {
                    WarpModMenu.dropdownMode = true;
                    for (int i = 0; i < regionButtons.Count; i++)
                    {
                        regionButtons[i].RemoveSprites();
                        RemoveSubObject(regionButtons[i]);
                    }
                    regionButtons = null;
                    regionDropdown.Show();
                    dropOffset = 0f;
                }
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                WarpSettings.Save();
                UpdateSettingPositions();
            }
            if (message == "STATS")
            {
                if (mode == Mode.Warp)
                {
                    //Enable Stats mode
                    if (warpStats != null)
                    {
                        RemoveSubObject(warpStats);
                        warpStats.RemoveSprites();
                        warpStats = null;
                    }
                    mode = Mode.Stats;
                    (sender as WarpButton).color = new Color(1f, 0.85f, 0f);
                    warpStats = new WarpStats(menu, this);
                    warpStats.GenerateStats(newRegion, "");
                    subObjects.Add(warpStats);
                    showStats = true;
                }
                else
                {
                    //Disable Stats mode
                    mode = Mode.Warp;
                    (sender as WarpButton).color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    if (warpStats != null)
                    {
                        RemoveSubObject(warpStats);
                        warpStats.RemoveSprites();
                        warpStats = null;
                    }
                    showStats = false;
                }
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                WarpSettings.Save();
            }
            if (message == "FAV")
            {
                if (!favourites.Contains(newRegion))
                {
                    Debug.Log("Fav region: " + newRegion);
                    favourites.Add(newRegion);
                    menu.PlaySound(SoundID.MENU_Player_Join_Game);
                }
                else
                {
                    Debug.Log("Unfav region: " + newRegion);
                    favourites.Remove(newRegion);
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
                WarpSettings.SaveFavourites();

                if (!dropdownMode)
                {
                    for (int i = 0; i < regionButtons.Count; i++)
                    {
                        regionButtons[i].RemoveSprites();
                        RemoveSubObject(regionButtons[i]);
                    }
                    GenerateRegionButtons();
                }
                else
                {
                    ListItem[] old = regionDropdown.GetItemList();
                    List<string> oldRegs = new List<string>();
                    foreach (ListItem item in old)
                    {
                        oldRegs.Add(item.name);
                    }
                    List<ListItem> newRegs = GetRegionListItems(game.overWorld.regions);
                    ListItem[] dummy = new ListItem[1]
                    {
                        new ListItem("TOPI PLEASE", 0)
                    };
                    regionDropdown.AddItems(false, dummy);
                    regionDropdown.RemoveItems(false, oldRegs.ToArray());
                    regionDropdown.AddItems(false, newRegs.ToArray());
                    regionDropdown.RemoveItems(false, new string[1] { "TOPI PLEASE" });
                    regionDropdown.ForceValue(newRegion);
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
                        RemoveSubObject(warpStats);
                        warpStats.RemoveSprites();
                        warpStats = null;
                    }
                    warpColor = new WarpColor(menu, this, new Vector2(0f, 0f), new Vector2());
                    subObjects.Add(warpColor);
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
                else
                {
                    menu.PlaySound(SoundID.MENU_Error_Ping);
                }
            }
        }

        public void GenerateRegionButtons()
        {
            regionButtons = new List<WarpButton>();
            int column = 0;
            int row = 0;
            List<string> favRegions = new List<string>();
            List<string> regions = new List<string>();
            for (int i = 0; i < game.overWorld.regions.Length; i++)
            {
                if (favourites.Contains(game.overWorld.regions[i].name))
                {
                    favRegions.Add(game.overWorld.regions[i].name);
                }
            }
            for (int i = 0; i < game.overWorld.regions.Length; i++)
            {
                if (!favRegions.Contains(game.overWorld.regions[i].name))
                {
                    regions.Add(game.overWorld.regions[i].name);
                }
            }
            if (alphabetical)
            {
                regions.Sort();
            }
            if (favRegions.Count > 0)
            {
                favRegions.Sort();
                regions.InsertRange(0, favRegions);
            }


            for (int i = 0; i < regions.Count; i++)
            {
                WarpButton button = new WarpButton(menu, this, regions[i], $"reg-{regions[i]}", statButton.pos + new Vector2(-55f + (35f * column), -(35f + (27f * row))), new Vector2(30f, 23f), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey));
                subObjects.Add(button);
                regionButtons.Add(button);
                column++;
                if (column >= 3)
                {
                    column = 0;
                    row++;
                }
            }
            dropOffset = -(regionButtons.Last().pos.y - statButton.pos.y + 40f);
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
            WarpSettings.Save();
        }

        public void GenerateRoomButtons(List<RoomInfo> roomList, SortType sort, ViewType view)
        {
            //Don't generate room buttons if the color customiser is open
            if (warpColor != null)
            {
                return;
            }
            if (pageLeft != null || pageRight != null)
            {
                pageLeft.RemoveSprites();
                RemoveSubObject(pageLeft);
                pageLeft = null;
                pageRight.RemoveSprites();
                RemoveSubObject(pageRight);
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
            float regionHeight = sortButtons.Last().pos.y;
            float categoryOffset = 0f;
            float screenWidth = game.rainWorld.options.ScreenSize.x - 20f;
            float screenHeight = game.rainWorld.options.ScreenSize.y;
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
                            if (ColorInfo.customSubregionColors.ContainsKey(newRegion))
                            {
                                color = ColorInfo.customSubregionColors[newRegion][info.subregion].rgb;
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
                string[] split = Regex.Split(info.name, "_(.+)");

                if(split.Length > 1)
                {
                    name = split[1];
                }
                else
                {
                    continue;
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
            bool largeRegion = false;
            for (int i = 0; i < roomButtons.Count; i++)
            {
                if (roomButtons[i].pos.x < 250f)
                {
                    largeRegion = true;
                    roomButtons[i].pos.x -= 2000f;
                }
                subObjects.Add(roomButtons[i]);
            }


            float sortHeight = regionHeight - 15f;
            //Add Color Key
            switch (view)
            {
                case ViewType.Type:
                    {
                        colorKey = new List<MenuLabel>();
                        keyLabel = new MenuLabel(menu, this, "ROOM TYPE", new Vector2(22f, sortHeight - 15f), new Vector2(), false);
                        keyLabel.label.alignment = FLabelAlignment.Left;
                        subObjects.Add(keyLabel);
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
                        subObjects.Add(keyLabel);
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
                        subObjects.Add(keyLabel);
                        for (int i = 0; i < WarpModMenu.subregionNames[newRegion].Count; i++)
                        {
                            MenuLabel label = new MenuLabel(menu, this, WarpModMenu.subregionNames[newRegion][i], new Vector2(), new Vector2(), false);
                            label.label.alignment = FLabelAlignment.Left;
                            while (WarpModMenu.subregionNames[newRegion].Count > ColorInfo.customSubregionColors[newRegion].Count)
                            {
                                ColorInfo.customSubregionColors[newRegion].Add(new HSLColor(1f, 1f, 1f));
                            }
                            label.label.color = ColorInfo.customSubregionColors[newRegion][i].rgb;
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
                    colorKey[i].pos = new Vector2(25f + (10f * i), keyLabel.pos.y - 15f);
                }
                else
                {
                    colorKey[i].pos = new Vector2(25f, sortHeight - 35f - (15f * i));
                }
                subObjects.Add(colorKey[i]);
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
                subObjects.Add(subLabel);
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
                    subObjects.Add(subregionLabels[i]);
                }
            }
            //TODO - this doesn't work
            for (int i = 0; i < categoryLabels.Count; i++)
            {
                if (categoryLabels[i].label.x < 250f)
                {
                    categoryLabels[i].label.SetPosition(categoryLabels[i].label.x - 2000f, categoryLabels[i].label.y);
                }
                subObjects.Add(categoryLabels[i]);
            }
            if (largeRegion && (pageLeft == null || pageRight == null))
            {
                pageLeft = new WarpButton(menu, this, "LEFT", "PAGELEFT", new Vector2(screenWidth - 135f, 55f), new Vector2(50f, 25f), new Color(0.3f, 0.3f, 0.3f));
                pageRight = new WarpButton(menu, this, "RIGHT", "PAGERIGHT", new Vector2(screenWidth - 80f, 55f), new Vector2(50f, 25f), new Color(1f, 0.9f, 0f));
                pageLeft.buttonBehav.greyedOut = true;
                subObjects.Add(pageLeft);
                subObjects.Add(pageRight);
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
                RemoveSubObject(roomButtons[i]);
            }
        }
        public void ObliterateCategoryLabels()
        {
            for (int i = 0; i < categoryLabels.Count; i++)
            {
                categoryLabels[i].RemoveSprites();
                RemoveSubObject(categoryLabels[i]);
            }
        }
        public void ObliterateSortButtons()
        {
            for (int i = 0; i < sortButtons.Count; i++)
            {
                sortButtons[i].RemoveSprites();
                RemoveSubObject(sortButtons[i]);
            }
            for (int i = 0; i < viewButtons.Count; i++)
            {
                viewButtons[i].RemoveSprites();
                RemoveSubObject(viewButtons[i]);
            }
        }
        public void ObliterateColorKeyLabels()
        {
            if (keyLabel != null)
            {
                keyLabel.RemoveSprites();
                RemoveSubObject(keyLabel);
            }
            for (int i = 0; i < colorKey.Count; i++)
            {
                colorKey[i].RemoveSprites();
                RemoveSubObject(colorKey[i]);
            }
            if (subregionLabels != null)
            {
                if (subLabel != null)
                {
                    subLabel.RemoveSprites();
                    RemoveSubObject(subLabel);
                }
                for (int i = 0; i < subregionLabels.Count; i++)
                {
                    subregionLabels[i].RemoveSprites();
                    RemoveSubObject(subregionLabels[i]);
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





