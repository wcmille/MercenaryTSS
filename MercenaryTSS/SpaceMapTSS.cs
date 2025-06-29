using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
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
        double scanRad = 20000;
        readonly DrawVoxels voxDraw = new DrawVoxels();
        readonly IMyTextSurface surface;
        double gridTextureSize = 1024;
        double desiredSmallSquareSize;
        double gridScale;
        readonly SigDraw sigDraw;
        int bigUpdate = 0;
        const double smallSquaresInGrid = 23 * 3;

        public SpaceMapTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

            gridScale = 1000;
            SetValues();
            this.surface = surface;
            voxDraw.ViewportCenter = viewport.Center;
            sigDraw = new SigDraw(TerminalBlock, voxDraw.TransformPos, 5.0f);
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
                if (bigUpdate % 5 == 0) 
                {
                    bigUpdate = 0;
                    MyIni myIni = new MyIni();
                    if (myIni.TryParse(TerminalBlock.CustomData))
                    {
                        gridScale = myIni.Get("Mercenary.SpaceMap", "gridScale").ToDouble(gridScale);
                        scanRad = myIni.Get("Mercenary.SpaceMap", "scanRadius").ToDouble(scanRad);
                        gridScale = MathHelper.Clamp(gridScale, 300, 5000);
                        scanRad = MathHelper.Clamp(scanRad,1000,25000);
                        myIni.Set("Mercenary.SpaceMap", "gridScale", gridScale);
                        myIni.Set("Mercenary.SpaceMap", "scanRadius", scanRad);
                        TerminalBlock.CustomData = myIni.ToString();
                        SetValues();
                    }
                }
                ++bigUpdate;
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

        private void SetValues()
        {
            voxDraw.ScanSize = new Vector3D(scanRad, scanRad, scanRad);
            voxDraw.PixelPerMeter = (float)(viewport.Width / (2.0 * scanRad));
            desiredSmallSquareSize = ((double)viewport.Width) / (2.0 * scanRad / gridScale); //in pixels
            gridTextureSize = desiredSmallSquareSize * smallSquaresInGrid; //in pixels
        }

        private void BlockMarkedForClose(IMyEntity ent)
        {
            Dispose();
        }

        private void Draw(ref MySpriteDrawFrame frame)
        {
            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites
            DrawBaseLayer(ref frame);

            originOffset = TerminalBlock.GetPosition();
            //sigDraw.ConvertGPS = 
            voxDraw.Select(originOffset, TerminalBlock.CubeGrid.NaturalGravity);
            voxDraw.DrawVox(ref frame);
            sigDraw.DrawGPS(ref frame);
            sigDraw.DrawSigs(ref frame);

            //Write Scale
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