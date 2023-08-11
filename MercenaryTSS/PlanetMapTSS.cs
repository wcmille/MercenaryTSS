using System;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
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
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10; // frequency that Run() is called.

        private readonly IMyTerminalBlock TerminalBlock;

        public PlanetMapTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block; // internal stored m_block is the ingame interface which has no events, so can't unhook later on, therefore this field is required.
            TerminalBlock.OnMarkForClose += BlockMarkedForClose; // required if you're gonna make use of Dispose() as it won't get called when block is removed or grid is cut/unloaded.

            // Called when script is created.
            // This class is instanced per LCD that uses it, which means the same block can have multiple instances of this script aswell (e.g. a cockpit with all its screens set to use this script).
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

        void Draw() // this is a custom method which is called in Run().
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

        //      string gps_string = "GPS:med wreck:31469.36:2860.77:34731.6:#FF75C9F1:";
        //      string latlon_string = "Lat: S 32.4966330180401: Lon: W 125.378729204909";

        //      var split = gps_string.Split(':');
        //      Vector3 origin = new Vector3(32768.5f, 32768.5f, 32768.5f);
        //      Vector3 gps = new Vector3(float.Parse(split[2]), float.Parse(split[3]), float.Parse(split[4]));
        //      Vector3 abs = gps - origin;
        //      double lat = Math.Abs(Math.Atan2(Math.Sqrt(abs.X * abs.X + abs.Z * abs.Z), abs.Y) * 180 / Math.PI) - 90;
        //      double lon = Math.Atan2(abs.Z, abs.X) * 180 / Math.PI;



        //      split = latlon_string.Split(':');
        //var lat_split = split[1].Split(' ');
        //      var theta = (float.Parse(lat_split[2]) * (lat_split[1] == "N" ? -1 : 1) + 90) * Math.PI / 180;
        //      var lon_split = split[3].Split(' ');
        //      var phi = (float.Parse(lon_split[2]) * (lon_split[1] == "E" ? -1 : 1)) * Math.PI / 180;
        //      var rho = 30000;
        //      var coords = new Vector3((float)(rho * Math.Sin(theta) * Math.Cos(phi)), (float)(-rho * Math.Cos(theta)), -(float)(rho * Math.Sin(theta) * Math.Sin(phi)));
        //      coords += origin;


        //Console.Out.WriteLine(String.Format("Lat: {0} {1}: Lon: {2} {3}", lat >= 0 ? "N" : "S" , Math.Abs(lat), lon >= 0 ? "E" : "W" , Math.Abs(lon)));
        //Console.Out.WriteLine();
        //Console.Out.WriteLine(String.Format("GPS:MAP_GPS:{0}:{1}:{2}:#FFFFFFFF:", coords.X, coords.Y, coords.Z));
    }
}
