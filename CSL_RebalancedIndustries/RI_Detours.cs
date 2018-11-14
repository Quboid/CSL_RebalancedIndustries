using ColossalFramework;
using ColossalFramework.UI;
using Harmony;
using UnityEngine;
using System;

namespace CSL_RebalancedIndustries
{
    [HarmonyPatch(typeof(ExtractingFacilityAI))]
    [HarmonyPatch("ProduceGoods")]
    class RI_ExtractingFacilityProduceGoods
    {
        public static void Prefix(ushort buildingID, ref Building buildingData, ExtractingFacilityAI __instance, out ushort __state)
        {
            if (Mod.IsIndustriesBuilding(__instance))
                __state = buildingData.m_customBuffer1;
            else
                __state = 0;
        }


        public static void Postfix(ushort buildingID, ref Building buildingData, ExtractingFacilityAI __instance, ref ushort __state)
        {
            ushort cargoDiff;

            if (Mod.IsIndustriesBuilding(__instance))
            {
                // Output
                cargoDiff = Convert.ToUInt16((buildingData.m_customBuffer1 - __state) / RI_Data.GetFactorCargo(__instance.m_outputResource));
                //Debug.Log($"ID:{buildingID}={(ushort)(__state + cargoDiff)}, {(__state + cargoDiff)}, state:{__state}, buff:{buildingData.m_customBuffer1}, diff:{cargoDiff}");
                buildingData.m_customBuffer1 = (ushort)(__state + cargoDiff);
            }
            else
            {
                Mod.DebugLine($"Unknown EF instance {__instance.name} ({__instance.GetType()})");
            }
        }
    }


    [HarmonyPatch(typeof(ProcessingFacilityAI))]
    [HarmonyPatch("ProduceGoods")]
    class RI_ProcessingFacilityProduceGoods
    {
        public static void Prefix(ushort buildingID, ref Building buildingData, ProcessingFacilityAI __instance, out ushort[] __state)
        {
            __state = new ushort[2];

            if (Mod.IsIndustriesBuilding(__instance))
            {
                __state[0] = buildingData.m_customBuffer1; // Output
                __state[1] = buildingData.m_customBuffer2; // Input
            }
            else
            {
                __state[0] = 0;
                __state[1] = 0;
            }
        }


        public static void Postfix(ushort buildingID, ref Building buildingData, ProcessingFacilityAI __instance, ref ushort[] __state)
        {
            ushort cargoDiff;

            if (Mod.IsIndustriesBuilding(__instance))
            {
                // Output
                cargoDiff = Convert.ToUInt16((buildingData.m_customBuffer1 - __state[0]) / RI_Data.GetFactorCargo(__instance.m_outputResource));
                buildingData.m_customBuffer1 = (ushort)(__state[0] + cargoDiff);

                // Input
                cargoDiff = Convert.ToUInt16((__state[1] - buildingData.m_customBuffer2) / RI_Data.GetFactorCargo(__instance.m_inputResource1));
                buildingData.m_customBuffer2 = (ushort)(__state[1] - cargoDiff);
                //Debug.Log($"ID:{buildingID}, state:{__state}, buff:{buildingData.m_customBuffer2}, diff:{cargoDiff}");
            }
            else
            {
                Mod.DebugLine($"Unknown PF instance {__instance.name} ({__instance.GetType()})");
            }
        }
    }


    [HarmonyPatch(typeof(UniqueFactoryAI))]
    [HarmonyPatch("ProduceGoods")]
    class RI_UniqueFactoryProduceGoods
    {
        public static void Prefix(ushort buildingID, ref Building buildingData, ProcessingFacilityAI __instance, out int[] __state)
        {
            __state = new int[4];

            if (Mod.IsIndustriesBuilding(__instance)) { 
                __state[0] = buildingData.m_customBuffer2;
                __state[1] = Mod.CombineBytes(buildingData.m_teens, buildingData.m_youngs);
                __state[2] = Mod.CombineBytes(buildingData.m_adults, buildingData.m_seniors);
                __state[3] = Mod.CombineBytes(buildingData.m_education1, buildingData.m_education2);
            }
            else
                __state[0] = __state[1] = __state[2] = __state[3] = 0;
        }


