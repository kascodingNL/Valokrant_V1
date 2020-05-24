using Jitter.Collision.Shapes;
using SharpDX;
using System;
using System.Collections.Generic;

namespace Valokrant.V1
{
    public class GameObject
    {
        public List<GameComponents> components = new List<GameComponents>();

        public GameObject()
        {
            components.Add(new Transform(new Vector3(0, .5f, .5f), new Quaternion(0, 0, 0, 0)));
        }

        public bool HasComponent(GameComponents component)
        {
            return components.Contains(component);
        }

        public GameComponents GetComponent(GameComponents type)
        {
            GameComponents toReturn = null;

            foreach(var comp in components)
            {
                if(comp.GetType() == type.GetType())
                {
                    toReturn = comp;
                    break;
                }
            }

            return toReturn;
        }
    }

    public class GameComponents
    {

    }

    public class MeshRenderer : GameComponents
    {
        public VertexPositionColor[] vertices { get; private set; }

        public MeshRenderer(VertexPositionColor[] verts)
        {
            vertices = verts;
        }
    }

    public class Transform : GameComponents
    {
        public Vector3 position { get; set; }
        public Quaternion quaternion { get; set; }

        /// <summary>
        /// Initializes an Transform
        /// </summary>
        /// <param name="position">Object world position</param>
        /// <param name="rotation">Object world rotation</param>
        public Transform(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.quaternion = rotation;
        }
    }

    public class Rigidbody : GameComponents
    {
        public Jitter.Dynamics.RigidBody jitterBody;

        public Rigidbody(Shape shape, Game gameScene)
        {
            Jitter.Dynamics.RigidBody body = new Jitter.Dynamics.RigidBody(shape);
            jitterBody = body;

            gameScene.PhysicsScene.PhysicsWorld.AddBody(body);
        }
    }
}
