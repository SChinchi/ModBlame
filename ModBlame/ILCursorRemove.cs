using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModBlame;

internal class ILCursorRemove
{
    private static readonly HashSet<string> removeEvents = [];
    private static readonly HashSet<string> removeRangeEvents = [];

    private static List<Regex> whitelist = [];

    internal static void Init()
    {
        PluginConfig.file.SettingChanged += ParseSettings;
        ParseSettings(null, null);

        new Hook(typeof(ILCursor).GetMethod(nameof(ILCursor.Remove)), LogILCursorRemove);
        new Hook(typeof(ILCursor).GetMethod(nameof(ILCursor.RemoveRange)), LogILCursorRemoveRange);
    }

    private static void ParseSettings(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
    {
        whitelist = [.. Utils.SplitText(PluginConfig.cursorRemoveWhitelist.Value).Select(p => new Regex(p))];
    }

    private static ILCursor LogILCursorRemove(Func<ILCursor, ILCursor> orig, ILCursor self)
    {
        if (PluginConfig.cursorRemoveEnabled.Value)
        {
            string caller = ExtractCaller();
            if (removeEvents.Contains(caller) == false && Utils.MatchPattern(caller, whitelist) == false)
            {
                Log.Warning($"ILCursor.Remove called by {caller}");
                removeEvents.Add(caller);
            }
        }
        return orig(self);
    }

    private static ILCursor LogILCursorRemoveRange(Func<ILCursor, int, ILCursor> orig, ILCursor self, int num)
    {
        if (PluginConfig.cursorRemoveEnabled.Value)
        {
            string caller = ExtractCaller();
            if (removeRangeEvents.Contains(caller) == false && Utils.MatchPattern(caller, whitelist) == false)
            {
                Log.Warning($"ILCursor.RemoveRange called by {caller}");
                removeRangeEvents.Add(caller);
            }
        }
        return orig(self, num);
    }

    private static string ExtractCaller()
    {
        // The frames should be a series of hook delegates and trampolines until we reach a method with an ILContext parameter.
        // This is either the hook that removed the instructions, or a generic pattern method that multiple hooks call.
        // In either case, this should give us enough to go by.
        StackTrace trace = new StackTrace();
        foreach (StackFrame frame in trace.GetFrames())
        {
            MethodBase method = frame.GetMethod();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.ParameterType == typeof(ILContext))
                {
                    return $"{method.DeclaringType}.{method.Name}";
                }
            }
        }
        // Fallback, but we should never really reach here.
        return trace.ToString();
    }
}
