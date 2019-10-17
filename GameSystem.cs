using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NNanomsg.Protocols;

// A simple container class that has a mutex around its payload
public class Payload
{
    protected String data;
    protected Mutex mutex;
    protected ManualResetEvent dataPlaced;

    public Payload()
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

// A channel is a thread-safe queue of messages. It tries to be as frictionless
// as possible by having two locks on the channel -- one for inserting messages
// and another for receiving messages -- and a third lock on the messages themselves.
// This allows for locks that simply increment a counter and for messages to be
// received on any number of threads at a time.
public class Channel
{
    protected int in_index;
    protected int out_index;
    protected int buffer_size;
    protected Payload[] messages;
    protected Mutex input_lock;
    protected Mutex output_lock;

    public Channel()
    {
        this.input_lock = new Mutex(false);
        this.output_lock = new Mutex(false);
        this.in_index = 0;
        this.out_index = 0;
        this.buffer_size = 4096;
        this.messages = new Payload[buffer_size];
        for (int i = 0; i < this.messages.Length; i++)
            messages[i] = new Payload();
    }

    public void Insert (String msg)
    {
        int index;

        // Because all of the Payload objects are initialized synchronously
        // above then all the locks should be initialized and in place. This
        // means we can bump the index and start putting the message in place
        input_lock.WaitOne();
        index = this.in_index;
        this.in_index = (this.in_index + 1) % this.buffer_size;
        input_lock.ReleaseMutex();

        messages[index].Put(msg);
    }

    public string Receive ()
    {
        int index;

        // Simply incrementing the index will be enough to ensure that
        // the next access will go to a different Payload. There's a lock
        // on the Payload anyway to enforce this, but the least amount of
        // friction on locks is preferred
        output_lock.WaitOne();
        index = out_index;
        this.out_index = (this.out_index + 1) % this.buffer_size;
        output_lock.ReleaseMutex();

        return String.Copy(messages[index].Data());
    }
}

public class Connection
{
    protected Thread thread;
    protected bool running = false;
    protected string url = "";
    protected Channel channel;

    // TODO:
    //   - Handle server hangups
    //   - Handle connection loss
    //   - Need checks on Receive in so main thread doesn't
    //   lock but entities may not update (and certainly no
    //   invalid type issue where null is being passed as
    //   an argument).

    public Connection (string url)
    {
        this.channel = new Channel();
        this.url = url;
        running = true;
        thread = new Thread(() => ThreadProc());
        thread.Start();
    }

    public void Close()
    {
        this.running = false;
        Debug.Log("Close...");
        thread.Join();
    }

    public string Receive()
    {
        return channel.Receive();
    }

    private void ThreadProc ()
    {
        Debug.Log("Thread begin.");

        using (var s = new SubscribeSocket())
        {
            Debug.Log("Connecting to " + url + "...");
            //Needs to match the first portion of the message being received.
            s.Subscribe("");
            s.Connect(url);
            Debug.Log("... connected!");
            while (running)
            {
                byte[] b = s.Receive();
                if (b != null)
                    channel.Insert(Encoding.ASCII.GetString(b));
                else
                    // Don't send bad data down the channel
                    Debug.Log("Thread GOT ZERO");
            }
        }

        running = false;
        Debug.Log("Thread end.");
    }
}

public class GameSystem : MonoBehaviour
{
    private Connection con = null;
    private GameObject obj;
    protected float offset = 0;

    void Awake()
    {
        Debug.Log("Awake");
        obj = GameObject.Find("Chal_Rig");
        if (obj == null)
            Debug.Log("obj was null");
        con = new Connection("tcp://45.55.192.66:8888");
    }

    void OnDestroy()
    {
        con.Close();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //string recieved = "-2,0,-11.918145127594";
        string received = con.Receive();
        Debug.Log("Received: " + received);
        string[] position = received.Split(',');
        float x = float.Parse(position[0]);
        float y = float.Parse(position[1]);
        float z = float.Parse(position[2]);
        //z = z + offset;
        //offset = offset + 0.01f;
        //Debug.Log("(x = " + x + ", y = " + y + ", z = " + z + ")");
        //obj.transform.position.Set(x, y, z);
        obj.transform.Translate(x, y, z);
        Debug.Log("(x = " + x + ", y = " + y + ", z = " + z + ") => (x = " + obj.transform.position.x + ", y = " + obj.transform.position.y + ", z = " + obj.transform.position.z + ")");
    }
}
