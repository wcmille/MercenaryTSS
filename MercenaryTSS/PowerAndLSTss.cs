﻿using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

//TODO: Show stockpile, off, etc as on capacity.
//TODO: Why might red be negative?

namespace MercenaryTSS
{
    [MyTextSurfaceScript("PowerAndLSTss", "BMC Power & Life Support")]
    public class PowerAndLSTSS : MyTSSCommon
    {
        private readonly IMyTerminalBlock TerminalBlock;
        readonly RectangleF viewport;
        readonly PowerWatcher pw;
        readonly GasWatcher gw;
        readonly GasWatcher ogw;
        readonly CargoWatcher cw;
        readonly SpriteDrawer sd;

        public PowerAndLSTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
            pw = new PowerWatcher(TerminalBlock);
            gw = new GasWatcher(TerminalBlock, MyResourceDistributorComponent.HydrogenId);
            ogw = new GasWatcher(TerminalBlock, MyResourceDistributorComponent.OxygenId);
            cw = new CargoWatcher(TerminalBlock);
            sd = new SpriteDrawer(viewport, surface);
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
                ogw.Refresh();
                cw.Refresh();
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

        private void Draw(ref MySpriteDrawFrame frame)
        {
            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites
            sd.Reset();
            sd.DrawSection(ref frame, pw, "IconEnergy", Surface.ScriptForegroundColor);
            sd.DrawSection(ref frame, gw, "IconHydrogen", Surface.ScriptForegroundColor);
            sd.DrawSection(ref frame, ogw, "IconOxygen", Surface.ScriptForegroundColor);
            sd.DrawCargo(ref frame, Surface.ScriptForegroundColor, "Ore/Ice", ()=>cw.Ice(), ()=>cw.IceBingo());
            sd.DrawCargo(ref frame, Surface.ScriptForegroundColor, "Ingot/Uranium", () => cw.Uranium(), ()=>cw.UraniumBingo());
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

        class SpriteDrawer
        {
            //readonly float y = 25;
            readonly float barHeight = 20.0f;
            readonly float iconWide = 32.0f;
            readonly float margin = 25.0f;
            readonly float barLength = 256.0f;
            readonly RectangleF viewport;
            readonly string text = "Uranium 999k";
            readonly float scale = 0.7f;
            readonly string font = "White";
            readonly Vector2 offset;
            readonly float disabledAlpha = 0.1f;
            readonly float textHeight;
            readonly float length100P;

            Vector2 pen;
            MySprite sprite;

            public SpriteDrawer(RectangleF viewport, IMyTextSurface surface)
            {
                this.viewport = viewport;

                //margin = 0.05f * viewport.Width;               
                barHeight = viewport.Height / ((4.125f * 3.0f) + 5.0f);
                margin = Math.Min(barHeight, 0.05f*viewport.Width);
                iconWide = barHeight * 1.5f;
                //barHeight = 0.04f * viewport.Height;
                StringBuilder b = new StringBuilder(text);
                offset = surface.MeasureStringInPixels(b, font, 1.0f);
                scale = barHeight / offset.Y;
                textHeight = offset.Y;

                offset = surface.MeasureStringInPixels(b, font, scale);
                offset.Y = 0;
                b = new StringBuilder(" 100 %");
                length100P = surface.MeasureStringInPixels(b, font, scale).X;
                barLength = viewport.Width - margin * 2.0f - iconWide - length100P;
            }

            public void Reset()
            {
                pen = viewport.Position + new Vector2(margin + iconWide, margin + barHeight * 0.5f);
            }

            public void DrawCargo(ref MySpriteDrawFrame frame, Color foreground, string resource, Func<float> amt, Func<float> bingoF)
            {
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = $"MyObjectBuilder_{resource}",
                    Position = new Vector2((margin + iconWide) * 0.5f, iconWide * 0.5f + pen.Y),
                    Size = new Vector2(iconWide, iconWide),
                    Color = foreground,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(sprite);

                sprite = new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = resource.Remove(0,resource.IndexOf('/')+1),
                    Position = pen,
                    Color = foreground,
                    FontId = font,
                    RotationOrScale = scale,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                sprite = new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = $"{KiloFormat(amt())}",
                    Position = pen + offset,
                    Color = foreground,
                    FontId = font,
                    RotationOrScale = scale,
                    Alignment = TextAlignment.RIGHT
                };
                frame.Add(sprite);

                var bingo = bingoF();
                if (bingo < -0.00001f)
                {
                    sprite = new MySprite
                    {
                        Type = SpriteType.TEXT,
                        Data = $"Empty in {TimeFormat(Math.Abs(bingo))}",
                        Position = pen + new Vector2(barLength, 0),
                        Color = foreground,
                        FontId = font,
                        RotationOrScale = scale,
                        Alignment = TextAlignment.RIGHT
                    };
                    frame.Add(sprite);
                }

                pen.Y += iconWide;          
            }

            public void DrawSection(ref MySpriteDrawFrame frame, IWatcher pw, string icon, Color foreground)
            {
                Color backbar = new Color(foreground.ToVector3() * 0.25f);

                //Draw Icon
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = icon,
                    Position = new Vector2((margin + iconWide) * 0.5f, barHeight * ((0.5f * 3.125f) - 0.5f) + pen.Y),
                    Size = new Vector2(iconWide, iconWide),
                    Color = foreground,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(sprite);

                //Draw Backbar
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength, barHeight),
                    Color = backbar,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                float cap = pw.CalcCapacity();
                //Draw Power Remaining
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * cap, barHeight),
                    Color = foreground,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                var offset = new Vector2(barLength*cap, 0);
                cap = pw.CalcUnusable();
                //Draw Potential Power Remaining
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen + offset,
                    Size = new Vector2(barLength * cap , barHeight),
                    Color = foreground.Alpha(disabledAlpha),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                //Indicate Scale
                var ccap = pw.CalcCapacity();
                if (!float.IsNaN(ccap))
                {
                    sprite = new MySprite
                    {
                        Type = SpriteType.TEXT,
                        Data = $"{ccap:P0}",
                        Position = new Vector2(pen.X + barLength + length100P, pen.Y - barHeight * 0.5f),
                        Color = foreground,
                        FontId = font,
                        RotationOrScale = scale,
                        Alignment = TextAlignment.RIGHT
                    };
                    frame.Add(sprite);
                }


