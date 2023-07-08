using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;
using System.IO;
using System.Text.RegularExpressions;

public static class WarpSettings
{
    public static void Load()
    {
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string path = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Settings.txt";
        if (!File.Exists(path))
        {
            Save();
        }
        else
        {
            try
            {
                char[] data = File.ReadAllText(path).ToCharArray();
                if (data.Length == 0)
                {
                    return;
                }
                for (int i = 0; i < data.Length; i++)
                {
                    switch (i)
                    {
                        //Menu Display
                        case 0:
                            WarpModMenu.showMenu = Convert.ToBoolean(int.Parse(data[i].ToString()));
                            break;
                        //Favourites Display
                        case 1:
                            WarpModMenu.showStats = Convert.ToBoolean(int.Parse(data[i].ToString()));
                            if (WarpModMenu.showStats)
                            {
                                WarpModMenu.mode = WarpModMenu.Mode.Stats;
                            }
                            else
                            {
                                WarpModMenu.mode = WarpModMenu.Mode.Warp;
                            }
                            break;
                        //Sort Type
                        case 2:
                            WarpModMenu.sortType = (WarpModMenu.SortType)int.Parse(data[i].ToString());
                            break;
                        //View Type
                        case 3:
                            WarpModMenu.viewType = (WarpModMenu.ViewType)int.Parse(data[i].ToString());
                            break;
                        //List mode
                        case 4:
                            WarpModMenu.dropdownMode = Convert.ToBoolean(int.Parse(data[i].ToString()));
                            break;
                        //List mode
                        case 5:
                            WarpModMenu.alphabetical = Convert.ToBoolean(int.Parse(data[i].ToString()));
                            break;
                    }
                }
            }
            catch
            {
                Save();
            }
        }
    }
    public static void Save()
    {
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string path = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Settings.txt";
        StringBuilder sb = new StringBuilder();
        sb.Append(Convert.ToInt32(WarpModMenu.showMenu));
        sb.Append(Convert.ToInt32(WarpModMenu.showStats));
        sb.Append((int)WarpModMenu.sortType);
        sb.Append((int)WarpModMenu.viewType);
        sb.Append(Convert.ToInt32(WarpModMenu.dropdownMode));
        sb.Append(Convert.ToInt32(WarpModMenu.alphabetical));
        string text = sb.ToString();
        if (!Directory.Exists(rootFolder + "Warp"))
        {
            Directory.CreateDirectory(rootFolder + "Warp");
        }
        File.WriteAllText(path, text);
    }

    public static void LoadFavourites()
    {
        WarpModMenu.favourites = new HashSet<string>();
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string path = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Favourites.txt";
        if (File.Exists(path))
        {
            try
            {
                string favString = File.ReadAllText(path);
                string[] favs = Regex.Split(favString, ":");
                for (int i = 0; i < favs.Length; i++)
                {
                    WarpModMenu.favourites.Add(favs[i]);
                    Debug.Log("Loading Fav: " + favs[i]);
                }
            }
            catch { }
        }
    }

    public static void SaveFavourites()
    {
        string rootFolder = Application.persistentDataPath + Path.DirectorySeparatorChar;
        string path = rootFolder + "Warp" + Path.DirectorySeparatorChar + "Favourites.txt";
        if (WarpModMenu.favourites != null && WarpModMenu.favourites.Count > 0)
        {
            string favString = String.Join(":", WarpModMenu.favourites);
            if (!Directory.Exists(rootFolder + "Warp"))
            {
                Directory.CreateDirectory(rootFolder + "Warp");
            }
            File.WriteAllText(path, favString);
        }
    }
}