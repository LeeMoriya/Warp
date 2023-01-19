using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                List<RoomInfo> roomList = rf.Generate(currentRegion, false);
                warpContainer.GenerateRoomButtons(roomList, WarpModMenu.sortType, WarpModMenu.viewType);
            }
            else
            {
                warpContainer.GenerateRoomButtons(WarpModMenu.masterRoomList[currentRegion], WarpModMenu.sortType, WarpModMenu.viewType);
            }
            if(WarpModMenu.mode == WarpModMenu.Mode.Stats)
            {
                warpContainer.warpStats = new WarpStats(menu, warpContainer, new Vector2(), new Vector2());
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