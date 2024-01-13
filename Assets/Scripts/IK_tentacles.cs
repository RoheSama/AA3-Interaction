using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OctopusController;



public class IK_tentacles : MonoBehaviour
{

    [SerializeField]
    Transform[] _tentacles = new Transform[4];

    [SerializeField]
    Transform[] _randomTargets;


    MyOctopusController _myController = new MyOctopusController();
    


    [Header("Exercise 3")]
    [SerializeField, Range(0, 360)]
    float _twistMin ;

    [SerializeField, Range(0, 360)]
    float _twistMax;

    [SerializeField, Range(0, 360)]
    float _swingMin;

    [SerializeField, Range(0, 360)]
    float _swingMax;

    [SerializeField]
    bool _updateTwistSwingLimits = false;




    [SerializeField]
    float TwistMin{set{ _myController.TwistMin = value; }}



    #region public methods


    public void NotifyTarget(Transform target, Transform region)
    {
        _myController.NotifyTarget(target, region);

    }

    public void NotifyShoot()
    {
        _myController.NotifyShoot();
    }


    #endregion


    // Start is called before the first frame update
    void Start()
    {
        
        _myController.TestLogging(gameObject.name);
        _myController.Init(_tentacles, _randomTargets);

        _myController.TwistMax = _twistMax;
        _myController.TwistMin = _twistMin;
        _myController.SwingMax = _swingMax;
        _myController.SwingMin = _swingMin;

    }



    // Update is called once per frame
    void Update()
    {
        _myController.UpdateTentacles();

        if (_updateTwistSwingLimits) {
            _myController.TwistMax = _twistMax;
            _myController.TwistMin = _twistMin;
            _myController.SwingMax = _swingMax;
            _myController.SwingMin = _swingMin;
            _updateTwistSwingLimits = false;
        }

    }
}
