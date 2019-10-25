using System;
using UnityEngine;
using Utilities;
using Script;
using System.Collections.Generic;
using NNanomsg;
using NNanomsg.Protocols;
using System.Threading;
using System.Text;
using System.Linq;

public class GameSystem : MonoBehaviour
{
    private Client client;

    public static bool Handler(Script.Event type, Dictionary<string, string> props)
    {
        // Id = 0, or the only Script in the handler currently.
        ScriptHandler.Update(0, props);
        return true;
    }

    void Awake ()
    {
        client = new Client("tcp://45.55.192.66");
    }

    void OnDestroy ()
    {
        client.Disconnect();
    }

    // Start is called before the first frame update
    void Start ()
    {
        client.Connect(Handler);
    }

    // Update is called once per frame
    void Update ()
    {
        ScriptHandler.Step(Time.deltaTime);
    }
}
