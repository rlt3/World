using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using NNanomsg.Protocols;
using Utilities;
using Script;

public class World
{
    protected String url;
    protected bool running;

    protected Thread authThread;
    protected Thread worldThread;

    protected Dictionary<String, int> registered;

    protected Dictionary<String, int> activeIds;
    protected Mutex activeLock;

    protected Channel eventChannel;

    public World ()
    {
        this.url = "tcp://0.0.0.0";
        this.running = true;
        this.eventChannel = new Channel();
        this.registered = new Dictionary<String, int>();
        this.activeIds = new Dictionary<String, int>();
        this.activeLock = new Mutex(false);
    }

    public void AddEvent (string evnt)
    {
        eventChannel.Insert(evnt);
    }

    /*
     * The World Server is simply a Publish server which loops over a queue of
     * events and a list of connections and sends each connection those events.
     *
     * Events from the world server will be sent in the form of:
     *      "<id> <event> <arg0> [... [<argN>]]
     */
    public void WorldThreadProc ()
    {
        using (var sock = new PublishSocket())
        {
            /*
             * World Server does nothing except output the received events from
             * the channel. The events are simply strings. Thus this means
             * there needs to be another thread crunching on the events and
             * connected ids. Thus, something else generates the events sent on
             * the world server.
             */
            sock.Bind(url + ":8888");
            while (running) {
                sock.Send(Encoding.UTF8.GetBytes(eventChannel.Receive()));

//                activeLock.WaitOne();
//                foreach (var entry in activeIds) {
//                    String data = entry.Key + " sync " + entry.Value;
//                    sock.Send(Encoding.UTF8.GetBytes(data));
//                }
//                /*
//                 * For testing out that we will get 'new connection' events
//                 * using this socket setup. Client's initial message should
//                 * always be '0' here and not '4' or '5' or whatever.
//                 */
//                foreach (var key in activeIds.Keys.ToList())
//                    activeIds[key] += 1;
//                activeLock.ReleaseMutex();
            }
        }
    }

    /*
     * The Authentication server is a request-reply server which handles
     * authentication for the World server. Right now registration is simply:
     *
     * 1) Generate a random hash as an id
     * 3) Register this id with the authentication server by sending:
     *      "REG <id>"
     * 4) Server will reply "<id>-ACK" to acknowledge the request. If the
     * request is denied "<id>-DENIED" will be sent.
     * 5) If acknowledged can now connect to World server with the hash as
     * an id and subscribe to that id.
     */
    public void AuthThreadProc ()
    {
        using (var s = new ReplySocket())
        {
            s.Bind(url + ":8889");
            while (running) {
                byte[] bytes = s.Receive();
                if (bytes == null)
                    continue;

                String reply = "Bad Request.";
                String request = Encoding.ASCII.GetString(bytes);
                String[] e = request.Split(' ');

                if (e[0] == "REG") {
                    Console.WriteLine("Got registration request: " + request);
                    registered[e[1]] = 0;
                    reply = e[1] + "-ACK";
                }
                else if (e[0] == "ACCESS") {
                    /*
                     * Check that id has registered.
                     * TODO:
                     *  When the new client has connected we can construct new
                     *  events to send to the client through the World server.
                     *  These events would be the 'newConnection' events such
                     *  as initial placements of current NPCs.
                     */
                    if (registered.ContainsKey(e[1])) {
                        activeLock.WaitOne();
                        activeIds[e[1]] = 0;
                        activeLock.ReleaseMutex();
                        reply = e[1] + "-ACK";
                        /*
                         * Need a collection of Entities on the server side.
                         * Need generic entity on Client side which to
                         * instantiate when it receives this event.
                         */
                        eventChannel.Insert(e[1] + " new");
                    } else {
                        reply = e[1] + "-DENIED";
                    }
                }
                else if (e[0] == "END") {
                    Console.WriteLine("Ending Connection: " + request);
                    registered.Remove(e[1]);
                    reply = e[1] + "-ACK";
                }

                s.Send(Encoding.UTF8.GetBytes(reply));
            }
        }
    }

    public void Begin ()
    {
        authThread = new Thread(() => AuthThreadProc());
        worldThread = new Thread(() => WorldThreadProc());

        Console.WriteLine("Start");
        authThread.Start();
        worldThread.Start();
    }

    public void End ()
    {
        Console.WriteLine("End");
        running = false;
        authThread.Join();
        worldThread.Join();
        Console.WriteLine("Done");
    }
}

public static class Program
{
    public static int Main (String[] args)
    {
        //World world = new World();
        //world.Begin();
        //Console.ReadLine();
        //world.End();

        //Vec3 a = new Vec3(2, 2, 2);
        //Vec3 b = new Vec3(3, 3, 3);
        //Vec3 c = a + b;
        //Console.WriteLine("({0},{1},{2})", c.x, c.y, c.z);


        //MoveScript a = new MoveScript(new Vec3(5,5,5), new Vec3(0,0,0), 5);
        //MoveScript b = new MoveScript(new Vec3(9,9,9), new Vec3(0,0,0), 5);
        //ScriptHandler.Register(a);
        //ScriptHandler.Register(b);

        MoveScript a = new MoveScript();
        ScriptHandler.Register(a);
        while (true) {
            ScriptHandler.Step(1f/60f);
        }

        return 0;
    }
}
