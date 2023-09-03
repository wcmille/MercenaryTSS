using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;


namespace MercenaryTSS
{
    [MyTextSurfaceScript("TSS_SpaceMap", "BMC Space Map")]
    public class SpaceMapTSS : MyTSSCommon
    {
        private readonly IMyTerminalBlock TerminalBlock;
        readonly RectangleF viewport;
        Point originOffset = new Point(131000, 5700000);
        float scale = 0.0025f;
        readonly HashSet<MyPlanet> planets = new HashSet<MyPlanet>();

        public SpaceMapTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

            HashSet<IMyEntity> ents = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var e in ents)
            {
                if (e is MyPlanet) planets.Add(e as MyPlanet);
            }
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
            var sprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Size = new Vector2(Math.Max(1024, 1024)),
                Alignment = TextAlignment.CENTER
            };
            frame.Add(sprite);

            //var p = MyGamePruningStructure.GetClosestPlanet(TerminalBlock.GetPosition());

            foreach (var p in planets)
            //if (p != null)
            {
                var pos = p.PositionComp.GetPosition();
                var posT = TransformPos(pos);
                sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Position = posT,
                    Data = "Circle",
                    Size=new Vector2((p as MyPlanet).AverageRadius * 2.0f * scale),
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(sprite);
            }
            DrawGPS(ref frame);
        }

        void DrawGPS(ref MySpriteDrawFrame frame)
        {
            var lhp = MyAPIGateway.Session.LocalHumanPlayer;
            if (lhp != null)
            {
                var pid = lhp.IdentityId;
                var gpsList = MyAPIGateway.Session.GPS.GetGpsList(pid);
                foreach (var g in gpsList)
                {
                    if (g != null && g.ShowOnHud)
                    {
                        var sprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            Position = TransformPos(g.Coords),
                            Size = new Vector2(5, 5),
                            Color = g.GPSColor,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)Math.PI / 4.0f
                        };
                        var backGround = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            Position = TransformPos(g.Coords),
                            Size = new Vector2(7, 7),
                            Color = Color.Black,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)Math.PI / 4.0f
                        };
                        frame.Add(backGround);
                        frame.Add(sprite);
                    }
                }
            }
        }

        private Vector2 TransformPos(Vector3D pos)
        {
            var result = new Vector2((float)pos.X - originOffset.X, (float)pos.Z-originOffset.Y);
            result *= scale;
            result += viewport.Center;

            return result;
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
    }
}