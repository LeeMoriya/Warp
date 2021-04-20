using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menu;
using UnityEngine;

public class WarpStats : RectangularMenuObject
{
    public MenuLabel stats;
    public MenuLabel stats2;
    public string data;
    public string data2;

    public WarpStats(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        stats = new MenuLabel(menu, this, "", new Vector2(), new Vector2(), false);
        this.subObjects.Add(stats);
        stats2 = new MenuLabel(menu, this, "", new Vector2(), new Vector2(), false);
        this.subObjects.Add(stats2);
    }

    public void GenerateStats(string region)
    {
        int rooms = 0;
        int screens = 0;
        int gates = 0;
        int shelters = 0;
        int swarm = 0;
        int trader = 0;
        int toll = 0;
        SortedDictionary<int, int> screenCount = new SortedDictionary<int, int>();
        foreach (RoomInfo item in WarpMenu.masterRoomList[region])
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
        data += "Gates: " + gates + Environment.NewLine;
        data += "Shelters: " + shelters + Environment.NewLine;
        if(swarm > 0)
        {
            data += "Swarm Rooms: " + swarm + Environment.NewLine;
        }
        if(trader > 0)
        {
            data += "Scav Traders: " + trader + Environment.NewLine;
        }
        if(toll > 0)
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
        WarpMenu.showStats = true;
    }
}