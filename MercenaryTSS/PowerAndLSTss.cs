using Sandbox.Game.Entities;
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
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MercenaryTSS
{
    [MyTextSurfaceScript("PowerAndLSTss", "Power & Life Support")]
    public class PowerAndLSTss : MyTSSCommon
    {
        private readonly IMyTerminalBlock TerminalBlock;
        readonly RectangleF viewport;
        readonly PowerWatcher pw;
        readonly GasWatcher gw;
        readonly SpriteDrawer sd;

        public PowerAndLSTss(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
            pw = new PowerWatcher(TerminalBlock);
            gw = new GasWatcher(TerminalBlock);
            sd = new SpriteDrawer(viewport);
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        public override void Dispose()
        {
            base.Dispose();
            TerminalBlock.OnMarkForClose -= BlockMarkedForClose;
        }

        public override void Run()
        {
            try
            {
                base.Run();
                pw.Refresh();
                gw.Refresh();
                var frame = Surface.DrawFrame();
                try
                {
                    Draw(ref frame);
                }
                finally
                {
                    frame.Dispose();
                }
            }
            catch (Exception e)
            {
                DrawError(e);
            }
        }

        private void BlockMarkedForClose(IMyEntity ent)
        {
            Dispose();
        }

        class SpriteDrawer
        {
            readonly float y = 25;
            readonly float barHeight = 20.0f;
            readonly float iconWide = 32.0f;
            readonly float margin = 25.0f;
            readonly float barLength = 256.0f;
            Vector2 pen;
            readonly RectangleF viewport;
            MySprite sprite;

            public SpriteDrawer(RectangleF viewport)
            {       
                this.viewport = viewport;
            }

            public void Reset()
            {
                pen = viewport.Position + new Vector2(margin + iconWide, margin + barHeight * 0.5f);
            }

            public void DrawPower(ref MySpriteDrawFrame frame, PowerWatcher pw)
            {
                //Draw Backbar
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "IconEnergy",
                    Position = new Vector2((margin + iconWide) * 0.5f, margin + (barHeight * 4.0f - y) * 0.5f + viewport.Position.Y),
                    Size = new Vector2(iconWide, iconWide),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(sprite);

                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength, barHeight),
                    Color = Color.Gray,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                //Draw Power Remaining
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * pw.CalcCapacity(), barHeight),
                    Color = Color.Green.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                pen.Y += y;
                //Draw Total Frame
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen + new Vector2(0, barHeight * 0.5f),
                    Size = new Vector2(barLength, barHeight * 2),
                    Color = Color.Gray,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                //Draw Total Consume
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * pw.CalcProduce(), barHeight),
                    Color = Color.Blue.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                pen.Y += barHeight;
                //Draw Total Consume
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * pw.CalcConsume(), barHeight),
                    Color = Color.Red.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);
                pen.Y += 2 * y;
            }
            public void DrawHydro(ref MySpriteDrawFrame frame, GasWatcher gw)
            {
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "IconHydrogen",
                    Position = new Vector2((margin + iconWide) * 0.5f, margin + 2 * y + (barHeight * 4.0f - y) * 1.5f + viewport.Position.Y),
                    Size = new Vector2(iconWide, iconWide),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(sprite);

                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength, barHeight),
                    Color = Color.Gray,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                //Draw Power Remaining
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * gw.CalcCapacity(), barHeight),
                    Color = Color.Green.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                pen.Y += y;
                //Draw Total Frame
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen + new Vector2(0, barHeight * 0.5f),
                    Size = new Vector2(barLength, barHeight * 2),
                    Color = Color.Gray,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                //Draw Total Consume
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * gw.CalcProduce(), barHeight),
                    Color = Color.Blue.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                pen.Y += barHeight;
                //Draw Total Consume
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * gw.CalcConsume(), barHeight),
                    Color = Color.Red.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);
                pen.Y += 2 * y;
            }

            public void DrawOxy(ref MySpriteDrawFrame frame, GasWatcher gw)
            {
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "IconOxygen",
                    Position = new Vector2((margin + iconWide) * 0.5f, margin + 4 * y + (barHeight * 4.0f - y) * 2.5f + viewport.Position.Y),
                    Size = new Vector2(iconWide, iconWide),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(sprite);

                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength, barHeight),
                    Color = Color.Gray,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                //Draw Power Remaining
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * gw.CalcOCapacity(), barHeight),
                    Color = Color.Green.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);
            }
        }

        private void Draw(ref MySpriteDrawFrame frame)
        {
            //Vector2 screenSize = Surface.SurfaceSize;
            //Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;
            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites
            sd.Reset();
            sd.DrawPower(ref frame, pw);
            sd.DrawHydro(ref frame, gw);
            sd.DrawOxy(ref frame, gw);
        }

        private void DrawError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

            try
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

        public class GasWatcher
        {
            readonly IMyTerminalBlock myTerminalBlock;
            readonly List<IMyGasTank> h2Tanks = new List<IMyGasTank>();
            readonly List<IMyGasTank> o2Tanks = new List<IMyGasTank>();

            public GasWatcher(IMyTerminalBlock myTerminalBlock)
            {
                this.myTerminalBlock = myTerminalBlock;
            }
            public float CalcOCapacity()
            {
                var current = o2Tanks.Sum(x => (float)x.FilledRatio * x.Capacity);
                var total = o2Tanks.Sum(x => x.Capacity);
                return current / total;
            }

            public float CalcCapacity()
            {
                var current = h2Tanks.Sum(x=> (float)x.FilledRatio * x.Capacity);
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
                        current += c.CurrentInputByType(MyResourceDistributorComponent.HydrogenId);
                    }
                }
                return current / total;
            }

            public float CalcConsume()
            {
                float current = 0.0f;
                float total = 0.0f;
                foreach(var block in h2Tanks) 
                {
                    var c = block.Components.Get<MyResourceSourceComponent>();
                    if (c != null)
                    {
                        current+=c.CurrentOutput;
                        total += c.MaxOutput;
                    }
                }
                return current / total;
            }

            public void Refresh()
            {
                o2Tanks.Clear();
                h2Tanks.Clear();
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock is IMyGasTank)
                    {
                        if ((myBlock as IMyGasTank).BlockDefinition.SubtypeName.Contains("Hydrogen")) h2Tanks.Add(myBlock as IMyGasTank);
                        else /*if ((myBlock as IMyGasTank).BlockDefinition.SubtypeName.Contains("Oxygen"))*/ o2Tanks.Add(myBlock as IMyGasTank);
                    }
                }
            }
        }

        public class PowerWatcher
        {
            readonly IMyTerminalBlock myTerminalBlock;
            readonly List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
            readonly List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
            readonly List<IMyPowerProducer> hydroenEngines = new List<IMyPowerProducer>();
            readonly List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
            readonly List<IMyReactor> reactors = new List<IMyReactor>();

            public PowerWatcher(IMyTerminalBlock myTerminalBlock)
            {
                this.myTerminalBlock = myTerminalBlock;
            }

            public float CalcCapacity()
            {
                var current = batteryBlocks.Sum(x => x.CurrentStoredPower);
                var total = batteryBlocks.Sum(x => x.MaxStoredPower);
                return current / total;
            }

            public float CalcProduce()
            {
                //batteryBlocks.Sum(x => x.CurrentOutput) + 
                var current = hydroenEngines.Sum(x=>x.CurrentOutput);
                var total = batteryBlocks.Sum(x => x.MaxOutput);
                return current / total;
            }

            public float CalcConsume()
            {
                var current = batteryBlocks.Sum(x => x.CurrentOutput) + hydroenEngines.Sum(x => x.CurrentOutput) - batteryBlocks.Sum(x => x.CurrentInput);
                var total = batteryBlocks.Sum(x => x.MaxOutput);
                return current / total;
            }

            public void Refresh()
            {
                batteryBlocks.Clear();
                windTurbines.Clear();
                hydroenEngines.Clear();
                solarPanels.Clear();
                reactors.Clear();

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
                            windTurbines.Add((IMyPowerProducer)myBlock);
                        }
                        else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                        {
                            hydroenEngines.Add((IMyPowerProducer)myBlock);
                        }
                        else if (myBlock is IMyReactor)
                        {
                            reactors.Add((IMyReactor)myBlock);
                        }
                        else if (myBlock is IMySolarPanel)
                        {
                            solarPanels.Add((IMySolarPanel)myBlock);
                        }
                    }
                }
            }
        }
    }
}