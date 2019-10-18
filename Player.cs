using System;
using Utilities;

public class Player
{
    public static int Main (String[] args)
    {
        Client client = new Client();
        client.Connect();
        Console.WriteLine(client.NextEvent());
        return 0;
    }
}

