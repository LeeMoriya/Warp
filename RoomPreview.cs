﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using System.Globalization;
using RWCustom;
using Menu;
using static System.Net.Mime.MediaTypeNames;

public class RoomPreview : Dialog
{
    public WarpModMenu.WarpContainer owner;
    public PauseMenu pauseMenu;
    public string roomName, regionName;
    public int width, height, defaultWaterLevel;
    public float floatWaterLevel;
    public Room.Tile[,] tiles;
    public bool water;
    public List<IntVector2> waterFallTiles, wormGrassTiles, garbageWormTiles;

    public FSprite previewSprite, bg;
    public int cooldown;
    public int openingCounter, closingCounter;
    public float bgLastAlpha, previewLastAlpha;
    public float scaleFactor;
    public FLabel instructions;
    public FLabel exportLabel;
    public WarpButton scaleButton;
    public WarpButton exportButton;
    public WarpButton openFolderButton;
    public int currentScaleMultiplier = 1;

    public Texture2D image;

    public RoomPreview(ProcessManager manager, WarpModMenu.WarpContainer owner, string roomName, string regionName, PauseMenu pauseMenu) : base(manager)
    {
        this.pauseMenu = pauseMenu;
        this.roomName = roomName;
        this.regionName = regionName;
        this.owner = owner;
        waterFallTiles = new List<IntVector2>();
        wormGrassTiles = new List<IntVector2>();
        garbageWormTiles = new List<IntVector2>();

        //Load data from room file
        string[] data = File.ReadAllLines(WorldLoader.FindRoomFile(this.roomName, false, ".txt"));

        //Determine water level
        if (data[1].Split(new char[]
        {
            '|'
        })[1] == "-1")
        {
            defaultWaterLevel = -1;
        }
        else
        {
            water = true;
            defaultWaterLevel = Convert.ToInt32(data[1].Split(new char[]
            {
                '|'
            })[1], CultureInfo.InvariantCulture);
            floatWaterLevel = new Vector2(10f + (float)0 * 20f, 10f + (float)defaultWaterLevel * 20f).y;
        }

        //Get room width and height
        string[] size = data[1].Split('|')[0].Split('*');
        this.width = Convert.ToInt32(size[0], CultureInfo.InvariantCulture);
        this.height = Convert.ToInt32(size[1], CultureInfo.InvariantCulture);

        //Generate blank tile list based on room size
        tiles = new Room.Tile[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Room.Tile(x, y, Room.Tile.TerrainType.Air, false, false, false, 0, (y <= defaultWaterLevel ? 1 : 0) + (y == defaultWaterLevel ? 1 : 0));
            }
        }

        IntVector2 tile = new IntVector2(0, height - 1);

        string[] tileList = data[11].Split('|');
        for (int i = 0; i < tileList.Length - 1; i++)
        {
            string[] currentTile = tileList[i].Split(',');
            tiles[tile.x, tile.y].Terrain = (Room.Tile.TerrainType)(int.Parse(currentTile[0], NumberStyles.Any, CultureInfo.InvariantCulture));

            for (int s = 1; s < currentTile.Length; s++)
            {
                switch (currentTile[s])
                {
                    case "1":
                        tiles[tile.x, tile.y].verticalBeam = true;
                        break;
                    case "2":
                        tiles[tile.x, tile.y].horizontalBeam = true;
                        break;
                    case "3":
                        if (tiles[tile.x, tile.y].shortCut < 1)
                        {
                            tiles[tile.x, tile.y].shortCut = 1;
                        }
                        break;
                    case "4":
                        tiles[tile.x, tile.y].shortCut = 2;
                        break;
                    case "5":
                        tiles[tile.x, tile.y].shortCut = 3;
                        break;
                    case "9":
                        tiles[tile.x, tile.y].shortCut = 4;
                        break;
                    case "12":
                        tiles[tile.x, tile.y].shortCut = 5;
                        break;
                    case "6":
                        tiles[tile.x, tile.y].wallbehind = true;
                        break;
                    case "7":
                        tiles[tile.x, tile.y].hive = true;
                        break;
                    case "8":
                        waterFallTiles.Add(tile);
                        break;
                    case "10":
                        garbageWormTiles.Add(tile);
                        break;
                    case "11":
                        wormGrassTiles.Add(tile);
                        break;
                }
            }
            tile.y--;
            if (tile.y < 0)
            {
                tile.x++;
                tile.y = height - 1;
            }
        }