                pen.Y += barHeight * 1.125f;
                //Draw Total Frame
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen + new Vector2(0, barHeight * 0.5f),
                    Size = new Vector2(barLength, barHeight * 2),
                    Color = backbar,
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                var cp = pw.CalcProduce();
                var cc = pw.CalcConsume();
                var max = Math.Max(cp, cc);
                var log = Math.Max(0.0f, Math.Ceiling(Math.Log10(max)));
                max = (float)Math.Pow(10.0f, log);
                cc /= max;
                cp /= max;

                //Draw Total Produce
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pen,
                    Size = new Vector2(barLength * cp, barHeight),
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
                    Size = new Vector2(barLength * cc, barHeight),
                    Color = Color.Red.Alpha(0.66f),
                    Alignment = TextAlignment.LEFT
                };
                frame.Add(sprite);

                float bingo = pw.CalcBingo();
                //Draw Bingo Text

                string dep = "Empty";
                string rech = "Full";

                if (Math.Abs(bingo) > 0.00001f)
                {
                    var t = bingo < 0.0 ? dep : rech;
                    sprite = new MySprite
                    {
                        Type = SpriteType.TEXT,
                        Data = $"{t} in {TimeFormat(Math.Abs(bingo))}",
                        Position = new Vector2(pen.X + barLength, pen.Y - (barHeight * (bingo < 0.0f ? 0.5f : 1.5f))),
                        Color = bingo > 0.0f ? Color.Black : foreground,
                        FontId = font,
                        RotationOrScale = scale,
                        Alignment = TextAlignment.RIGHT
                    };
                    frame.Add(sprite);
                }

                //Indicate Scale
                sprite = new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = $"{MFormat(max)}",
                    Position = new Vector2(pen.X + barLength + length100P, pen.Y - barHeight),
                    Color = foreground,
                    FontId = font,
                    RotationOrScale = scale,
                    Alignment = TextAlignment.RIGHT
                };
                frame.Add(sprite);

                pen.Y += barHeight * 2;
            }

            public static string TimeFormat(double seconds)
            {
                if (seconds < 1.0) return $"{seconds * 1000:F0}ms";
                if (seconds < 10.0) return $"{seconds:F1}s";
                if (seconds < 100) return $"{seconds:F0}s";
                if (seconds < 90 * 60) return $"{seconds / 60.0:F0}m";
                if (seconds < 3600 * 36) return $"{seconds / 3600.0:F0}h";
                return $"{seconds / (3600.0 * 24):F0}d";
            }

            static string MFormat(float num)
            {
                if (num >= 1000000000000)
                    return (num / 1000000000000).ToString("#,0T");

                if (num >= 1000000000)
                    return (num / 1000000000).ToString("#,0G");

                if (num >= 1000000)
                    return (num / 1000000).ToString("#,0M");

                if (num >= 1000)
                    return (num / 1000).ToString("#,0k");

                return num.ToString("#,0");
            }

            static string KiloFormat(float num)
            {
                if (num >= 100000000)
                    return (num / 1000000).ToString("#,0M");

                if (num >= 10000000)
                    return (num / 1000000).ToString("0.#") + "M";

                if (num >= 100000)
                    return (num / 1000).ToString("#,0k");

                if (num >= 10000)
                    return (num / 1000).ToString("0.#") + "k";

                return num.ToString("#,0");
            }
        }
    }
}