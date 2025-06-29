using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace MercenaryTSS
{
    internal class SigDraw
    {
        readonly IMyTerminalBlock terminalBlock;
        readonly double minDist2 = 1000 * 1000;
        readonly float markerSize = 5.0f;
        readonly RadioUtil ru;
        private Func<Vector3D, Vector2> ConvertGPS { get; set; }

        public SigDraw(IMyTerminalBlock terminalBlock, Func<Vector3D, Vector2> convertGPS, float markerSize)
        {
            this.markerSize = markerSize;
            this.terminalBlock = terminalBlock;
            this.ConvertGPS = convertGPS;
            ru = new RadioUtil();
        }

        public void DrawGPS(ref MySpriteDrawFrame frame)
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
                            Position = ConvertGPS(g.Coords),
                            Size = new Vector2(markerSize),
                            Color = g.GPSColor,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)Math.PI / 4.0f
                        };
                        var backGround = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            Position = ConvertGPS(g.Coords),
                            Size = new Vector2(markerSize + 2),
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

        public void DrawSigs(ref MySpriteDrawFrame frame)
        {
            var lhp = MyAPIGateway.Session.LocalHumanPlayer;
            if (lhp != null)
            {
                ru.GetAllRelayedBroadcasters(lhp);

                foreach (var sig in ru.radioBroadcasters)
                {
                    if (sig != null && sig.ShowOnHud)
                    {
                        if ((sig.BroadcastPosition - terminalBlock.GetPosition()).LengthSquared() > minDist2)
                        {
                            var relation = lhp.GetRelationTo(sig.Owner);
                            Color color;
                            if (relation == MyRelationsBetweenPlayerAndBlock.Enemies) color = Color.Red;
                            else if (relation == MyRelationsBetweenPlayerAndBlock.Owner) color = Color.LightSkyBlue;
                            else if (relation == MyRelationsBetweenPlayerAndBlock.Neutral) color = Color.White;
                            else if (relation == MyRelationsBetweenPlayerAndBlock.Friends) color = Color.Green;
                            else if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare) color = Color.Green;
                            else color = Color.Orange;

                            var pos = ConvertGPS(sig.BroadcastPosition);
                            var sprite = new MySprite()
                            {
                                Type = SpriteType.TEXTURE,
                                Data = "SquareSimple",
                                Position = pos,
                                Size = new Vector2(markerSize, markerSize),
                                Color = color,
                                Alignment = TextAlignment.CENTER,
                            };
                            var backGround = new MySprite()
                            {
                                Type = SpriteType.TEXTURE,
                                Data = "SquareSimple",
                                Position = pos,
                                Size = new Vector2(markerSize + 2, markerSize + 2),
                                Color = Color.Black,
                                Alignment = TextAlignment.CENTER,
                            };
                            frame.Add(backGround);
                            frame.Add(sprite);
                        }
                    }
                }
            }
        }
    }
}
