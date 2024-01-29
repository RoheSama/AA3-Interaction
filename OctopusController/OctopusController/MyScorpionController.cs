using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;


namespace OctopusController
{

    public class MyScorpionController
    {
        public struct PositionRotation
        {
            Vector3 position;
            Quaternion rotation;

            public PositionRotation(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }

            // PositionRotation to Vector3
            public static implicit operator Vector3(PositionRotation pr)
            {
                return pr.position;
            }
            // PositionRotation to Quaternion
            public static implicit operator Quaternion(PositionRotation pr)
            {
                return pr.rotation;
            }
        }

        //TAIL
        Transform tailTarget;
        Transform tailEndEffector;
        MyTentacleController _tail;
        float animationRange = 5f;

        float[] _tailBoneAngles;
        Vector3[] _tailBoneAxis;
        Vector3[] _tailBoneOffsets;

        public delegate float ErrorFunction(Vector3 target, float[] solution);
        private ErrorFunction _errorFunction;

        public float DeltaGradient = 0.1f; // Used to simulate gradient (degrees)
        public float LearningRate = 4.0f; // How much we move depending on the gradient

        public float StopThreshold = 0.01f; // If closer than this, it stops
        public float SlowdownThreshold = 0.25f; // If closer than this, it linearly slows down

        private float _distanceWeight = 1f;
        private float _orientationWeight = 1f;
        private Vector3 _targetOrientationDirection;
        private Vector3 _endEffectorOrientationDirection;
        private float _angleBetweenOrientationVectors;


        //LEGS
        Transform[] legTargets;
        Transform[] legFutureBases;
        MyTentacleController[] _legs = new MyTentacleController[6];

        List<Vector3[]> bonePositionsCopy;
        List<float[]> legsDistances;
        bool doneFABRIK;
        float _angleThreshold = 1.0f;

        float _legFarAwayThreashold = 0.8f;
        bool[] _legIsMoving;
        Vector3[] _legsBaseOrigin;
        Vector3[] _legsBaseDestination;
        float[] _legsMoveBaseTimer;
        float _legsMoveDuration = 0.10f;
        bool _startedWalking = false;
        bool _updateTail = false;

        float _legMoveHeight = 0.4f;

        private List<Vector3[]> _originalLegsPositions;
        private List<Quaternion[]> _originalLegsRotations;


        #region public
        public void InitLegs(Transform[] LegRoots, Transform[] LegFutureBases, Transform[] LegTargets)
        {
            _legs = new MyTentacleController[LegRoots.Length];
            bonePositionsCopy = new List<Vector3[]>();
            legsDistances = new List<float[]>();
            _legIsMoving = new bool[LegRoots.Length];
            _legsBaseOrigin = new Vector3[LegRoots.Length];
            _legsBaseDestination = new Vector3[LegRoots.Length];
            _legsMoveBaseTimer = new float[LegRoots.Length];
            _originalLegsPositions = new List<Vector3[]>(LegRoots.Length);
            _originalLegsRotations = new List<Quaternion[]>(LegRoots.Length);

            for (int i = 0; i < LegRoots.Length; i++)
            {
                _legs[i] = new MyTentacleController();
                _legs[i].LoadTentacleJoints(LegRoots[i], TentacleMode.LEG);
                _legs[i].LoadTentacleJoints(LegRoots[i], TentacleMode.LEG);

                bonePositionsCopy.Add(new Vector3[_legs[i].Bones.Length]);
                legsDistances.Add(new float[_legs[i].Bones.Length - 1]);

                _originalLegsPositions.Add(new Vector3[_legs[i].Bones.Length]);
                _originalLegsRotations.Add(new Quaternion[_legs[i].Bones.Length]);

                for (int boneI = 0; boneI < legsDistances[i].Length; ++boneI)
                {
                    legsDistances[i][boneI] = Vector3.Distance(_legs[i].Bones[boneI].position, _legs[i].Bones[boneI + 1].position);

                    _originalLegsPositions[i][boneI] = _legs[i].Bones[boneI].localPosition;
                    _originalLegsRotations[i][boneI] = _legs[i].Bones[boneI].localRotation;
                }
                int lastBoneI = _legs[i].Bones.Length - 1;
                _originalLegsPositions[i][lastBoneI] = _legs[i].Bones[lastBoneI].localPosition;
                _originalLegsRotations[i][lastBoneI] = _legs[i].Bones[lastBoneI].localRotation;


                _legIsMoving[i] = false;
                _legsBaseOrigin[i] = _legs[i].Bones[0].position;
                _legsBaseDestination[i] = legFutureBases[i].position;
                _legsMoveBaseTimer[i] = 0f;
            }

            legFutureBases = legFutureBases;
            legTargets = legTargets;
        }

