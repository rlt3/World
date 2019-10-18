using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NNanomsg.Protocols;

namespace Utilities 
{
    public class Client
    {
        protected static String url = "tcp://0.0.0.0";
        protected static String id = "Leroy";
        protected static bool running = true;

        protected RequestSocket interaction;
        protected SubscribeSocket world;

        protected String req_register;
        protected String req_access;
        protected String acknowledged;
        protected String denied;

        public Client ()
        {
            req_register = "REG " + id;
            req_access = "ACCESS " + id;
            acknowledged = id + "-ACK";
            //denied = id + "-DENIED";

            interaction = new RequestSocket();
            world = new SubscribeSocket();
        }

        public void Request (String data)
        {
            interaction.Send(Encoding.UTF8.GetBytes(data));
            String reply = Response();
            if (reply != acknowledged)
                throw new Exception("Cannot register: " + reply);
        }

        public String Response ()
        {
            return Encoding.ASCII.GetString(interaction.Receive());
        }

        public String NextEvent ()
        {
            return Encoding.ASCII.GetString(world.Receive());
        }

        public void Connect ()
        {
            /*
             * Request our id from the server.
             */
            interaction.Connect(url + ":8889");
            Request(req_register);

            /* 
             * Connection must be made before requesting access or otherwise
             * we may miss some key events that have already been sent by the
             * time we connected.
             */
            world.Subscribe(id);
            world.Connect(url + ":8888");

            /*
             * Request access from the world server.
             */
            Request(req_access);
        }

    public static int Main (String[] args)
    {
        using (var s = new RequestSocket())
        {
            String register = "REG " + id;
            s.Connect(url + ":8889");
            s.Send(Encoding.UTF8.GetBytes(register));
            String reply = Encoding.ASCII.GetString(s.Receive());
            if (reply != id + "-ACK")
                throw new Exception("Cannot auth: " + reply);
        }

        using (var s = new SubscribeSocket())
        {
            s.Subscribe(id);
            s.Connect(url + ":8888");
            while (running) {
                Console.WriteLine(Encoding.ASCII.GetString(s.Receive()));
            }
        }

        return 0;
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
