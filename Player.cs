using System;
using System.Collections.Generic;
using Utilities;
using Script;

// 
// A dummy class for immitating Player interactions with a server.
//
public class Player
{
    public static bool Handler (Event type, Dictionary<string, string> props)
    {
        MoveScript ms = new MoveScript(); 
        ms.Update(props);
        Console.WriteLine(ms.location.ToString());
        return true;
    }

    public static int Main (String[] args)
    {
        Client client = new Client("tcp://45.55.192.66");
        client.Connect();
        client.Listen(Handler);
        return 0;
    }
}
