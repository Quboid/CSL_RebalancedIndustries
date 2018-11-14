using System.Collections.Generic;
using System.Linq;

namespace CSL_RebalancedIndustries
{
    class RI_Data
    {
        // Industries buildings have at least MINVEHICLES trucks, extractor warehouses have at least MINVEHICLES * 2, other warehouses have at least MINVEHICLES+1 to help ensure less frequent deliveries are regular
        public const short MIN_VEHICLES = 3;

        public static Dictionary<ushort, ushort> oldBuffer = new Dictionary<ushort, ushort>();

        public static decimal GetFactorCargo(TransferManager.TransferReason material)
        {
            switch (material)
            {
                case TransferManager.TransferReason.Grain:
                    return 3m;
                case TransferManager.TransferReason.AnimalProducts:
                    return 1.5m;
                case TransferManager.TransferReason.Flours:
                    return 1.5m;
                case TransferManager.TransferReason.Logs:
                    return 2m;
                case TransferManager.TransferReason.PlanedTimber:
                    return 2m;
                case TransferManager.TransferReason.Paper:
                    return 2m;
                case TransferManager.TransferReason.Ore:
                    return 1.5m;
                case TransferManager.TransferReason.Metals:
                    return 1.5m;
                case TransferManager.TransferReason.Glass:
                    return 1.5m;
                case TransferManager.TransferReason.Oil:
                    return 3m;
                case TransferManager.TransferReason.Plastics:
                    return 1.5m;
                case TransferManager.TransferReason.Petroleum:
                    return 2m;
            }
            return 1m;
        }


        public static int GetMilestone(uint level)
        {
            switch (level)
            {
                case 1:
                    return 75;
                case 2:
                    return 200;
                case 3:
                    return 400;
                case 4:
                    return 650;
            }

            return 0;
        }


        public static RI_BuildingFactor GetFactorBuilding(PlayerBuildingAI ai)
        {
            
            if (ai is ExtractingFacilityAI)
            {
                if (Mod.IsField(ai)) // Extracting farms don't use .Workers, they're redone from scratch
                {
                    return new RI_BuildingFactor { Costs = 4m, Production = 2m, Workers = 1m };
                }
                return new RI_BuildingFactor { Costs = 1m, Production = 1m, Workers = 2m };
            }
            if (ai is ProcessingFacilityAI)
            {
                if (Mod.IsField(ai)) // Pasture
                {
                    return new RI_BuildingFactor { Costs = 2m, Production = 1m, Workers = 3m };
                }
            }
            if (ai is WarehouseAI ai_w)
            {
                // Warehouse production affects truck count
                switch (ai_w.m_storageType)
                {
                    case TransferManager.TransferReason.Grain:
                        return new RI_BuildingFactor { Costs = 2m, Production = 1m, Workers = 4m };
                    case TransferManager.TransferReason.Logs:
                    case TransferManager.TransferReason.Ore:
                    case TransferManager.TransferReason.Oil:
                        return new RI_BuildingFactor { Costs = 1m, Production = 2m, Workers = 2m };
                }
                return new RI_BuildingFactor { Costs = 0.5m, Production = 1.5m, Workers = 1m };
            }

            Mod.DebugLine($"GetFactorBuilding: Unknown PlayerBuildingAI={ai.name}");
            return new RI_BuildingFactor { Costs = 1m, Production = 1m, Workers = 1m };
        }


        public static RI_UniqueFactoryProfile GetUniqueFactoryProfile(UniqueFactoryAI ai)
        {
            switch (ai.name)
            {
                case "Furniture Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 320,  Workers = new int[] { 25, 18, 8, 4 } }; // 55
                case "Bakery 01":
                    return new RI_UniqueFactoryProfile { Cost = 260,  Workers = new int[] { 15, 9, 4, 2 } }; // 30
                case "Industrial Steel Plant 01":
                    return new RI_UniqueFactoryProfile { Cost = 1800,  Workers = new int[] { 60, 45, 30, 5 } }; // 150
                case "Household Plastic Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 480,  Workers = new int[] { 25, 18, 8, 4 } }; // 55
                case "Toy Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 760,  Workers = new int[] { 25, 18, 8, 4 } };  // 55
                case "Printing Press 01":
                    return new RI_UniqueFactoryProfile { Cost = 560,  Workers = new int[] { 22, 16, 8, 4 } }; // 50
                case "Lemonade Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 800,  Workers = new int[] { 55, 35, 15, 5 } }; // 110
                case "Electronics Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 1800, Workers = new int[] { 55, 40, 20, 10 } }; // 125
                case "Clothing Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 840,  Workers = new int[] { 35, 20, 10, 5 } }; // 70
                case "Petroleum Refinery 01":
                    return new RI_UniqueFactoryProfile { Cost = 2600, Workers = new int[] { 60, 45, 30, 15 } }; // 150
                case "Soft Paper Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 2200,  Workers = new int[] { 60, 50, 12, 8 } }; // 130
                case "Car Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 3400, Workers = new int[] { 70, 60, 20, 10 } }; // 160
                case "Sneaker Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 1920, Workers = new int[] { 35, 30, 10, 5 } }; // 80
                case "Modular House Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 2400, Workers = new int[] { 70, 45, 15, 10 } }; // 140
                case "Food Factory 01":
                    return new RI_UniqueFactoryProfile { Cost = 1920, Workers = new int[] { 55, 35, 15, 5 } }; // 110
                case "Dry Dock 01":
                    return new RI_UniqueFactoryProfile { Cost = 3800, Workers = new int[] { 80, 50, 20, 10 } }; // 160
            }

            //Mod.DebugLine($"GetEmployeeRatio: Unknown UniqueFactoryAI={ai.name}");
            return new RI_UniqueFactoryProfile { Cost = -1, Workers = new int[] { -1, -1, -1, -1 } };
        }