        image = new Texture2D(width * 3, height * 3);
        Color[] transparent = new Color[width * 3 * height * 3];
        for (int i = 0; i < transparent.Length; i++)
        {
            transparent[i] = new Color(0.35f, 0.35f, 0.35f, 1f);
        }
        image.SetPixels(0, 0, width * 3, height * 3, transparent);

        for (int x = 0; x < width * 3; x += 3)
        {
            for (int y = 0; y < height * 3; y += 3)
            {
                Color[] tilePixels = new Color[9];

                if (tiles[x / 3, y / 3].wallbehind)
                {
                    tilePixels = new Color[9]
                    {
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                        new Color(0.25f, 0.25f, 0.25f, 1f),
                    };
                    image.SetPixels(x, y, 3, 3, tilePixels);
                }
                if (tiles[x / 3, y / 3].Terrain == Room.Tile.TerrainType.Solid && tiles[x / 3, y / 3].Terrain != Room.Tile.TerrainType.Slope)
                {
                    tilePixels = new Color[9]
                    {
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x, y, 3, 3, tilePixels);
                }

                //Determine what angle the slope is
                int xt = x / 3;
                int yt = y / 3;

                if (IdentifySlope(new IntVector2(xt, yt), tiles) == Room.SlopeDirection.UpRight)
                {
                    tilePixels = new Color[4]
                    {
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x, y, 2, 2, tilePixels);
                    image.SetPixel(x, y + 2, new Color(0.2f, 0.2f, 0.2f, 1f));
                    image.SetPixel(x + 2, y, new Color(0.2f, 0.2f, 0.2f, 1f));
                }
                else if (IdentifySlope(new IntVector2(xt, yt), tiles) == Room.SlopeDirection.UpLeft)
                {
                    tilePixels = new Color[4]
                    {
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x + 1, y, 2, 2, tilePixels);
                    image.SetPixel(x + 2, y + 2, new Color(0.2f, 0.2f, 0.2f, 1f));
                    image.SetPixel(x, y, new Color(0.2f, 0.2f, 0.2f, 1f));
                }
                else if (IdentifySlope(new IntVector2(xt, yt), tiles) == Room.SlopeDirection.DownRight)
                {
                    tilePixels = new Color[4]
                    {
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x, y + 1, 2, 2, tilePixels);
                    image.SetPixel(x + 2, y + 2, new Color(0.2f, 0.2f, 0.2f, 1f));
                    image.SetPixel(x, y, new Color(0.2f, 0.2f, 0.2f, 1f));
                }
                else if (IdentifySlope(new IntVector2(xt, yt), tiles) == Room.SlopeDirection.DownLeft)
                {
                    tilePixels = new Color[4]
                    {
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                           new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x + 1, y + 1, 2, 2, tilePixels);
                    image.SetPixel(x, y + 2, new Color(0.2f, 0.2f, 0.2f, 1f));
                    image.SetPixel(x + 2, y, new Color(0.2f, 0.2f, 0.2f, 1f));
                }

                if (tiles[x / 3, y / 3].Terrain == Room.Tile.TerrainType.Floor)
                {
                    tilePixels = new Color[6]
                    {
                        new Color(0.21f, 0.21f, 0.21f, 1f),
                        new Color(0.21f, 0.21f, 0.21f, 1f),
                        new Color(0.21f, 0.21f, 0.21f, 1f),
                        new Color(0.21f, 0.21f, 0.21f, 1f),
                        new Color(0.21f, 0.21f, 0.21f, 1f),
                        new Color(0.21f, 0.21f, 0.21f, 1f),
                    };
                    image.SetPixels(x, y + 1, 3, 2, tilePixels);
                }
                if (tiles[x / 3, y / 3].verticalBeam)
                {
                    tilePixels = new Color[3]
                    {
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x + 1, y, 1, 3, tilePixels);
                }
                if (tiles[x / 3, y / 3].horizontalBeam)
                {
                    tilePixels = new Color[3]
                    {
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                        new Color(0.2f, 0.2f, 0.2f, 1f),
                    };
                    image.SetPixels(x, y + 1, 3, 1, tilePixels);
                }
                if (yt < defaultWaterLevel)
                {
                    Color[] sample = image.GetPixels(x, y, 3, 3);
                    for (int s = 0; s < sample.Length; s++)
                    {
                        sample[s] = Color.Lerp(sample[s], new Color(0f, 0.5f, 1f, 1f), 0.2f);
                    }
                    image.SetPixels(x, y, 3, 3, sample);
                }
                if (tiles[x / 3, y / 3].shortCut == 1) //Shortcut trail
                {
                    image.SetPixel(x + 1, y + 1, new Color(0.5f, 0.5f, 0.5f));
                }
                if (tiles[x / 3, y / 3].shortCut == 2) //Exit
                {
                    image.SetPixel(x + 1, y, new Color(0.2f, 1f, 0.2f));
                    image.SetPixel(x, y + 1, new Color(0.2f, 1f, 0.2f));
                    image.SetPixel(x + 2, y + 1, new Color(0.2f, 1f, 0.2f));
                    image.SetPixel(x + 1, y + 2, new Color(0.2f, 1f, 0.2f));
                    image.SetPixel(x + 1, y + 1, new Color(0.2f, 1f, 0.2f));
                }
                if (tiles[x / 3, y / 3].shortCut == 3) //Den
                {
                    image.SetPixel(x + 1, y, new Color(1f, 0.2f, 0.2f));
                    image.SetPixel(x, y + 1, new Color(1f, 0.2f, 0.2f));
                    image.SetPixel(x + 2, y + 1, new Color(1f, 0.2f, 0.2f));
                    image.SetPixel(x + 1, y + 2, new Color(1f, 0.2f, 0.2f));
                    image.SetPixel(x + 1, y + 1, new Color(1f, 0.2f, 0.2f));
                }
                if (tiles[x / 3, y / 3].shortCut == 4) //Whack-a-mole
                {
                    image.SetPixel(x + 1, y, new Color(0.2f, 0.2f, 1f));
                    image.SetPixel(x, y + 1, new Color(0.2f, 0.2f, 1f));
                    image.SetPixel(x + 2, y + 1, new Color(0.2f, 0.2f, 1f));
                    image.SetPixel(x + 1, y + 2, new Color(0.2f, 0.2f, 1f));
                    image.SetPixel(x + 1, y + 1, new Color(0.2f, 0.2f, 1f));
                }
                if (tiles[x / 3, y / 3].shortCut == 5) //Gate room
                {
                    image.SetPixel(x + 1, y, new Color(1f, 1f, 0.2f));
                    image.SetPixel(x, y + 1, new Color(1f, 1f, 0.2f));
                    image.SetPixel(x + 2, y + 1, new Color(1f, 1f, 0.2f));
                    image.SetPixel(x + 1, y + 2, new Color(1f, 1f, 0.2f));
                    image.SetPixel(x + 1, y + 1, new Color(1f, 1f, 0.2f));
                }
                if (tiles[x / 3, y / 3].Terrain == Room.Tile.TerrainType.ShortcutEntrance) //Shortcut entrance
                {
                    if (GetTile(tiles, xt - 1, yt).shortCut > 0)
                    {
                        image.SetPixel(x + 2, y, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 2, y + 1, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 2, y + 2, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 1, y + 1, new Color(0.5f, 0.5f, 0.5f));
                    }
                    if (GetTile(tiles, xt + 1, yt).shortCut > 0)
                    {
                        image.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x, y + 1, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x, y + 2, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 1, y + 1, new Color(0.5f, 0.5f, 0.5f));
                    }
                    if (GetTile(tiles, xt, yt + 1).shortCut > 0)
                    {
                        image.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 1, y, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 2, y, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 1, y + 1, new Color(0.5f, 0.5f, 0.5f));
                    }
                    if (GetTile(tiles, xt, yt - 1).shortCut > 0)
                    {
                        image.SetPixel(x, y + 2, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 1, y + 2, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 2, y + 2, new Color(0.5f, 0.5f, 0.5f));
                        image.SetPixel(x + 1, y + 1, new Color(0.5f, 0.5f, 0.5f));
                    }
                }
            }
        }

        #region One-Pixel-Density
        //One pixel quality
        //Texture2D image = new Texture2D(width, height);
        //Color[] transparent = new Color[width * height];
        //for (int i = 0; i < transparent.Length; i++)
        //{
        //    transparent[i] = new Color(0.35f, 0.35f, 0.35f, 1f);
        //}
        //image.SetPixels(0, 0, width, height, transparent);

        //for (int x = 0; x < width; x++)
        //{
        //    for (int y = 0; y < height; y++)
        //    {
        //        Color col = new Color(0.1f, 0.1f, 0.1f, 1f);
        //        if (tiles[x, y].wallbehind)
        //        {
        //            col = new Color(0.25f, 0.25f, 0.25f, 1f);
        //        }
        //        if (tiles[x, y].Terrain != Room.Tile.TerrainType.Air)
        //        {
        //            col = new Color(0.35f, 0.35f, 0.35f, 1f);
        //        }
        //        if (tiles[x, y].Terrain == Room.Tile.TerrainType.Floor)
        //        {
        //            col = new Color(0.4f, 0.4f, 0.4f, 1f);
        //        }
        //        if (tiles[x, y].horizontalBeam || tiles[x, y].verticalBeam)
        //        {
        //            col = Color.Lerp(col, new Color(0.45f, 0.45f, 0.45f, 1f), 0.8f);
        //        }
        //        if (tiles[x, y].shortCut == 1)
        //        {
        //            col = new Color(0.3f, 0.3f, 0.3f, 1f);
        //        }
        //        if (tiles[x, y].shortCut == 2)
        //        {
        //            col = new Color(0f, 1f, 0f, 1f);
        //        }
        //        if (tiles[x, y].shortCut == 3)
        //        {
        //            col = new Color(1f, 0f, 0f, 1f);
        //        }
        //        if (tiles[x, y].shortCut == 4)
        //        {
        //            col = new Color(1f, 0.4f, 0f, 1f);
        //        }
        //        if (tiles[x, y].shortCut == 5)
        //        {
        //            col = new Color(1f, 1f, 0f, 1f);
        //        }
        //        if (y < defaultWaterLevel)
        //        {
        //            col = Color.Lerp(col, new Color(0f, 0.5f, 1f, 1f), 0.25f);
        //        }
        //        image.SetPixel(x, y, col);
        //    }
        //}
        #endregion

        image.Apply();
        image.filterMode = FilterMode.Point;

        if (!Futile.atlasManager.DoesContainAtlas($"warpPreview_{roomName}"))
        {
            Futile.atlasManager.LoadAtlasFromTexture($"warpPreview_{roomName}", image, false);
        }

        bg = new FSprite("Futile_White", true);
        bg.SetAnchor(0.5f, 0.5f);
        bg.x = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.x / 2);
        bg.y = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.y / 2);
        bg.scale = 5000f;
        bg.color = new Color(0f, 0f, 0f);
        bg.alpha = 0f;
        pages[0].Container.AddChild(bg);

        previewSprite = new FSprite($"warpPreview_{roomName}", true);
        previewSprite.SetAnchor(0.5f, 0.5f);
        previewSprite.x = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.x / 2) + 0.01f;
        previewSprite.y = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.y / 2);
        previewSprite.alpha = 0f;

        float scaleWidth = Custom.rainWorld.options.ScreenSize.x / (width * 3);
        float scaleHeight = Custom.rainWorld.options.ScreenSize.y / (height * 3);

        // Choose the minimum scaling factor to fit either the width or height
        scaleFactor = Math.Min(scaleWidth, scaleHeight);

        previewSprite.scale = scaleFactor * 0.83f;
        pages[0].Container.AddChild(previewSprite);

        instructions = new FLabel("font", "Left click on the preview to warp to that location  -  Right click to dismiss");
        instructions.SetAnchor(0.5f, 0.5f);
        instructions.x = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.x / 2) + 0.01f;
        instructions.y = 20.01f;
        instructions.alpha = 0f;
        pages[0].Container.AddChild(instructions);

        exportLabel = new FLabel("font", "Shift + E  -  Export options");
        exportLabel.SetAnchor(0.5f, 0.5f);
        exportLabel.x = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.x / 2) + 0.01f;
        exportLabel.y = 45f;
        exportLabel.alpha = 0f;
        exportLabel.color = new Color(0.5f, 0.5f, 0.5f);
        pages[0].Container.AddChild(exportLabel);

        openingCounter = 10;
        closingCounter = -1;
        cooldown = 30;

        //Export(roomName, image, 2);
    }

    public void Export(string roomName, Texture2D originalTexture, int scale)
    {
        int newWidth = originalTexture.width * scale;
        int newHeight = originalTexture.height * scale;

        RenderTexture renderTexture = new RenderTexture(newWidth, newHeight, 0);
        //RenderTexture lastTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Graphics.Blit(originalTexture, renderTexture);

        Texture2D outputTexture = new Texture2D(newWidth, newHeight);
        outputTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        outputTexture.Apply();

        byte[] png = outputTexture.EncodeToPNG();

        string path = $"{UnityEngine.Application.persistentDataPath}{Path.DirectorySeparatorChar}Warp{Path.DirectorySeparatorChar}Export{Path.DirectorySeparatorChar}{regionName}{Path.DirectorySeparatorChar}";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        File.WriteAllBytes(path + $"{roomName}_preview.png", png);
    }

    public Room.SlopeDirection IdentifySlope(IntVector2 pos, Room.Tile[,] tiles)
    {
        if (GetTile(tiles, pos.x, pos.y).Terrain == Room.Tile.TerrainType.Slope)
        {
            if (GetTile(tiles, pos.x - 1, pos.y).Terrain == Room.Tile.TerrainType.Solid)
            {
                if (GetTile(tiles, pos.x, pos.y - 1).Terrain == Room.Tile.TerrainType.Solid)
                {
                    return Room.SlopeDirection.UpRight;
                }
                if (GetTile(tiles, pos.x, pos.y + 1).Terrain == Room.Tile.TerrainType.Solid)
                {
                    return Room.SlopeDirection.DownRight;
                }
            }
            else if (GetTile(tiles, pos.x + 1, pos.y).Terrain == Room.Tile.TerrainType.Solid)
            {
                if (GetTile(tiles, pos.x, pos.y - 1).Terrain == Room.Tile.TerrainType.Solid)
                {
                    return Room.SlopeDirection.UpLeft;
                }
                if (GetTile(tiles, pos.x, pos.y + 1).Terrain == Room.Tile.TerrainType.Solid)
                {
                    return Room.SlopeDirection.DownLeft;
                }
            }
        }
        return Room.SlopeDirection.Broken;
    }

    public Room.Tile GetTile(Room.Tile[,] tiles, int x, int y)
    {
        if (x > -1 && y > -1 && x < tiles.GetLength(0) && y < tiles.GetLength(1))
        {
            return tiles[x, y];
        }
        if (tiles[Custom.IntClamp(x, 0, tiles.GetLength(0) - 1), Custom.IntClamp(y, 0, tiles.GetLength(1) - 1)].Terrain != Room.Tile.TerrainType.ShortcutEntrance && tiles[Custom.IntClamp(x, 0, tiles.GetLength(0) - 1), Custom.IntClamp(y, 0, tiles.GetLength(1) - 1)].shortCut == 0)
        {
            return tiles[Custom.IntClamp(x, 0, tiles.GetLength(0) - 1), Custom.IntClamp(y, 0, tiles.GetLength(1) - 1)];
        }
        Room.Tile tile = tiles[Custom.IntClamp(x, 0, tiles.GetLength(0) - 1), Custom.IntClamp(y, 0, tiles.GetLength(1) - 1)];
        return new Room.Tile(tile.X, tile.Y, (tile.Terrain != Room.Tile.TerrainType.ShortcutEntrance) ? tile.Terrain : Room.Tile.TerrainType.Solid, tile.verticalBeam, tile.horizontalBeam, tile.wallbehind, 0, tile.waterInt);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        bgLastAlpha = bg.alpha;
        previewLastAlpha = previewSprite.alpha;

        if (openingCounter >= 0)
        {
            bg.alpha = Mathf.Lerp(bgLastAlpha, Mathf.Lerp(0f, 0.8f, Mathf.InverseLerp(10, 0, openingCounter)), timeStacker);
            previewSprite.alpha = Mathf.Lerp(previewLastAlpha, Mathf.Lerp(0f, 1f, Mathf.InverseLerp(10, 0, openingCounter)), timeStacker);
            instructions.alpha = Mathf.Lerp(bgLastAlpha, Mathf.Lerp(0f, 0.8f, Mathf.InverseLerp(10, 0, openingCounter)), timeStacker);
            exportLabel.alpha = Mathf.Lerp(bgLastAlpha, Mathf.Lerp(0f, 0.8f, Mathf.InverseLerp(10, 0, openingCounter)), timeStacker);
        }
        else if (closingCounter >= 0)
        {
            openingCounter = -1;
            bg.alpha = Mathf.Lerp(bgLastAlpha, Mathf.Lerp(0.8f, 0f, Mathf.InverseLerp(10, 0, closingCounter)), timeStacker);
            previewSprite.alpha = Mathf.Lerp(previewLastAlpha, Mathf.Lerp(1f, 0f, Mathf.InverseLerp(10, 0, closingCounter)), timeStacker);
            instructions.alpha = Mathf.Lerp(previewLastAlpha, Mathf.Lerp(1f, 0f, Mathf.InverseLerp(10, 0, closingCounter)), timeStacker);
            exportLabel.alpha = Mathf.Lerp(previewLastAlpha, Mathf.Lerp(1f, 0f, Mathf.InverseLerp(10, 0, closingCounter)), timeStacker);
        }
    }

    public override void Update()
    {
        base.Update();
        cooldown--;

        if (openingCounter >= 0) { openingCounter--; }
        if (closingCounter >= 0) { closingCounter--; }

        if (cooldown <= 0 && openingCounter == -1 && closingCounter == -1 && Input.GetMouseButton(1))
        {
            closingCounter = 10;
        }

        if (closingCounter < 0 && Input.GetMouseButton(0))
        {
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            mousePos -= new Vector2(previewSprite.x, previewSprite.y);
            mousePos /= previewSprite.scale * 3;
            mousePos.x += width / 2;
            mousePos.y += height / 2;
            //Debug.Log($"Room: {width}x{height} - Coord: {Mathf.RoundToInt(mousePos.x)}x{Mathf.RoundToInt(mousePos.y)}");
            if (mousePos.x > 0 && mousePos.x < width && mousePos.y > 0 && mousePos.y < height)
            {
                WarpModMenu.coords = new IntVector2(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
                WarpModMenu.newRoom = roomName;
                WarpModMenu.warpActive = true;
                pauseMenu.Singal(null, "CONTINUE");
                closingCounter = 0;
            }
        }
        if (closingCounter == 0)
        {
            pages[0].Container.RemoveAllChildren();
            manager.StopSideProcess(this);
            owner.previewVisible = false;
            WarpModMenu.roomPreview = null;
        }

        if(exportButton == null && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.E))
        {
            scaleButton = new WarpButton(this, pages[0], "X1", "SCALE", new Vector2(683f - 135f, 30f), new Vector2(50f, 30f), new Color(0.5f, 0.5f, 0.5f));
            exportButton = new WarpButton(this, pages[0], "EXPORT", "EXPORT", new Vector2(683f - 75f, 30f), new Vector2(80f, 30f), new Color(0.5f, 0.5f, 0.5f));
            openFolderButton = new WarpButton(this, pages[0], "OPEN FOLDER", "OPEN", new Vector2(683f + 15f, 30f), new Vector2(120f, 30f), new Color(0.5f, 0.5f, 0.5f));
            pages[0].subObjects.Add(scaleButton);
            pages[0].subObjects.Add(exportButton);
            pages[0].subObjects.Add(openFolderButton);

            exportLabel.y = 20f;
            exportLabel.text = $"{image.width} x {image.height} pixels  -  {roomName}_preview.png";
            exportLabel.color = new Color(0.8f, 0.8f, 0.8f);
            instructions.text = "";

            if (!Directory.Exists($"{UnityEngine.Application.persistentDataPath}{Path.DirectorySeparatorChar}Warp{Path.DirectorySeparatorChar}Export"))
            {
                Directory.CreateDirectory($"{UnityEngine.Application.persistentDataPath}{Path.DirectorySeparatorChar}Warp{Path.DirectorySeparatorChar}Export");
            }
        }
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if(message == "SCALE")
        {
            if(currentScaleMultiplier == 1)
            {
                currentScaleMultiplier = 2;
                scaleButton.menuLabel.text = "X2";
            }
            else if (currentScaleMultiplier == 2)
            {
                currentScaleMultiplier = 5;
                scaleButton.menuLabel.text = "X5";
            }
            else if(currentScaleMultiplier == 5)
            {
                currentScaleMultiplier = 10;
                scaleButton.menuLabel.text = "X10";
            }
            else if(currentScaleMultiplier == 10)
            {
                currentScaleMultiplier = 1;
                scaleButton.menuLabel.text = "X1";
            }
            exportLabel.text = $"{image.width * currentScaleMultiplier} x {image.height * currentScaleMultiplier} pixels  -  {roomName}_preview.png";
            PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        }
        if(message == "EXPORT")
        {
            Export(roomName, image, currentScaleMultiplier);
            exportLabel.text = $"Saved: {UnityEngine.Application.persistentDataPath}/Warp/Export/{regionName}/{roomName}_preview.png";
            PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        }
        if(message == "OPEN")
        {
            UnityEngine.Application.OpenURL($"{UnityEngine.Application.persistentDataPath}{Path.DirectorySeparatorChar}Warp{Path.DirectorySeparatorChar}Export");
        }
    }

    public override void ShutDownProcess()
    {
        base.ShutDownProcess();
        pages[0].Container.RemoveAllChildren();
    }
}

