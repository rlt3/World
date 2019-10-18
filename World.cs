using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NNanomsg.Protocols;
using Utilities;

public class World
{
    protected String url;
    protected bool running;

    protected Thread authThread;
    protected Thread worldThread;

    protected Dictionary<String, int> registered;

    protected Dictionary<String, int> activeIds;
    protected Mutex activeLock;

    public World ()
    {
        this.url = "tcp://0.0.0.0";
        this.running = true;
        this.registered = new Dictionary<String, int>();
        this.activeIds = new Dictionary<String, int>();
        this.activeLock = new Mutex(false);
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
        using (var s = new PublishSocket())
        {
            s.Bind(url + ":8888");
            while (running) {
                activeLock.WaitOne();
                foreach (var entry in activeIds) {
                    String data = entry.Key + " sync " + entry.Value;
                    s.Send(Encoding.UTF8.GetBytes(data));
                }
                /*
                 * For testing out that we will get 'new connection' events
                 * using this socket setup. Client's initial message should
                 * always be '0' here and not '4' or '5' or whatever.
                 */
                foreach (var key in activeIds.Keys.ToList())
                    activeIds[key] += 1;
                activeLock.ReleaseMutex();
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
                     */
                    if (registered.ContainsKey(e[1])) {
                        activeLock.WaitOne();
                        activeIds[e[1]] = 0;
                        activeLock.ReleaseMutex();
                        reply = e[1] + "-ACK";
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
        Console.ReadLine();
        world.End();
        return 0;
    }
}
