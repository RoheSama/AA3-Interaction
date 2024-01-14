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
    private Transform[] tailBonesT;
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

    [Header("UI Controller")]
    [SerializeField] private UI_Controller ui;

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
        List<Quaternion> startTailRotations = new List<Quaternion>();
        List<Transform> tailBones = new List<Transform>();
        Transform currentTailBone = tail;

        while (currentTailBone.childCount > 0)
        {
            startTailRotations.Add(currentTailBone.rotation);
            tailBones.Add(currentTailBone);
            currentTailBone = currentTailBone.GetChild(1);
        }

        startTailRotations.Add(currentTailBone.rotation);
        tailBones.Add(currentTailBone);

        tailRotations = startTailRotations.ToArray();
        tailBonesT = tailBones.ToArray();

        tailEE = tailBonesT[tailBonesT.Length - 1];
    }

    
    private void SetTailTargetPosition(Vector3 offsetDirection)
    {
        movingBall.SetTailTargetLocalPosition(offsetDirection * tailTargetBallLength);
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

        UpdateInputs();

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

    private void UpdateInputs()
    {
        throw new NotImplementedException();
    }

    private void MoveScorpion()
    {
        float t = animTime / animDuration;
        float sint = Mathf.Clamp01(t * 1.2f) * 2f * Mathf.PI * leftRightSines;
        moveOffset.x = Mathf.Sin(sint) * leftRight;

        currentForward = mainBody.position - lastBodyPosition;

        if (currentForward.sqrMagnitude > 0.0001f)
        {
            currentForward = currentForward.normalized;
        }

        lastBodyPosition = mainBody.position;

        Body.position = Vector3.Lerp(StartPos.position, EndPos.position, t) + moveOffset;
    }

    private void UpdateLegsAndBody()
    {
        Vector3 bodyLegsAvgPos = Vector3.zero;

        Vector3 leftLegsAvgPos = Vector3.zero;
        Vector3 rightLegsAvgPos = Vector3.zero;

        for (int legI = 0; legI < futureLegBases.Length; ++legI)
        {
            Vector3 hitOrigin = futureLegBases[legI].position + (-futureLegBaseDirection * futureLegBaseOrigin);
            //Debug.DrawLine(hitOrigin, hitOrigin + (futureLegBaseDirection * futureLegBaseDistance), Color.magenta, Time.deltaTime);

            RaycastHit hit;
            if (Physics.Raycast(hitOrigin, futureLegBaseDirection, out hit, futureLegBaseDistance))
            {
                futureLegBases[legI].position = hit.point;
            }

            bodyLegsAvgPos += futureLegBases[legI].position;

            if (legI % 2 == 0)
                rightLegsAvgPos += futureLegBases[legI].position;
            else
                leftLegsAvgPos += futureLegBases[legI].position;
        }

        float numLegs = (float)futureLegBases.Length;
        bodyLegsAvgPos /= numLegs;
        mainBody.position = bodyLegsAvgPos + bodyToLegsOffset;

        float numLegsEachSide = numLegs / 2f;
        rightLegsAvgPos /= numLegsEachSide;
        leftLegsAvgPos /= numLegsEachSide;

        if (currentForward.sqrMagnitude > 0.0001f)
        {
            Vector3 newRightBodyAxis = (rightLegsAvgPos - leftLegsAvgPos).normalized;

            Vector3 newUpBodyAxis = Vector3.Cross(currentForward, newRightBodyAxis).normalized;
            Vector3 newForwardAxis = Vector3.Cross(newUpBodyAxis, newRightBodyAxis).normalized;

            lookRotation = Quaternion.LookRotation(-currentForward, newUpBodyAxis);
        }
    }

    private void RotateBody()
    {
        if (currentForward.sqrMagnitude > 0.0001f)
        {
            mainBody.rotation = Quaternion.RotateTowards(mainBody.rotation, lookRotation, 200f * Time.deltaTime);

            futureLegBasesHolder.rotation = Quaternion.AngleAxis(mainBody.rotation.eulerAngles.y, Vector3.up);
        }
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