        public static void Postfix(ushort buildingID, ref Building buildingData, ProcessingFacilityAI __instance, ref int[] __state)
        {
            ushort cargoDiff;

            if (Mod.IsIndustriesBuilding(__instance))
            {
                // Input
                cargoDiff = Convert.ToUInt16((__state[0] - buildingData.m_customBuffer2) / RI_Data.GetFactorCargo(__instance.m_inputResource1));
                buildingData.m_customBuffer2 = (ushort)(__state[0] - cargoDiff);

                cargoDiff = Convert.ToUInt16((__state[1] - Mod.CombineBytes(buildingData.m_teens, buildingData.m_youngs)) / RI_Data.GetFactorCargo(__instance.m_inputResource2));
                Mod.SplitBytes(Convert.ToUInt16(__state[1] - cargoDiff), ref buildingData.m_teens, ref buildingData.m_youngs);

                cargoDiff = Convert.ToUInt16((__state[2] - Mod.CombineBytes(buildingData.m_adults, buildingData.m_seniors)) / RI_Data.GetFactorCargo(__instance.m_inputResource3));
                Mod.SplitBytes(Convert.ToUInt16(__state[2] - cargoDiff), ref buildingData.m_adults, ref buildingData.m_seniors);

                cargoDiff = Convert.ToUInt16((__state[3] - Mod.CombineBytes(buildingData.m_education1, buildingData.m_education2)) / RI_Data.GetFactorCargo(__instance.m_inputResource4));
                Mod.SplitBytes(Convert.ToUInt16(__state[3] - cargoDiff), ref buildingData.m_education1, ref buildingData.m_education2);

                //Debug.Log($"UF:{__instance.name}, ID:{buildingID}, lastDiff:{cargoDiff}");
                //Debug.Log($"state:{__state[0]},{__state[1]},{__state[2]},{__state[3]}");
                //Debug.Log($"  new:{buildingData.m_customBuffer2},{_combine(buildingData.m_teens, buildingData.m_youngs)},{_combine(buildingData.m_adults, buildingData.m_seniors)},{_combine(buildingData.m_education1, buildingData.m_education2)}");
            }
            else
            {
                Mod.DebugLine($"Unknown UF instance {__instance.name} ({__instance.GetType()})");
            }
        }
    }


    [HarmonyPatch(typeof(IndustryBuildingAI))]
    [HarmonyPatch("GetResourcePrice")]
    class RI_FactorCargoPrice
    {
        public static int Postfix(int price, TransferManager.TransferReason material)
        {
            return Convert.ToInt32(price * RI_Data.GetFactorCargo(material));
        }
    }


    [HarmonyPatch(typeof(CityServiceWorldInfoPanel))]
    [HarmonyPatch("OnSetTarget")]
    class RI_CSWIPOnSetTarget
    {
        public static void Postfix(ref ExtractingFacilityAI ___m_extractingFacilityAI, ref ProcessingFacilityAI ___m_processingFacilityAI, ref InstanceID ___m_InstanceID, ref UIProgressBar ___m_inputBuffer, ref UIPanel ___m_inputSection, ref UIProgressBar ___m_outputBuffer, ref UIPanel ___m_outputSection)
        {
            ushort id = ___m_InstanceID.Building;

            ExtractingFacilityAI ai_ef = ___m_extractingFacilityAI;
            if (ai_ef != null)
            {
                int customBuffer = Convert.ToInt32(Singleton<BuildingManager>.instance.m_buildings.m_buffer[id].m_customBuffer1 * RI_Data.GetFactorCargo(ai_ef.m_outputResource));
                int capacity = Convert.ToInt32(ai_ef.GetOutputBufferSize(id, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[id]) * RI_Data.GetFactorCargo(ai_ef.m_outputResource));
                //Debug.Log($"EFAI-OST: {id} - {customBuffer}/{capacity}");
                //___m_outputBuffer.value = IndustryWorldInfoPanel.SafelyNormalize(customBuffer, capacity);
                ___m_outputSection.tooltip = StringUtils.SafeFormat(
                    ColossalFramework.Globalization.Locale.Get("INDUSTRYPANEL_BUFFERTOOLTIP"),
                    IndustryWorldInfoPanel.FormatResource((uint)customBuffer),
                    IndustryWorldInfoPanel.FormatResourceWithUnit((uint)capacity, ai_ef.m_outputResource)
                );
            }
        }
    }


