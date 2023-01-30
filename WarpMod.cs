using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System;
using UnityEngine;
using System.Reflection;
using BepInEx;

[BepInPlugin("LeeMoriya.Warp", "Warp", "1.8")]
public class WarpMod : BaseUnityPlugin
{
    public bool init = false;
    public WarpMod()
    {

    }
    public void Awake()
    {
        On.RainWorld.OnModsInit += delegate (On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (!init)
            {
                WarpModMenu.MenuHook();
                init = true;
            }
        };
    }
}

