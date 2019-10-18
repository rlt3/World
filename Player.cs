using System;
using Utilities;

public class Player
{
    public static int Main (String[] args)
    {
        Client client;
        
        if (args.Length > 0)
            client = new Client(args[0]);
        else
            client = new Client();

        client.Connect();
        Console.WriteLine(client.NextEvent());
        return 0;
    }
}

