﻿using Ballz.GameSession.World;
using Ballz.Messages;
using Ballz.Utils;
using Microsoft.Xna.Framework;
using FarseerPhysics;
using FarseerPhysics.Dynamics;


using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ballz.GameSession.Physics
{
    /// <summary>
    /// Physics control is called by BallzGame update to simulate discrete GamePhysics.
    /// </summary>
    /// <remarks>
    /// In each update step, this component syncs the state of all entities into the physics world,
    /// runs a simulation step, and syncs positions and velocities back to the entities.
    /// </remarks>
    public class PhysicsControl : GameComponent
    {
        new Ballz Game;

        /// <summary>
        /// The main acces point to the physics engine.
        /// </summary>
        FarseerPhysics.Dynamics.World PhysicsWorld;

        public PhysicsControl(Ballz game) : base(game)
        {
            Game = game;
        }
        
        Dictionary<Body, int> EntityIdByPhysicsBody = new Dictionary<Body, int>();

        /// <summary>
        /// List of the physics bodies that represent the terrain
        /// </summary>
        List<Body> TerrainBodies = new List<Body>();

        /// <summary>
        /// Terrain revision of the physics terrain shapes in <see cref="TerrainBodies"/>
        /// </summary>
        int TerrainRevision = -1;

        public override void Initialize()
        {
            PhysicsWorld = new FarseerPhysics.Dynamics.World(new Vector2(0f, -9.82f));

            // Add a ground plate for physics testing
            var ground = new Body(PhysicsWorld);
            ground.BodyType = BodyType.Static;
            ground.CreateFixture(new EdgeShape(new Vector2(-10, 0), new Vector2(20, 0)));
        }

        /// <summary>
        /// Removes the old terrain physics bodies and replaces them with new ones,
        /// generated from the given terrain.
        /// </summary>
        public void UpdateTerrainBody(Terrain terrain)
        {
            foreach (var body in TerrainBodies)
            {
                var fixtures = body.FixtureList.ToArray();
                foreach (var fixture in fixtures)
                {
                    fixture.Dispose();
                }
                body.Dispose();
            }

            TerrainBodies.Clear();

            // Update the terrain explicitly
            terrain.update();
            var outlines = terrain.getOutline();


            foreach (var outline in outlines)
            {
                var vertices = new Vector2[outline.Count];

                for (int i = 0; i < outline.Count; i++)
                {
                    vertices[i] = outline[i] * terrain.Scale;
                }

                var shape = new ChainShape(new Vertices(vertices));

                var body = new Body(PhysicsWorld);
                body.BodyType = BodyType.Static;
                body.Friction = 1.0f;
                body.CreateFixture(shape);

                TerrainBodies.Add(body);
            }
        }

        /// <summary>
        /// Removes the physics body of the given entity from the physics world.
        /// </summary>
        public void RemoveEntity(Entity e, World.World worldState)
        {
            e.Dispose();
            worldState.Entities.Remove(e);
            if(e.PhysicsBody != null)
            {
                EntityIdByPhysicsBody.Remove(e.PhysicsBody);
                var fixtures = e.PhysicsBody.FixtureList.ToArray();
                foreach (var fixture in fixtures)
                {
                    fixture.Dispose();
                }
                e.PhysicsBody.Dispose();
                e.PhysicsBody = null;
            }
        }

        /// <summary>
        /// Syncs the given world state into the physics world.
        /// Creates new physics bodies for entities that don't have one yet.
        /// </summary>
        public void PreparePhysicsEngine(World.World worldState)
        {
            if (worldState.StaticGeometry.Revision != TerrainRevision)
                UpdateTerrainBody(worldState.StaticGeometry);

            TerrainRevision = worldState.StaticGeometry.Revision;

            // Add Bodies for new entities
            var entities = worldState.Entities.ToArray();
            foreach (var e in entities)
            {
                if(e.Disposed || e.Position.LengthSquared() > 100 * 100)
                {
                    RemoveEntity(e, worldState);
                    continue;
                }

                Body body = e.PhysicsBody;
                if (body == null)
                {
                    body = new Body(PhysicsWorld);
                    body.BodyType = e.IsStatic ? BodyType.Static : BodyType.Dynamic;
                    body.SleepingAllowed = true;
                    body.Awake = true;
                    body.CreateFixture(new CircleShape(e.Radius, 1.0f));
                    e.PhysicsBody = body;
                    EntityIdByPhysicsBody[body] = e.ID;
                    if (e is Ball)
                    {
                        body.Friction = 2.0f;
                        body.Restitution = 0.1f;
                        body.FixedRotation = true;
                        body.Mass = 10f;
                    }

                    if (e is Shot)
                    {
                        var shot = e as Shot;
                        body.OnCollision += (a, b, contact) =>
                        {
                            if (shot.TargetId > 0)
                                return true;

                            Vector2 normal;
                            FixedArray2<Vector2> points;
                            contact.GetWorldManifold(out normal, out points);
                            shot.TargetPosition = points[0];

                            Fixture targetFixture;
                            if (contact.FixtureA.Body == shot.PhysicsBody)
                                targetFixture = contact.FixtureB;
                            else
                                targetFixture = contact.FixtureA;

                            if (EntityIdByPhysicsBody.ContainsKey(targetFixture.Body))
                                shot.TargetId = EntityIdByPhysicsBody[targetFixture.Body];

                            ApplyImpact(shot, targetFixture, shot.TargetPosition, worldState);

                            shot.Dispose();
                            return true;
                        };
                    }
                }

                if(body.Position != e.Position || body.Rotation != e.Rotation)
                    body.SetTransform(e.Position, e.Rotation);
                if (body.LinearVelocity != e.Velocity)
                    body.LinearVelocity = e.Velocity;
            }
        }

        /// <summary>
        /// Runs a single simulation step in the physics engine and syncs the state of the physics bodies back to the given world.
        /// </summary>
        public void PhysicsStep(World.World worldState, float elapsedSeconds)
        {
            // Update the physics world
            PhysicsWorld.Step(elapsedSeconds);

            // Sync back the positions and velocities
            foreach (var e in worldState.Entities)
            {
                var body = e.PhysicsBody;

                if (e.PhysicsBody == null || e.Disposed)
                    continue;

                e.Position = body.Position;
                e.Velocity = body.LinearVelocity;

                if (e is Ball)
                {
                    const float dg90 = 2 * (float)Math.PI * 90f / 360f;
                    if (e.Velocity.LengthSquared() > 0.0001f)
                        e.Rotation = dg90 * (e.Velocity.X > 0 ? 1 : -1);
                    else
                        e.Rotation = dg90;
                }
            }
        }

        public override void Update(GameTime time)
        {
            using (new PerformanceReporter(Game))
            {
                if (Game.Match.State != Logic.SessionState.Running)
                    return;

                var worldState = Game.World;
                float elapsedSeconds = (float)time.ElapsedGameTime.TotalSeconds;

                PreparePhysicsEngine(worldState);
                PhysicsStep(worldState, elapsedSeconds);
                worldState.Water.Step(worldState, elapsedSeconds);

                // Update shots
                var shots = (from e in worldState.Entities
                             where e is Shot
                             select (e as Shot))
                             .ToArray();

                foreach (var shot in shots)
                {
                    if (shot.IsInstantShot)
                    {
                        Vector2 targetPos = Vector2.Zero;
                        Fixture targetFixture = null;
                        Func<Fixture, Vector2, Vector2, float, float> raycastCallback =
                            (Fixture fixture, Vector2 position, Vector2 normal, float fraction) =>
                            {
                                targetFixture = fixture;
                                targetPos = position;
                                return fraction;
                            };

                        PhysicsWorld.RayCast(raycastCallback, shot.Position, shot.Position + 100 * shot.Velocity);
                        ApplyImpact(shot, targetFixture, targetPos, worldState);
                        worldState.Entities.Remove(shot);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the impact of a shot projectile.
        /// </summary>
        public void ApplyImpact(Shot shot, Fixture targetFixture, Vector2 targetPos, World.World worldState)
        {
            // Did the shot hit anything?
            if (targetFixture != null)
            {
                // Terrain hit? Then add an explosion there.
                if (TerrainBodies.Contains(targetFixture.Body))
                {
                    worldState.StaticGeometry.SubtractCircle(targetPos.X, targetPos.Y, 0.04f * shot.Velocity.Length() * shot.ExplosionRadius);
                }
                // Otherwise, find the entity that belongs to the hit body
                else if (EntityIdByPhysicsBody.ContainsKey(targetFixture.Body))
                {
                    int entityId = EntityIdByPhysicsBody[targetFixture.Body];

                    var entity = worldState.EntityById(entityId);
                    var ball = entity as Ball;
                    if (ball != null)
                    {
                        ball.Health -= shot.HealthImpactAtDirectHit;
                        if (ball.Health < 0)
                            ball.Health = 0;

                        ball.PhysicsBody.ApplyLinearImpulse(10*shot.Velocity);
                    }
                }
            }
        }


        public void HandleMessage(object sender, Message message)
        {
            if (message.Kind == Message.MessageType.LogicMessage)
            {
                LogicMessage msg = (LogicMessage)message;

                if (msg.Kind == LogicMessage.MessageType.GameMessage)
                {
                    Enabled = !Enabled;
                }
            }
        }


    }
}