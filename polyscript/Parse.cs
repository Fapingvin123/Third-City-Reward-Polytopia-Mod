using BepInEx.Logging;
using Polytopia.Data;
using Newtonsoft.Json.Linq;

namespace focmod;

public static class Parse
{
    private static Dictionary<PolytopiaBackendBase.Common.TribeType, string> overrides = new();
    public static void Load(ManualLogSource logger)
    {
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