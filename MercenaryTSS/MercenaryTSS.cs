﻿using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

//Thanks to DiK from https://steamcommunity.com/sharedfiles/filedetails/?id=2608746450

namespace BMC.MercenaryTSS
{
    class cargoItemType
    {
        public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        public int amount;
    }

    public class ConfigIt
    {
        public bool ConfigCheck = false;
        MyIni config = new MyIni();
        IMyTerminalBlock myTerminalBlock;
        public ConfigIt(IMyTerminalBlock mtb)
        {
            myTerminalBlock = mtb;
        }
        public void CreateConfig()
        {
            config.AddSection("Settings");

            config.Set("Settings", "TextSize", "1.0");
            config.Set("Settings", "Battery", "false");
            config.Set("Settings", "WindTurbine", "false");
            config.Set("Settings", "HydrogenEngine", "false");
            config.Set("Settings", "Tanks", "false");
            config.Set("Settings", "Solar", "false");
            config.Set("Settings", "Reactor", "false");
            config.Set("Settings", "Ore", "false");
            config.Set("Settings", "Ingot", "false");
            config.Set("Settings", "Component", "false");
            config.Set("Settings", "Items", "false");

            config.Invalidate();
            myTerminalBlock.CustomData = config.ToString();
        }

        public void LoadConfig()
        {
            ConfigCheck = false;

            if (config.TryParse(myTerminalBlock.CustomData))
            {
                if (config.ContainsSection("Settings"))
                {
                    ConfigCheck = true;
                }
                else
                {
                    MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Value error");
                }
            }
            else
            {
                MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Syntax error");
            }
        }
    }

    // Text Surface Scripts (TSS) can be selected in any LCD's scripts list.
    // These are meant as fast no-sync (sprites are not sent over network) display scripts, and the Run() method only executes player-side (no DS).
    // You can still use a session comp and access it through this to use for caches/shared data/etc.
    //
    // The display name has localization support aswell, same as a block's DisplayName in SBC.
    [MyTextSurfaceScript("InventoryCounter", "BMC Inv Count")]
    public class InventoryCounter : MyTSSCommon
    {
        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;
        Vector2 right;
        Vector2 newLine;

        //List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
        //List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        //List<IMyPowerProducer> hydroenEngines = new List<IMyPowerProducer>();
        //List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        //List<IMyReactor> reactors = new List<IMyReactor>();
        //List<IMyGasTank> tanks = new List<IMyGasTank>();

        List<IMyInventory> inventorys = new List<IMyInventory>();
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        //Dictionary<string, cargoItemType> cargoOres = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoIngots = new Dictionary<string, cargoItemType>();
        //Dictionary<string, cargoItemType> cargoComponents = new Dictionary<string, cargoItemType>();
        //Dictionary<string, cargoItemType> cargoItems = new Dictionary<string, cargoItemType>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        float textSize = 1.0f;

        private readonly IMyTerminalBlock TerminalBlock;
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10; // frequency that Run() is called.
        ConfigIt configit;


        public InventoryCounter(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block; // internal stored m_block is the ingame interface which has no events, so can't unhook later on, therefore this field is required.
            TerminalBlock.OnMarkForClose += BlockMarkedForClose; // required if you're gonna make use of Dispose() as it won't get called when block is removed or grid is cut/unloaded.
            mySurface = surface;
            // Called when script is created.
            // This class is instanced per LCD that uses it, which means the same block can have multiple instances of this script aswell (e.g. a cockpit with all its screens set to use this script).
            configit = new ConfigIt(TerminalBlock);
        }

        public override void Dispose()
        {
            base.Dispose(); // do not remove
            TerminalBlock.OnMarkForClose -= BlockMarkedForClose;

            // Called when script is removed for any reason, so that you can clean up stuff if you need to.
        }

        void BlockMarkedForClose(IMyEntity ent)
        {
            Dispose();
        }

        void Draw2() // this is a custom method which is called in Run().
        {
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

            var frame = Surface.DrawFrame();

            // Drawing sprites works exactly like in PB API.
            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites

            // there are also some helper methods from the MyTSSCommon that this extends.
            // like: AddBackground(frame, Surface.ScriptBackgroundColor); - a grid-textured background

            // the colors in the terminal are Surface.ScriptBackgroundColor and Surface.ScriptForegroundColor, the other ones without Script in name are for text/image mode.

            var text = MySprite.CreateText("Hi!", "Monospace", Surface.ScriptForegroundColor, 1f, TextAlignment.LEFT);
            text.Position = screenCorner + new Vector2(16, 16); // 16px from topleft corner of the visible surface
            frame.Add(text);

            // add more sprites and stuff

            frame.Dispose(); // send sprites to the screen
        }

