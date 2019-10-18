using System;
using System.Threading;

namespace Utilities 
{
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
