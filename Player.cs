using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NNanomsg.Protocols;

public class Player
{
    protected static String url = "tcp://0.0.0.0";
    protected static String id = "Leroy";
    protected static bool running = true;

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

