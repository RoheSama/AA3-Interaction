using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotifyRegion : MonoBehaviour
{

    Transform _region;

    public Transform Region { get { return _region; } }

    void Start()
    {

    }
 

    void Update()
    {
    


    }




    private void OnCollisionExit(Collision collision)
    {
        _region = null;
    }


    private void OnCollisionEnter(Collision collision)
    {
        _region = collision.collider.transform;
        _region = null;
    }




}