        public void InitTail(Transform TailBase)
        {
            _tail = new MyTentacleController();
            _tail.LoadTentacleJoints(TailBase, TentacleMode.TAIL);

            _tailBoneAxis = new Vector3[_tail.Bones.Length];
            _tailBoneAngles = new float[_tail.Bones.Length];
            _tailBoneOffsets = new Vector3[_tail.Bones.Length];
            for (int i = 0; i < _tail.Bones.Length; ++i)
            {

                if (i == 0)
                {
                    _tailBoneAxis[i] = Vector3.up; // Allows tail to rotate sideways
                    _tailBoneAngles[i] = _tail.Bones[i].localEulerAngles.y;
                    _tailBoneOffsets[i] = _tail.Bones[i].position;

                }
                else
                {
                    _tailBoneAxis[i] = Vector3.right;
                    _tailBoneAngles[i] = _tail.Bones[i].localEulerAngles.x;
                    _tailBoneOffsets[i] = Quaternion.Inverse(_tail.Bones[i - 1].rotation) * (_tail.Bones[i].position - _tail.Bones[i - 1].position);
                }

            }

            _errorFunction = DistanceFromTargetAndOrientation;

            tailEndEffector = _tail.EndEffectorSphere;
        }

        public void ResetTailBoneAngles()
        {
            for (int i = 0; i < _tail.Bones.Length; i++)
            {
                _tailBoneAngles[i] = i == 0 ? _tail.Bones[i].localEulerAngles.y : _tail.Bones[i].localEulerAngles.x;
            }
        }

        //TODO: Check when to start the animation towards target and implement Gradient Descent method to move the joints.
        public void NotifyTailTarget(Transform target)
        {
            if (Vector3.Distance(target.position, tailEndEffector.position) < animationRange)
            {
                tailTarget = target;
            }
        }

        //TODO: Notifies the start of the walking animation
        public void NotifyStartWalk()
        {
            _startedWalking = true;
        }

        public void NotifyStartUpdateTail()
        {
            _updateTail = true;
        }

        public void NotifyStopUpdateTail()
        {
            _updateTail = false;
        }

        public void ResetLegs()
        {
            for (int i = 0; i < _originalLegsPositions.Count; ++i)
            {
                for (int j = 0; j < _originalLegsPositions[i].Length; ++j)
                {
                    _legs[i].Bones[j].localPosition = _originalLegsPositions[i][j];
                    _legs[i].Bones[j].localRotation = _originalLegsRotations[i][j];
                }
                _legIsMoving[i] = false;
                _legsMoveBaseTimer[i] = 0f;
            }
        }


        //TODO: create the apropiate animations and update the IK from the legs and tail

        public void UpdateIK()
        {
            UpdateLegPos();
            UpdateTail();
        }
        #endregion


