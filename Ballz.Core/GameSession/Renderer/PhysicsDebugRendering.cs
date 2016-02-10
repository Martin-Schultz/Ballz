﻿using Ballz.GameSession.World;
using Ballz.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MathFloat.MathF;

namespace Ballz.GameSession.Renderer
{
    public class DebugRenderer : DrawableGameComponent
    {
        new Ballz Game;
        BasicEffect LineEffect;
        VertexPositionColor[] sphereVertices;
        VertexPositionTexture[] quad;
        List<VertexPositionColor[]> terrainVertices = new List<VertexPositionColor[]>();
        private World.World debugWorld;
        private int terrainRevision = -1;
        private SpriteBatch spriteBatch;
        private Texture2D whiteTexture;

        private Effect VectorFieldEffect;

        public DebugRenderer(Ballz _game) : base(_game)
        {
            Game = _game;
        }

        public override void Draw(GameTime gameTime)
        {
            //DrawSphere(Vector2.Zero, new Vector2(0.0f,1.0f));
            debugWorld = Game.World;
            foreach (Entity ball in debugWorld.Entities)
            {
                if (ball.Disposed)
                    continue;

                DrawSphere(ball.Position, ball.Rotation, ball.Radius, ball.PhysicsBody?.Awake ?? false);
            }

            foreach(var rope in debugWorld.Ropes)
            {
                DrawRope(rope);
            }

            DrawTerrain();
            drawWater();

            base.Draw(gameTime);
        }