        public static RI_EmployeeRatio GetEmployeeRatio(PlayerBuildingAI ai)
        {
            if (ai is IndustryBuildingAI) return _getEmployeeRatioIB((IndustryBuildingAI)ai);
            if (ai is WarehouseAI) return _getEmployeeRatioW((WarehouseAI)ai);

            Mod.DebugLine($"GetEmployeeRatio: Unknown PlayerBuildingAI={ai.name}");
            return new RI_EmployeeRatio { Level = new int[] { 1, 1, 1, 1 } };
        }


        public static RI_EmployeeRatio _getEmployeeRatioIB(IndustryBuildingAI ai)
        {
            switch (ai.m_industryType)
            {
                case DistrictPark.ParkType.Farming:
                    return new RI_EmployeeRatio { Level = new int[] { 2, 4, 1, 0 } };
            }
            return new RI_EmployeeRatio { Level = new int[] { 1, 1, 1, 1 } };
        }


        public static RI_EmployeeRatio _getEmployeeRatioW(WarehouseAI ai)
        {
            return new RI_EmployeeRatio { Level = new int[] { 8, 4, 2, 1 } };
        }


        // Matches the start of the AI instance.name property
        public static string[] GetProcessorFieldNameStarts()
        {
            return new string[]
            {
                "Animal Pasture 01",
                "Animal Pasture 02",
                "Cattle Shed 01"
            };
        }


        public static Dictionary<string, string> GetWSResets()
        {
            Dictionary<string, string> overrides = new Dictionary<string, string>
            {
                { "1549450597.Corn Small IND_Data", "Crop Field Corn 01" },
                { "1549450597.Corn Medium IND_Data", "Crop Field Corn 02" },
                { "1549450597.Corn Large IND_Data", "Crop Field Corn 03" },
                { "1549451958.Corn Large 2 IND_Data", "Crop Field Corn 03" },
                { "1548892240.Cotton Small IND_Data", "Crop Field Corn 01" },
                { "1548892240.Cotton Medium IND_Data", "Crop Field Corn 02" },
                { "1548892240.Cotton Large IND_Data", "Crop Field Corn 03" },
                { "1549087066.Cotton Large 2 IND_Data", "Crop Field Corn 03" },
                { "1549447947.Rapeseed Small IND_Data", "Crop Field Corn 01" },
                { "1549447947.Rapeseed Medium IND_Data", "Crop Field Corn 02" },
                { "1549447947.Rapeseed Large IND_Data", "Crop Field Corn 03" },
                { "1550498284.Rapeseed Large 2 IND_Data", "Crop Field Corn 03" },
                { "1548936570.Vegetable V3 Small IND_Data", "Crop Field Corn 01" },
                { "1548936570.Vegetable V3 Medium IND_Data", "Crop Field Corn 02" },
                { "1548936570.Vegetable V3 Large IND_Data", "Crop Field Corn 03" },
                { "1549043406.Vegetable V3 Large 2 IND_Data", "Crop Field Corn 03" },
                { "1553615243.Wheat Cut Small IND_Data", "Crop Field Corn 01" },
                { "1553615243.Wheat Cut Medium IND_Data", "Crop Field Corn 02" },
                { "1553615243.Wheat Cut Large IND_Data", "Crop Field Corn 03" },
                { "1553625671.Wheat Cut Large 2 IND_Data", "Crop Field Corn 03" },
                { "1548433014.Wheat Ripe Small IND_Data", "Crop Field Corn 01" },
                { "1548433014.Wheat Ripe Medium IND_Data", "Crop Field Corn 02" },
                { "1548433014.Wheat Ripe Large IND_Data", "Crop Field Corn 03" },
                { "1549038637.Wheat Ripe Large 2 IND_Data", "Crop Field Corn 03" },
            };
            return overrides;
        }
    }


    class RI_BuildingFactor
    {
        public decimal Costs { get; set; }
        public decimal Production { get; set; }
        public decimal Workers { get; set; }

        public RI_BuildingFactor() { }
    }


    class RI_EmployeeRatio
    {
        public int[] Level = new int[4];

        public RI_EmployeeRatio() { }

        public double GetTotal() => Level.Sum();

        public double GetModifier(int level)
        {
            return Level[level] / GetTotal();
        }
    }


    class RI_UniqueFactoryProfile
    {
        public int Cost { get; set; }
        public int[] Workers = new int[4];
    }
}
