using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using EnumsNET;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Linq.Expressions.Interpreter;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using Unity.Collections;
using Unity.Jobs;
using Unity.Services.Analytics.Internal;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace focmod;

/* With comments */

public static class Main
{
    public const int bundleSize = 3;
    private static ManualLogSource? modLogger;
    public static void Load(ManualLogSource logger)
    {
        //PolyMod.Loader.AddPatchDataType("unitEffect", typeof(UnitEffect));
        PolyMod.Registry.autoidx++;
        PolyMod.Loader.AddPatchDataType("customstuff", typeof(CityReward));
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
        logger.LogMessage("City Rewards dll loaded.");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SessionManager), nameof(SessionManager.StartNewSession))]
    public static void InitDict() {
        customs.Clear();
    }

    public static Dictionary<int, string> customs = new Dictionary<int, string>();

    //Would've been nice if it had worked...
    /*public static CityReward[] newcityrewards = new CityReward[]
    {
        CityReward.Workshop,
        CityReward.Explorer,
        EnumCache<CityReward>.GetType("customone"),
        CityReward.CityWall,
        CityReward.Resources,
        EnumCache<CityReward>.GetType("customtwo"),
        CityReward.PopulationGrowth,
        CityReward.BorderGrowth,
        EnumCache<CityReward>.GetType("customthree"),
        CityReward.Park,
        CityReward.SuperUnit,
        EnumCache<CityReward>.GetType("customfour"),
    };*/


    [HarmonyPostfix]
    [HarmonyPatch(typeof(ImprovementDataExtensions), nameof(ImprovementDataExtensions.GetCityRewardsForLevel))]
    public static void GetCityRewardsForLevelOverwrite(ref Il2CppStructArray<CityReward> __result, ImprovementData data, int level)
    {
        int num = System.Math.Min(level - 1, CityRewardData.cityRewards.Length / 2 - 1) * 2;
        CityReward thirdoption = EnumCache<CityReward>.GetType("customfour");
        switch (level)
        {
            case 1:
                thirdoption = EnumCache<CityReward>.GetType("customone");
                break;
            case 2:
                thirdoption = EnumCache<CityReward>.GetType("customtwo");
                break;
            case 3:
                thirdoption = EnumCache<CityReward>.GetType("customthree");
                break;
            case 4:
                thirdoption = EnumCache<CityReward>.GetType("customfour");
                break;          
        }

        //We can't actually define a new cityRewards and do that[num+2] (even though that would make sense)
        __result = new CityReward[]
        {
        CityRewardData.cityRewards[num],
        CityRewardData.cityRewards[num + 1],
        thirdoption
        };
        
    

    }

    public static bool isCustomReward(string s)
    {
        //Is it even a city reward?
        string[] words = s.Split("_");
        if (words[1] != "rewards")
        {
            return false;
        }


        if (int.TryParse(words[2], out int whatever))
        {
            return true;
        }

        return false;
    }

    public static CityReward getEnum(string s)
    {
        int a = int.Parse(s.Split("_")[2]);
        return (CityReward)a;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIIconData), nameof(UIIconData.GetSprite))]
    public static void Override(UIIconData __instance, ref Sprite __result, string id)
    {
        if (Main.isCustomReward(id))
        {
            __result = PolyMod.Registry.GetSprite(EnumCache<CityReward>.GetName(Main.getEnum(id)));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityRewardAction), nameof(CityRewardAction.Execute))]
    public static void CustomRewards(CityRewardAction __instance, GameState state)
    {
        TileData tile = state.Map.GetTile(__instance.Coordinates);
        if (tile == null || tile.improvement == null) //Safety first
        {
            return;
        }
        PlayerState playerState;
        if (!state.TryGetPlayer(tile.owner, out playerState))
        {
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customone"))
        {
            state.ActionStack.Add(new TrainAction(playerState.Id, UnitData.Type.Warrior, __instance.Coordinates, 0));
        }
    }
}


