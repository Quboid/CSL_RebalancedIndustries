using Harmony;
using ICities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CSL_RebalancedIndustries
{
    public class RI_Loading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (!(mode == LoadMode.LoadGame || mode == LoadMode.LoadScenario || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario)) {
                return;
            }
            HarmonyInstance harmony = Mod.GetHarmonyInstance();
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Building[] buffer = ColossalFramework.Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            // Reset re-balanced WorkShop assets before applying modifiers
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);

                if (prefab.m_class.m_service == ItemClass.Service.PlayerIndustry)
                {
                    Dictionary<string, string> resetList = RI_Data.GetWSResets();
                    if (resetList.ContainsKey(prefab.name))
                        ResetBuildingToVanilla(ref prefab, PrefabCollection<BuildingInfo>.FindLoaded(resetList[prefab.name]));
                }
            }


            // Apply Modifiers
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                if (PrefabCollection<BuildingInfo>.GetLoaded(i) != null)
                {
                    BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);

                    if (prefab.m_class.m_service == ItemClass.Service.PlayerIndustry)
                    {
                        //Mod.DebugLine($" - {prefab.m_buildingAI}");
                        if (prefab.m_buildingAI is ExtractingFacilityAI ai_ef)
                        {
                            //int oldExtract = ai_ef.m_extractRate; int oldOutputRate = ai_ef.m_outputRate; int oldVehicles = ai_ef.m_outputVehicleCount;
                            RI_BuildingFactor factor = RI_Data.GetFactorBuilding(ai_ef);
                            ai_ef.m_constructionCost = Convert.ToInt32(ai_ef.m_constructionCost / factor.Costs);
                            ai_ef.m_maintenanceCost = Convert.ToInt32(ai_ef.m_maintenanceCost / factor.Costs);
                            ai_ef.m_outputVehicleCount = Math.Max(RI_Data.MIN_VEHICLES, Convert.ToInt32(Math.Floor(ai_ef.m_outputVehicleCount / RI_Data.GetFactorCargo(ai_ef.m_outputResource))));

                            ai_ef.m_extractRate = Convert.ToInt32(ai_ef.m_extractRate / factor.Production);
                            ai_ef.m_outputRate = Convert.ToInt32(ai_ef.m_outputRate / factor.Production);
                            ai_ef.m_electricityConsumption = Convert.ToInt32(ai_ef.m_electricityConsumption / factor.Production);
                            ai_ef.m_waterConsumption = Convert.ToInt32(ai_ef.m_waterConsumption / factor.Production);
                            ai_ef.m_sewageAccumulation = Convert.ToInt32(ai_ef.m_sewageAccumulation / factor.Production);
                            ai_ef.m_garbageAccumulation = Convert.ToInt32(ai_ef.m_garbageAccumulation / factor.Production);
                            ai_ef.m_fireHazard = Convert.ToInt32(ai_ef.m_fireHazard / factor.Production);
                            //Mod.DebugLine($"Production F={Convert.ToInt32(factor.Production)}, ai={ai_ef.GetType().ToString()}, Extract:{oldExtract}=>{ai_ef.m_extractRate}, Output:{oldOutputRate}=>{ai_ef.m_outputRate}, Output:{oldVehicles}=>{ai_ef.m_outputVehicleCount}");
                            SwitchWorkPlaces(ai_ef);
                        }
                        else if (prefab.m_buildingAI is UniqueFactoryAI ai_uf) // Must be processed before ProcessingFacilityAI
                        {
                            //Mod.DebugLine($"UF: {prefab.m_buildingAI.name}");
                            RI_UniqueFactoryProfile profile = RI_Data.GetUniqueFactoryProfile(ai_uf);
                            if (profile.Cost >= 0) ai_uf.m_maintenanceCost = Convert.ToInt32(profile.Cost * 100 / 16);
                            if (profile.Workers[0] >= 0) ai_uf.m_workPlaceCount0 = profile.Workers[0];
                            if (profile.Workers[1] >= 0) ai_uf.m_workPlaceCount1 = profile.Workers[1];
                            if (profile.Workers[2] >= 0) ai_uf.m_workPlaceCount2 = profile.Workers[2];
                            if (profile.Workers[3] >= 0) ai_uf.m_workPlaceCount3 = profile.Workers[3];
                            //Mod.DebugLine($"{ai_uf.name}: {profile.Cost} - {profile.Workers[0]},{profile.Workers[1]},{profile.Workers[2]},{profile.Workers[3]}");
                        }
                        else if (prefab.m_buildingAI is ProcessingFacilityAI ai_pf) // Must be processed after UniqueFactoryAI
                        {
                            if (Mod.IsField(ai_pf)) // Pasture
                            {
                                RI_BuildingFactor factor = RI_Data.GetFactorBuilding(ai_pf);
                                ai_pf.m_constructionCost = Convert.ToInt32(ai_pf.m_constructionCost / factor.Costs);
                                ai_pf.m_maintenanceCost = Convert.ToInt32(ai_pf.m_maintenanceCost / factor.Costs);
                                SwitchWorkPlaces(ai_pf);
                            }
                            ai_pf.m_outputVehicleCount = Math.Max(RI_Data.MIN_VEHICLES, Convert.ToInt32(Math.Floor(ai_pf.m_outputVehicleCount / RI_Data.GetFactorCargo(ai_pf.m_outputResource))));
                            ai_pf.m_outputVehicleCount += 1;
                        }
                        else if (prefab.m_buildingAI is WarehouseAI ai_w)
                        {
                            //Mod.DebugLine($"Warehouse {ai_w.name}: T={ai_w.m_truckCount}, SC={ai_w.m_storageCapacity}, ST={ai_w.m_storageType}");
                            if (Mod.IsExtractorWarehouse(ai_w))
                            {
                                if (RI_Data.GetFactorCargo(ai_w.m_storageType) != 1)
                                {
                                    decimal subfactor = (RI_Data.GetFactorCargo(ai_w.m_storageType) - 1) / 1.5m + 1;
                                    ai_w.m_truckCount = Convert.ToInt32(Math.Ceiling(ai_w.m_truckCount / subfactor));
                                    if (ai_w.m_truckCount < RI_Data.MIN_VEHICLES)
                                        ai_w.m_truckCount = RI_Data.MIN_VEHICLES;
                                    if (ai_w.m_truckCount < (2 * RI_Data.MIN_VEHICLES))
                                        ai_w.m_truckCount += 1;
                                }
                            }
                            else
                            {
                                if (RI_Data.GetFactorBuilding(ai_w).Production != 1)
                                {
                                    ai_w.m_truckCount = Convert.ToInt32(ai_w.m_truckCount / RI_Data.GetFactorBuilding(ai_w).Production);
                                    if (ai_w.m_truckCount < RI_Data.MIN_VEHICLES)
                                        ai_w.m_truckCount = RI_Data.MIN_VEHICLES;
                                    if (ai_w.m_truckCount < (2 * RI_Data.MIN_VEHICLES))
                                        ai_w.m_truckCount += 1;
                                }
                            }
                            ai_w.m_constructionCost = Convert.ToInt32(ai_w.m_constructionCost / RI_Data.GetFactorBuilding(ai_w).Costs);
                            ai_w.m_maintenanceCost = Convert.ToInt32(ai_w.m_maintenanceCost / RI_Data.GetFactorBuilding(ai_w).Costs);
                            SwitchWorkPlaces(ai_w);
                            //Mod.DebugLine($"       Now {ai_w.name}: T={ai_w.m_truckCount}, SC={ai_w.m_storageCapacity}, ST={ai_w.m_storageType}");
                        }
                        else if (!(prefab.m_buildingAI is AuxiliaryBuildingAI || prefab.m_buildingAI is MainIndustryBuildingAI || prefab.m_buildingAI is DummyBuildingAI))
                        {
                            Mod.DebugLine($"Applying modifiers: Unknown prefab={prefab.name}, {prefab.m_buildingAI}");
                        }
                    }
                }
            }
        }


        private void SwitchWorkPlaces(IndustryBuildingAI ai, int minimumSize = 10)
        {
            if (ai is ExtractingFacilityAI && Mod.IsField(ai)) // It's an extracting field
            {
                RI_EmployeeRatio ratio = RI_Data.GetEmployeeRatio(ai);
                int workPlaceTotal = (int) Math.Ceiling(Math.Sqrt(ai.m_info.m_cellLength * ai.m_info.m_cellWidth) / 2);

                //int oldTotal = ai.m_workPlaceCount0 + ai.m_workPlaceCount1 + ai.m_workPlaceCount2 + ai.m_workPlaceCount3;

                ai.m_workPlaceCount0 = Convert.ToInt32(Math.Round(workPlaceTotal * ratio.GetModifier(0)));
                ai.m_workPlaceCount1 = Convert.ToInt32(Math.Round(workPlaceTotal * ratio.GetModifier(1)));
                ai.m_workPlaceCount2 = Convert.ToInt32(Math.Round(workPlaceTotal * ratio.GetModifier(2)));
                ai.m_workPlaceCount3 = Convert.ToInt32(Math.Round(workPlaceTotal * ratio.GetModifier(3)));
                //Mod.DebugLine($"_switchWorkplaces: maths={ratio.GetModifier(0)}x{workPlaceTotal}={workPlaceTotal * ratio.GetModifier(0)}, wpc0={ai.m_workPlaceCount0}");
                //Mod.DebugLine($"FARM FIELD:{ai.m_workPlaceCount0 + ai.m_workPlaceCount1 + ai.m_workPlaceCount2 + ai.m_workPlaceCount3} (was {oldTotal}, should be {workPlaceTotal})");
            }
            else
            {
                //Mod.DebugLine($"SWP: subservice={ai.m_info.m_class.m_subService}");
                _switchWorkPlaces(ai, minimumSize, ref ai.m_workPlaceCount0, ref ai.m_workPlaceCount1, ref ai.m_workPlaceCount2, ref ai.m_workPlaceCount3);
            }
        }

        private void SwitchWorkPlaces(WarehouseAI ai, int minimumSize = 10)
        {
            _switchWorkPlaces(ai, minimumSize, ref ai.m_workPlaceCount0, ref ai.m_workPlaceCount1, ref ai.m_workPlaceCount2, ref ai.m_workPlaceCount3);
        }

        private void _switchWorkPlaces(PlayerBuildingAI ai, int minimumSize, ref int wp0, ref int wp1, ref int wp2, ref int wp3)
        {
            int oldTotal = wp0 + wp1 + wp2 + wp3;
            RI_EmployeeRatio ratio = RI_Data.GetEmployeeRatio(ai);

            if (oldTotal >= minimumSize)
            {
                decimal factor = RI_Data.GetFactorBuilding(ai).Workers;
                //Mod.DebugLine($"{ai.name} - AI: {ai.GetType().ToString()}, factor: {factor}x");
                //Mod.DebugLine($"    _switchWorkplaces: old={oldTotal}; wp0={wp0}, wp1={wp1}, wp2={wp2}, wp3={wp3}");
                wp0 = Convert.ToInt32(Math.Round(wp0 / factor));
                wp1 = Convert.ToInt32(Math.Round(wp1 / factor));
                wp2 = Convert.ToInt32(Math.Round(wp2 / factor));
                wp3 = Convert.ToInt32(Math.Round(wp3 / factor));
                //Mod.DebugLine($"    _switchWorkplaces: new={wp0 + wp1 + wp2 + wp3}; wp0={wp0}, wp1={wp1}, wp2={wp2}, wp3={wp3}");
                //Mod.DebugLine($"");
            }
        }


        private void ResetBuildingToVanilla(ref BuildingInfo target, BuildingInfo template)
        {
            IndustryBuildingAI targetAI = (IndustryBuildingAI)target.m_buildingAI;
            IndustryBuildingAI templateAI = (IndustryBuildingAI)template.m_buildingAI;

            //Mod.DebugLine($"ConvertWS: name={target.name},{template.name}");
            //Mod.DebugLine($" - construction={targetAI.m_constructionCost},{templateAI.m_constructionCost} - wp0:{targetAI.m_workPlaceCount0},{templateAI.m_workPlaceCount0} - wp1:{targetAI.m_workPlaceCount1},{templateAI.m_workPlaceCount1}");

            targetAI.m_maintenanceCost = templateAI.m_maintenanceCost;
            targetAI.m_constructionCost = templateAI.m_constructionCost;
            targetAI.m_workPlaceCount0 = templateAI.m_workPlaceCount0;
            targetAI.m_workPlaceCount1 = templateAI.m_workPlaceCount1;
            targetAI.m_workPlaceCount2 = templateAI.m_workPlaceCount2;
            targetAI.m_workPlaceCount3 = templateAI.m_workPlaceCount3;
            targetAI.m_electricityConsumption = templateAI.m_electricityConsumption;
            targetAI.m_waterConsumption = templateAI.m_waterConsumption;
            targetAI.m_sewageAccumulation = templateAI.m_sewageAccumulation;
            targetAI.m_garbageAccumulation = templateAI.m_garbageAccumulation;
            targetAI.m_fireHazard = templateAI.m_fireHazard;

            // Cast AI handling
            if (targetAI is ExtractingFacilityAI targetAIef)
            {
                ExtractingFacilityAI templateAIef = (ExtractingFacilityAI)templateAI;
                //Mod.DebugLine($"Reset ai={targetAI.GetType().ToString()}, Vehicles:{targetAIef.m_outputVehicleCount}=>{templateAIef.m_outputVehicleCount}");
                targetAIef.m_extractRate = templateAIef.m_extractRate;
                targetAIef.m_extractRadius = templateAIef.m_extractRadius;
                targetAIef.m_outputRate = templateAIef.m_outputRate;
                targetAIef.m_outputVehicleCount = templateAIef.m_outputVehicleCount;
            }

            //Mod.DebugLine($"AI type={targetAI.GetType().ToString()}");
        }
    }
}
