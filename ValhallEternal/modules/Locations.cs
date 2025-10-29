using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;
using ValhallEternal.common;

namespace ValhallEternal.modules
{
    internal static class Locations
    {
        public static void AddLocationsToWorldGen()
        {
            GameObject prefab = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/locations/ve_eternal_tribute_loc.prefab");

            Enum.TryParse(ValConfig.TributeLocationBiome.Value, out Heightmap.Biome targetBiome);
            //Logger.LogInfo($"Target biome for tribute location: {targetBiome} - {prefab}");
            LocationConfig tributeLocConfig = new LocationConfig();
            tributeLocConfig.Biome = targetBiome;
            tributeLocConfig.Quantity = ValConfig.MaxTributeLocationsGeneration.Value;
            tributeLocConfig.Priotized = true;
            tributeLocConfig.ExteriorRadius = 5f;
            tributeLocConfig.SlopeRotation = true;
            tributeLocConfig.MinAltitude = 1f;
            tributeLocConfig.ClearArea = false;
            tributeLocConfig.RandomRotation = false;
            tributeLocConfig.MinDistanceFromSimilar = ValConfig.MinDistanceBetweenTributeLocations.Value;

            ZoneManager.Instance.AddCustomLocation(new CustomLocation(prefab, true, tributeLocConfig));
        }
    }
}
