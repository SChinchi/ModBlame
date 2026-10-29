using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModBlame;

internal class PluginConfig
{
    internal static ConfigFile file { get; private set; }
    internal static ConfigEntry<bool> noOrigEnabled { get; private set; }
    internal static ConfigEntry<string> noOrigWhitelist { get; private set; }
    internal static ConfigEntry<bool> cursorRemoveEnabled { get; private set; }
    internal static ConfigEntry<string> cursorRemoveWhitelist { get; private set; }
    internal static ConfigEntry<bool> logSpamEnabled { get; private set; }
    internal static ConfigEntry<string> logSpamText { get; private set; }

    internal static void Init(ConfigFile config)
    {
        file = config;

        noOrigEnabled = config.Bind(
            "Hook No Orig",
            "Enable",
            true,
            "Whether hooks without calling orig will be reported."
        );
        noOrigWhitelist = config.Bind(
            "Hook No Orig",
            "Whitelist",
            "RoR2BepInExPack,MiscFixes,R2API.SkinIDRS.SetCustomIDRS,MultiplayerModTesting.Empty",
            "Comma-separated namespace/method name partials to be ignored. Regex matched."
        );

        cursorRemoveEnabled = config.Bind(
            "ILCursor Remove",
            "Enable",
            true,
            "Whether hooks that use ILCursor.Remove and ILCursor.RemoveRange will be reported."
        );
        cursorRemoveWhitelist = config.Bind(
            "ILCursor Remove",
            "Whitelist",
            "RoR2BepInExPack,MiscFixes",
            "Comma-separated namespace/method name partials to be ignored. Regex matched."
        );

        logSpamEnabled = config.Bind(
            "Log Spam",
            "Enable",
            true,
            "Whether blacklisted text will log the caller origin."
        );
        logSpamText = config.Bind(
            "Log Spam",
            "Text",
            "",
            "Comma-separated text to check for. Regex matched."
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void RiskOfOptionsInit(string assemblyLocation)
    {
        FileInfo iconFile = null;
        FileInfo[] files = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)).GetFiles("icon.png", SearchOption.TopDirectoryOnly);
        if (files != null && files.Length > 0)
        {
            iconFile = files[0];
        }
        if (iconFile != null)
        {
            string name = $"{ModBlame.PluginName}Icon";
            Texture2D texture = new Texture2D(256, 256);
            texture.name = name;
            if (texture.LoadImage(File.ReadAllBytes(iconFile.FullName)))
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprite.name = name;
                ModSettingsManager.SetModIcon(sprite, ModBlame.PluginGUID, ModBlame.PluginName);
            }
        }

        ModSettingsManager.SetModDescription("Find out which mods might be doing cursed stuff.");

        ModSettingsManager.AddOption(new CheckBoxOption(noOrigEnabled));
        ModSettingsManager.AddOption(new StringInputFieldOption(noOrigWhitelist));
        ModSettingsManager.AddOption(new CheckBoxOption(cursorRemoveEnabled));
        ModSettingsManager.AddOption(new StringInputFieldOption(cursorRemoveWhitelist));
        ModSettingsManager.AddOption(new CheckBoxOption(logSpamEnabled));
        ModSettingsManager.AddOption(new StringInputFieldOption(logSpamText));
    }
}
