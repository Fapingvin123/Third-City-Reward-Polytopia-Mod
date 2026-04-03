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
using PolyMod;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace focmod;

/* With comments */

public static class Main
{
    public static ManualLogSource modLogger;
    public static void Load(ManualLogSource logger)
    {
        PolyMod.Loader.AddPatchDataType("customstuff", typeof(CityReward));
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
        logger.LogMessage("City Rewards dll loaded.");
    }

    #region Visuals

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RewardPopup), nameof(RewardPopup.SetRewards))]
    private static void RewardPopup_RewardWarriorRender(RewardPopup __instance, PlayerState playerState, Il2CppStructArray<CityReward> rewards, bool isReplay = false)
    {
        var gameLogicData = GameManager.GameState.GameLogicData;
        gameLogicData.TryGetData(playerState.tribe, out TribeData tribeData);
        int i = 0;
        foreach (var reward in __instance.rewardButtonsList)
        {
            if (rewards[i] == EnumCache<CityReward>.GetType("customone"))
            {
                UnitData @override;
                gameLogicData.TryGetData(Parse.GetOverride(playerState.tribe), out @override);
                UIUnitRenderer uiunitRenderer = UIUtils.GetUIUnitRenderer(@override.type, tribeData, playerState.skinType);
                reward.sprite = null;
                reward.SetUnitRenderer(uiunitRenderer);

            }
            i++;
        }
    }


    public static bool isCustomReward(string s)
    {
        //Is it even a city reward?
        string[] words = s.Split("_");
        if (words[1] != "rewards")
            return false;

        if (int.TryParse(words[2], out int whatever))
            return true;

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

    private static void AddHouseIfNotPresent(CityPlot plot, PolytopiaSpriteRenderer house)
    {
        bool flag = false;
        foreach (var h in plot.houses)
        {
            if (h.sprite == house.sprite) { flag = true; break; }
        }
        if (!flag) plot.AddHouse(house);
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
        bool cityhasobelisk = a.improvement.HasReward(EnumCache<CityReward>.GetType("customtwo"));

        if (cityhasobelisk)
        {
            PolytopiaBackendBase.Common.TribeType tribe = __instance.Tribe;
            PolytopiaBackendBase.Common.SkinType skinType = __instance.SkinType;
            PolytopiaSpriteRenderer house = __instance.GetHouse(tribe, __instance.HOUSE_WORKSHOP, skinType);
            house.sprite = PolyMod.Registry.GetSprite("healobelisk");
            int count = __instance.plots.Count;
            int num = (int)System.Math.Floor(System.Math.Sqrt(count));
            //AddHouseIfNotPresent(__instance.plots[(num*(num-1))/2], house);
            AddHouseIfNotPresent(__instance.plots[((num*(num+1))/2) - 1], house);
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

    private static bool TrySpawnResource(TileData citytile, out string decision)
    {
        if (citytile.terrain == Polytopia.Data.TerrainData.Type.Field)
        {
            decision = "createfruit";
            return true;
        }
        if (citytile.terrain == Polytopia.Data.TerrainData.Type.Forest)
        {
            decision = "creategame";
            return true;
        }
        if (citytile.terrain == Polytopia.Data.TerrainData.Type.Water)
        {
            decision = "createfish";
            return true;
        }
        decision = "none"; return false;
    }

    public static void Populate(GameState state, TileData tile, int FruitsToSpawn)
    {
        var citytiles = ActionUtils.GetCityAreaSorted(state, tile);
        citytiles.Reverse();
        int counter = FruitsToSpawn;
        for (int i = 0; i < citytiles.Count; i++)
        {
            if (TrySpawnResource(citytiles[i], out string decision) && citytiles[i].resource == null && citytiles[i].improvement == null)
            {
                if (counter > 0)
                {
                    Tile tilerender = MapRenderer.Current.GetTileInstance(tile.coordinates);
                    tilerender.SpawnSparkles();
                    state.ActionStack.Add(new BuildAction(tile.owner, EnumCache<ImprovementData.Type>.GetType(decision), citytiles[i].coordinates, false));
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
        if (!state.TryGetPlayer(tile.owner, out playerState) || !GameManager.GameState.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
        {
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customone")) // Elite Warrior / Free Unit
        {
            GameManager.GameState.GameLogicData.TryGetData(Parse.GetOverride(playerState.tribe), out UnitData @override);
            @override = GameManager.GameState.GameLogicData.GetOverride(@override, tribeData);
            state.ActionStack.Add(new TrainAction(playerState.Id, @override.type, __instance.Coordinates, 0));
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customtwo")) // Healing Obelisk
        {
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customthree")) // Bountiful Lands
        {
            Main.Populate(state, tile, 5);
            return;
        }
        if (__instance.Reward == EnumCache<CityReward>.GetType("customfour")) // Free Tech
        {
            var unlockableTech = Main.FOCGetUnlockableTech(playerState);
            if (unlockableTech == null || unlockableTech.Count == 0)
            {
                state.ActionStack.Add(new IncreaseCurrencyAction(playerState.Id, tile.coordinates, 10, 0));
                return;
            }
            if (!playerState.AutoPlay)
            {
                NotificationManager.Notify(Localization.Get("foc.freetechdesc"), Localization.Get("wcontroller.reward.customfour") + "!");
                return;
            }

            // AI Still selects a random technology
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

    #region FreeTechRework

    private static TechView currentTechView = null;

    private static bool PlayerCanFreeTech(PlayerState player, GameState gameState)
    {
        var cities = player.GetCityTiles(gameState);
        bool flag = false;
        foreach (TileData city in cities)
        {
            if (city != null && city.improvement != null && city.improvement.type == ImprovementData.Type.City && city.owner == player.Id)
            {
                if (city.improvement.HasReward(EnumCache<CityReward>.GetType("customfour"))) { flag = true; break; }
            }
        }
        return flag;
    }

    private static bool EligibleFreeTech()
    {
        return GameManager.LocalPlayer != null && GameManager.GameState != null && PlayerCanFreeTech(GameManager.LocalPlayer, GameManager.GameState);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechView), nameof(TechView.UpdateInfoText))]
    private static void CanAlsoUnlockFreeTech(TechView __instance)
    {
        currentTechView = __instance;
        if (EligibleFreeTech())
        {
            __instance.infoText.text += Localization.Get("foc.freetechavailable");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetTechPrice))]
    public static void TechIsActuallyFree(ref int __result, TechData techData, PlayerState playerState, GameState state)
    {
        if (EligibleFreeTech() && playerState.Id == GameManager.LocalPlayer.Id)
        {
            __result = 0;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.RefreshState))]
    private static void OverrideState(TechItem __instance)
    {
        if (EligibleFreeTech() && (__instance.state == TechItem.State.Expensive || __instance.state == TechItem.State.Available))
        {
            __instance.state = TechItem.State.Available;
            __instance.outline.color = Color.yellow;
            __instance.button.CanRegisterHover = true;
            __instance.resourceWidget.label.color = Color.yellow;
            __instance.bg.color = new Color(0.72f, 0.62f, 0.33f);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ResearchAction), nameof(ResearchAction.Execute))]
    private static void RemoveOneFreeTechReward(ResearchAction __instance, GameState state)
    {
        if (__instance.PlayerId != GameManager.LocalPlayer.Id) return;
        if (EligibleFreeTech())
        {
            PlayerState player = GameManager.LocalPlayer;
            var cities = player.GetCityTiles(state);
            foreach (TileData city in cities)
            {
                if (city != null && city.improvement != null && city.improvement.type == ImprovementData.Type.City && city.owner == player.Id)
                {
                    if (city.improvement.HasReward(EnumCache<CityReward>.GetType("customfour")))
                    {
                        Il2CppSystem.Collections.Generic.List<CityReward> newlist = new();
                        bool flag = false;
                        foreach (CityReward reward in city.improvement.rewards)
                        {
                            if (reward == EnumCache<CityReward>.GetType("customfour") && !flag)
                            {
                                flag = true; continue;
                            }
                            newlist.Add(reward);
                        }
                        city.improvement.rewards = newlist;
                        break;
                    }
                }
            }
            currentTechView.RefreshTechItems();
        }
    }

    #endregion
}


