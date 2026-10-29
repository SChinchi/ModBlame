using HarmonyLib;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModBlame;

internal class HookNoOrig
{
    private static List<Regex> whitelist = [];

    internal static void Init()
    {
        PluginConfig.file.SettingChanged += ParseSettings;
        ParseSettings(null, null);

        new Hook(typeof(PatchInfo).GetMethod("AddPrefixes", (BindingFlags)(-1)), InspectHarmonyPrefixForReturnFalse);

        MethodBase hookBaseConstructor = typeof(Hook).GetConstructor([
            typeof(MethodBase),
            typeof(MethodInfo),
            typeof(object),
            typeof(HookConfig).MakeByRefType()
        ]);
        new Hook(hookBaseConstructor, InspectHookForNoOrigInvoke);
    }

    private static void ParseSettings(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
    {
        whitelist = [.. Utils.SplitText(PluginConfig.noOrigWhitelist.Value).Select(p => new Regex(p))];
    }

    private delegate void PatchInfoAddPrefixesDelegate(PatchInfo patchInfo, string owner, params HarmonyMethod[] methods);
    private static void InspectHarmonyPrefixForReturnFalse(PatchInfoAddPrefixesDelegate orig, PatchInfo self, string owner, params HarmonyMethod[] methods)
    {
        orig(self, owner, methods);

        foreach (HarmonyMethod harmonyMethod in methods)
        {
            MethodInfo method = harmonyMethod?.method;
            if (method != null && method.ReturnType == typeof(bool))
            {
                string methodName = $"{method.DeclaringType}.{method.Name}";
                if (PluginConfig.noOrigEnabled.Value && Utils.MatchPattern(methodName, whitelist) == false)
                {
                    ILHook temporaryHook = null;
                    try
                    {
                        temporaryHook = new ILHook(method, ScanForReturnFalse, new ILHookConfig { ManualApply = true });
                        temporaryHook.Apply();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to investigate {methodName} for Harmony prefix\n{e}");
                    }
                    finally
                    {
                        temporaryHook?.Dispose();
                    }
                }
            }
        }
    }

    private static void ScanForReturnFalse(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(
                x => x.MatchLdcI4(0),
                x => x.MatchRet()))
        {
            string methodName = il.Method.Name.Replace("::", ".");
            Log.Warning($"HarmonyPrefix returns false from {methodName}");
        }
    }

    private static string origName = null;
    private delegate void HookConstructorDelegate(Hook hook, MethodBase from, MethodInfo to, object target, ref HookConfig config);
    private static void InspectHookForNoOrigInvoke(HookConstructorDelegate orig, Hook self, MethodBase method, MethodInfo to, object target, ref HookConfig hookConfig)
    {
        orig(self, method, to, target, ref hookConfig);

        string methodName = $"{to.DeclaringType}.{to.Name}";
        if (PluginConfig.noOrigEnabled.Value && Utils.MatchPattern(methodName, whitelist) == false)
        {
            ILHook temporaryHook = null;
            try
            {
                // If the method is a coroutine, "to" does not expose directly the code it executes.
                bool isCoroutine = to.ReturnType == typeof(System.Collections.IEnumerator)
                    || (to.ReturnType.IsGenericType && to.ReturnType.GetGenericTypeDefinition() == typeof(IEnumerator<>));
                MethodInfo toActualMethod = isCoroutine ? to.GetStateMachineTarget() : to;
                // Since for coroutines we don't scan "to" itself, we need to cache the name of the orig delegate now,
                // because we won't have access to it from the method we scan.
                origName = "NULL";
                foreach (ParameterInfo parameter in to.GetParameters())
                {
                    if (typeof(Delegate).IsAssignableFrom(parameter.ParameterType))
                    {
                        origName = parameter.ParameterType.Name;
                    }
                }
                temporaryHook = new ILHook(toActualMethod, ScanForNoOrigInvoke, new ILHookConfig { ManualApply = true });
                temporaryHook.Apply();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to investigate {methodName} for not calling orig\n{e}");
            }
            finally
            {
                temporaryHook?.Dispose();
            }
        }
    }

    private static void ScanForNoOrigInvoke(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        MethodReference methodReference = null;
        // Checks for the existence of orig.Invoke, but at the moment the following is a false negative since not all paths do so:
        //
        // HookMethod(Action orig)
        // {
        //     if (flag)
        //     {
        //         orig();
        //     }
        // }
        while (c.TryGotoNext(x => x.MatchCallOrCallvirt(out methodReference)))
        {
            if (methodReference.FullName.Contains(origName))
            {
                return;
            }
        }
        string methodName = il.Method.Name.Replace("::", ".");
        Log.Warning($"Orig skipped by {methodName}");
    }
}
