using ColossalFramework.IO;
using Harmony;
using ICities;
using System;
using System.IO;

namespace CSL_RebalancedIndustries
{
    public class Mod : IUserMod
    {
        public string Name => "Rebalanced Industries";
        public string Description => "Rebalances Industries DLC buildings, reducing cargo traffic";
        private static readonly string harmonyId = "quboid.csl_mods.csl_rebind";
        private static HarmonyInstance harmonyInstance;
        private static readonly object padlock = new object();
        private static bool debugInitialised = false;
        public static readonly string debugPath = Path.Combine(DataLocation.localApplicationData, "rebind_debug.txt");

        /*
        public void OnEnabled()
        {
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);

                if (prefab.m_class.m_service == ItemClass.Service.PlayerIndustry && prefab.m_class.m_subService == ItemClass.SubService.PlayerIndustryFarming)
                {
                    IndustryBuildingAI ai = (IndustryBuildingAI)prefab.m_buildingAI;

                    ai.m_constructionCost = 10;
                }
            }
        }*/


        public static HarmonyInstance GetHarmonyInstance()
        {
            lock (padlock) { 
                if (harmonyInstance == null)
                {
                    harmonyInstance = HarmonyInstance.Create(harmonyId);
                }

                return harmonyInstance;
            }
        }


        public static void DebugLine(String line)
        {
            if (!debugInitialised)
            {
                File.WriteAllText(Mod.debugPath, $"Rebind:Rebalanced Industries log\n");
                debugInitialised = true;
            }

            File.AppendAllText(debugPath, line + $"\n");
        }

        
        public static bool IsIndustriesBuilding(IndustryBuildingAI building)
        {
            switch (building.m_industryType)
            {
                case DistrictPark.ParkType.Industry:
                case DistrictPark.ParkType.Farming:
                case DistrictPark.ParkType.Forestry:
                case DistrictPark.ParkType.Ore:
                case DistrictPark.ParkType.Oil:
                    return true;
            }
            return false;
        }


        public static bool IsExtractorWarehouse(WarehouseAI ai)
        {
            switch (ai.m_storageType)
            {
                case TransferManager.TransferReason.Grain:
                case TransferManager.TransferReason.Logs:
                case TransferManager.TransferReason.Ore:
                case TransferManager.TransferReason.Oil:
                    return true;
            }
            return false;
        }


        public static bool IsField(PlayerBuildingAI ai)
        {
            if (ai is ExtractingFacilityAI) { 
                if (ai.m_info.m_class.m_subService == ItemClass.SubService.PlayerIndustryFarming)
                {
                    return true;
                }
            }
            if (ai is ProcessingFacilityAI) // Rebalance pastures as fields
            {
                foreach (string name in RI_Data.GetProcessorFieldNameStarts())
                {
                    if (ai.name.Length >= name.Length && ai.name.Substring(0, name.Length) == name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static ushort CombineBytes(byte large, byte small)
        {
            return Convert.ToUInt16((large << 8) + small);
        }


        public static void SplitBytes(ushort value, ref byte large, ref byte small)
        {
            large = (byte)(value >> 8);
            small = (byte)(value & 0xFF);
        }
    }
}
