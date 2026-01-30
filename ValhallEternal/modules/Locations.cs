using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValhallEternal.common;

namespace ValhallEternal.modules
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
        }

        public static void AddLocationsToWorldGen(Heightmap.Biome targetBiome, string prefabname, int maxLocationAmount = 10)
        {

            GameObject prefab = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/locations/{prefabname}.prefab");
            GameObject locationContainer = ZoneManager.Instance.CreateLocationContainer(prefab);

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
