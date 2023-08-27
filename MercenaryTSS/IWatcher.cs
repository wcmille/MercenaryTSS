using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;

namespace MercenaryTSS
{
    public interface IWatcher
    {
        float CalcBingo();
        float CalcCapacity();
        float CalcProduce();
        float CalcConsume();
        void Refresh();
    }

    public class GasWatcher : IWatcher
    {
        readonly IMyTerminalBlock myTerminalBlock;
        readonly List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
        readonly MyDefinitionId gasId;

        public GasWatcher(IMyTerminalBlock myTerminalBlock, MyDefinitionId gasId)
        {
            this.myTerminalBlock = myTerminalBlock;
            this.gasId = gasId;
        }

        public float CalcCapacity()
        {
            var current = h2Tanks.Sum(x => (float)x.FilledRatio * x.Capacity);
            var total = h2Tanks.Sum(x => x.Capacity);
            return current / total;
        }

        public float CalcProduce()
        {
            float current = 0.0f;
            float total = 0.0f;
            foreach (var block in h2Tanks)
            {
                var c = block.Components.Get<MyResourceSourceComponent>();
                if (c != null)
                {
                    total += c.MaxOutput;
                }
            }
            foreach (var block in h2Tanks)
            {
                var c = block.Components.Get<MyResourceSinkComponent>();

                if (c != null)
                {
                    current += c.CurrentInputByType(gasId);
                }
            }
            return current / total;
        }

        public float CalcConsume()
        {
            float current = 0.0f;
            float total = 0.0f;
            foreach (var block in h2Tanks)
            {
                var c = block.Components.Get<MyResourceSourceComponent>();
                if (c != null)
                {
                    current += c.CurrentOutput;
                    total += c.MaxOutput;
                }
            }
            return current / total;
        }

        public float CalcBingo()
        {
            var cap = h2Tanks.Sum(x => (float)x.FilledRatio * x.Capacity);
            var max = h2Tanks.Sum(x => x.Capacity);

            float net = 0.0f;
            foreach (var block in h2Tanks)
            {
                var c = block.Components.Get<MyResourceSourceComponent>();
                if (c != null)
                {
                    net -= c.CurrentOutput;
                }
                var c2 = block.Components.Get<MyResourceSinkComponent>();

                if (c2 != null)
                {
                    net += c2.CurrentInputByType(gasId);
                }
            }
            if (net < 0.0f) { return cap / net; }
            if (net > 0.0f) { return (max - cap) / net; }
            return 0.0f;
        }

