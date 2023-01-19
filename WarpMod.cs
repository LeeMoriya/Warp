﻿using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System;
using UnityEngine;
using System.Reflection;
using BepInEx;

[BepInPlugin("LeeMoriya.Warp", "Warp", "1.7")]
public class WarpMod : BaseUnityPlugin
{
    public WarpMod()
    {

    }
    public void Awake()
    {
        On.RainWorld.OnModsInit += delegate (On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            WarpModMenu.MenuHook();
        };
    }
}

