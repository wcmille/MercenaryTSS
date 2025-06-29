using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MercenaryTSS
{
    // Text Surface Scripts (TSS) can be selected in any LCD's scripts list.
    // These are meant as fast no-sync (sprites are not sent over network) display scripts, and the Run() method only executes player-side (no DS).
    // You can still use a session comp and access it through this to use for caches/shared data/etc.
    //
    // The display name has localization support aswell, same as a block's DisplayName in SBC.
    [MyTextSurfaceScript("PlanetMapTSS", "Planet Map")]
    public class PlanetMapTSS : MyTSSCommon
    {
        private readonly IMyTerminalBlock TerminalBlock;
        readonly string textureMapBase = "GV_Mercator_";
        readonly RectangleF viewPort;
        readonly Vector2 presenceRadius = new Vector2(19 * 2 + 1, 19 * 2 + 1);
        Vector3D center;
        //readonly RadioUtil ru = new RadioUtil();
        
        //private const double minDist2 = 1000.0 * 1000.0;
        readonly SigDraw sigDraw;

        public PlanetMapTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            // internal stored m_block is the ingame interface which has no events, so can't unhook later on, therefore this field is required.
            TerminalBlock = (IMyTerminalBlock)block;

            // required if you're gonna make use of Dispose() as it won't get called when block is removed or grid is cut/unloaded.
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

            var x = surface.SurfaceSize.X;
            var y = surface.SurfaceSize.Y;
            if (x / y >= 2.0f)
            {
                x = y * 2.0f;
            }
            else
            {
                y = x * 0.5f;
            }
            presenceRadius *= y / 512.0f;

            var minSurf = Math.Min(surface.TextureSize.X, surface.TextureSize.Y);
            float markerSize = 5.0f;
            if (minSurf <= 257) markerSize = 2.5f;
            var ss = new Vector2(x, y);
            viewPort = new RectangleF((surface.TextureSize - ss) / 2f, ss);
            sigDraw = new SigDraw(TerminalBlock, this.GPSToVector, markerSize);
        }

        protected PlanetMapTSS(string mapBase, IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : this(surface, block, size)
        {
            textureMapBase = mapBase;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100; // frequency that Run() is called.


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

        // gets called at the rate specified by NeedsUpdate
        // it can't run every tick because the LCD is capped at 6fps anyway.
        public override void Run()
        {
            try
            {
                base.Run(); // do not remove

                Draw();
            }
            catch (Exception e) // no reason to crash the entire game just for an LCD script, but do NOT ignore them either, nag user so they report it :}
            {
                DrawError(e);
            }
        }

        void Draw() // this is a custom method which is called in Run().
        {
            //Vector2 screenSize = Surface.SurfaceSize;
            //Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

            var frame = Surface.DrawFrame();
            {
                // Drawing sprites works exactly like in PB API.
                // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites

                // there are also some helper methods from the MyTSSCommon that this extends.
                // like: AddBackground(frame, Surface.ScriptBackgroundColor); - a grid-textured background

                // the colors in the terminal are Surface.ScriptBackgroundColor and Surface.ScriptForegroundColor, the other ones without Script in name are for text/image mode.

                DrawMap(frame);
                float ngm;
                var grav = MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(TerminalBlock.GetPosition(), out ngm);
                if (!Vector3D.IsZero(grav))
                {
                    var pos = GPSToVector(-grav, viewPort);
                    DrawLoc(frame, pos);
                    center = MyGamePruningStructure.GetClosestPlanet(TerminalBlock.GetPosition()).PositionComp.GetPosition();
                    sigDraw.DrawGPS(ref frame);//, planetPos);
                    sigDraw.DrawSigs(ref frame);//, planetPos);
                }
                // add more sprites and stuff
            }
            frame.Dispose(); // send sprites to the screen
        }

        void DrawMap(MySpriteDrawFrame frame)
        {
            var planet = MyGamePruningStructure.GetClosestPlanet(TerminalBlock.GetPosition());
            string name = "Alien";
            if (planet != null)
            {
                name = planet.Name;
                int dashLoc = name.IndexOf('-');
                if (dashLoc > 0)
                {
                    name = name.Substring(0, dashLoc);
                }
            }

            // Create background sprite
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Alignment = TextAlignment.CENTER,
                Size = new Vector2(viewPort.Width, viewPort.Height),
                Color = Color.Black
            };
            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = textureMapBase + name,
                Alignment = TextAlignment.CENTER,
                Size = new Vector2(viewPort.Width, viewPort.Height)
            };
            frame.Add(sprite);

            //sprite = new MySprite
            //{
            //    Type = SpriteType.TEXT,
            //    Data = name,
            //    Color = Color.GhostWhite,
            //    Position = new Vector2(20f,20f),
            //    FontId = "White",
            //    Alignment = TextAlignment.LEFT,
            //    RotationOrScale = 0.5f
            //};
            //frame.Add(sprite);
        }

        void DrawLoc(MySpriteDrawFrame frame, Vector2 pos)
        {
            // Create background sprite
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = presenceRadius,
                Color = Color.Blue.Alpha(0.50f),
                Alignment = TextAlignment.CENTER
            };
            frame.Add(sprite);
        }

        public Vector2 GPSToVector(Vector3D location)
        {
            return GPSToVector(location - center, viewPort);
        }

        public static Vector2 GPSToVector(Vector3D location, RectangleF viewport)
        {
            return new Vector2
            {
                X = (float)((viewport.Width / 2.0f + viewport.Width / 2.0f * Math.Atan2(location.X, -location.Z) / Math.PI) + viewport.X),
                Y = (float)((viewport.Height / 2.0f - viewport.Height * Math.Atan2(-location.Y, Math.Sqrt(location.X * location.X + location.Z * location.Z)) / Math.PI) + viewport.Y)
            };
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
    }
}
