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

    protected Mutex activeLock;

    protected Channel eventChannel;

    public World ()
    {
        this.url = "tcp://0.0.0.0";
        this.running = true;
        this.eventChannel = new Channel();
        this.registered = new Dictionary<String, int>();
        this.activeLock = new Mutex(false);
        ScriptHandler.Register(new MoveScript());
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
                string e = eventChannel.Receive();
                Console.WriteLine("Sending: `" + e + "`");
                sock.Send(Encoding.UTF8.GetBytes(e));
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
                        reply = e[1] + "-ACK";
                        /*
                         * Need a collection of Entities on the server side.
                         * Need generic entity on Client side which to
                         * instantiate when it receives this event.
                         */
                        var props = ScriptHandler.Properties();
                        foreach (var pair in props)
                            eventChannel.Insert(e[1] + " new " + pair.Value);
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
        World world = new World();
        world.Begin();

        while (true) {
            Thread.Sleep(1000 / 60);
            ScriptHandler.Step(1f / 60f);
        }

        world.End();
        return 0;
    }
}