    [HarmonyPatch(typeof(CityServiceWorldInfoPanel))]
    [HarmonyPatch("UpdateBindings")]
    class RI_CSWIPUpdateBindings
    {
        public static void Postfix(ref ExtractingFacilityAI ___m_extractingFacilityAI, ref ProcessingFacilityAI ___m_processingFacilityAI, ref InstanceID ___m_InstanceID, 
                                   ref UIPanel ___m_inputSection, ref UIPanel ___m_inputTooltipArea, ref UIPanel ___m_outputSection, ref UIPanel ___m_outputTooltipArea)
        {
            ushort id = ___m_InstanceID.Building;

            ExtractingFacilityAI ai_ef = ___m_extractingFacilityAI;
            if (ai_ef != null)
            {

                Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
                _updateTooltip(id, building.m_customBuffer1, ai_ef.GetOutputBufferSize(id, ref building), ai_ef.m_outputResource, ref ___m_outputSection, ref ___m_outputTooltipArea);
                //Debug.Log($"EF-{id}");
            }

            ProcessingFacilityAI ai_pf = ___m_processingFacilityAI;
            if (ai_pf != null)
            {
                Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
                _updateTooltip(id, building.m_customBuffer2, ai_pf.GetInputBufferSize1(id, ref building), ai_pf.m_inputResource1, ref ___m_inputSection, ref ___m_inputTooltipArea);
                _updateTooltip(id, building.m_customBuffer1, ai_pf.GetOutputBufferSize(id, ref building), ai_pf.m_outputResource, ref ___m_outputSection, ref ___m_outputTooltipArea);
                //Debug.Log($"PF-{id}");
            }
        }

        private static void _updateTooltip(int id, int volume, int bufferSize, TransferManager.TransferReason cargo, ref UIPanel panel, ref UIPanel panel2)
        {
            //Debug.Log($"uTt-{id}");
            int customBuffer = Convert.ToInt32(volume * RI_Data.GetFactorCargo(cargo));
            int outputBufferSize = Convert.ToInt32(bufferSize * RI_Data.GetFactorCargo(cargo));
            panel2.tooltip = panel.tooltip = StringUtils.SafeFormat(ColossalFramework.Globalization.Locale.Get("INDUSTRYPANEL_BUFFERTOOLTIP"), IndustryWorldInfoPanel.FormatResource((uint)customBuffer), IndustryWorldInfoPanel.FormatResourceWithUnit((uint)outputBufferSize, cargo));
        } 
    }


    [HarmonyPatch(typeof(WarehouseWorldInfoPanel))]
    [HarmonyPatch("UpdateBindings")]
    class RI_WWIPUpdateBindings
    {
        public static void Postfix(ref InstanceID ___m_InstanceID, ref UILabel ___m_capacityLabel, ref UIPanel ___m_buffer)
        {
            ushort id = ___m_InstanceID.Building;
            Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
            WarehouseAI ai = (WarehouseAI)building.Info.m_buildingAI;
            TransferManager.TransferReason cargoType = ai.GetActualTransferReason(id, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[id]);

            /*Debug.Log($"id:{id} - {ai.name}: {cargoType} ({RI_Data.GetFactorCargo(cargoType)}x), m_sT={ai.m_storageType}, " +
                $"{(ulong)(building.m_customBuffer1 * 100 * RI_Data.GetFactorCargo(cargoType))}/" +
                $"{(uint)(ai.m_storageCapacity * RI_Data.GetFactorCargo(cargoType))} (actual {ai.m_storageCapacity})"
                );*/

            string text = StringUtils.SafeFormat(
                ColossalFramework.Globalization.Locale.Get("INDUSTRYPANEL_BUFFERTOOLTIP"), 
                IndustryWorldInfoPanel.FormatResource((ulong)(building.m_customBuffer1 * 100 * RI_Data.GetFactorCargo(cargoType))), 
                IndustryWorldInfoPanel.FormatResourceWithUnit((uint)(ai.m_storageCapacity * RI_Data.GetFactorCargo(cargoType)), cargoType)
            );
            ___m_buffer.tooltip = text;
            ___m_capacityLabel.text = text;
        }
    }


