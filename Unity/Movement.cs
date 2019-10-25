using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Script;

public class Movement : MonoBehaviour
{
    private MoveScript script;

    void Awake ()
    {
        script = new MoveScript();
        ScriptHandler.Register(script);
    }

    // Start is called before the first frame update
    void Start ()
    {
    }

    // Update is called once per frame
    void Update ()
    {
        gameObject.transform.position = new Vector3(script.location.x, script.location.y, script.location.z);
    }
}
