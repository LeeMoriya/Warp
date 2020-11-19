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
            Debug.Log("CRS is true");
            if (Directory.Exists(string.Concat(new object[]
            {
                Custom.RootFolderDirectory(),
                "Mods",
                Path.DirectorySeparatorChar,
                "CustomResources"
            })))
            {
                Debug.Log("CRS path found");
                foreach (string dir in Directory.GetDirectories(Custom.RootFolderDirectory() + "Mods" + Path.DirectorySeparatorChar + "CustomResources"))
                {
                    Debug.Log("Found Dir: " + dir);
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
}