    [HarmonyPatch(typeof(UniqueFactoryWorldInfoPanel))]
    [HarmonyPatch("GetInputBufferProgress")]
    class RI_UFWIPGetInputBufferProgress
    {
        public static void Postfix(int resourceIndex, ref int amount, ref int capacity, ref InstanceID ___m_InstanceID)
        {
            Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[___m_InstanceID.Building];
            UniqueFactoryAI ai = building.Info.m_buildingAI as UniqueFactoryAI;

            switch (resourceIndex)
            {
                case 0:
                    amount = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource1) * building.m_customBuffer2);
                    capacity = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource1) * ai.GetInputBufferSize1(___m_InstanceID.Building, ref building));
                    break;

                case 1:
                    amount = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource2) * Mod.CombineBytes(building.m_teens, building.m_youngs));
                    capacity = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource2) * ai.GetInputBufferSize2(___m_InstanceID.Building, ref building));
                    break;

                case 2:
                    amount = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource3) * Mod.CombineBytes(building.m_adults, building.m_seniors));
                    capacity = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource3) * ai.GetInputBufferSize3(___m_InstanceID.Building, ref building));
                    break;

                case 3:
                    amount = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource4) * Mod.CombineBytes(building.m_education1, building.m_education2));
                    capacity = Convert.ToInt32(RI_Data.GetFactorCargo(ai.m_inputResource4) * ai.GetInputBufferSize4(___m_InstanceID.Building, ref building));
                    break;
            }
        }
    }


    [HarmonyPatch(typeof(DistrictPark))]
    [HarmonyPatch("IndustrySimulationStep")]
    class RI_DistrictParkIndustrySimulationStep
    {
        static bool[] initialised = new bool[] { false, false, false, false, false, false };

        public static void Prefix(byte parkID)
        {
            //Mod.DebugLine($"DPIP-Pre: {__instance.m_parkProperties.m_industryLevelInfo[1].m_workerLevelupRequirement}");

            DistrictManager dm = Singleton<DistrictManager>.instance;
            DistrictPark dp = dm.m_parks.m_buffer[parkID];
            uint level = (uint)dp.m_parkLevel;

            //Debug.Log($"DPIP-Pre - Inst:{__instance}, IsPark:{__instance.IsPark} IsInd:{__instance.IsIndustry}, {(uint)__instance.m_parkLevel}({__instance.m_parkLevel}), Type:{__instance.m_parkType}  -  ";
            //Debug.Log($"DPIP-Pre - Inst:{dp}, IsInd:{dp.IsIndustry}, {level}({dp.m_parkLevel}), Type:{dp.m_parkType}");
            
            if (DistrictPark.IsIndustryType(dp.m_parkType)) {
                if (initialised[level]) return;
                initialised[level] = true;
                //Debug.Log($"DPIP-Pre: {dm.m_properties.m_parkProperties.m_industryLevelInfo[level].m_workerLevelupRequirement}");
                dm.m_properties.m_parkProperties.m_industryLevelInfo[level].m_workerLevelupRequirement = RI_Data.GetMilestone(level);
            }
        }
    }

    /*
    [HarmonyPatch(typeof(PrefabInfo))]
    [HarmonyPatch("GetLocalizedTooltip")]
    [HarmonyPatch(new Type[] { })]
    class RI_PIGetLocalizedTooltip
    {
        public static string Postfix(string text, PrefabInfo __instance)
        {
            BuildingAI buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[__instance.m_instanceID.Building].Info.m_buildingAI;
            Debug.Log($"{buildingAI.name}");
            if (buildingAI is ExtractingFacilityAI ai && ai.m_info.m_class.m_subService == ItemClass.SubService.IndustrialFarming)
            {
                Debug.Log($" - {ai.m_constructionCost} \"{text}\"");
            }
            return text;
        }
    }*/
}
