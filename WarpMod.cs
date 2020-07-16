using Partiality.Modloader;
using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;

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
    public static PartialityMod mod;
    public WarpMod()
    {
        this.ModID = "Warp";
        this.Version = "V1.0";
        this.author = "LeeMoriya";
    }
    public override void OnEnable()
    {
        base.OnEnable();
        WarpMenu.MenuHook();
        mod = this;
    }
}

