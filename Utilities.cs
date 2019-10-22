using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using NNanomsg.Protocols;
using System.Diagnostics;
using Script;

using EventHandler = System.Func<Script.Event, System.Collections.Generic.Dictionary<string, string>, bool>;

namespace Utilities 
{
    public class Client
    {
        protected static bool running = true;

        protected String url;
        protected bool connected;
        protected RequestSocket interaction;
        protected SubscribeSocket world;
        protected String id;
        protected String req_register;
        protected String req_access;
        protected String req_end;
        protected String acknowledged;
        protected String denied;
        protected Thread listenThread;

		public static string RandomString (int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}

        public Client (String url)
        {
            this.url = url;
            Setup(RandomString(10));
        }

        ~Client ()
        {
            if (connected)
                Disconnect();
        }

        public void Disconnect ()
        {
            Request(req_end);
            //world.Shutdown();
            //interaction.Shutdown();
            world.Dispose();
            interaction.Dispose();
            connected = false;
            listenThread.Join();
        }

        public void Setup (String id)
        {
            this.id = id;
            this.interaction = new RequestSocket();
            this.world = new SubscribeSocket();
            this.req_register = "REG " + id;
            this.req_access = "ACCESS " + id;
            this.req_end = "END " + id;
            this.acknowledged = id + "-ACK";
            this.denied = id + "-DENIED";
            this.connected = false;
            this.listenThread = null;
        }

        protected void Request (String data)
        {
            interaction.Send(Encoding.UTF8.GetBytes(data));
            String reply = Response();
            if (reply != acknowledged)
                throw new Exception("Cannot register: " + reply);
        }

        protected string Response ()
        {
            return Encoding.ASCII.GetString(interaction.Receive());
        }

        protected string NextEvent ()
        {
            return Encoding.ASCII.GetString(world.Receive());
        }

        public void Connect ()
        {
            // Request our id from the server.
            interaction.Connect(url + ":8889");
            Request(req_register);

            // Connection must be made before requesting access or otherwise
            // we may miss some key events that have already been sent by the
            // time we connected.
            Console.WriteLine("Accessing...");
            world.Subscribe(id);
            world.Connect(url + ":8888");

            // Request access from the world server.
            Request(req_access);

            Console.WriteLine("Connected with id " + id);
            this.connected = true;
        }

        // Start Listening in the background for events from the World server.
        // Every unique event will be passed to the EventHandler to handle
        // what to do on an event-by-event basis.
        public void Listen (EventHandler handler)
        {
            if (!connected)
                throw new Exception("Cannot listen to an unopen connection!");
            this.listenThread = new Thread(() => ListenProc(handler));
            this.listenThread.Start();
        }

        private void ListenProc (EventHandler handler)
        {
            while (connected) {
                string data = NextEvent();

                Console.WriteLine("Received Event: " + data);

                string[] e = data.Split(" ");
                Event event_type = Event.Default;
                Dictionary<string,string> event_data = null;

                if (e[0] == id && e[1] == "new") {
                    //  This message has the format:
                    //      <hash> new
                    //         id <id>
                    //         <key0> <value0>
                    //         <key1> <value1>
                    //         ...
                    //  (Not including newlines -- formatted for readability)
                    //  This is building that dictionary to pass to the script
                    //  so it can update from the network.
                    //
                    Debug.Assert(e.Length >= 4);
                    event_data = new Dictionary<string,string>();
                    for (int i = 2; i < e.Length; i += 2)
                        event_data[e[i]] = e[i+1];
                }

                handler(event_type, event_data);
            }
        }
    }

    // A simple container class that has a mutex around its payload
    public class SafeContainer
    {
        protected String data;
        protected Mutex mutex;
        protected ManualResetEvent dataPlaced;

        public SafeContainer()
        {
            this.data = null;
            this.mutex = new Mutex(false);
            this.dataPlaced = new ManualResetEvent(false);
        }

        public void Put (String str)
        {
            mutex.WaitOne();
            this.data = String.Copy(str);
            dataPlaced.Set();
            mutex.ReleaseMutex();
        }

        public String Data ()
        {
            string str;
            mutex.WaitOne();
            // If the data is null at this point then the Receiving
            // threads have worked faster than the Inserting threads.
            // We simply need to block until there has been data placed
            // for us to read.
            if (this.data == null)
            {
                mutex.ReleaseMutex();
                dataPlaced.WaitOne();
                dataPlaced.Reset();
                mutex.WaitOne();
            }
            str = String.Copy(this.data);
            this.data = null;
            mutex.ReleaseMutex();
            return str;
        }
    }

    // A channel is a thread-safe queue of messages. It tries to be as
    // frictionless as possible by having two locks on the channel -- one for
    // inserting messages and another for receiving messages -- and a third
    // lock on the messages themselves.  This allows for locks that simply
    // increment a counter and for messages to be received on any number of
    // threads at a time.
    public class Channel
    {
        protected int in_index;
        protected int out_index;
        protected int buffer_size;
        protected SafeContainer[] messages;
        protected Mutex input_lock;
        protected Mutex output_lock;

        public Channel()
        {
            this.input_lock = new Mutex(false);
            this.output_lock = new Mutex(false);
            this.in_index = 0;
            this.out_index = 0;
            this.buffer_size = 4096;
            this.messages = new SafeContainer[buffer_size];
            for (int i = 0; i < this.messages.Length; i++)
                messages[i] = new SafeContainer();
        }

        public void Insert (String msg)
        {
            int index;

            // Because all of the SafeContainer objects are initialized
            // synchronously above then all the locks should be initialized and
            // in place. This means we can bump the index and start putting the
            // message in place
            input_lock.WaitOne();
            index = this.in_index;
            this.in_index = (this.in_index + 1) % this.buffer_size;
            input_lock.ReleaseMutex();

            messages[index].Put(msg);
        }

        public string Receive ()
        {
            int index;

            // Simply incrementing the index will be enough to ensure that the
            // next access will go to a different SafeContainer. There's a lock
            // on the SafeContainer anyway to enforce this, but the least
            // amount of friction on locks is preferred
            output_lock.WaitOne();
            index = out_index;
            this.out_index = (this.out_index + 1) % this.buffer_size;
            output_lock.ReleaseMutex();

            return String.Copy(messages[index].Data());
        }
    }
}
