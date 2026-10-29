using BepInEx;
using BepInEx.Bootstrap;
using System.Security.Permissions;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ModBlame;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class ModBlame : BaseUnityPlugin
{
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "___Chinchi";
    public const string PluginName = "ModBlame";
    public const string PluginVersion = "1.0.0";

    private void Awake()
    {
        Log.Init(Logger);
        PluginConfig.Init(Config);

        HookNoOrig.Init();
        ILCursorRemove.Init();
        LogSpam.Init();
    }

    // Trying to avoid a soft BepInDependency, which would make this initialise before us.
    private void Start()
    {
        if (Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
        {
            PluginConfig.RiskOfOptionsInit(Info.Location);
        }
    }
}