        void DrawError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

            try // first try printing the error on the LCD
            {
                Vector2 screenSize = Surface.SurfaceSize;
                Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

                var frame = Surface.DrawFrame();

                var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Color.Black);
                frame.Add(bg);

                var text = MySprite.CreateText($"ERROR: {e.Message}\n{e.StackTrace}\n\nPlease send screenshot of this to mod author.\n{MyAPIGateway.Utilities.GamePaths.ModScopeName}", "White", Color.Red, 0.7f, TextAlignment.LEFT);
                text.Position = screenCorner + new Vector2(16, 16);
                frame.Add(text);

                frame.Dispose();
            }
            catch (Exception e2)
            {
                MyLog.Default.WriteLineAndConsole($"Also failed to draw error on screen: {e2.Message}\n{e2.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
            }
        }

        // gets called at the rate specified by NeedsUpdate
        // it can't run every tick because the LCD is capped at 6fps anyway.
        public override void Run()
        {
            try
            {
                base.Run(); // do not remove

                // hold L key to see how the error is shown, remove this after you've played around with it =)
                if (MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.L))
                    throw new Exception("Oh noes an error :}");

                Draw();
            }
            catch (Exception e) // no reason to crash the entire game just for an LCD script, but do NOT ignore them either, nag user so they report it :}
            {
                DrawError(e);
            }
        }

        public void Draw()
        {
            if (myTerminalBlock.CustomData.Length <= 0)
                configit.CreateConfig();

            configit.LoadConfig();

            if (!configit.ConfigCheck)
                return;

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);

            //batteryBlocks.Clear();
            //windTurbines.Clear();
            //hydroenEngines.Clear();
            //solarPanels.Clear();
            //reactors.Clear();
            inventorys.Clear();
            //tanks.Clear();

            foreach (var myBlock in myFatBlocks)
            {
                //if (myBlock is IMyBatteryBlock)
                //{
                //    batteryBlocks.Add((IMyBatteryBlock)myBlock);
                //}
                //else if (myBlock is IMyPowerProducer)
                //{
                //    if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                //    {
                //        windTurbines.Add((IMyPowerProducer)myBlock);
                //    }
                //    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                //    {
                //        hydroenEngines.Add((IMyPowerProducer)myBlock);
                //    }
                //    else if (myBlock is IMyReactor)
                //    {
                //        reactors.Add((IMyReactor)myBlock);
                //    }
                //    else if (myBlock is IMySolarPanel)
                //    {
                //        solarPanels.Add((IMySolarPanel)myBlock);
                //    }
                //}
                //else if (myBlock is IMyGasTank)
                //{
                //    tanks.Add((IMyGasTank)myBlock);
                //}

                if (myBlock.HasInventory)
                {
                    for (int i = 0; i < myBlock.InventoryCount; i++)
                    {
                        inventorys.Add(myBlock.GetInventory(i));
                    }
                }
            }

            //cargoOres.Clear();
            cargoIngots.Clear();
            //cargoComponents.Clear();
            //cargoItems.Clear();

            foreach (var inventory in inventorys)
            {
                if (inventory.ItemCount == 0)
                    continue;

                inventoryItems.Clear();
                inventory.GetItems(inventoryItems);

                foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                {
                    var type = item.Type.TypeId.Split('_')[1];
                    var name = item.Type.SubtypeId;
                    var amount = item.Amount.ToIntSafe();

                    var myType = new cargoItemType { item = item, amount = 0 };

                    //if (type == "Ore")
                    //{
                    //    if (!cargoOres.ContainsKey(name))
                    //        cargoOres.Add(name, myType);

                    //    cargoOres[name].amount += amount;
                    //}
                    if (type == "Ingot")
                    {
                        if (!cargoIngots.ContainsKey(name))
                            cargoIngots.Add(name, myType);

                        cargoIngots[name].amount += amount;
                    }
                    //else if (type == "Component")
                    //{
                    //    if (!cargoComponents.ContainsKey(name))
                    //        cargoComponents.Add(name, myType);

                    //    cargoComponents[name].amount += amount;
                    //}
                    //else
                    //{
                    //    if (!cargoItems.ContainsKey(name))
                    //        cargoItems.Add(name, myType);

                    //    cargoItems[name].amount += amount;
                    //}
                }
            }

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(5, 5) + myViewport.Position;

            textSize = 1.0f;//config.Get("Settings", "TextSize").ToSingle(defaultValue: 1.0f);
            right = new Vector2(mySurface.SurfaceSize.X - 10, 0);
            newLine = new Vector2(0, 30 * textSize);
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            //if (config.Get("Settings", "Tanks").ToBoolean())
            //    DrawTanksSprite(ref myFrame, ref myPosition, mySurface);

            //if (config.Get("Settings", "Solar").ToBoolean())
            //    DrawSolarPanelSprite(ref myFrame, ref myPosition, mySurface);

            //if (config.Get("Settings", "Reactor").ToBoolean())
            //    DrawReactorSprite(ref myFrame, ref myPosition, mySurface);

            //if (config.Get("Settings", "Ore").ToBoolean())
            //    DrawOreSprite(ref myFrame, ref myPosition, mySurface);

            //if (config.Get("Settings", "Ingot").ToBoolean())
                DrawIngotSprite(ref myFrame, ref myPosition, mySurface);

            //if (config.Get("Settings", "Component").ToBoolean())
            //    DrawComponentSprite(ref myFrame, ref myPosition, mySurface);

            //if (config.Get("Settings", "Items").ToBoolean())
            //    DrawItemsSprite(ref myFrame, ref myPosition, mySurface);

            myFrame.Dispose();
        }

        //void DrawTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        //{
        //    var hydrogenTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
        //    var oxygenTanks = tanks.Where(block => !block.BlockDefinition.SubtypeName.Contains("Hydrogen"));

        //    var currentHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Average(block => block.FilledRatio * 100);
        //    var totalHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Sum(block => block.Capacity);

        //    var currentOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Average(block => block.FilledRatio * 100);
        //    var totalOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Sum(block => block.Capacity);

        //    WriteTextSprite(ref frame, "Hydrogen Tanks", position, TextAlignment.LEFT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, currentHydrogen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, KiloFormat((int)totalHydrogen), position + right, TextAlignment.RIGHT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Oxygen Tanks", position, TextAlignment.LEFT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, currentOxygen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, KiloFormat((int)totalOxygen), position + right, TextAlignment.RIGHT);

        //    position += newLine + newLine;
        //}

        //void DrawSolarPanelSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        //{
        //    var current = solarPanels.Sum(block => block.CurrentOutput);
        //    var currentMax = solarPanels.Sum(block => block.MaxOutput);
        //    var total = solarPanels.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

        //    WriteTextSprite(ref frame, "Solar Panels", position, TextAlignment.LEFT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

        //    position += newLine + newLine;
        //}

        //void DrawReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        //{
        //    var current = reactors.Sum(block => block.CurrentOutput);
        //    var total = reactors.Sum(block => block.MaxOutput);

        //    WriteTextSprite(ref frame, "Reactors", position, TextAlignment.LEFT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

        //    position += newLine;

        //    WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
        //    WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

        //    position += newLine + newLine;
        //}

        //void DrawOreSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        //{
        //    WriteTextSprite(ref frame, "Ores", position, TextAlignment.LEFT);

        //    position += newLine;

        //    foreach (var item in cargoOres)
        //    {
        //        MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

        //        WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
        //        WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

        //        position += newLine;
        //    }
        //}

        void DrawIngotSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "Ingots", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoIngots)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
        }

        //void DrawComponentSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        //{
        //    WriteTextSprite(ref frame, "Components", position, TextAlignment.LEFT);

        //    position += newLine;

        //    foreach (var item in cargoComponents)
        //    {
        //        MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

        //        WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
        //        WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

        //        position += newLine;
        //    }
        //}

        //void DrawItemsSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        //{
        //    WriteTextSprite(ref frame, "Items", position, TextAlignment.LEFT);

        //    position += newLine;

        //    foreach (var item in cargoItems)
        //    {
        //        MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

        //        WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
        //        WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

        //        position += newLine;
        //    }
        //}

        static string KiloFormat(int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + " M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0 K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + " K";

            return num.ToString("#,0");
        }

        void WriteTextSprite(ref MySpriteDrawFrame frame, string text, Vector2 position, TextAlignment alignment)
        {
            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = textSize,
                Color = mySurface.ScriptForegroundColor,
                Alignment = alignment,
                FontId = "White"
            };

            frame.Add(sprite);
        }
    }
}
