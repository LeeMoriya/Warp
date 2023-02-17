using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System;
using UnityEngine;
using System.Reflection;
using BepInEx;

[BepInPlugin("LeeMoriya.Warp", "Warp", "1.81")]
public class WarpMod : BaseUnityPlugin
{
    public bool init = false;
    public WarpMod()
    {

    }
    public void OnEnable()
    {
        if (!init)
        {
            WarpModMenu.MenuHook();
            init = true;
        }
    }
}

