using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menu;
using UnityEngine;

public class WarpStats : RectangularMenuObject
{
    public MenuLabel regionLabel;
    public MenuLabel roomLabel;
    public MenuLabel stats;
    public MenuLabel stats2;
    public RoundedRect rect;
    public FSprite BG;
    public string data;
    public string data2;

    public WarpStats(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        this.BG = new FSprite("pixel", true);
        this.BG.SetAnchor(0f, 1f);
        this.BG.x = 170f;
        this.BG.y = 724f;
        this.BG.scaleY = 400f;
        this.BG.scaleX = 150f;
        this.BG.color = new Color(0.1f, 0.1f, 0.1f);
        this.BG.alpha = 0f;
        this.Container.AddChild(this.BG);
        //Region Stats
        stats = new MenuLabel(menu, this, "", new Vector2(22f, (owner as WarpModMenu.WarpContainer).colorKey.Last().pos.y - 20f), new Vector2(), false);
        stats.label.color = new Color(0.65f, 0.65f, 0.65f);
        this.subObjects.Add(stats);
        stats2 = new MenuLabel(menu, this, "", new Vector2(), new Vector2(), false);
        stats2.label.color = new Color(0.65f, 0.65f, 0.65f);
        this.subObjects.Add(stats2);
        //Room Stats
        //roomLabel = new MenuLabel(menu, this, "", new Vector2(550f, 625f), new Vector2(), true);
        //roomLabel.label.alignment = FLabelAlignment.Left;
        //this.subObjects.Add(roomLabel);
    }

    public override void Update()
    {
        base.Update();
        //if(!this.BG.localRect.Contains(this.BG.GlobalToLocal(Input.mousePosition)) && Input.GetMouseButton(0))
        //{
        //    this.RemoveSprites();
        //    (this.owner as WarpModMenu.WarpContainer).warpStats = null;
        //    menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
        //    (this.owner as WarpModMenu.WarpContainer).RemoveSubObject(this);
        //}
    }

    public void GenerateStats(string region, string room)
    {
        Debug.Log("Generating " + region + " stats");
        int rooms = 0;
        int screens = 0;
        int gates = 0;
        int shelters = 0;
        int swarm = 0;
        int trader = 0;
        int toll = 0;
        SortedDictionary<int, int> screenCount = new SortedDictionary<int, int>();
        foreach (RoomInfo item in WarpModMenu.masterRoomList[region])
        {
            rooms++;
            screens += item.cameras;
            switch (item.type)
            {
                case RoomInfo.RoomType.Gate:
                    gates++;
                    break;
                case RoomInfo.RoomType.Shelter:
                    shelters++;
                    break;
                case RoomInfo.RoomType.SwarmRoom:
                    swarm++;
                    break;
                case RoomInfo.RoomType.ScavTrader:
                    trader++;
                    break;
                case RoomInfo.RoomType.ScavOutpost:
                    toll++;
                    break;
            }
            if (!screenCount.ContainsKey(item.cameras))
            {
                screenCount.Add(item.cameras, 0);
            }
            screenCount[item.cameras]++;
        }
        data = "ROOMS: " + rooms + Environment.NewLine + "SCREENS: " + screens;
        data += Environment.NewLine + Environment.NewLine;
        //data += "Subregions:" + Environment.NewLine;
        //if (WarpModMenu.subregionNames.ContainsKey(region) && WarpModMenu.subregionNames[region].Count >= 2)
        //{
        //    //First Subregion name for a region should be it's full name
        //    for (int i = 1; i < WarpModMenu.subregionNames[region].Count; i++)
        //    {
        //        data += WarpModMenu.subregionNames[region][i] + Environment.NewLine;
        //    }
        //}
        data += "Gates: " + gates + Environment.NewLine;
        data += "Shelters: " + shelters + Environment.NewLine;
        if (swarm > 0)
        {
            data += "Swarm Rooms: " + swarm + Environment.NewLine;
        }
        if (trader > 0)
        {
            data += "Scav Traders: " + trader + Environment.NewLine;
        }
        if (toll > 0)
        {
            data += "Scav Tolls: " + toll + Environment.NewLine;
        }
        data2 = "";
        for (int i = 0; i < screenCount.Keys.Count; i++)
        {
            data2 += Environment.NewLine + screenCount.Keys.ElementAt(i) + " Screen: " + screenCount[screenCount.Keys.ElementAt(i)];
        }
        stats.label.text = data;
        stats.label.alignment = FLabelAlignment.Left;
        stats.label.anchorY = 1f;
        stats2.label.text = data2;
        stats2.label.alignment = FLabelAlignment.Left;
        stats2.label.anchorY = 1f;
        stats2.pos.x = stats.pos.x;
        stats2.pos.y = stats.pos.y - stats.label.textRect.height;
        //Room Stats
        //roomLabel.label.text = "ROOM: " + room;
        WarpModMenu.showStats = true;
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        this.BG.RemoveFromContainer();
    }
}