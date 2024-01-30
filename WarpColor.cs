using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Menu;
using UnityEngine;

public class WarpColor : RectangularMenuObject, Slider.ISliderOwner
{
    public WarpModMenu.WarpContainer warpContainer;
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
        warpContainer = (owner as WarpModMenu.WarpContainer);
        currentCol = new HSLColor(hue, 1f, 0.5f);
        anchor = new Vector2(160f, menu.manager.rainWorld.screenSize.y - 350f);
        currentRegion = WarpModMenu.newRegion;

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

        switch (WarpModMenu.viewType)
        {
            case WarpModMenu.ViewType.Type:
                {
                    category = Category.Type;
                    CreateTypeButtons();
                    break;
                }
            case WarpModMenu.ViewType.Size:
                {
                    category = Category.Size;
                    CreateSizeButtons();
                    break;
                }
            case WarpModMenu.ViewType.Subregion:
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
        if (currentRegion != WarpModMenu.newRegion && WarpModMenu.masterRoomList.ContainsKey(WarpModMenu.newRegion))
        {
            currentRegion = WarpModMenu.newRegion;
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
            if (!WarpModMenu.masterRoomList.ContainsKey(currentRegion))
            {
                RoomFinder rf = new RoomFinder();
                List<RoomInfo> roomList = rf.GetRegionInfo(currentRegion);
                warpContainer.GenerateRoomButtons(roomList, WarpModMenu.sortType, WarpModMenu.viewType);
            }
            else
            {
                warpContainer.GenerateRoomButtons(WarpModMenu.masterRoomList[currentRegion], WarpModMenu.sortType, WarpModMenu.viewType);
            }
            if(WarpModMenu.mode == WarpModMenu.Mode.Stats)
            {
                warpContainer.warpStats = new WarpStats(menu, warpContainer);
                warpContainer.warpStats.GenerateStats(WarpModMenu.newRegion, "");
                warpContainer.subObjects.Add(warpContainer.warpStats);
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
        if (WarpModMenu.masterRoomList.ContainsKey(currentRegion))
        {
            //If no custom colors are defined for this region's subregions, create a new entry with default colors
            if (!ColorInfo.customSubregionColors.ContainsKey(currentRegion))
            {
                ColorInfo.customSubregionColors.Add(currentRegion, new List<HSLColor>());
                for (int i = 0; i < WarpModMenu.subregionNames[currentRegion].Count; i++)
                {
                    ColorInfo.customSubregionColors[currentRegion].Add(ColorInfo.subregionColors[i]);
                }
            }
            while(ColorInfo.customSubregionColors[currentRegion].Count < WarpModMenu.subregionNames[currentRegion].Count)
            {
                ColorInfo.customSubregionColors[currentRegion].Add(ColorInfo.subregionColors[ColorInfo.customSubregionColors[currentRegion].Count]);
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
                    if (WarpModMenu.subregionNames[currentRegion].Count >= selectedColor)
                    {
                        categoryLabel.label.text = WarpModMenu.subregionNames[currentRegion][selectedColor];
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

public static class ColorInfo
{
    public static HSLColor[] typeColors = new HSLColor[6]
    {
        //1 Room
        new HSLColor(0.33f,0.99f,0.67f),
        //2 Gate
        new HSLColor(0.18f,0.99f,0.63f),
        //3 Shelter
        new HSLColor(0.51f,0.99f,0.61f),
        //4 SwarmRoom
        new HSLColor(0.73f,0.99f,0.76f),
        //5 ScavTrader
        new HSLColor(0.06f,0.99f,0.63f),
        //6 ScavOutpost
        new HSLColor(0.99f,0.99f,0.66f)
    };
    public static HSLColor[] sizeColors = new HSLColor[9]
    {
        //1 Cam
        new HSLColor(0.33f,0.99f,0.67f),
        //2 Cam
        new HSLColor(0.18f,0.99f,0.68f),
        //3 Cam
        new HSLColor(0.08f,0.99f,0.61f),
        //4 Cam
        new HSLColor(0f,0.99f,0.69f),
        //5 Cam
        new HSLColor(0.82f,0.99f,0.73f),
        //6 Cam
        new HSLColor(0.71f,0.99f,0.66f),
        //7 Cam
        new HSLColor(0.6f,0.99f,0.59f),
        //8 Cam
        new HSLColor(0.54f,0.99f,0.65f),
        //9+ Cam
        new HSLColor(0.46f,0.99f,0.58f)
    };
    //Default subregion colors
    public static HSLColor[] subregionColors = new HSLColor[15]
    {
        //Default
        new HSLColor(0.46f,0.99f,0.99f),
        //1 Subregion
        new HSLColor(0.33f,0.99f,0.67f),
        //2 Subregion
        new HSLColor(0.18f,0.99f,0.68f),
        //3 Subregion
        new HSLColor(0.08f,0.99f,0.61f),
        //4 Subregion
        new HSLColor(0f,0.99f,0.69f),
        //5 Subregion
        new HSLColor(0.82f,0.99f,0.73f),
        //6 Subregion
        new HSLColor(0.71f,0.99f,0.66f),
        //7 Subregion
        new HSLColor(0.6f,0.99f,0.59f),
        //8 Subregion
        new HSLColor(0.54f,0.99f,0.65f),
        //9 Subregion
        new HSLColor(0.46f,0.99f,0.58f),
        //10 Subregion
        new HSLColor(0.82f,0.99f,0.73f),
        //11 Subregion
        new HSLColor(0.71f,0.99f,0.66f),
        //12 Subregion
        new HSLColor(0.6f,0.99f,0.59f),
        //13 Subregion
        new HSLColor(0.54f,0.99f,0.65f),
        //14 Subregion
        new HSLColor(0.46f,0.99f,0.58f),
    };

    public static Dictionary<string, List<HSLColor>> customSubregionColors = new Dictionary<string, List<HSLColor>>();

    public static void Wipe()
    {
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string savePath = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Colors.txt";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    public static void Save()
    {
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string savePath = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Colors.txt";
        StringBuilder sb = new StringBuilder();
        //Type
        sb.Append("[TYPE]");
        sb.AppendLine();
        for (int i = 0; i < typeColors.Length; i++)
        {
            sb.Append(Math.Round(typeColors[i].hue, 2) + ":" + Math.Round(typeColors[i].saturation, 2) + ":" + Math.Round(typeColors[i].lightness, 2));
            sb.AppendLine();
        }
        //Size
        sb.Append("[SIZE]");
        sb.AppendLine();
        for (int i = 0; i < sizeColors.Length; i++)
        {
            sb.Append(Math.Round(sizeColors[i].hue, 2) + ":" + Math.Round(sizeColors[i].saturation, 2) + ":" + Math.Round(sizeColors[i].lightness, 2));
            sb.AppendLine();
        }
        sb.Append("[SUBREGION]");
        sb.AppendLine();
        for (int i = 0; i < customSubregionColors.Keys.Count; i++)
        {
            sb.Append("[" + customSubregionColors.ElementAt(i).Key + "]");
            sb.AppendLine();
            for (int c = 0; c < customSubregionColors.ElementAt(i).Value.Count; c++)
            {
                sb.Append(Math.Round(customSubregionColors.ElementAt(i).Value[c].hue, 2) + ":" + Math.Round(customSubregionColors.ElementAt(i).Value[c].saturation, 2) + ":" + Math.Round(customSubregionColors.ElementAt(i).Value[c].lightness, 2));
                sb.AppendLine();
            }
        }
        sb.Append("[END]");
        string text = sb.ToString();
        if (!Directory.Exists(rootFolder + "Warp"))
        {
            Directory.CreateDirectory(rootFolder + "Warp");
        }
        File.WriteAllText(savePath, text);
        Debug.Log("Custom Warp colors saved");
    }

    public static void Load()
    {
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string savePath = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Colors.txt";
        if (File.Exists(savePath))
        {
            string[] array = new string[]
            {
                string.Empty
            };
            array = File.ReadAllLines(savePath);
            int section = 0;
            int index = 0;
            string currentReg = "";
            if (array != new string[] { string.Empty } && array.Length > 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (!array[i].StartsWith("//") && array[i] != "")
                    {
                        if (array[i] == "[END]")
                        {
                            break;
                        }
                        if (section == 3)
                        {
                            //Determine region
                            if (array[i].StartsWith("["))
                            {
                                char[] trim = { '[', ']' };
                                string reg = array[i].Trim(trim);
                                currentReg = reg;
                                //Region in save file is present
                                if (!customSubregionColors.ContainsKey(reg))
                                {
                                    customSubregionColors.Add(reg, new List<HSLColor>());
                                }
                                else
                                {
                                    customSubregionColors[reg] = new List<HSLColor>();
                                }
                            }
                            else
                            {
                                if (customSubregionColors.ContainsKey(currentReg))
                                {
                                    string[] col = Regex.Split(array[i], ":");
                                    try
                                    {
                                        HSLColor hsl = new HSLColor(float.Parse(col[0]), float.Parse(col[1]), float.Parse(col[2]));
                                        customSubregionColors[currentReg].Add(hsl);
                                    }
                                    catch
                                    {
                                        Debug.LogError("Error reading color from save file");
                                    }
                                }
                                else
                                {
                                    Debug.Log("REGION NOT PRESENT IN CUSTOM COLOR DICT");
                                }
                            }
                        }
                        if (array[i] == "[SUBREGION]")
                        {
                            section = 3;
                            index = 0;
                        }
                        if (section == 2)
                        {
                            string[] col = Regex.Split(array[i], ":");
                            try
                            {
                                HSLColor hsl = new HSLColor(float.Parse(col[0]), float.Parse(col[1]), float.Parse(col[2]));
                                sizeColors[index] = hsl;
                            }
                            catch
                            {
                                Debug.LogError("Error reading color from save file");
                            }
                            index++;
                        }
                        if (array[i] == "[SIZE]")
                        {
                            section = 2;
                            index = 0;
                        }
                        if (section == 1)
                        {
                            string[] col = Regex.Split(array[i], ":");
                            try
                            {
                                HSLColor hsl = new HSLColor(float.Parse(col[0]), float.Parse(col[1]), float.Parse(col[2]));
                                typeColors[index] = hsl;
                            }
                            catch
                            {
                                Debug.LogError("Error reading color from save file");
                            }
                            index++;
                        }
                        if (array[i] == "[TYPE]")
                        {
                            section = 1;
                        }
                    }
                }
            }
        }
        else
        {
            if (!Directory.Exists(rootFolder + "Warp"))
            {
                Directory.CreateDirectory(rootFolder + "Warp");
            }
            if (Directory.Exists(rootFolder + "Warp"))
            {
                File.WriteAllText(savePath, Warp.Resources.Colors);
                Load();
            }
            Debug.Log("Custom Warp colors loaded");
        }
    }
}