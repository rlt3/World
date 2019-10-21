using System;
using System.Collections.Generic;
using System.Threading;

namespace Script {
    public enum Event {
        NewEntity,
        Default
    }

    public class Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3 (float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vec3 operator+ (Vec3 lhs, Vec3 rhs)
        {
            return new Vec3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static Vec3 operator- (Vec3 lhs, Vec3 rhs)
        {
            return new Vec3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static Vec3 operator* (Vec3 lhs, float scalar)
        {
            return new Vec3(lhs.x * scalar, lhs.y * scalar, lhs.z * scalar);
        }

        public static Vec3 FromString (string data)
        {
            string[] s = data.Split(",");
            float x = float.Parse(s[0]);
            float y = float.Parse(s[1]);
            float z = float.Parse(s[2]);
            return new Vec3(x, y, z);
        }

        public override string ToString ()
        {
            return x + "," + y + "," + z;
        }

        public bool near (Vec3 other)
        {
            if (Math.Abs(x - other.x) > 0.1)
                return false;
            if (Math.Abs(y - other.y) > 0.1)
                return false;
            if (Math.Abs(z - other.z) > 0.1)
                return false;
            return true;
        }

        public float magnitude ()
        {
            return (float) Math.Sqrt(x * x + y * y + z * z);
        }

        public Vec3 normalize ()
        {
            var length = magnitude();
            return new Vec3(x / length, y / length, z / length);
        }
    }

    public class ScriptBase {
        /*
         * Can update this class to have an 'Update' function which accepts
         * a Dictionary<string, string> as an argument. This is simply a list
         * of key-value pairs where the key is the name of a property and the
         * value is that property's value serialized.
         */
        public ScriptBase ()
        {
        }

        public virtual void Update (Dictionary<string, string> props)
        {
        }

        public virtual void Step (float dt)
        {
        }
    }

    /*
     * The class that runs the virtual methods of the Scripts
     *
     * When a client connects, an initial series of messages are sent
     * which fill out the NPCS and any other key information. These NPCS
     * are sent with an id that corresponds to them inside the
     * ScriptHandler list. That is, id = 0, is simply index 0 into the
     * ScriptHandler's list.
     */
    public static class ScriptHandler {
        static List<ScriptBase> list = new List<ScriptBase>();
        static Mutex list_lock = new Mutex(false);

        public static void Register (ScriptBase s)
        {
            list_lock.WaitOne();
            list.Add(s);
            list_lock.ReleaseMutex();
        }

        public static void Update (int id, Dictionary<string, string> props)
        {
            list_lock.WaitOne();
            list[id].Update(props);
            list_lock.ReleaseMutex();
        }

        public static void Step (float dt)
        {
            list_lock.WaitOne();
            foreach (var script in list) {
                script.Step(dt);
            }
            list_lock.ReleaseMutex();
        }
    }

    public class MoveScript : ScriptBase
    {
        public Vec3 location;
        public Vec3 direction;
        public float speed;
        public int path_point;
        public List<Vec3> path;

        public MoveScript () : base()
        {
            this.speed = 1.0f;

            path = new List<Vec3>();
            path.Add(new Vec3(2,0,-12));
            path.Add(new Vec3(-2,0,-12));
            path.Add(new Vec3(-2,0,-9));
            path.Add(new Vec3(2,0,-9));

            this.path_point = 0;
            this.location = path[path_point];
            next_path_point();
        }

        private void next_path_point ()
        {
            if (this.path_point + 1 >= path.Count)
                path_point = 0;
            else
                path_point++;
            this.direction = (path[path_point] - this.location).normalize();
        }

        public override void Update (Dictionary<string, string> props)
        {
            foreach (var pair in props) {
                if (pair.Key == "location")
                    this.location = Vec3.FromString(pair.Value);
                else if (pair.Key == "path_point")
                    this.path_point = Int32.Parse(pair.Value);
            }
            next_path_point();
        }

        public override void Step (float dt)
        {
            var dir = this.direction * (this.speed * dt);
            this.location = this.location + dir;
            if (this.location.near(path[path_point]))
                next_path_point();
        }
    }
}
