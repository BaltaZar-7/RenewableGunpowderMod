using HarmonyLib;
using Il2Cpp;
using Il2CppNewtonsoft.Json.Linq;
using MelonLoader;
using ModSettings;
using System.Collections.Generic;
using UnityEngine;

namespace RenewableGunpowderMod
{
    // ---- CONFIG ----
    internal class GunpowderSettings : JsonModSettings
    {
        [Section("Spawn chance (%)")]
        [Name("Dusting Sulfur chance")]
        [Slider(0f, 50f)]
        public float sulfurChance = 1f;

        [Name("Stump Remover chance")]
        [Slider(0f, 50f)]
        public float stumpChance = 1f;

        [Name("Scrap Lead chance")]
        [Slider(0f, 50f)]
        public float leadChance = 1f;

        protected override void OnConfirm()
        {
            base.OnConfirm();
            MelonLogger.Msg($"[SETTINGS] Settings saved: Sulfur={sulfurChance}% Stump={stumpChance}% Lead={leadChance}%");
        }
    }

    internal static class Settings
    {
        internal static GunpowderSettings instance;

        internal static void OnLoad()
        {
            instance = new GunpowderSettings();
            instance.AddToModSettings("Renewable Gunpowder - Beachcombing Loot");
        }
    }

    // ---- PATCH ----
    [HarmonyPatch(typeof(RadialObjectSpawner), "GetNextPrefabToSpawn")]
    internal class BeachcombingLootPatch
    {
        private static void Postfix(RadialObjectSpawner __instance, ref GameObject __result)
        {
            try
            {
                if (__instance == null || !__instance.name.Contains("RadialSpawnSpline"))
                    return;

                string originalPrefab = __result != null ? __result.name : "<null>";

                float totalChance = Settings.instance.sulfurChance + Settings.instance.stumpChance + Settings.instance.leadChance;
                float roll = UnityEngine.Random.Range(0f, 100f);

                if (roll > totalChance)
                {
                    return;
#if DEBUG
                    MelonLogger.Msg($"[DEBUG] Keeping original spawn: Spawner={__instance.name}, Prefab={originalPrefab}, roll={roll}");
#endif
                }

                float cumulative = 0f;

                if (Settings.instance.sulfurChance > 0f)
                {
                    cumulative += Settings.instance.sulfurChance;
                    if (roll <= cumulative)
                    {
                        ReplacePrefab(ref __result, "GEAR_DustingSulfur");
                        return;
                    }
                }

                if (Settings.instance.stumpChance > 0f)
                {
                    cumulative += Settings.instance.stumpChance;
                    if (roll <= cumulative)
                    {
                        ReplacePrefab(ref __result, "GEAR_StumpRemover");
                        return;
                    }
                }

                if (Settings.instance.leadChance > 0f)
                {
                    cumulative += Settings.instance.leadChance;
                    if (roll <= cumulative)
                    {
                        ReplacePrefab(ref __result, "GEAR_ScrapLead");
                        return;
                    }
                }

#if DEBUG
                MelonLogger.Msg($"[DEBUG] No replacement chosen: Spawner={__instance.name}, Prefab={originalPrefab}, roll={roll}");
#endif
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[ERROR] Beachcombing patch exception: {ex}");
            }
        }

        private static void ReplacePrefab(ref GameObject result, string gearName)
        {
            GameObject prefab = GearItem.LoadGearItemPrefab(gearName)?.gameObject;
            if (prefab != null)
            {
                result = prefab;
#if DEBUG
                MelonLogger.Msg($"[DEBUG] Replacing spawn with {gearName}");
#endif
            }
            else
            {
                MelonLogger.Warning($"[WARNING] Prefab loading was not successful: {gearName}");
            }
        }
    }

    // ---- MAIN ----
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Settings.OnLoad();
            MelonLogger.Msg("[INFO] RenewableGunpowderMod Loaded. Beachcombing loot Updated.");
        }
    }
}
