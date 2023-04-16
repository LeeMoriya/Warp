using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System;
using UnityEngine;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using System.IO;

[BepInPlugin("LeeMoriya.Warp", "Warp", "1.83")]
public class WarpMod : BaseUnityPlugin
{
    public bool init = false;
    public ManualLogSource logger;
    public WarpMod()
    {
        logger = new ManualLogSource("Warp");
    }
    public void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        //On.RainWorld.Update += RainWorld_Update;
    }

    //private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    //{
    //    try
    //    {
    //        orig.Invoke(self);
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogException(e);
    //    }
    //}

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);
        if (!init)
        {
            WarpModMenu.MenuHook();
            try
            {
                Futile.atlasManager.LoadAtlas("sprites\\warpatlas");
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
            init = true;
        }
    }
}

