using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

class RoomFinder
{
    public List<RoomInfo> DebugList(int rooms, int cameras)
    {
        List<RoomInfo> debugList = new List<RoomInfo>();
        for (int i = 0; i < cameras; i++)
        {
            for (int c = 0; c < rooms; c++)
            {
                RoomInfo info = new RoomInfo()
                {
                    name = "XX_00",
                    cameras = i + 1,
                    type = RoomInfo.RoomType.Room,
                    color = new Color(1f, 1f, 1f),
                    subregion = 0,
                    subregionName = "A"
                };
                debugList.Add(info);
            }
        }
        if (WarpModMenu.masterRoomList.ContainsKey("XX"))
        {
            WarpModMenu.masterRoomList["XX"] = debugList;
        }
        else
        {
            WarpModMenu.masterRoomList.Add("XX", debugList);
        }
        return debugList;
    }

    public List<string> RoomList(string region, bool CRS)
    {
        string[] array = new string[]
        {
            string.Empty
        };
        string rootFolder = Custom.RootFolderDirectory() + Path.DirectorySeparatorChar;
        //Vanilla region
        if (File.Exists(string.Concat(new object[]
        {
            rootFolder,
            "World",
            Path.DirectorySeparatorChar,
            region,
            Path.DirectorySeparatorChar,
            "world_",
            region,
            ".txt"
        })))
        {
            array = File.ReadAllLines(string.Concat(new object[]
            {
                rootFolder,
                "World",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "world_",
                region,
                ".txt"
            }));
        }
        //Get list of rooms between ROOMS and END ROOMS
        bool flag = false;
        List<string> roomList = new List<string>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == "END ROOMS")
            {
                break;
            }
            if (flag && array[i].Length > 0 && array[i][0] != ' ' && array[i][0] != '/')
            {
                string[] roomName = Regex.Split(array[i], " : ");
                if (roomName != null)
                {
                    roomList.Add(roomName[0]);
                }
            }
            //ROOMS section starts
            if (array[i] == "ROOMS")
            {
                flag = true;
            }
        }
        //Custom Region
        if (CRS)
        {
            if (Directory.Exists(string.Concat(new object[]
            {
                rootFolder,
                "Mods",
                Path.DirectorySeparatorChar,
                "CustomResources"
            })))
            {
                foreach (string dir in Directory.GetDirectories(rootFolder + "Mods" + Path.DirectorySeparatorChar + "CustomResources"))
                {
                    if (Directory.Exists(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + region))
                    {
                        if (File.Exists(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + region + Path.DirectorySeparatorChar + "world_" + region + ".txt"))
                        {
                            array = File.ReadAllLines(string.Concat(new object[]
                            {
                            dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + region,
                            Path.DirectorySeparatorChar,
                            "world_",
                            region,
                            ".txt"
                            }));
                            bool flag2 = false;
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (array[i] == "END ROOMS")
                                {
                                    break;
                                }
                                if (flag2)
                                {
                                    string[] roomName = Regex.Split(array[i], " : ");
                                    if (roomName != null && !roomList.Contains(roomName[0]) && (roomName[0].StartsWith(region) || roomName[0].StartsWith("GATE")))
                                    {
                                        roomList.Add(roomName[0]);
                                    }
                                }
                                if (array[i] == "ROOMS")
                                {
                                    flag2 = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return roomList;
    }

    public List<RoomInfo> Generate(string region, bool CRS)
    {
        string rootFolder = Custom.RootFolderDirectory() + Path.DirectorySeparatorChar;
        CRS = true;
        List<RoomInfo> roomList = new List<RoomInfo>();
        List<RoomInfo> crsRoomList = new List<RoomInfo>();

        //Vanilla Rooms
        string[] array = new string[]
        {
            string.Empty
        };
        //Check Region is Vanilla
        if (File.Exists(string.Concat(new object[]
        {
            rootFolder,
            "World",
            Path.DirectorySeparatorChar,
            region,
            Path.DirectorySeparatorChar,
            "world_",
            region,
            ".txt"
        })))
        //Load Vanilla Region into String Array
        {
            array = File.ReadAllLines(string.Concat(new object[]
            {
                rootFolder,
                "World",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "world_",
                region,
                ".txt"
            }));
            //Check Each Line for Names and Tags
            bool roomSection = false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == "END ROOMS")
                {
                    break;
                }
                if (roomSection)
                {
                    if (array[i].Length > 0 && array[i][0] != ' ' && array[i][0] != '/')
                    {
                        RoomInfo info = new RoomInfo();
                        //Check for conditionals
                        string[] roomLine = Regex.Split(array[i], " : ");
                        if (roomLine != null)
                        {
                            //Assign Room Name
                            if (roomLine[0].Contains(")"))
                            {
                                info.name = Regex.Split(roomLine[0], "\\)")[1];
                                Debug.Log(info.name);
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
                            roomList.Add(info);
                        }
                    }
                }
                if (array[i] == "ROOMS")
                {
                    roomSection = true;
                }
            }
        }
        //Check the CustomResources folder for custom regions
        //Check that the region is activated in the .json file
        //If it is, check that the chosen region exists in this regionPack
        //If it does, add a path to this modded region folder to a list
        List<string> moddedPaths = new List<string>();
        if (CRS)
        {
            string dir = rootFolder + "mods" + Path.DirectorySeparatorChar + "moreslugcats";
            //Region pack is activated, check if matching region XX is found in pack Regions folder
            if (File.Exists(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + region + Path.DirectorySeparatorChar + "World_" + region + ".txt"))
            {
                moddedPaths.Add(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + region);
            }
            //Add modded rooms to roomList
            List<string> vanillaRoomNames = new List<string>();
            if (roomList.Count > 0)
            {
                for (int i = 0; i < roomList.Count; i++)
                {
                    vanillaRoomNames.Add(roomList[i].name);
                }
            }

            for (int i = 0; i < moddedPaths.Count; i++)
            {
                bool crsRoomSection = false;
                string[] crsWorldFile = File.ReadAllLines(moddedPaths[i] + Path.DirectorySeparatorChar + "World_" + region + ".txt");
                for (int l = 0; l < crsWorldFile.Length; l++)
                {
                    if (crsWorldFile[l] == "END ROOMS")
                    {
                        break;
                    }
                    if (crsRoomSection)
                    {
                        RoomInfo info = new RoomInfo();
                        if (crsWorldFile[l].Length > 0 && !crsWorldFile[l].StartsWith("//"))
                        {
                            string[] roomLine = Regex.Split(crsWorldFile[l], " : ");
                            if (roomLine != null)
                            {
                                //Assign Room Name
                                if (roomLine[0].Contains(")"))
                                {
                                    info.name = Regex.Split(roomLine[0], "\\)")[1];
                                    Debug.Log(info.name);
                                }
                                else
                                {
                                    info.name = roomLine[0];
                                }
                                // hide easter egg maps from warp menu
                                if (info.name.ToLowerInvariant() == "hr_layers_of_reality"
                                    || info.name.ToLowerInvariant() == "vs_basement01"
                                    || info.name.ToLowerInvariant() == "vs_basement02")
                                    continue;
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
                                //If this room isn't vanilla, add it
                                if (!vanillaRoomNames.Contains(info.name))
                                {
                                    crsRoomList.Add(info);
                                }
                                //if there is already a room with this name but the type is different, add it
                                else if (roomList.Exists(x => x.name == info.name && x.type != info.type))
                                {
                                    crsRoomList.Add(info);
                                }
                            }
                        }
                    }
                    if (crsWorldFile[l] == "ROOMS")
                    {
                        crsRoomSection = true;
                    }
                }
            }
        }
        //Grab number of cameras from each room file
        string[] roomFile = new string[]
        {
            string.Empty
        };
        foreach (RoomInfo info in roomList)
        {
            //Check if Room .txt Exists
            if (File.Exists(string.Concat(new object[]
            {
            rootFolder,
            "World",
            Path.DirectorySeparatorChar,
            region + "-Rooms",
            Path.DirectorySeparatorChar,
            info.name + ".txt"
            })))
            //Load Room .txt into Array
            {
                roomFile = File.ReadAllLines(string.Concat(new object[]
                {
                rootFolder,
                "World",
                Path.DirectorySeparatorChar,
                region + "-Rooms",
                Path.DirectorySeparatorChar,
                info.name + ".txt"
                }));
                //Split line with camera info
                if (roomFile != new string[] { string.Empty } && roomFile.Length > 3)
                {
                    string[] cameras = roomFile[3].Split(new char[]
                    {
                    '|'
                    });
                    if (cameras != null)
                    {
                        info.cameras = cameras.Length;
                    }
                }
            }
        }
        //Check for cameras from modded regions
        if (CRS)
        {
            string[] crsRoomFile = new string[]
            {
                string.Empty
            };
            for (int i = 0; i < moddedPaths.Count; i++)
            {
                foreach (RoomInfo info in crsRoomList)
                {
                    //Check if Room .txt Exists
                    if (File.Exists(string.Concat(new object[]
                    {
                        moddedPaths[i] + "-Rooms" + Path.DirectorySeparatorChar +  info.name + ".txt"
                    })))
                    //Load Room .txt into Array
                    {
                        crsRoomFile = File.ReadAllLines(string.Concat(new object[]
                        {
                            moddedPaths[i] + "-Rooms" + Path.DirectorySeparatorChar + info.name + ".txt"
                        }));
                        //Split line with camera info
                        if (crsRoomFile != new string[] { string.Empty } && crsRoomFile.Length > 3)
                        {
                            string[] cameras = crsRoomFile[3].Split(new char[]
                            {
                            '|'
                            });
                            if (cameras != null)
                            {
                                info.cameras = cameras.Length;
                            }
                        }
                    }
                }
            }
        }
        //Grab each room's subregion from the map .txt
        List<string> subregionNames = new List<string>();
        string[] propFile = new string[]
        {
            string.Empty
        };
        //Check Properties .txt exists
        if (File.Exists(string.Concat(new object[]
        {
            rootFolder,
            "World",
            Path.DirectorySeparatorChar,
            region,
            Path.DirectorySeparatorChar,
            "Properties.txt",
        })))
        //Load Properties .txt into String Array
        {
            propFile = File.ReadAllLines(string.Concat(new object[]
            {
                rootFolder,
                "World",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "Properties.txt",
            }));
            if (propFile != new string[] { string.Empty })
            {
                subregionNames.Add("None");
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
                    WarpModMenu.subregionNames[region] = subregionNames;
                }
            }
        }
        //Grab each room's subregion from the map .txt
        string[] mapFile = new string[]
        {
            string.Empty
        };
        //Check Map .txt exists
        if (File.Exists(string.Concat(new object[]
        {
            rootFolder,
            "World",
            Path.DirectorySeparatorChar,
            region,
            Path.DirectorySeparatorChar,
            "map_",
            region,
            ".txt"
        })))
        //Load Map .txt into String Array
        {
            mapFile = File.ReadAllLines(string.Concat(new object[]
            {
                rootFolder,
                "World",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "map_",
                region,
                ".txt"
            }));
            if (mapFile != new string[] { string.Empty })
            {
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
        }
        if (CRS)
        {
            for (int c = 0; c < moddedPaths.Count; c++)
            {
                //Grab each room's subregion from the map .txt
                string[] crsPropFile = new string[]
                {
                    string.Empty
                };
                //Check Properties .txt exists
                if (File.Exists(string.Concat(new object[]
                {
                    moddedPaths[c] + Path.DirectorySeparatorChar + "Properties.txt",
                })))
                //Load Properties .txt into String Array
                {
                    crsPropFile = File.ReadAllLines(string.Concat(new object[]
                    {
                        moddedPaths[c] + Path.DirectorySeparatorChar + "Properties.txt",
                    }));
                    if (crsPropFile != new string[] { string.Empty })
                    {
                        subregionNames = new List<string>();
                        subregionNames.Add("None");
                        for (int i = 0; i < crsPropFile.Length; i++)
                        {
                            if (crsPropFile[i].StartsWith("Subregion"))
                            {
                                //[0] Room Name - [1] prop Data
                                string[] subName = Regex.Split(Custom.ValidateSpacedDelimiter(crsPropFile[i], ":"), ": ");
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
                            WarpModMenu.subregionNames[region] = subregionNames;
                        }
                    }
                }
                //Grab each room's subregion from the map .txt
                string[] crsMapFile = new string[]
                {
                    string.Empty
                };
                //Check Map .txt exists
                if (File.Exists(string.Concat(new object[]
                {
                    moddedPaths[c] + Path.DirectorySeparatorChar + "map_" + region + ".txt"
                })))
                //Load Map .txt into String Array
                {
                    crsMapFile = File.ReadAllLines(string.Concat(new object[]
                    {
                        moddedPaths[c] + Path.DirectorySeparatorChar + "map_" + region + ".txt"
                    }));
                    if (crsMapFile != new string[] { string.Empty })
                    {
                        for (int i = 0; i < crsMapFile.Length; i++)
                        {
                            if (crsMapFile[i].StartsWith("Off") || crsMapFile[i].StartsWith("Connection"))
                            {
                                continue;
                            }
                            //[0] Room Name - [1] Map Data
                            string[] nameAndInfo = Regex.Split(Custom.ValidateSpacedDelimiter(crsMapFile[i], ":"), ": ");
                            //Find matching room in RoomList and give it its subregion number
                            foreach (RoomInfo info in crsRoomList)
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
                }
            }
        }

        if (CRS)
        {
            //Combining the two lists
            foreach (RoomInfo info in crsRoomList)
            {
                if (info.subregionName == "Null")
                {
                    info.subregionName = "None";
                }
                //If a room with this name does not exist, add it
                if (!roomList.Exists(x => x.name == info.name))
                {
                    roomList.Add(info);
                }
                //If a room with this name exists but another property has changed, replace it
                if (roomList.Exists(x => x.name == info.name && (x.type != info.type || x.cameras != info.cameras || x.subregion != info.subregion)))
                {
                    int index = roomList.FindIndex(x => x.name == info.name);
                    roomList[index] = info;
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