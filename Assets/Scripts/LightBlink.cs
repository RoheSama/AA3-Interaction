using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBlink : MonoBehaviour
{
    [SerializeField]
    float _duration;

    Light _myLight;
    Color initColor;
    float _endtime;
    void Start()
    {
        _myLight = gameObject.GetComponent<Light>();
        initColor =  _myLight.color;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            _endtime = Time.time + _duration;
        }


        if(Time.time  <= _endtime)
        { 
            float t = (_endtime - Time.time)/_duration ;
            _myLight.color = Color.Lerp(initColor, new Color(1.5f,1.5f,1.5f), t);
        }
        else
        {
            _myLight.color = initColor;

        }

    }




}
