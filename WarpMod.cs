using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System;
using UnityEngine;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using System.IO;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

[BepInPlugin("LeeMoriya.Warp", "Warp", "1.84.8")]
public class WarpMod : BaseUnityPlugin
{
    public bool init = false;
    public WarpMod()
    {

    }
    public void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

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

