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

namespace focmod;

/* With comments */

public static class Main
{
    public const int bundleSize = 3;
    private static ManualLogSource modLogger;
    public static void Load(ManualLogSource logger)
    {
        //PolyMod.Loader.AddPatchDataType("unitEffect", typeof(UnitEffect));
        PolyMod.Registry.autoidx++;
        PolyMod.Loader.AddPatchDataType("customstuff", typeof(CityReward));
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
        logger.LogMessage("City Rewards dll loaded.");
    }

    #region Visuals


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
    [HarmonyPatch(typeof(CityRenderer), nameof(CityRenderer.RefreshCity))]
    public static void insertObelisk(CityRenderer __instance)
    {
        if (__instance.dataChanged)
        {
            return;
        }
        var a = GameManager.GameState.Map.GetTile(__instance.Coordinates);
        a.improvement.HasReward(EnumCache<CityReward>.GetType("customtwo"));
        bool cityhasobelisk = a.improvement.HasReward(EnumCache<CityReward>.GetType("customtwo"));
        int num = 100;
        int num2 = 9;

        if (cityhasobelisk)
        {
            PolytopiaBackendBase.Common.TribeType tribe = __instance.Tribe;
            PolytopiaBackendBase.Common.SkinType skinType = __instance.SkinType;
            CityPlot nextRandomPlot = __instance.GetNextRandomPlot(ref num, num2);
            //int.TryParse(EnumCache<CityReward>.GetType("customtwo").ToString().Split("_")[2], out int enumnum);
            PolytopiaSpriteRenderer house = __instance.GetHouse(tribe, 1555, skinType); //temporary solution
            int n = nextRandomPlot.houses.Count;
            bool hasObelisk = false;
            for (int i = 0; i < n; i++)
            {
                if (nextRandomPlot.houses[i].sprite == PolyMod.Registry.GetSprite("healobelisk"))
                {
                    hasObelisk = true;
                    break;
                }
            }
            if (!hasObelisk)
            {
                nextRandomPlot.AddHouse(house);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityRenderer), nameof(CityRenderer.GetHouse))]
    public static void GetObelisk(ref PolytopiaSpriteRenderer __result, CityRenderer __instance, PolytopiaBackendBase.Common.TribeType tribe = PolytopiaBackendBase.Common.TribeType.Xinxi, int type = 1, PolytopiaBackendBase.Common.SkinType skinType = PolytopiaBackendBase.Common.SkinType.Default)
    {
        //int.TryParse(EnumCache<CityReward>.GetType("customtwo").ToString().Split("_")[2], out int enumnum);
        if (type == 1555)
        {
            __result.sprite = PolyMod.Registry.GetSprite("healobelisk");
        }
    }

