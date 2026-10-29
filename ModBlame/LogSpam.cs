using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModBlame;

internal class LogSpam
{
    static List<Regex> blacklist = [];

    internal static void Init()
    {
        PluginConfig.file.SettingChanged += ParseSettings;
        ParseSettings(null, null);

        static void ApplyHooks(Type[] arguments, Delegate delegateMethod, bool isFormat = false)
        {
            string postfix = isFormat ? "Format" : "";
            new Hook(typeof(Debug).GetMethod(nameof(Debug.Log) + postfix, arguments), delegateMethod);
            new Hook(typeof(Debug).GetMethod(nameof(Debug.LogWarning) + postfix, arguments), delegateMethod);
            new Hook(typeof(Debug).GetMethod(nameof(Debug.LogError) + postfix, arguments), delegateMethod);
        }
        ApplyHooks([typeof(object)], CatchLog_object);
        ApplyHooks([typeof(object), typeof(UnityEngine.Object)], CatchLog_object_Object);
        ApplyHooks([typeof(string), typeof(object[])], CatchLog_string_params, isFormat: true);
        ApplyHooks([typeof(UnityEngine.Object), typeof(string), typeof(object[])], CatchLog_Object_string_params, isFormat: true);
    }

    private static void ParseSettings(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
    {
        blacklist = [.. Utils.SplitText(PluginConfig.logSpamText.Value).Select(p => new Regex(p))];
    }

    private static void CatchLog_object(Action<object> orig, object message)
    {
        CheckForSpam(message);
        orig(message);
    }

    private static void CatchLog_object_Object(Action<object, UnityEngine.Object> orig, object message, UnityEngine.Object context)
    {
        CheckForSpam(message);
        orig(message, context);
    }

    private delegate void LogDelegate_string_params(string text, params object[] args);
    private static void CatchLog_string_params(LogDelegate_string_params orig, string text, params object[] args)
    {
        CheckForSpam(string.Format(text, args));
        orig(text, args);
    }

    private delegate void LogDelegate_Object_string_params(UnityEngine.Object context, string text, params object[] args);
    private static void CatchLog_Object_string_params(LogDelegate_Object_string_params orig, UnityEngine.Object context, string text, params object[] args)
    {
        CheckForSpam(string.Format(text, args));
        orig(context, text, args);
    }

    private static void CheckForSpam(object message)
    {
        if (PluginConfig.logSpamEnabled.Value && Utils.MatchPattern(message.ToString(), blacklist))
        {
            Log.Warning($"Blacklisted text \"{message}\" encountered:\n{new System.Diagnostics.StackTrace()}");
        }
    }
}
