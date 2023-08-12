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
        readonly string textureMapBase = "GVK_KharakMercator";

        public PlanetMapTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block; // internal stored m_block is the ingame interface which has no events, so can't unhook later on, therefore this field is required.
            TerminalBlock.OnMarkForClose += BlockMarkedForClose; // required if you're gonna make use of Dispose() as it won't get called when block is removed or grid is cut/unloaded.

            // Called when script is created.
            // This class is instanced per LCD that uses it, which means the same block can have multiple instances of this script aswell (e.g. a cockpit with all its screens set to use this script).
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
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

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
                    var pos = GPSToVector(-grav, (int)screenSize.X, (int)screenSize.Y);
                    DrawLoc(frame, pos);
                    var planetPos = MyGamePruningStructure.GetClosestPlanet(TerminalBlock.GetPosition()).PositionComp.GetPosition();
                    DrawGPS(frame, planetPos, screenSize);
                }
                // add more sprites and stuff
            }
            frame.Dispose(); // send sprites to the screen
        }

        void DrawMap(MySpriteDrawFrame frame)
        {
            // Create background sprite
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = textureMapBase,
                Alignment = TextAlignment.CENTER
            };
            // Add the sprite to the frame
            frame.Add(sprite);
        }

        void DrawLoc(MySpriteDrawFrame frame, Vector2 pos)
        {
            // Create background sprite
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = new Vector2(19 * 2 + 1, 19 * 2 + 1),
                Color = Color.Blue.Alpha(0.50f),
                Alignment = TextAlignment.CENTER
            };
            frame.Add(sprite);
        }

        void DrawGPS(MySpriteDrawFrame frame, Vector3D center, Vector2 screenSize)
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
                            Position = GPSToVector(g.Coords - center, (int)screenSize.X, (int)screenSize.Y),
                            Size = new Vector2(5, 5),
                            Color = g.GPSColor,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)Math.PI / 4.0f
                        };
                        frame.Add(sprite);
                    }
                }
            }
        }

        public static Vector2I GPSToVector(Vector3D location, int screenWidth, int screenHeight)
        {
            return new Vector2I
            {
                X = screenWidth / 2 + (int)(screenWidth / 2 * -Math.Atan2(location.X, location.Z) / Math.PI),
                Y = screenHeight / 2 - (int)(screenHeight * Math.Atan2(-location.Y, Math.Sqrt(location.X * location.X + location.Z * location.Z)) / Math.PI)
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
