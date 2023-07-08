using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using Application = UnityEngine.Application;

class RoomFinder
{
    public List<RoomInfo> GetRegionInfo(string region)
    {
        //Region information can be found in multiple locations, vanilla world folders, world folders from Downpour mod folders, the mergedmods folder and workshop folders.
        //The AssetManager should in theory check all of these locations when we search for a region's specific world file.

        //The final list of room information that will be returned.
        List<RoomInfo> roomList = new List<RoomInfo>();

        //Check if a world file for this region exists
        string filePath = AssetManager.ResolveFilePath($"world/{region}/world_{region}.txt");

        //The path to the -rooms folder is unecessary because in the instances of vanilla regions that have been modified, the mergedmods folder will only contain
        //the modified files and not the originals. Therefore we should only use the updated world file location, parse it for room names, and then use the AssetManager
        //to get the most up-to-date version of that room file for parsing.

        //Parse the world file for room names and tags and add them to the room list
        roomList.AddRange(ParseWorldFile(filePath));

        //Loop through each room in the roomList and check the true location of that room file for additional info
        foreach (RoomInfo info in roomList)
        {
            //Check if Room .txt Exists
            string roomPath = AssetManager.ResolveFilePath($"world/{region}-rooms/{info.name}.txt");
            if (File.Exists(roomPath))
            {
                string[] roomFile = File.ReadAllLines(roomPath);
                Debug.Log($"{info.name}: {roomFile[3]}");
                string[] cameraCount = Regex.Split(roomFile[3], @"\|");
                if (cameraCount != null && cameraCount.Length > 0)
                {
                    info.cameras = cameraCount.Length;
                }
            }
        }

        //Parse the region's properties file for subregion names
        List<string> subregionNames = new List<string>();
        string propPath = AssetManager.ResolveFilePath($"world/{region}/properties.txt");
        if (File.Exists(propPath))
        {
            subregionNames.Add(Custom.rainWorld.inGameTranslator.Translate("None"));
            string[] propFile = File.ReadAllLines(propPath);

            for (int i = 0; i < propFile.Length; i++)
            {
                if (propFile[i].StartsWith("Subregion"))
                {
                    //[0] Room Name - [1] prop Data
                    string[] subName = Regex.Split(Custom.ValidateSpacedDelimiter(propFile[i], ":"), ": ");
                    if (subName.Length > 0)
                    {
                        subregionNames.Add(subName[1]);
                    }
                }
            }
            if (!WarpModMenu.subregionNames.ContainsKey(region))
            {
                WarpModMenu.subregionNames.Add(region, subregionNames);
            }
            else
            {
                WarpModMenu.subregionNames[region].Clear();
                WarpModMenu.subregionNames[region].AddRange(subregionNames);
            }
        }

        //Parse the map.txt for subregion assignments
        string mapPath = AssetManager.ResolveFilePath($"world/{region}/map_{region}.txt");
        if (File.Exists(mapPath))
        {
            string[] mapFile = File.ReadAllLines(mapPath);

            for (int i = 0; i < mapFile.Length; i++)
            {
                if (mapFile[i].StartsWith("Off") || mapFile[i].StartsWith("Connection"))
                {
                    continue;
                }
                //[0] Room Name - [1] Map Data
                string[] nameAndInfo = Regex.Split(Custom.ValidateSpacedDelimiter(mapFile[i], ":"), ": ");
                //Find matching room in RoomList and give it its subregion number
                foreach (RoomInfo info in roomList)
                {
                    if (info.name == nameAndInfo[0])
                    {
                        //Split Map Data by delimiter, last item in the array is subregion number
                        string subRegionName = null;
                        string[] spl = Regex.Split(nameAndInfo[1], "><");
                        if (spl.Length >= 6)
                            subRegionName = (spl[5].Trim() == "" ? null : spl[5].Trim());

                        info.subregion = 0;
                        info.subregionName = "None";
                        for (int j = 1; j < subregionNames.Count; j++)
                        {
                            if (subregionNames[j] == subRegionName)
                            {
                                info.subregionName = subRegionName;
                                info.subregion = j;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log("Assigning default subregion colors");
        if (!ColorInfo.customSubregionColors.ContainsKey(region))
        {
            ColorInfo.customSubregionColors.Add(region, new List<HSLColor>());
            for (int i = 0; i < subregionNames.Count; i++)
            {
                ColorInfo.customSubregionColors[region].Add(ColorInfo.subregionColors[i]);
            }
        }
        else if (subregionNames.Count > ColorInfo.customSubregionColors[region].Count)
        {
            while (subregionNames.Count > ColorInfo.customSubregionColors[region].Count)
            {
                ColorInfo.customSubregionColors[region].Add(new HSLColor(0.5f, 0f, 1f));
            }
        }
        Debug.Log("Default subregion colors assigned");

        if (!WarpModMenu.masterRoomList.ContainsKey(region))
        {
            WarpModMenu.masterRoomList.Add(region, roomList);
            Debug.Log(region + " added to master list");
        }

        roomList.Sort(RoomInfo.SortByTypeAndName);
        return roomList;
    }

    public List<RoomInfo> ParseWorldFile(string path)
    {
        List<RoomInfo> roomInfo = new List<RoomInfo>();
        if (File.Exists(path))
        {
            bool roomSection = false;
            string[] worldFile = File.ReadAllLines(path);
            for (int i = 0; i < worldFile.Length; i++)
            {
                if (worldFile[i] == "END ROOMS")
                {
                    break;
                }
                if (roomSection)
                {
                    if (worldFile[i].Length > 0 && worldFile[i][0] != ' ' && worldFile[i][0] != '/')
                    {
                        RoomInfo info = new RoomInfo();
                        //Check for conditionals
                        string[] roomLine = Regex.Split(worldFile[i], " : ");
                        if (roomLine != null)
                        {
                            //Assign Room Name
                            if (roomLine[0].Contains("}"))
                            {
                                info.name = Regex.Split(roomLine[0], "\\}")[1];
                            }
                            else if (roomLine[0].Contains(")"))
                            {
                                info.name = Regex.Split(roomLine[0], "\\)")[1];
                            }
                            else
                            {
                                info.name = roomLine[0];
                            }
                            //Check Room Tag - Default = Room
                            info.type = RoomInfo.RoomType.Room;
                            if (roomLine.Length > 2)
                            {
                                switch (roomLine[2])
                                {
                                    case "GATE":
                                        info.type = RoomInfo.RoomType.Gate;
                                        break;
                                    case "SHELTER":
                                        info.type = RoomInfo.RoomType.Shelter;
                                        break;
                                    case "SWARMROOM":
                                        info.type = RoomInfo.RoomType.SwarmRoom;
                                        break;
                                    case "SCAVTRADER":
                                        info.type = RoomInfo.RoomType.ScavTrader;
                                        break;
                                    case "SCAVOUTPOST":
                                        info.type = RoomInfo.RoomType.ScavOutpost;
                                        break;
                                }
                            }
                            //Check that a room with this name doesn't already exist
                            if(roomInfo.Find(x => x.name == info.name) == null)
                            {
                                roomInfo.Add(info);
                            }
                        }
                    }
                }
                if (worldFile[i] == "ROOMS")
                {
                    roomSection = true;
                }
            }
        }
        return roomInfo;
    }
}

public class RoomInfo : IComparable<RoomInfo>, IEquatable<RoomInfo>
{
    public enum RoomType
    {
        Room,
        Gate,
        Shelter,
        SwarmRoom,
        ScavTrader,
        ScavOutpost
    }
    public string name;
    public int cameras;
    public RoomType type;
    public int subregion;
    public Color color;
    public string subregionName;
    public int CompareTo(RoomInfo other)
    {
        return this.name.CompareTo(other.name);
    }

    public static int SortByTypeAndName(RoomInfo a, RoomInfo b)
    {
        int i = a.type.CompareTo(b.type);
        if (i == 0)
        {
            return a.name.CompareTo(b.name);
        }
        else
        {
            return i;
        }
    }

    public static int SortBySizeAndName(RoomInfo a, RoomInfo b)
    {
        int i = a.cameras.CompareTo(b.cameras);
        if (i == 0)
        {
            return a.name.CompareTo(b.name);
        }
        else
        {
            return i;
        }
    }

    public static int SortBySubregionAndName(RoomInfo a, RoomInfo b)
    {
        int i = a.subregion.CompareTo(b.subregion);
        if (i == 0)
        {
            return a.name.CompareTo(b.name);
        }
        else
        {
            return i;
        }
    }

    public bool Equals(RoomInfo other)
    {
        if (this.name == other.name)
        {
            return true;
        }
        else
        {
            return false;
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
                //File.WriteAllText(savePath, Warp.Resources.Colors);
            }
            Load();
            Debug.Log("Custom Warp colors loaded");
        }
    }
}