using Jitter;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Valokrant.V1.PhysX;

namespace Valokrant.V1.PhysX
{
    public class PhysicsScene
    {
        public CollisionSystem Collision { get; private set; }
        public World PhysicsWorld { get; private set; }

        public bool Multithreaded { get; private set; }

        public PhysicsScene(bool multithreaded)
        {
            Collision = new CollisionSystemSAP();
            PhysicsWorld = new World(Collision);

            this.Multithreaded = multithreaded;

            var ground = new Jitter.Dynamics.RigidBody(new BoxShape(100, 1, 100));

            ground.IsStatic = true;
            ground.Position = new Jitter.LinearMath.JVector(0, -1, 0);
            PhysicsWorld.AddBody(ground);
        }

        public PhysicsScene()
        {
            Collision = new CollisionSystemSAP();
            PhysicsWorld = new World(Collision);

            Multithreaded = true;
        }

        public void Simulate(float step)
        {
            PhysicsWorld.Step(step, Multithreaded);
        }
    }
}
