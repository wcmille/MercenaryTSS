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
        Vector3D originOffset = new Vector3D(131000, 5700000, 0);
        const double scanRad = 20000;
        readonly float pixelPerMeter = 0.0025f;
        Vector3D scanSize = new Vector3D(scanRad, scanRad, scanRad);
        readonly List<MyVoxelBase> planets = new List<MyVoxelBase>();
        readonly IMyTextSurface surface;
        readonly double gridTextureSize = 1024;
        readonly double desiredSmallSquareSize;
        readonly double gridScale;
        readonly SigDraw sigDraw;

        public SpaceMapTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

            pixelPerMeter = (float)(viewport.Width / (2.0 * scanRad));
            //const double gridSpriteWidth = 2048;
            const double smallSquaresInGrid = 23 * 3;
            //const double smallSquareSizeOnTexture = 2048.0 / (23.0 * 4.0);
            gridScale = 1000;
            desiredSmallSquareSize = ((double)viewport.Width) / (2.0 * scanRad / gridScale); //in pixels
            gridTextureSize = desiredSmallSquareSize * smallSquaresInGrid; //in pixels

            this.surface = surface;
            sigDraw = new SigDraw(TerminalBlock, TransformPos, 5.0f);
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
            if (TerminalBlock.CubeGrid.NaturalGravity.Y > 0)
            {
                sigDraw.ConvertGPS = TransformPosMirror;
            }
            else 
            {
                sigDraw.ConvertGPS = TransformPos;
            }
            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites
            DrawBaseLayer(ref frame);

            originOffset = TerminalBlock.GetPosition();
            //var p = MyGamePruningStructure.GetClosestPlanet(TerminalBlock.GetPosition());
            DrawVoxels(ref frame);
            sigDraw.DrawGPS(ref frame);
            sigDraw.DrawSigs(ref frame);

            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = $"Scale: 1 sq = {3*gridScale}m",
                Color = surface.ScriptForegroundColor,
                Position = viewport.Position,
                FontId = "White",
                Alignment = TextAlignment.LEFT,
                RotationOrScale = 0.5f
            };
            frame.Add(sprite);
        }

        private void DrawBaseLayer(ref MySpriteDrawFrame frame)
        {
            var drawPoint = viewport.Center;
            drawPoint.Y += (float)desiredSmallSquareSize * 1.5f;
            var sprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Size = new Vector2((float)gridTextureSize),
                Alignment = TextAlignment.LEFT,
                Color = surface.ScriptForegroundColor.Alpha(0.25f),
                Position = drawPoint,
            };
            frame.Add(sprite);

            sprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Size = new Vector2((float)gridTextureSize),
                Alignment = TextAlignment.RIGHT,
                Color = surface.ScriptForegroundColor.Alpha(0.25f),
                Position = drawPoint,
            };

            frame.Add(sprite);
            sprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "CircleHollow",
                Size = new Vector2(32),
                Alignment = TextAlignment.CENTER,
                Color = surface.ScriptForegroundColor.Alpha(0.25f),
                //Position = drawPoint,
            };
            frame.Add(sprite);
        }

        private void DrawVoxels(ref MySpriteDrawFrame frame)
        {
            //try
            //{
            var box = new BoundingBoxD(originOffset - scanSize, originOffset + scanSize);
            planets.Clear();
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref box, planets);
            foreach (var p in planets)
            {
                var data = "Circle";
                var wv = ((VRage.Game.ModAPI.Ingame.IMyEntity)p).WorldVolume;
                var pos = wv.Center;
                var posT = sigDraw.ConvertGPS(pos);
                float radius = (float)wv.Radius * 0.5f;
                float rot = 0.0f;
                Color color = Color.DarkGray.Alpha(0.5f);
                if (p is MyPlanet)
                {
                    if (sigDraw.ConvertGPS == TransformPos)
                    {
                        data = "GV_Polar_AlienS";
                        rot = (float)Math.PI * 1.5f;
                    }
                    else
                    {
                        data = "GV_Polar_AlienN";
                        rot = (float)Math.PI * 1.5f;
                        //var r = posT;
                        //r -= viewport.Center;
                        //r.X = -r.X;
                        //posT = r + viewport.Center;
                    }
                    radius = (p as MyPlanet).AverageRadius;
                    color = Color.White.Alpha(0.1f);
                }
                else color = Color.Darken(color, 0.4);
                var sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Position = posT,
                    Data = data,
                    Size = new Vector2(radius * 2 * pixelPerMeter),
                    Alignment = TextAlignment.CENTER,
                    Color = color,
                    RotationOrScale = rot,
                };
                frame.Add(sprite);
            }
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception($"Line: {line}", ex);
            //}
        }

        private Vector2 TransformPos(Vector3D pos)
        {
            pos -= originOffset;
            var result = new Vector2((float)pos.X, (float)pos.Z);
            result *= pixelPerMeter;
            result += viewport.Center;

            return result;
        }

        private Vector2 TransformPosMirror(Vector3D pos)
        {
            pos -= originOffset;
            var result = new Vector2((float)-pos.X, (float)pos.Z);
            result *= pixelPerMeter;
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