        #region private
        //TODO: Implement the leg base animations and logic
        private void UpdateLegPos()
        {
            if (!_startedWalking) return;

            for (int legI = 0; legI < _legs.Length; ++legI)
            {
                float futureBaseDistance = Vector3.Distance(_legs[legI].Bones[0].position, legFutureBases[legI].position);

                if (futureBaseDistance > _legFarAwayThreashold && !_legIsMoving[legI])
                {

                    _legIsMoving[legI] = true;
                    _legsMoveBaseTimer[legI] = 0f;
                    _legsBaseOrigin[legI] = _legs[legI].Bones[0].position;
                    _legsBaseDestination[legI] = legFutureBases[legI].position;
                }

                if (_legIsMoving[legI])
                {
                    _legsMoveBaseTimer[legI] += Time.deltaTime;
                    float t = _legsMoveBaseTimer[legI] / _legsMoveDuration;
                    _legs[legI].Bones[0].position = ComputeBaseBonePosition(legI, t);

                    if (t > 0.999f)
                    {
                        _legIsMoving[legI] = false;
                    }
                }


                updateLegFABRIK(_legs[legI].Bones, legTargets[legI], bonePositionsCopy[legI], legsDistances[legI]);

            }

        }

        private void updateLegFABRIK(Transform[] joints, Transform target, Vector3[] positionsCopy, float[] distances)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                positionsCopy[i] = joints[i].position;
            }

            doneFABRIK = false;