        public void Refresh()
        {
            h2Tanks.Clear();
            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);

            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock is IMyGasTank)
                {
                    if (((MyGasTankDefinition)myBlock.BlockDefinition).StoredGasId == gasId) h2Tanks.Add(myBlock as IMyGasTank);
                }
            }
        }
    }

    public class PowerWatcher : IWatcher
    {
        readonly IMyTerminalBlock myTerminalBlock;
        readonly List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
        //readonly List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        readonly List<IMyPowerProducer> hydroEngines = new List<IMyPowerProducer>();
        //readonly List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        //readonly List<IMyReactor> reactors = new List<IMyReactor>();

        float max, capacity, produce, consume, maxOut;

        public PowerWatcher(IMyTerminalBlock myTerminalBlock)
        {
            this.myTerminalBlock = myTerminalBlock;
        }

        public float CalcCapacity()
        {
            return capacity / max;
        }

        public float CalcProduce()
        {
            return produce / maxOut;
        }

        public float CalcConsume()
        {
            return consume / maxOut;
        }

        public float CalcBingo()
        {
            var net = produce - consume;
            if (net < 0.0f) { return capacity * 3600 / net; }
            if (net > 0.0f) { return (max - capacity) * 3600 / net; }
            return 0.0f;
        }

        public void Refresh()
        {
            batteryBlocks.Clear();
            //windTurbines.Clear();
            hydroEngines.Clear();
            //solarPanels.Clear();
            //reactors.Clear();

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);
            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock is IMyBatteryBlock)
                {
                    batteryBlocks.Add(myBlock as IMyBatteryBlock);
                }
                else if (myBlock is IMyPowerProducer)
                {
                    if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                    {
                        //windTurbines.Add((IMyPowerProducer)myBlock);
                        hydroEngines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                    {
                        hydroEngines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock is IMyReactor)
                    {
                        //reactors.Add((IMyReactor)myBlock);
                        hydroEngines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock is IMySolarPanel)
                    {
                        //solarPanels.Add((IMySolarPanel)myBlock);
                        hydroEngines.Add((IMyPowerProducer)myBlock);
                    }
                }
            }

            max = batteryBlocks.Sum(x => x.MaxStoredPower);
            capacity = batteryBlocks.Sum(x => x.CurrentStoredPower);
            produce = hydroEngines.Sum(x => x.CurrentOutput);
            consume = batteryBlocks.Sum(x => x.CurrentOutput) + hydroEngines.Sum(x => x.CurrentOutput) - batteryBlocks.Sum(x => x.CurrentInput);
            maxOut = batteryBlocks.Sum(x => x.MaxOutput);
        }
    }

    public class CargoWatcher
    {
        readonly List<IMyInventory> inventories = new List<IMyInventory>();
        readonly List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
        readonly Dictionary<string, int> cargo = new Dictionary<string, int>();
        //readonly VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        DateTime lastTime;
        private float iceBingo, oldIce;
        private float uraniumBingo, oldUranium;
        readonly IMyTerminalBlock myTerminalBlock;
        int refreshCount = 0;

        public CargoWatcher(IMyTerminalBlock terminalBlock)
        {
            myTerminalBlock = terminalBlock;
            cargo.Add("Ice", 0);
            cargo.Add("Uranium", 0);
            lastTime = DateTime.Now;
        }

        public float Ice()
        {
            return cargo["Ice"];
        }

        public float IceBingo()
        {
            return iceBingo;
        }

        public float UraniumBingo()
        {
            return uraniumBingo;
        }

        public float Uranium()
        {
            return cargo["Uranium"];
        }
        public void Refresh()
        {
            refreshCount++;

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks();

            inventories.Clear();

            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock.HasInventory && myBlock.IsFunctional)
                {
                    for (int i = 0; i < myBlock.InventoryCount; i++)
                    {
                        inventories.Add(myBlock.GetInventory(i));
                    }
                }
            }

            cargo["Ice"] = 0;
            cargo["Uranium"] = 0;

            foreach (var inventory in inventories)
            {
                if (inventory.ItemCount != 0)
                {
                    inventoryItems.Clear();
                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems)
                    {
                        var type = item.Type.TypeId;
                        var subtype = item.Type.SubtypeId;
                        string name = null;
                        if (subtype == "Ice") name = "Ice";
                        else if (subtype == "Uranium" && type == "MyObjectBuilder_Ingot") name = "Uranium";

                        if (name == "Ice" || name == "Uranium")
                        {
                            var amount = item.Amount.ToIntSafe();
                            cargo[name] += amount;
                        }
                    }
                }
            }
            if (refreshCount % 6 == 0)
            {
                var oldTime = lastTime;
                lastTime = DateTime.Now;
                var span = lastTime - oldTime;
                {
                    var cIce = cargo["Ice"];
                    float burnRate = (float)((cIce - oldIce) / span.TotalSeconds);
                    iceBingo = (cIce != oldIce) ? (cIce / burnRate) : 0.0f;
                    oldIce = cIce;
                }
                {
                    var cUranium = cargo["Uranium"];
                    float burnRate = (float)((cUranium - oldUranium) / span.TotalSeconds);
                    uraniumBingo = (cUranium != oldUranium) ? (cUranium / burnRate) : 0.0f;
                    oldUranium = cUranium;
                }
                refreshCount = 0;
            }
        }
    }
}
