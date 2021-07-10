using Partiality.Modloader;
using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using Partiality;
using System;
using UnityEngine;
using System.Reflection;

//Remove PublicityStunt requirement
//--------------------------------------------------------------------------------------
[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }
        public string AssemblyName { get; }
    }
}
//--------------------------------------------------------------------------------------


public class WarpMod : PartialityMod
{
    //AutoUpdate Support
    public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/4/1";
    public int version = 16;
    public string keyE = "AQAB";
    public string keyN = "lDaM5h0hJUvZcIdiWXH4qfdia/V8UWzikqRIiC9jVGA87jMrafo4EWOTk0MMIQZWHVy+msVzvEAVR3V45wZShFu7ylUndroL5u4zyqHfVeAeDIALfBrM3J4BIM1rMi4wieYdLIF6t2Uj4GVH7iU59AIfobew1vICUILu9Zib/Aw2QY6Nc+0Cz6Lw3xh7DL/trIMaW7yQfYRZUaEZBHelN2JGyUjKkbby4vL6gySfGlVl1OH0hYYhrhNwnQrOow8WXFMIu/WyTA3cY3wqkjd4/WRJ+EvYtMKTwfG+TZiHGst9Bg1ZTFfvEvrTFiPadTf19iUnfyL/QJaTAD8qe+rba5KwirIElovqFpYNH9tAr7SpjixjbT3Igmz+SlqGa9wSbm1QWt/76QqpyAYV/b5G/VzbytoZrhkEVdGuaotD4tXh462AhK5xoigB8PEt+T3nWuPdoZlVo5hRCxoNleH4yxLpVv8C7TpQgQHDqzHMcEX79xjiYiCvigCq7lLEdxUD0fhnxSYVK0O+y7T+NXkk3is/XqJxdesgyYUMT81MSou9Ur/2nv9H8IvA9QeIqso05hK3c496UOaRJS27WJhrxABtU+HHtxo9SifmXjisDj3IV46uTeVp5bivDTu1yBymgnU8qli/xmwWxKvOisi9ZOZsg4vFHaY31gdUBWOz4dU=";
    // ------------------------------------------------
    public static PartialityMod mod;
    //Mod Support Toggles
    public static bool customRegions = false;
    public static bool jollyCoop = false;
    public static bool msc = false;
    public WarpMod()
    {
        this.ModID = "Warp";
        this.Version = "V1.5";
        this.author = "LeeMoriya";
    }
    public override void OnEnable()
    {
        base.OnEnable();
        WarpModMenu.MenuHook();
        mod = this;
    }

    public static void CheckForMSC()
    {
        try
        {
            if (typeof(RainWorld).GetMethod("MoreSlugcatsInit", BindingFlags.Public | BindingFlags.Instance) != null)
            {
                Debug.Log("MSC Detected!");
                msc = true;
            }
        }
        catch
        {
            Debug.Log("Some kind of error when detecting MSC");
            msc = false;
        }
    }

    public static void DisableMSCWarp(RainWorld rw)
    {
        //Disables MSC's outdated Warp version
        var disableWarp = rw.setup.GetType().GetField("disableWarp");
        object temp = rw.setup;
        disableWarp.SetValue(temp, true);
        rw.setup = (RainWorldGame.SetupValues)temp;
        Debug.Log("MSC Warp Disabled");
    }
}