            if (!doneFABRIK)
            {
                float targetRootDist = Vector3.Distance(positionsCopy[0], target.position);

                // Update joint positions
                if (targetRootDist > distances.Sum())
                {
                    // The target is unreachable
                    for (int i = 0; i < joints.Length - 1; ++i)
                    {
                        // Find the distance between the target and the joint
                        float targetToJointDist = Vector3.Distance(target.position, positionsCopy[i]);
                        float ratio = distances[i] / targetToJointDist;

                        // Find the new joint position
                        positionsCopy[i + 1] = (1 - ratio) * positionsCopy[i] + ratio * target.position;

                    }

                    doneFABRIK = true;
                }
                else
                {
                    float tolerance = 0.05f;
                    float targetToEndEffectorDistance = Vector3.Distance(positionsCopy[positionsCopy.Length - 1], target.position);

                    while (targetToEndEffectorDistance > tolerance)
                    {
                        positionsCopy[positionsCopy.Length - 1] = target.position;

                        for (int i = positionsCopy.Length - 2; i >= 0; --i)
                        {
                            // Find the distance between the new joint position (i+1) and the current joint (i)
                            float distanceJoints = Vector3.Distance(positionsCopy[i], positionsCopy[i + 1]);
                            float ratio = distances[i] / distanceJoints;

                            // Find the new joint position
                            positionsCopy[i] = (1 - ratio) * positionsCopy[i + 1] + ratio * positionsCopy[i];
                        }

                        positionsCopy[0] = joints[0].position;

                        for (int i = 1; i < positionsCopy.Length - 1; ++i)
                        {
                            // Find the distance between the new joint position (i+1) and the current joint (i)
                            float distanceJoints = Vector3.Distance(positionsCopy[i - 1], positionsCopy[i]);
                            float ratio = distances[i - 1] / distanceJoints;

                            // Find the new joint position
                            positionsCopy[i] = (1 - ratio) * positionsCopy[i - 1] + ratio * positionsCopy[i];
                        }

                        targetToEndEffectorDistance = Vector3.Distance(positionsCopy[positionsCopy.Length - 1], target.position); // Recompute
                    }

                    doneFABRIK = true;

                }
                for (int i = 0; i < joints.Length - 1; i++)
                {

                    Vector3 oldDir = (joints[i + 1].position - joints[i].position).normalized;
                    Vector3 newDir = (positionsCopy[i + 1] - positionsCopy[i]).normalized;

                    Vector3 axis = Vector3.Cross(oldDir, newDir).normalized;
                    float angle = Mathf.Acos(Vector3.Dot(oldDir, newDir)) * Mathf.Rad2Deg;

                    if (angle > _angleThreshold)
                    {
                        joints[i].rotation = Quaternion.AngleAxis(angle, axis) * joints[i].rotation;
                    }

                    //joints[i].position = copy[i]; // just for testing
                }

            }
        }



        public void SetLearningRate(float learningRate)
        {
            LearningRate = learningRate;
        }

        private void UpdateTail()
        {
            if (tailTarget != null && _updateTail)
            {
                if (Vector3.Distance(tailTarget.position, tailEndEffector.position) > StopThreshold)
                {
                    ApproachTarget(tailTarget.position);
                }
            }
        }

        #endregion



        // Gradient Descent functions
        public void ApproachTarget(Vector3 target)
        {

            for (int i = 0; i < _tailBoneAngles.Length; ++i)
            {
                _tailBoneAngles[i] = _tailBoneAngles[i] - (LearningRate * CalculateGradient(target, _tailBoneAngles, i, DeltaGradient));
            }

            for (int i = 0; i < _tailBoneAngles.Length; i++)
            {
                //_tail.Bones[i].localRotation = Quaternion.identity;

                Vector3 localEulerAngles = _tail.Bones[i].localEulerAngles;
                if (i == 0)
                {
                    _tail.Bones[i].localEulerAngles =
                        new Vector3(localEulerAngles.x, 0, localEulerAngles.z) + new Vector3(0, _tailBoneAngles[i], 0);
                }
                else
                {
                    _tail.Bones[i].localEulerAngles =
                        new Vector3(0, localEulerAngles.y, localEulerAngles.z) + new Vector3(_tailBoneAngles[i], 0, 0);
                }

            }
        }

        public float DistanceFromTarget(Vector3 target, float[] Solution)
        {
            Vector3 point = ForwardKinematics(Solution);
            return Vector3.Distance(point, target);
        }

        public void SetDistanceAndOrientationWeight(float distanceWeight, float orientationWeight)
        {
            _distanceWeight = distanceWeight;
            _orientationWeight = orientationWeight;
        }

        public float DistanceFromTargetAndOrientation(Vector3 target, float[] Solution)
        {
            PositionRotation posRot = ForwardKinematics(Solution);
            Vector3 point = posRot;
            Quaternion rotation = posRot;
            _endEffectorOrientationDirection = rotation * _tailBoneOffsets[_tailBoneOffsets.Length - 1].normalized;

            return Vector3.Distance(point, target) * _distanceWeight + OrientationToTarget() * _orientationWeight;
        }


        public float CalculateGradient(Vector3 target, float[] Solution, int i, float delta)
        {
            Solution[i] += delta; 
            float deltaDistanceFromTarget = _errorFunction(target, Solution);

            Solution[i] -= delta; 
            float distanceFromTarget = _errorFunction(target, Solution);

            return (deltaDistanceFromTarget - distanceFromTarget) / delta;
        }

      

        private float OrientationToTarget()
        {
            float dot = Vector3.Dot(_targetOrientationDirection, _endEffectorOrientationDirection);

            dot = Mathf.Abs(dot - 1f);

            return dot;
        }


        public void SetOrientationDirections(Vector3 targetOrientationDirection)
        {
            _targetOrientationDirection = targetOrientationDirection;
        }


        public PositionRotation ForwardKinematics(float[] Solution)
        {
            Vector3 prevPoint = _tail.Bones[0].transform.position;

            Quaternion rotation = Quaternion.AngleAxis(_tail.Bones[0].localEulerAngles.x, Vector3.right);

            for (int i = 0; i < Solution.Length - 1; ++i)
            {
                Vector3 prev = prevPoint;
                rotation = rotation * Quaternion.AngleAxis(Solution[i], _tailBoneAxis[i]);

                if (i == 0)
                {
                    prevPoint += rotation * _tailBoneOffsets[i + 1];
                }
                else
                {
                    prevPoint += rotation * _tailBoneOffsets[i + 1];
                }

                Debug.DrawLine(prev, prevPoint, Color.blue);
            }

            return new PositionRotation(prevPoint, rotation);
        }



        private Vector3 ComputeBaseBonePosition(int legI, float t)
        {
            Vector3 bonePosition = Vector3.Lerp(_legsBaseOrigin[legI], _legsBaseDestination[legI], t);

            bonePosition += Mathf.Sin(t * Mathf.PI) * _legMoveHeight * Vector3.up;

            return bonePosition;
        }

    }


}