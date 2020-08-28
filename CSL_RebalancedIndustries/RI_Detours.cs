using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Harmony;
using ICities;
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
            int cargoDiff;

            if (Mod.IsIndustriesBuilding(__instance))
            {
                // Output
                cargoDiff = Convert.ToInt32((buildingData.m_customBuffer1 - __state) / RI_Data.GetFactorCargo(__instance.m_outputResource));
                //Debug.Log($"ID:{buildingID}={(ushort)Mathf.Clamp(__state + cargoDiff, 0, 64000)} ({__state + cargoDiff}), state:{__state}, buff:{buildingData.m_customBuffer1}, diff:{cargoDiff}");
                buildingData.m_customBuffer1 = (ushort)Mathf.Clamp(__state + cargoDiff, 0, 64000);
            }
            else
            {
                Mod.DebugLine($"Unknown EF instance {__instance.name} ({__instance.GetType()})");
            }
        }
    }


    // Includes Unique Factories
    [HarmonyPatch(typeof(ProcessingFacilityAI))]
    [HarmonyPatch("ProduceGoods")]
    class RI_ProcessingFacilityProduceGoods
    {
        public static void Prefix(ushort buildingID, ref Building buildingData, ProcessingFacilityAI __instance, out ushort[] __state)
        {
            __state = new ushort[5];

            if (Mod.IsIndustriesBuilding(__instance))
            {
                __state[0] = buildingData.m_customBuffer1; // Output (increases during production)
                __state[1] = buildingData.m_customBuffer2; // Input1 (used during prodction)
                __state[2] = Mod.CombineBytes(buildingData.m_teens, buildingData.m_youngs); // Input2
                __state[3] = Mod.CombineBytes(buildingData.m_adults, buildingData.m_seniors); // Input3
                __state[4] = Mod.CombineBytes(buildingData.m_education1, buildingData.m_education2); // Input4
            }
            else
                __state[0] = __state[1] = __state[2] = __state[3] = __state[4] = 0;
        }


        public static void Postfix(ushort buildingID, ref Building buildingData, ProcessingFacilityAI __instance, ref ushort[] __state)
        {
            int cargoDiff = 0;

            if (Mod.IsIndustriesBuilding(__instance))
            {
                // Input
                if (__instance.m_inputResource1 != TransferManager.TransferReason.None)
                {
                    cargoDiff = Convert.ToInt32((__state[1] - buildingData.m_customBuffer2) / RI_Data.GetFactorCargo(__instance.m_inputResource1));
                    buildingData.m_customBuffer2 = (ushort)Mathf.Clamp(__state[1] - cargoDiff, 0, 64000);
                }

                if (__instance.m_inputResource2 != TransferManager.TransferReason.None)
                {
                    cargoDiff = Convert.ToInt32((__state[2] - Mod.CombineBytes(buildingData.m_teens, buildingData.m_youngs)) / RI_Data.GetFactorCargo(__instance.m_inputResource2));
                    Mod.SplitBytes((ushort)Mathf.Clamp(__state[2] - cargoDiff, 0, 64000), ref buildingData.m_teens, ref buildingData.m_youngs);
                }

                if (__instance.m_inputResource3 != TransferManager.TransferReason.None)
                {
                    cargoDiff = Convert.ToInt32((__state[3] - Mod.CombineBytes(buildingData.m_adults, buildingData.m_seniors)) / RI_Data.GetFactorCargo(__instance.m_inputResource3));
                    Mod.SplitBytes((ushort)Mathf.Clamp(__state[3] - cargoDiff, 0, 64000), ref buildingData.m_adults, ref buildingData.m_seniors);
                }

                if (__instance.m_inputResource4 != TransferManager.TransferReason.None)
                {
                    cargoDiff = Convert.ToInt32((__state[4] - Mod.CombineBytes(buildingData.m_education1, buildingData.m_education2)) / RI_Data.GetFactorCargo(__instance.m_inputResource4));
                    Mod.SplitBytes((ushort)Mathf.Clamp(__state[4] - cargoDiff, 0, 64000), ref buildingData.m_education1, ref buildingData.m_education2);
                }

                // Output (materials being produced)
                if (__instance.m_outputResource != TransferManager.TransferReason.None)
                {
                    //try
                    //{
                    //} catch (OverflowException Ex)
                    //{
                    //    Debug.Log($"Output overflow caught: cargoDiff:{buildingData.m_customBuffer1}-{__state[0]}={(buildingData.m_customBuffer1 - __state[0])} factor:{RI_Data.GetFactorCargo(__instance.m_outputResource)}\n{Ex.ToString()}");
                    //    Singleton<SimulationManager>.instance.SimulationPaused = true;
                    //}

                    cargoDiff = Convert.ToInt32((buildingData.m_customBuffer1 - __state[0]) / RI_Data.GetFactorCargo(__instance.m_outputResource));
                    buildingData.m_customBuffer1 = (ushort)Mathf.Clamp(__state[0] + cargoDiff, 0, 64000);
                    //Debug.Log($"Out ID:{buildingID}, state:{__state}, buff:{buildingData.m_customBuffer1}, diff:{cargoDiff}");
                }

                //if (__instance is UniqueFactoryAI)
                //    Debug.Log($"PF:{__instance.name}, ID:{buildingID}, lastDiff:{cargoDiff} (Old:{__state[0]}-{__state[1]},{__state[2]},{__state[3]},{__state[4]} - New:{buildingData.m_customBuffer1}-{buildingData.m_customBuffer2},{Mod.CombineBytes(buildingData.m_teens, buildingData.m_youngs)},{Mod.CombineBytes(buildingData.m_adults, buildingData.m_seniors)},{Mod.CombineBytes(buildingData.m_education1, buildingData.m_education2)})");
                //else
                //    Debug.Log($"PF:{__instance.name}, ID:{buildingID}, lastDiff:{cargoDiff} (Old:{__state[0]}-{__state[1]} - New:{buildingData.m_customBuffer1}-{buildingData.m_customBuffer2})");
            }
            else
            {
                Mod.DebugLine($"Unknown PF instance {__instance.name} ({__instance.GetType()})");
            }
        }
    }


    [HarmonyPatch(typeof(IndustryBuildingAI))]
    [HarmonyPatch("GetResourcePrice")]
    class RI_IBGetResourcePrice
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
                    Locale.Get("INDUSTRYPANEL_BUFFERTOOLTIP"),
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
                                   ref UIPanel ___m_inputSection, /*ref UIPanel ___m_inputTooltipArea,*/ ref UIPanel ___m_outputSection)/*, ref UIPanel ___m_outputTooltipArea)*/
        {
            ushort id = ___m_InstanceID.Building;

            ExtractingFacilityAI ai_ef = ___m_extractingFacilityAI;
            if (ai_ef != null)
            {

                Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
                _updateTooltip(id, building.m_customBuffer1, ai_ef.GetOutputBufferSize(id, ref building), ai_ef.m_outputResource, ref ___m_outputSection);//, ref ___m_outputTooltipArea);
                //Debug.Log($"EF-{id}");
            }

            ProcessingFacilityAI ai_pf = ___m_processingFacilityAI;
            if (ai_pf != null)
            {
                Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
                _updateTooltip(id, building.m_customBuffer2, ai_pf.GetInputBufferSize1(id, ref building), ai_pf.m_inputResource1, ref ___m_inputSection);//, ref ___m_inputTooltipArea);
                _updateTooltip(id, building.m_customBuffer1, ai_pf.GetOutputBufferSize(id, ref building), ai_pf.m_outputResource, ref ___m_outputSection);//, ref ___m_outputTooltipArea);
                //Debug.Log($"PF-{id}");
            }
        }

        private static void _updateTooltip(int id, int volume, int bufferSize, TransferManager.TransferReason cargo, ref UIPanel panel)//, ref UIPanel panel2)
        {
            //Debug.Log($"uTt-{id}");
            int customBuffer = Convert.ToInt32(volume * RI_Data.GetFactorCargo(cargo));
            int outputBufferSize = Convert.ToInt32(bufferSize * RI_Data.GetFactorCargo(cargo));
            //panel2.tooltip = 
            panel.tooltip = StringUtils.SafeFormat(ColossalFramework.Globalization.Locale.Get("INDUSTRYPANEL_BUFFERTOOLTIP"), IndustryWorldInfoPanel.FormatResource((uint)customBuffer), IndustryWorldInfoPanel.FormatResourceWithUnit((uint)outputBufferSize, cargo));
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
                Locale.Get("INDUSTRYPANEL_BUFFERTOOLTIP"), 
                IndustryWorldInfoPanel.FormatResource((ulong)(building.m_customBuffer1 * 100 * RI_Data.GetFactorCargo(cargoType))), 
                IndustryWorldInfoPanel.FormatResourceWithUnit((uint)(ai.m_storageCapacity * RI_Data.GetFactorCargo(cargoType)), cargoType)
            );
            ___m_buffer.tooltip = text;
            ___m_capacityLabel.text = text;
        }
    }


    [HarmonyPatch(typeof(UniqueFactoryWorldInfoPanel))]
    [HarmonyPatch("UpdateBindings")]
    class RI_UFWIPUpdateBindings
    {
        public static void Postfix(ref InstanceID ___m_InstanceID, ref UILabel ___m_expenses)
        {
            ushort id = ___m_InstanceID.Building;
            Building building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
            UniqueFactoryAI ai = (UniqueFactoryAI)building.Info.m_buildingAI;
            int volume;
            byte health = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id].m_health;

            volume = health * ai.m_inputRate1 * 16 / 100;
            long input1 = volume * IndustryBuildingAI.GetResourcePrice(ai.m_inputResource1) / (long)RI_Data.GetFactorCargo(ai.m_inputResource1) / 10000;
            volume = health * ai.m_inputRate2 * 16 / 100;
            long input2 = volume * IndustryBuildingAI.GetResourcePrice(ai.m_inputResource2) / (long)RI_Data.GetFactorCargo(ai.m_inputResource2) / 10000;
            volume = health * ai.m_inputRate3 * 16 / 100;
            long input3 = volume * IndustryBuildingAI.GetResourcePrice(ai.m_inputResource3) / (long)RI_Data.GetFactorCargo(ai.m_inputResource3) / 10000;
            volume = health * ai.m_inputRate4 * 16 / 100;
            long input4 = volume * IndustryBuildingAI.GetResourcePrice(ai.m_inputResource4) / (long)RI_Data.GetFactorCargo(ai.m_inputResource4) / 10000;
            ___m_expenses.text = (input1 + input2 + input3 + input4).ToString(Settings.moneyFormatNoCents, LocaleManager.cultureInfo);
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
