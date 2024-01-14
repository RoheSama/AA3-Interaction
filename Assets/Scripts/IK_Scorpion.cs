using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OctopusController;
using System;

public class IK_Scorpion : MonoBehaviour
{
    MyScorpionController _myController= new MyScorpionController();

    public IK_tentacles _myOctopus;

    [Header("Body")]
    float animTime;
    public float animDuration = 5;
    bool animPlaying = false;
    public Transform Body;
    public Transform StartPos;
    public Transform EndPos;

    private Vector3 moveOffset = Vector3.zero;

    private Quaternion lookRotation;

    private Vector3 lastBodyPosition = Vector3.zero;
    private Vector3 currentForward = Vector3.zero;

    [Header("Tail")]
    public Transform tailTarget;
    public Transform tail;
    private Transform[] tailBones;
    private Transform tailEE;

    private Vector3 tailForwardEE;

    private Quaternion[] tailRotations;

    private float tailTargetBallLength;
    private bool _targetRightSide = false;

    private Vector3 ballHitToCenterDir;
    public Vector3 BallHitToCenterDir => ballHitToCenterDir;


    [Header("Legs")]
    public Transform[] legs;
    public Transform[] legTargets;
    public Transform[] futureLegBases;
    public Transform futureLegBasesHolder;

    [Header("Animation")]
    [SerializeField] private Transform mainBody;
    private Vector3 bodyToLegsOffset;

    [SerializeField, Min(0f)] private float leftRight = 2f;
    [SerializeField, Min(0)] private int numLeftRight = 2;
    private float leftRightSines;

    bool goalAchieved = false;

    readonly float futureLegBaseOrigin = 2f;
    readonly float futureLegBaseDistance = 5f;
    readonly Vector3 futureLegBaseDirection = Vector3.down;

    private bool reset = false;

    [Header("Ball")]
    [SerializeField] private MovingBall movingBall;

    // Start is called before the first frame update
    void Start()
    {
        _myController.InitLegs(legs,futureLegBases,legTargets);
        _myController.InitTail(tail);

        ResetTail();

        SetTailTargetPosition(Vector3.forward);


        bodyToLegsOffset = (mainBody.position.y - futureLegBases[0].position.y) * Vector3.up;

        lastBodyPosition = mainBody.position;

        leftRightSines = (float)numLeftRight / 2f;

        SetStartTailRotations();
    }

    private void SetStartTailRotations()
    {
        throw new NotImplementedException();
    }

    private void SetTailTargetPosition(Vector3 forward)
    {
        throw new NotImplementedException();
    }

    private void ResetTail()
    {
        throw new NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        if(animPlaying)
            animTime += Time.deltaTime;

        NotifyTailTarget();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NotifyStartWalk();
            animTime = 0;
            animPlaying = true;
        }

        if (animTime < animDuration)
        {
            Body.position = Vector3.Lerp(StartPos.position, EndPos.position, animTime / animDuration);
        }
        else if (animTime >= animDuration && animPlaying)
        {
            Body.position = EndPos.position;
            animPlaying = false;
        }

        _myController.UpdateIK();
    }
    
    //Function to send the tail target transform to the dll
    public void NotifyTailTarget()
    {
        _myController.NotifyTailTarget(tailTarget);
    }

    //Trigger Function to start the walk animation
    public void NotifyStartWalk()
    {

        _myController.NotifyStartWalk();
    }
}
