using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace MercenaryTSS
{
    public class DrawVoxels
    {
        readonly Dictionary<string, IDrawPlanet> planets = new Dictionary<string, IDrawPlanet>();
        readonly List<MyVoxelBase> voxels = new List<MyVoxelBase>();
        public Vector3D ScanSize { get; set; }
        public float PixelPerMeter { get; set; }
        Vector3D originOffset;
        public Vector2 ViewportCenter { get; set; } 

        public DrawVoxels()
        {
            this.PixelPerMeter = 0.0025f;
            planets.Add("Alien", new MapW6Pix("Alien"));
            planets.Add("Kharak", new MapW2Pix("Kharak"));
            //Initialize Planets that have data.
        }

        IDrawPlanet ClosestPlanet { get; set; }

        static int Partition<T>(List<T> list, Func<T, bool> predicate)
        {
            int i = 0;
            for (int j = 0; j < list.Count; j++)
            {
                if (predicate(list[j]))
                {
                    // Manual swap using a temp variable
                    T temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                    i++;
                }
            }
            return i; // index of the partition point
        }

        public Func<Vector3D, Vector2> Select(Vector3D originOffst, Vector3D grav)
        {
            originOffset = originOffst;
            var box = new BoundingBoxD(originOffset - ScanSize, originOffset + ScanSize);
            voxels.Clear();
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref box, voxels);
            Partition(voxels, (x)=> { return !(x is MyPlanet); });
            //TODO: Only works for one planet.
            foreach (var v in voxels)
            {
                if (v is MyPlanet)
                {
                    var p = v as MyPlanet;
                    var name = p.Name;
                    int dashLoc = name.IndexOf('-');
                    if (dashLoc > 0)
                    {
                        name = name.Substring(0, dashLoc);
                    }
                    if (planets.ContainsKey(name))
                    {
                        ClosestPlanet = planets[name];
                        ClosestPlanet.SelectTransform(grav);                        
                    }
                    else throw new Exception($"Planet {name} not found in planets dictionary.");
                }
            }
            return TransformPos;
        }

        public Vector2 TransformPos(Vector3D pos)
        {
            pos -= originOffset;
            var result = (ClosestPlanet == null) ? new Vector2((float)pos.X, (float)pos.Z) : ClosestPlanet.Transform2D(pos);
            result = -result;
            result *= PixelPerMeter;
            result += ViewportCenter;

            return result;
        }

        public void DrawVox(ref MySpriteDrawFrame frame)
        {
            //try
            //{
            foreach (var v in voxels)
            {
                var data = "Circle";
                var wv = ((VRage.Game.ModAPI.Ingame.IMyEntity)v).WorldVolume;
                var pos = wv.Center;
                var posT = TransformPos(pos);
                float radius = (float)wv.Radius * 0.5f;
                float rot = 0.0f;
                Color color = Color.DarkGray.Alpha(0.5f);
                if (v is MyPlanet)
                {
                    radius = (v as MyPlanet).AverageRadius;
                    color = Color.White.Alpha(0.75f);
                    data = ClosestPlanet.Data;
                    //rot = (float)Math.PI * 0.5f;
                }
                else color = Color.Darken(color, 0.4);
                var sprite = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Position = posT,
                    Data = data,
                    Size = new Vector2(radius * 2 * PixelPerMeter),
                    Alignment = TextAlignment.CENTER,
                    Color = color,
                    RotationOrScale = rot,
                };
                frame.Add(sprite);
            }
        }
    }

    interface IDrawPlanet
    {
        Func<Vector3D, Vector2> SelectTransform(Vector3D grav);
        string Data { get; }
        Func<Vector3D, Vector2> Transform2D { get; }
    }

    public class MyDefaultPlanet : IDrawPlanet
    {
        public string Name { get; set; }

        public string Data
        {
            get
            {
                return "Circle";
            }
        }

        public Func<Vector3D, Vector2> Transform2D
        {
            get
            {
                return TransformPos;
            }
        }

        public Func<Vector3D, Vector2> SelectTransform(Vector3D grav)
        {
            return Transform2D;
        }

        private Vector2 TransformPos(Vector3D pos)
        {
            return new Vector2((float)pos.X, (float)pos.Z);
        }
    }

    public class MapW2Pix : IDrawPlanet
    {
        protected string planetName;
        public string Data { get; protected set; }
        public Func<Vector3D, Vector2> Transform2D { get; protected set; }

        public MapW2Pix(string planetName)
        {
            this.planetName = planetName;
         }
        public virtual Func<Vector3D, Vector2> SelectTransform(Vector3D grav)
        {
            if (grav.Y > 0)
            {
                Transform2D = TransformPosMirror;
                Data = $"GV_Polar_{planetName}N";
            }
            else
            {
                Transform2D = TransformPos;
                Data = $"GV_Polar_{planetName}S";
            }
            return Transform2D;
        }

        protected Vector2 TransformPos(Vector3D pos)
        {
            return new Vector2((float)pos.X, (float)pos.Z);
        }

        protected Vector2 TransformPosMirror(Vector3D pos)
        {
            return new Vector2((float)-pos.X, (float)pos.Z);
        }
    }

    public class MapW6Pix : MapW2Pix
    {
        public MapW6Pix(string planetName) : base(planetName)
        {
        }
        public override Func<Vector3D, Vector2> SelectTransform(Vector3D grav)
        {
            switch (grav.AbsMaxComponent())
            {
                case 0:
                    if (grav.X > 0)
                    {
                        Transform2D = TransformPosNx;
                        Data = $"GV_Polar_{planetName}Nx";
                    }
                    else
                    {
                        Transform2D = TransformPosPx;
                        Data = $"GV_Polar_{planetName}Px";
                    }
                    break;
                case 1:
                    if (grav.Y > 0)
                    {
                        Transform2D = TransformPosMirror;
                        Data = $"GV_Polar_{planetName}N";
                    }
                    else
                    {
                        Transform2D = TransformPos;
                        Data = $"GV_Polar_{planetName}S";
                    }
                    break;
                case 2:
                    if (grav.Z > 0)
                    {
                        Transform2D = TransformPosNz;
                        Data = $"GV_Polar_{planetName}Nz";
                    }
                    else
                    {
                        Transform2D = TransformPosPz;
                        Data = $"GV_Polar_{planetName}Pz";
                    }
                    break;
            }
            return Transform2D;
        }
        private Vector2 TransformPosPx(Vector3D pos) //right
        {
            return new Vector2((float)pos.Z, (float)pos.Y);
        }
        private Vector2 TransformPosNx(Vector3D pos) //left
        {
            return new Vector2((float)-pos.Z, (float)pos.Y);
        }

        private Vector2 TransformPosPz(Vector3D pos) //back
        {
            return new Vector2((float)-pos.X, (float)pos.Y);
        }

        private Vector2 TransformPosNz(Vector3D pos) //front
        {
            return new Vector2((float)pos.X, (float)pos.Y);
        }
    }
}
