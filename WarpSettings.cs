using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;
using System.IO;

public static class WarpSettings
{
    public static void Load()
    {
        string path = Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + "UserData" + Path.DirectorySeparatorChar + "Warp" + Path.DirectorySeparatorChar + "Settings.txt";
        if (!File.Exists(path))
        {
            Save();
        }
        else
        {
            char[] data = File.ReadAllText(path).ToCharArray();
            for (int i = 0; i < data.Length; i++)
            {
                switch (i)
                {
                    //Menu Display
                    case 0:
                        WarpMenu.showMenu = Convert.ToBoolean(int.Parse(data[i].ToString()));
                        break;
                    //Favourites Display
                    case 1:
                        WarpMenu.showStats = Convert.ToBoolean(int.Parse(data[i].ToString()));
                        break;
                    //Sort Type
                    case 2:
                        WarpMenu.sortType = (WarpMenu.SortType)int.Parse(data[i].ToString());
                        break;
                    //View Type
                    case 3:
                        WarpMenu.viewType = (WarpMenu.ViewType)int.Parse(data[i].ToString());
                        break;
                }
            }
        }
    }
    public static void Save()
    {
        string path = Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + "UserData" + Path.DirectorySeparatorChar + "Warp" + Path.DirectorySeparatorChar + "Settings.txt";
        StringBuilder sb = new StringBuilder();
        sb.Append(Convert.ToInt32(WarpMenu.showMenu));
        sb.Append(Convert.ToInt32(WarpMenu.showStats));
        sb.Append((int)WarpMenu.sortType);
        sb.Append((int)WarpMenu.viewType);
        string text = sb.ToString();
        if (!Directory.Exists(Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + "UserData" + Path.DirectorySeparatorChar + "Warp"))
        {
            Directory.CreateDirectory(Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + "UserData" + Path.DirectorySeparatorChar + "Warp");
        }
        File.WriteAllText(path, text);
    }
}
public class WarpFavourites : RectangularMenuObject
{
    public WarpFavourites(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {

    }
}