    #endregion
    #region AI

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AI), nameof(AI.ChooseCityReward))]
    public static bool AI_ChooseCityReward(GameState gameState, TileData tile, CityReward[] rewards, ref CityReward __result)
    {
        GameLogicData gld = gameState.GameLogicData;
        CityReward[] rewardarray = GetRewardsForLevel(gld.GetImprovementData(tile.improvement.type), tile.improvement.level - 1);



        System.Random random = new System.Random();
        int num = random.Next(0, rewardarray.Length);


        __result = rewardarray[num];

        return false;
    }

    public static CityReward[] GetRewardsForLevel(ImprovementData data, int level)
    {
        return data.GetCityRewardsForLevel(level);
    }

    #endregion

    #region Custom Rewards
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

        __result = new CityReward[]
        {
        CityRewardData.cityRewards[num],
        CityRewardData.cityRewards[num + 1],
        thirdoption
        };

    }

    public static void Populate(GameState state, TileData tile, int FruitsToSpawn)
    {
        var citytiles = ActionUtils.GetCityAreaSorted(state, tile);
        citytiles.Reverse();
        int counter = FruitsToSpawn;
        for (int i = 0; i < citytiles.Count; i++)
        {
            if (citytiles[i].terrain == Polytopia.Data.TerrainData.Type.Field && citytiles[i].resource == null && citytiles[i].improvement == null)
            {
                if (counter > 0)
                {
                    Tile tilerender = MapRenderer.Current.GetTileInstance(tile.coordinates);
                    tilerender.SpawnSparkles();
                    state.ActionStack.Add(new BuildAction(tile.owner, EnumCache<ImprovementData.Type>.GetType("createfruit"), citytiles[i].coordinates, false));
                    counter--;
                }
            }
        }
        state.ActionStack.Add(new IncreaseCurrencyAction(tile.owner, tile.coordinates, 1 * counter, 0));
    }

    public static List<TechData> FOCGetUnlockableTech(PlayerState player)
    {
        var gld = GameManager.GameState.GameLogicData;
        if (player.tribe == PolytopiaBackendBase.Common.TribeType.None)
        {
            return null;
        }
        TribeData tribe;
        if (GameManager.GameState.GameLogicData.TryGetData(player.tribe, out tribe))
        {
            List<TechData> list = new List<TechData>();
            for (int i = 0; i < player.availableTech.Count; i++)
            {
                TechData @override;
                if (gld.TryGetData(player.availableTech[i], out @override))
                {
                    @override = gld.GetOverride(@override, tribe);
                    foreach (TechData techData in @override.techUnlocks)
                    {
                        TechData override2 = gld.GetOverride(techData, tribe);
                        if (!player.HasTech(override2.type) && !list.Contains(override2))
                        {
                            list.Add(override2);
                        }
                    }
                }
            }
            return list;
        }
        return null;
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
            state.ActionStack.Add(new TrainAction(playerState.Id, EnumCache<UnitData.Type>.GetType("rewardwarrior"), __instance.Coordinates, 0));
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customtwo"))
        {
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customthree"))
        {
            Main.Populate(state, tile, 5);
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customfour"))
        {
            var unlockableTech = Main.FOCGetUnlockableTech(playerState);
            if (unlockableTech == null || unlockableTech.Count == 0)
            {
                state.ActionStack.Add(new IncreaseCurrencyAction(playerState.Id, tile.coordinates, 10, 0));
                return;
            }
            var tech = unlockableTech[state.RandomHash.Range(0, unlockableTech.Count, tile.coordinates.X, tile.coordinates.Y)];
            TechData.Type techtype = tech.type;
            state.ActionStack.Add(new ResearchAction(playerState.Id, techtype, 0));
            return;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EndTurnCommand), nameof(EndTurnCommand.ExecuteDefault))]
    private static void EndTurnCommand_ExecuteDefault(EndTurnCommand __instance, GameState state)
    {

        for (int i = 0; i < state.Map.Tiles.Length; i++)
        {
            TileData tileData = state.Map.Tiles[i];
            if (tileData.unit != null && tileData.unit.owner == __instance.PlayerId && tileData.improvement != null)
            {
                if (!tileData.IsBeingCaptured(state) && tileData.improvement.type == ImprovementData.Type.City)
                {
                    if (tileData.improvement.HasReward(EnumCache<CityReward>.GetType("customtwo")) && tileData.unit.health < tileData.unit.GetMaxHealth(state))
                    {
                        Tile tile = MapRenderer.Current.GetTileInstance(tileData.coordinates);
                        UnitState unit = tileData.unit;
                        int hpincrease = 20;
                        if (unit.health + 20 >= unit.GetMaxHealth(state))
                        {
                            hpincrease = (unit.GetMaxHealth(state) - unit.health);
                        }
                        if (unit.HasEffect(UnitEffect.Poisoned))
                        {
                            unit.RemoveEffect(UnitEffect.Poisoned);
                        }
                        tileData.unit.health += (ushort)hpincrease;
                        tile.Heal(hpincrease);
                    }
                }
            }
        }
    }
#endregion
}


