//using Sandbox.Game.Entities;
//using Sandbox.Game.GameSystems.TextSurfaceScripts;
//using Sandbox.ModAPI;
//using System;
//using VRage.Game;
//using VRage.Game.GUI.TextPanel;
//using VRage.Game.ModAPI;
//using VRage.ModAPI;
//using VRage.Utils;
//using VRageMath;


//namespace MercenaryTSS
//{
//    [MyTextSurfaceScript("TSS_DetailedInfo", "BMC Detailed Info")]
//    public class DetailedInfoTSS : MyTSSCommon
//    {
//        private readonly IMyTerminalBlock TerminalBlock;
//        readonly RectangleF viewport;
//        string blockName;

//        public DetailedInfoTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
//        {
//            TerminalBlock = (IMyTerminalBlock)block;
//            TerminalBlock.OnMarkForClose += BlockMarkedForClose;

//            viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
//        }

//        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

//        public override void Dispose()
//        {
//            base.Dispose();
//            TerminalBlock.OnMarkForClose -= BlockMarkedForClose;
//        }

//        public override void Run()
//        {
//            try
//            {
//                base.Run();
//                var frame = Surface.DrawFrame();
//                try
//                {
//                    Draw(ref frame);
//                }
//                finally
//                {
//                    frame.Dispose();
//                }
//            }
//            catch (Exception e)
//            {
//                DrawError(e);
//            }
//        }

//        private void BlockMarkedForClose(IMyEntity ent)
//        {
//            Dispose();
//        }

//        private void Draw(ref MySpriteDrawFrame frame)
//        {
//            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites
//            //var line = 67;
//            try
//            {
//                //line = 70;
//                var fatties = ((MyCubeGrid)TerminalBlock.CubeGrid).GetFatBlocks();
//                IMyTerminalBlock b = null;
//                //line = 73;

//                foreach (var fattie in fatties)
//                {
//                    //line = 77;
//                    if (fattie.DisplayNameText.Contains("Maintenance") && fattie is IMyTerminalBlock)
//                    {
//                        //line = 80;
//                        b = fattie as IMyTerminalBlock;
//                        break;
//                    }
//                }
//                //line = 85;
//                var theText = "Nothing.";
//                if (b != null)
//                {
//                    theText = b.DisplayNameText + "\n" + b.DetailedInfo;
//                }
//                //line = 88;
//                var sprite = new MySprite
//                {
//                    Type = SpriteType.TEXT,
//                    Data = theText,
//                    Alignment = TextAlignment.LEFT,
//                    Position = new Vector2(0, 0),
//                    FontId = "White",
//                    Color = Color.White,
//                    RotationOrScale = 1.0f,
//                };
//                //line = 99;
//                frame.Add(sprite);
//            }
//            catch (Exception e)
//            {
//                throw;// new Exception($"Line {line}");
//            }
//        }

//        private void DrawError(Exception e)
//        {
//            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

//            try
//            {
//                Vector2 screenSize = Surface.SurfaceSize;
//                Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

//                var frame = Surface.DrawFrame();

//                var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Color.Black);
//                frame.Add(bg);

//                var text = MySprite.CreateText($"ERROR: {e.Message}\n{e.StackTrace}\n\nPlease send screenshot of this to mod author.\n{MyAPIGateway.Utilities.GamePaths.ModScopeName}", "White", Color.Red, 0.7f, TextAlignment.LEFT);
//                text.Position = screenCorner + new Vector2(16, 16);
//                frame.Add(text);

//                frame.Dispose();
//            }
//            catch (Exception e2)
//            {
//                MyLog.Default.WriteLineAndConsole($"Also failed to draw error on screen: {e2.Message}\n{e2.StackTrace}");

//                if (MyAPIGateway.Session?.Player != null)
//                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
//            }
//        }
//    }
//}