        public override void Initialize()
        {
            debugWorld = Game.World;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            LineEffect = new BasicEffect(Game.GraphicsDevice);
            LineEffect.EnableDefaultLighting();
            LineEffect.DiffuseColor = new Vector3(1, 1, 1);
            LineEffect.VertexColorEnabled = true;
            LineEffect.LightingEnabled = false;
            LineEffect.TextureEnabled = false;

            VectorFieldEffect = Game.Content.Load<Effect>("Effects/VectorField");

            sphereVertices = new VertexPositionColor[18];

            for (int i = 0; i <= 16; i++)
            {
                float angle = (float)Math.PI * 2.0f * (float)i / 16;
                sphereVertices[i].Color = Color.GreenYellow;
                sphereVertices[i].Position = new Vector3(Sin(angle), Cos(angle), 0);
            }

            sphereVertices[17].Color = Color.GreenYellow;
            sphereVertices[17].Position = Vector3.Zero;

            quad = new VertexPositionTexture[]
            {
                new VertexPositionTexture(new Vector3(0, 0, 0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(1, 1, 0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(0, 1, 0.5f), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(0, 0, 0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(1, 0, 0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(1, 1, 0.5f), new Vector2(1, 1)),
            };

            Matrix terrainWorld = Matrix.CreateScale(0.03f);

            spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            whiteTexture = new Texture2D(Game.GraphicsDevice,1,1,false,SurfaceFormat.Color);
            whiteTexture.SetData(new Color[1]
            {
                Color.White
            });
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        private void UpdateTerrain()
        {
            if(terrainRevision != Game.World.StaticGeometry.Revision)
            {
                List<List<Vector2>> outline = Game.World.StaticGeometry.GetOutline();
                terrainVertices.Clear();
                foreach (List<Vector2> lineStrip in outline)
                {
                    VertexPositionColor[] lineVertices = new VertexPositionColor[lineStrip.Count];
                    for (int i = 0; i < lineStrip.Count; i++)
                    {
                        lineVertices[i].Color = Color.GreenYellow;
                        lineVertices[i].Position = new Vector3(lineStrip[i],0) * Game.World.StaticGeometry.Scale;
                    }

                    terrainVertices.Add(lineVertices);
                }

                terrainRevision = Game.World.StaticGeometry.Revision;
            }
        }

        public override void Update(GameTime gameTime)
        {
            debugWorld = Game.World;
            UpdateTerrain();
            base.Update(gameTime);
        }

        public void HandleMessage(object sender, Messages.Message msg)
        {
            if (msg.Kind == Message.MessageType.InputMessage)
            {
                InputMessage ipmsg = (InputMessage)msg;
                if (ipmsg.Kind == InputMessage.MessageType.ControlsConsole && ipmsg.Pressed.HasValue && ipmsg.Pressed.Value)
                {
                    Enabled = !Enabled;
                    Visible = !Visible;
                    debugWorld = Game.World;
                }
            }
        }

        protected override void OnEnabledChanged(object sender, EventArgs args)
        {
            base.OnEnabledChanged(sender, args);
        }
            
        public void DrawSphere(Vector2 position, float direction, float radius, bool awake)
        {
            LineEffect.Projection = Game.Camera.Projection;
            LineEffect.View = Game.Camera.View;
            LineEffect.World = Matrix.CreateScale(radius);
            LineEffect.World *= Matrix.CreateRotationZ(direction);
            LineEffect.World *= Matrix.CreateTranslation(new Vector3(position, 0));

            if(awake)
                LineEffect.DiffuseColor = new Vector3(1, 0, 0);

            LineEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, sphereVertices, 0, sphereVertices.Length - 1);
        }

        public void DrawRope(Rope rope)
        {
            foreach(var segment in rope.PhysicsSegments)
            {
                DrawSphere(segment.Position, segment.Rotation, Rope.SegmentLength * 0.5f * 0.8f, true);
            }
        }

        public void DrawTerrain()
        {
            LineEffect.Projection = Game.Camera.Projection;
            LineEffect.View = Game.Camera.View;
            LineEffect.World = Matrix.Identity;
            LineEffect.DiffuseColor = Color.GreenYellow.ToVector3();

            LineEffect.CurrentTechnique.Passes[0].Apply();
            foreach (VertexPositionColor[] lineVertices in terrainVertices)
            {
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, lineVertices, 0, lineVertices.Length - 1);
            }
        }
        
        public Vector2 WorldToScreen(Vector3 Position)
        {
            var screenSpace = Vector4.Transform(Position, (Game.Camera.Projection * Game.Camera.View));
            screenSpace /= screenSpace.W;
            return new Vector2
            {
                X = (0.5f + 0.5f * screenSpace.X) * Game.GraphicsDevice.Viewport.Width,
                Y = (1 - (0.5f + 0.5f * screenSpace.Y)) * Game.GraphicsDevice.Viewport.Height,
            };
        }

        public Vector2 WorldToScreen(Vector2 Position)
        {
            return WorldToScreen(new Vector3(Position, 0));
        }

        Texture2D WaterTexture = null;

        const float MaxWaterVelocity = 0.01f;
        const int WaterGridSize = 5;

        public void drawWater()
        {
            var blending = new BlendState();
            blending.AlphaSourceBlend = Blend.SourceAlpha;
            blending.AlphaDestinationBlend = Blend.InverseSourceAlpha;
            blending.ColorSourceBlend = Blend.SourceAlpha;
            blending.ColorDestinationBlend = Blend.InverseSourceAlpha;

            var water = debugWorld.Water;
            var w = water.Width / WaterGridSize;
            var h = water.Height / WaterGridSize;

            if (WaterTexture == null)
                WaterTexture = new Texture2D(Game.GraphicsDevice, w, h, false, SurfaceFormat.Color);

            Color[] waterColors = new Color[w*h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var velocity = water.Velocity(x * WaterGridSize, y * WaterGridSize);
                    velocity /= MaxWaterVelocity;

                    velocity *= 0.5f;
                    velocity += new Vector2(0.5f);

                    if (velocity.X > 1)
                        velocity.X = 1;
                    if (velocity.Y > 1)
                        velocity.Y = 1;

                    waterColors[y * w + x] = new Color(new Vector4(velocity.X, velocity.Y, 0, water[x * WaterGridSize, y * WaterGridSize]));
                }

            WaterTexture.SetData(waterColors);

            //var pos = new Vector2(x * debugWorld.StaticGeometry.Scale, y * debugWorld.StaticGeometry.Scale);
            //var sPos = WorldToScreen(pos);

            var topLeft = WorldToScreen(new Vector2(0, h) * debugWorld.StaticGeometry.Scale);
            var bottomRight = WorldToScreen(new Vector2(w, 0) * debugWorld.StaticGeometry.Scale);

            var destRect = new Rectangle(topLeft.ToPoint(), (bottomRight - topLeft).ToPoint());

            VectorFieldEffect.Techniques[0].Passes[0].Apply();
            VectorFieldEffect.Parameters["VectorField"].SetValue(WaterTexture);
            VectorFieldEffect.Parameters["ArrowSymbol"].SetValue(Game.Content.Load<Texture2D>("Textures/Arrow"));
            VectorFieldEffect.Parameters["GridSize"].SetValue(new Vector2(w, h));
            

            GraphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None
            };
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);

        }
    }
}
