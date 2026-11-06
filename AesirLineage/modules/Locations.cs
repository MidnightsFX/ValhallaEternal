using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.modules
{
    internal static class Locations
    {
        public static void SetupLocations() {
            AddLocationsToWorldGen(Heightmap.Biome.Meadows, "ve_tribute_gefjun", ValConfig.MaxMeadowsLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.BlackForest, "ve_tribute_vor", ValConfig.MaxBlackForestLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.Swamp, "ve_tribute_baldur", ValConfig.MaxSwampLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.Mountain, "ve_tribute_skaldi", ValConfig.MaxMountainLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.Plains, "ve_tribute_syn", ValConfig.MaxPlainsLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.Mistlands, "ve_tribute_freya", ValConfig.MaxMistlandsLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.AshLands, "ve_tribute_hel", ValConfig.MaxAshlandsLocations.Value);
            AddLocationsToWorldGen(Heightmap.Biome.DeepNorth, "ve_tribute_vidarr", ValConfig.MaxDeepNorthLocations.Value,
                fallback: "ve_tribute_skaldi", deity: Deities.Deity.Vidarr);
        }

        /// <summary>
        /// Places a shrine prefab from the bundle. While a deity has no shrine of its own in the bundle,
        /// <paramref name="fallback"/>'s is copied and given to <paramref name="deity"/> instead; its shrine
        /// isn't networked, so the location rebuilds it from this copy every time it loads.
        /// </summary>
        public static void AddLocationsToWorldGen(Heightmap.Biome targetBiome, string prefabname, int maxLocationAmount = 10,
            string fallback = null, Deities.Deity? deity = null)
        {

            GameObject prefab = AesirLineage.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/locations/{prefabname}.prefab");
            bool borrowed = prefab == null && fallback != null;
            if (borrowed) { prefab = AesirLineage.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/locations/{fallback}.prefab"); }
            if (prefab == null) {
                Logger.LogWarning($"Tribute generation: no prefab for {prefabname}; {targetBiome} gets no shrines.");
                return;
            }
            GameObject locationContainer = ZoneManager.Instance.CreateLocationContainer(prefab);
            if (borrowed) {
                // The location takes its name from the container, so the copy needs its own.
                locationContainer.name = prefabname;
                foreach (SacrificeUI shrine in locationContainer.GetComponentsInChildren<SacrificeUI>(true)) {
                    shrine.enableExclusiveDeityMode = true;
                    shrine.ExclusiveDeity = deity.Value;
                }
            }

            // Top level Mock replacement, inline
            // This prevents the need to register mocks as prefabs ahead of time
            //for (int i = 0; i < prefab.transform.childCount; i++) {
            //    GameObject child = prefab.transform.GetChild(i).gameObject;
            //    if (child.name.StartsWith("JVLmock")) {
            //        string mockprefabname = child.name.Split(new[] { '_' }, 2)[1];
            //        Logger.LogDebug($"Mocking replacement of {mockprefabname}");
            //        GameObject mockedgo = PrefabManager.Instance.GetPrefab(mockprefabname);
            //        if (mockedgo != null) {
            //            GameObject replacement = GameObject.Instantiate(mockedgo);
            //            replacement.transform.position = child.transform.position;
            //        } else {
            //            Logger.LogWarning($"Could not find prefab with name: {mockprefabname}");
            //        }
            //        //child.SetActive(false);
            //        GameObject.Destroy(child);
            //    }
            //}

            //SacrificeUI sacrificeUI = prefab.GetComponentInChildren<SacrificeUI>();

            Logger.LogInfo($"Tribute generation: {targetBiome} location {locationContainer.name}");
            LocationConfig tributeLocConfig = new LocationConfig();
            tributeLocConfig.Biome = targetBiome;
            tributeLocConfig.Quantity = maxLocationAmount;
            tributeLocConfig.Priotized = true;
            tributeLocConfig.ExteriorRadius = 5f;
            tributeLocConfig.SlopeRotation = true;
            tributeLocConfig.MinAltitude = 1f;
            tributeLocConfig.ClearArea = false;
            tributeLocConfig.RandomRotation = true;
            tributeLocConfig.MinDistanceFromSimilar = ValConfig.MinDistanceBetweenTributeLocations.Value;

            ZoneManager.Instance.AddCustomLocation(new CustomLocation(locationContainer, fixReference: true, tributeLocConfig));
        }
    }
}
