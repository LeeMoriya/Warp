using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class RoomFinder
{
    public List<string> RoomList(string region)
    {
        string[] array = new string[]
        {
            string.Empty
        };
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
        return roomList;
    }
}

