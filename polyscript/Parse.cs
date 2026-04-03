using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using EnumsNET;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMicrosoft.Win32;
using Il2CppSystem;
using Il2CppSystem.Linq.Expressions.Interpreter;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.UIElements.UIR;
using Newtonsoft.Json.Linq;

namespace focmod;

public static class Parse
{
    private static Dictionary<PolytopiaBackendBase.Common.TribeType, string> overrides = new();
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Parse));
        PolyMod.Loader.AddTypeHandler(typeof(PolytopiaBackendBase.Common.TribeType), HandleTest);
        static void HandleTest(JObject token, bool onCreatedEnumCache)
        {
            if (token["rewardoneOverride"] != null)
            {
                string value = token["rewardoneOverride"].ToObject<string>();
                if (EnumCache<PolytopiaBackendBase.Common.TribeType>.TryGetType(token.Path.Split('.').Last(), out var type)) { }
                overrides.Add(type, value);
            }
        }
    }

    public static UnitData.Type GetOverride(PolytopiaBackendBase.Common.TribeType tribeType)
    {
        if(overrides.TryGetValue(tribeType, out string value))
        {
            return EnumCache<UnitData.Type>.GetType(value);
        }
        return EnumCache<UnitData.Type>.GetType("rewardwarrior");
    }
}