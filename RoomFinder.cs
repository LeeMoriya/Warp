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
    public List<string> RoomList(string region, bool CRS)
    {
        string[] array = new string[]
        {
            string.Empty
        };
        //Vanilla region
        if (File.Exists(string.Concat(new object[]
        {
            Custom.RootFolderDirectory(),
            "World",
            Path.DirectorySeparatorChar,
            "Regions",
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
                Custom.RootFolderDirectory(),
                "World",
                Path.DirectorySeparatorChar,
                "Regions",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "world_",
                region,
                ".txt"
            }));
        }
        bool flag = false;
        List<string> roomList = new List<string>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == "END ROOMS")
            {
                break;
            }
            if (flag)
            {
                string[] roomName = Regex.Split(array[i], " : ");
                if (roomName != null)
                {
                    roomList.Add(roomName[0]);
                }
            }
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
                Custom.RootFolderDirectory(),
                "Mods",
                Path.DirectorySeparatorChar,
                "CustomResources"
            })))
            {
                foreach (string dir in Directory.GetDirectories(Custom.RootFolderDirectory() + "Mods" + Path.DirectorySeparatorChar + "CustomResources"))
                {
                    if (Directory.Exists(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + "Regions" + Path.DirectorySeparatorChar + region))
                    {
                        if (File.Exists(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + "Regions" + Path.DirectorySeparatorChar + region + Path.DirectorySeparatorChar + "world_" + region + ".txt"))
                        {
                            array = File.ReadAllLines(string.Concat(new object[]
                            {
                            dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + "Regions" +Path.DirectorySeparatorChar + region,
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
        //Explanation:
        //----------------------------------------------------------------------------------------
        //A New list of RoomInfo objects is created
        //The Vanilla world_XX.txt is scanned for rooms and tags
        //New RoomInfos are added to the list with these name and type fields

        //CRS: Search Mod\CustomResources folder for directories
        //Check each directory for regionInfo/packInfo.json
        //Check that region is activated
        //If so, search the folder for a matching region prefix
        //If found, scan the world_XX.txt for rooms and tags
        //Add rooms and tags to the roomList if they are not duplicates
        //If a room is a duplicate, check to see if tag has changed
        //Assign modded tag if its different to current tag

        //Using the name fields from the list, the Rooms folder is then scanned
        //Individual XX_Room.txts are scanned for the number of cameras
        //List is looped through and entries with matching names have their camera count assigned

        //CRS: string containing path to modded region folder is stored
        //Modded region room folder is scanned for matching room .txts
        //Camera count is assigned to any new rooms
        //Camera count is updated for existing rooms if it's different

        //map_XX.txt is then scanned, each room line is split up to grab the subregion number
        //List is looped through again and subregion numbers are assigned

        //CRS: modded region map_XX.txt is scanned for subregion numbers
        //If the room already exists new subregion numbers are assigned if they're different
        //----------------------------------------------------------------------------------------

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
            Custom.RootFolderDirectory(),
            "World",
            Path.DirectorySeparatorChar,
            "Regions",
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
                Custom.RootFolderDirectory(),
                "World",
                Path.DirectorySeparatorChar,
                "Regions",
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
                    RoomInfo info = new RoomInfo();
                    string[] roomLine = Regex.Split(array[i], " : ");
                    if (roomLine != null)
                    {
                        //Assign Room Name
                        info.name = roomLine[0];
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
            if (Directory.Exists(string.Concat(new object[]
            {
            Custom.RootFolderDirectory(),
            "Mods",
            Path.DirectorySeparatorChar,
            "CustomResources"
            })))
            {
                string customResources = Custom.RootFolderDirectory() + "Mods" + Path.DirectorySeparatorChar + "CustomResources";
                foreach (string dir in Directory.GetDirectories(customResources))
                {
                    //Check whether CRS version is using regionInfo or packInfo
                    string jsonType = "";
                    if (Directory.GetFiles(dir).Contains(dir + Path.DirectorySeparatorChar + "packInfo.json"))
                    {
                        jsonType = "packInfo.json";
                    }
                    else if (Directory.GetFiles(dir).Contains(dir + Path.DirectorySeparatorChar + "regionInfo.json"))
                    {
                        jsonType = "regionInfo.json";
                    }
                    if (jsonType != "")
                    {
                        string[] crsRegion = new string[]
                        {
                            string.Empty
                        };
                        crsRegion = File.ReadAllLines(dir + Path.DirectorySeparatorChar + jsonType);
                        for (int i = 0; i < crsRegion.Length; i++)
                        {
                            if (crsRegion[i].Contains("activated"))
                            {
                                string[] crsLine = Regex.Split(crsRegion[i], ": ");
                                if (crsLine.Length > 0)
                                {
                                    if (crsLine[1].StartsWith("true"))
                                    {
                                        //Region pack is activated, check if matching region XX is found in pack Regions folder
                                        if (File.Exists(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + "Regions" + Path.DirectorySeparatorChar + region + Path.DirectorySeparatorChar + "World_" + region + ".txt"))
                                        {
                                            moddedPaths.Add(dir + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + "Regions" + Path.DirectorySeparatorChar + region + Path.DirectorySeparatorChar);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log(".json file not found in: " + dir);
                    }
                }
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
                string[] crsWorldFile = File.ReadAllLines(moddedPaths[i] + "World_" + region + ".txt");
                for (int l = 0; l < crsWorldFile.Length; l++)
                {
                    if (crsWorldFile[l] == "END ROOMS")
                    {
                        break;
                    }
                    if (crsRoomSection)
                    {
                        RoomInfo info = new RoomInfo();
                        string[] roomLine = Regex.Split(crsWorldFile[l], " : ");
                        if (roomLine != null)
                        {
                            //Assign Room Name
                            info.name = roomLine[0];
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
            Custom.RootFolderDirectory(),
            "World",
            Path.DirectorySeparatorChar,
            "Regions",
            Path.DirectorySeparatorChar,
            region,
            Path.DirectorySeparatorChar,
            "Rooms",
            Path.DirectorySeparatorChar,
            info.name + ".txt"
            })))
            //Load Room .txt into Array
            {
                roomFile = File.ReadAllLines(string.Concat(new object[]
                {
                Custom.RootFolderDirectory(),
                "World",
                Path.DirectorySeparatorChar,
                "Regions",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "Rooms",
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
                        moddedPaths[i] + "Rooms" + Path.DirectorySeparatorChar +  info.name + ".txt"
                    })))
                    //Load Room .txt into Array
                    {
                        crsRoomFile = File.ReadAllLines(string.Concat(new object[]
                        {
                            moddedPaths[i] + "Rooms" + Path.DirectorySeparatorChar + info.name + ".txt"
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
            Custom.RootFolderDirectory(),
            "World",
            Path.DirectorySeparatorChar,
            "Regions",
            Path.DirectorySeparatorChar,
            region,
            Path.DirectorySeparatorChar,
            "Properties.txt",
        })))
        //Load Properties .txt into String Array
        {
            propFile = File.ReadAllLines(string.Concat(new object[]
            {
                Custom.RootFolderDirectory(),
                "World",
                Path.DirectorySeparatorChar,
                "Regions",
                Path.DirectorySeparatorChar,
                region,
                Path.DirectorySeparatorChar,
                "Properties.txt",
            }));
            if (propFile != new string[] { string.Empty })
            {
                for (int i = 0; i < propFile.Length; i++)
                {
                    if (propFile[i].StartsWith("Subregion"))
                    {
                        //[0] Room Name - [1] prop Data
                        string[] subName = Regex.Split(propFile[i], ": ");
                        if (subName.Length > 0)
                        {
                            subregionNames.Add(subName[1]);
                        }
                    }
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
            Custom.RootFolderDirectory(),
            "World",
            Path.DirectorySeparatorChar,
            "Regions",
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
                Custom.RootFolderDirectory(),
                "World",
                Path.DirectorySeparatorChar,
                "Regions",
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
                    if (mapFile[i].StartsWith("Off"))
                    {
                        break;
                    }
                    //[0] Room Name - [1] Map Data
                    string[] nameAndInfo = Regex.Split(mapFile[i], ": ");
                    //Split Map Data by comma, last item in the array is subregion number
                    int subRegion = -1;
                    if (Regex.Split(nameAndInfo[1], ",").Length >= 6)
                    {
                        subRegion = int.Parse(Regex.Split(nameAndInfo[1], ",")[5]);
                    }
                    //Find matching room in RoomList and give it its subregion number
                    foreach (RoomInfo info in roomList)
                    {
                        if (info.name == nameAndInfo[0])
                        {
                            info.subregion = subRegion;
                            if (subRegion > 0)
                            {
                                info.subregionName = subregionNames[subRegion - 1];
                            }
                            else
                            {
                                info.subregionName = "Default";
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
                    moddedPaths[c] + "Properties.txt",
                })))
                //Load Properties .txt into String Array
                {
                    crsPropFile = File.ReadAllLines(string.Concat(new object[]
                    {
                        moddedPaths[c] + "Properties.txt",
                    }));
                    if (crsPropFile != new string[] { string.Empty })
                    {
                        subregionNames = new List<string>();
                        for (int i = 0; i < crsPropFile.Length; i++)
                        {
                            if (crsPropFile[i].StartsWith("Subregion"))
                            {
                                //[0] Room Name - [1] prop Data
                                string[] subName = Regex.Split(crsPropFile[i], ": ");
                                if (subName.Length > 0)
                                {
                                    subregionNames.Add(subName[1]);
                                }
                            }
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
                    moddedPaths[c] + "map_" + region + ".txt"
                })))
                //Load Map .txt into String Array
                {
                    crsMapFile = File.ReadAllLines(string.Concat(new object[]
                    {
                        moddedPaths[c] + "map_" + region + ".txt"
                    }));
                    if (crsMapFile != new string[] { string.Empty })
                    {
                        for (int i = 0; i < crsMapFile.Length; i++)
                        {
                            if (crsMapFile[i].StartsWith("Off"))
                            {
                                break;
                            }
                            //[0] Room Name - [1] Map Data
                            string[] nameAndInfo = Regex.Split(crsMapFile[i], ": ");
                            //Split Map Data by comma, last item in the array is subregion number
                            int subRegion = -1;
                            if (Regex.Split(nameAndInfo[1], ",").Length >= 6)
                            {
                                subRegion = int.Parse(Regex.Split(nameAndInfo[1], ",")[5]);
                            }
                            //Find matching room in RoomList and give it its subregion number
                            foreach (RoomInfo info in crsRoomList)
                            {
                                if (info.name == nameAndInfo[0])
                                {
                                    info.subregion = subRegion;
                                    if (subRegion > 0)
                                    {
                                        info.subregionName = subregionNames[subRegion - 1];
                                    }
                                    else
                                    {
                                        info.subregionName = "Default";
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
                if(info.subregionName == "Null")
                {
                    info.subregionName = "Default";
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

        if (!WarpMenu.masterRoomList.ContainsKey(region))
        {
            WarpMenu.masterRoomList.Add(region, roomList);
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
    public static Color[] typeColors = new Color[6]
    {
        //1 Room
        new Color(0.55f,1f,0.55f),
        //2 Gate
        new Color(1f,1f,0.55f),
        //3 Shelter
        new Color(1f,0.75f,0.55f),
        //4 SwarmRoom
        new Color(1f,0.55f,0.55f),
        //5 ScavTrader
        new Color(1f,0.3f,0.3f),
        //6 ScavOutpost
        new Color(0f,1f,1f)
    };
    public static Color[] sizeColors = new Color[10]
    {
        //0 Cam
        new Color(1f,1f,1f),
        //1 Cam
        new Color(0.55f,1f,0.55f),
        //2 Cam
        new Color(1f,1f,0.55f),
        //3 Cam
        new Color(1f,0.75f,0.55f),
        //4 Cam
        new Color(1f,0.55f,0.55f),
        //5 Cam
        new Color(1f,0.3f,0.3f),
        //6 Cam
        new Color(0.5f,0f,0.5f),
        //7 Cam
        new Color(0.5f,0f,1f),
        //8 Cam
        new Color(0f,0.5f,1f),
        //9+ Cam
        new Color(0f,1f,1f)
    };
    public static Color[] subregionColors = new Color[10]
    {
        //1 Subregion
        new Color(0.55f,1f,0.55f),
        //2 Subregion
        new Color(1f,1f,0.55f),
        //3 Subregion
        new Color(1f,0.75f,0.55f),
        //4 Subregion
        new Color(1f,0.55f,0.55f),
        //5 Subregion
        new Color(1f,0.3f,0.3f),
        //6 Subregion
        new Color(0.5f,0f,0.5f),
        //7 Subregion
        new Color(0.5f,0f,1f),
        //8 Subregion
        new Color(0f,0.5f,1f),
        //9+ Subregion
        new Color(0f,1f,1f),
        //0 Subregion
        new Color(1f,1f,1f),
    };
}


////Grab Subregion Names from Properties .txt
//string[] propFile = new string[]
//{
//            string.Empty
//};
////Check Properties .txt exists
//if (File.Exists(string.Concat(new object[]
//{
//    Custom.RootFolderDirectory(),
//    "World",
//    Path.DirectorySeparatorChar,
//    "Regions",
//    Path.DirectorySeparatorChar,
//    region,
//    Path.DirectorySeparatorChar,
//    "Properties.txt"
//})))
////Load Properties .txt into String Array
//{
//    propFile = File.ReadAllLines(string.Concat(new object[]
//    {
//        Custom.RootFolderDirectory(),
//        "World",
//        Path.DirectorySeparatorChar,
//        "Regions",
//        Path.DirectorySeparatorChar,
//        region,
//        Path.DirectorySeparatorChar,
//        "Properties.txt"
//    }));
//}
////Add Subregion Names to List
//List<string> subregionNames = new List<string>();
//for (int i = 0; i<propFile.Length; i++)
//{
//    if (propFile[i].StartsWith("Sub"))
//    {
//        string[] subLine = Regex.Split(propFile[i], ": ");
//        if (subLine.Length > 0)
//        {
//            subregionNames.Add(subLine[1]);
//        }
//    }
//}