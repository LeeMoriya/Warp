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
    public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/10/1";
    public int version = 0;
    public string keyE = "AQAB";
    public string keyN = "st3BC7gf2eDIQAxPg4qLtTermfWIQv6D96LdTdurG+wIgGw3ntnWRb2SSaICj1QooD/zPGV4FIrq1oeNvnpou8v3ztpuq82mH3beaX5VD+w7jQ05dukS+szpiVjrpxrM7Xs6C/NXUJZ5hERwnOUMb0BdhCRbo2WHu3MM5vXHHHoqu/QbcsJpzfaU9lIlB7/sYRcDkzG35t2wM2qayaNH6yvNFy07PYrvQJtPGJ+W193+VtkBEnrcUUJYd3vPetnInxlXMjyXKpYontEJY752ICSJ4fegxIDnXjNJi7lsM99wWO71dNOCFLEwGZghEoaniU2l3PF4FBHIy3IvVvg6C7ULhKAx2VM0VqA358yARIfA5ug/q20rl/RAk29K+5D0XrPnlz8BlUlI5FpGqwzwit4NIKQMho7ErmrBU0UuXmy0bEy+cpo46gTFKHeZFvuZ4awH/shdG/LSRGH1P32uLq3yk8BdQThnrIYXr1joV8HzCzbhstaUfb/VwC/SqRq1R6FW4ipIJGyCTPESDAeu9DvB3gfN6WROfqBiOmud8CyalCvpnzgIfnEpvWFqx2rNNBqCfFc5ujnYvMnEj7t48oRpqIvDxTQC+/gRHNqRmgMkLiG/ABPcTUYAXIq52r/XjVRPxmjjgy9Cdbw9/6yO1wJhsI/EThq9RNjoXzfIJzU=";
    public static PartialityMod mod;
    //Mod Support Toggles
    public static bool customRegions = false;
    public static bool jollyCoop = false;
    public static bool mapWarp = true;
    public static bool msc = false;
    public WarpMod()
    {
        this.ModID = "Warp";
        this.Version = "1.65";
        this.author = "LeeMoriya";
    }
    public override void OnEnable()
    {
        base.OnEnable();
        WarpModMenu.MenuHook();
        try { WarpConsole.RegisterCommands(); }
        catch { };
        MapWarp.Hook(); //Created by Henpemaz
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

