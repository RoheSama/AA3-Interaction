using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;


namespace OctopusController
{
    public enum TentacleMode { LEG, TAIL, TENTACLE };

    public class MyOctopusController
    {

        private MyTentacleController[] _tentacles = new MyTentacleController[4];

        private Transform _currentRegion;
        private Transform _target;
        private int _tentacleNear;

        private Transform[] _randomTargets;// = new Transform[4];

        bool _interceptShotBall;

        private float _twistMin, _twistMax;
        private float _swingMin, _swingMax;

        private float _start, _end;
        private bool _isShooting;

        float _targetTimer = 0f;

        readonly float _targetDuration = 3f;
        readonly float _moveToTargetDuration = 0.75f;
        float _moveToTargetTimer = 0f;

        int _tentacleToTargetIndex = -1;

        private Vector3 _clampedAnglesMin = new Vector3(-20, 0, -3);
        private Vector3 _clampedAnglesMax = new Vector3(20, 0, 3);

        bool _done = false;

        readonly float _epsilon = 0.1f;

        private int _mtries = 10;
        private int[] _tries;

        float _theta;
        float _sin;
        float _cos;

        private Vector3[] tpos;

        #region public methods

        public float TwistMin { set => _twistMin = value; }
        public float TwistMax { set => _twistMax = value; }
        public float SwingMin { set => _swingMin = value; }
        public float SwingMax { set => _swingMax = value; }


        public void TestLogging(string objectName)
        {
            Debug.Log("hello, I am initializing my Octopus Controller in object " + objectName);
        }

        public void Init(Transform[] tentacleRoots, Transform[] randomTargets)
        {
            _tentacles = new MyTentacleController[tentacleRoots.Length];

            for (int i = 0; i < tentacleRoots.Length; i++)
            {
                _tentacles[i] = new MyTentacleController();
                _tentacles[i].LoadTentacleJoints(tentacleRoots[i], TentacleMode.TENTACLE);
            }
            _randomTargets = randomTargets;
        }


        public void NotifyTarget(Transform target, Transform region)
        {
            _currentRegion = region;
            _target = target;
        }

        public void NotifyShoot(bool interceptShotBall)
        {
            //TODO. what happens here?
            Debug.Log("Shoot");

            _interceptShotBall = interceptShotBall;
            if (interceptShotBall)
            {
                _targetTimer = 0f;
                //_moveToTargetTimer = 0f;
            }
        }


        public void UpdateTentacles()
        {
            update_ccd();

            if (_interceptShotBall)
            {
                if (_targetTimer < _targetDuration)
                {
                    _targetTimer += Time.deltaTime;
                }

                if (_targetTimer < _moveToTargetDuration)
                {
                    _moveToTargetTimer += Time.deltaTime;
                    _moveToTargetTimer = Mathf.Clamp(_moveToTargetTimer, 0f, _moveToTargetDuration);
                }
                else if (_targetTimer > _targetDuration)
                {
                    _moveToTargetTimer -= Time.deltaTime;
                    _moveToTargetTimer = Mathf.Clamp(_moveToTargetTimer, 0f, _moveToTargetDuration);

                    if (_moveToTargetTimer < 0.0001f)
                    {
                        _tentacleToTargetIndex = -1;
                    }
                }

            }
        }

        #endregion


        #region private and internal methods

        private Vector3 _r2;
        void update_ccd()
        {
            for (int tentacleI = 0; tentacleI < _tentacles.Length; ++tentacleI)
            {
                Transform[] tentacleBones = _tentacles[tentacleI].Bones;

                Vector3 tentacleTargetPos;
                if (_interceptShotBall && tentacleI == _tentacleToTargetIndex)
                {
                    tentacleTargetPos = Vector3.Lerp(_randomTargets[tentacleI].position, _target.position, _moveToTargetTimer / _moveToTargetDuration);
                }
                else
                {
                    tentacleTargetPos = _randomTargets[tentacleI].position;
                }

                _done = false;
                if (!_done)
                {

                    if (_tries[tentacleI] <= _mtries)
                    {
                        for (int i = tentacleBones.Length - 2; i >= 0; i--)
                        {
                            // The vector from the ith joint to the end effector
                            Vector3 r1 = (tentacleBones[tentacleBones.Length - 1].transform.position - tentacleBones[i].transform.position).normalized;

                            // The vector from the ith joint to the target
                            Vector3 r2 = (tentacleTargetPos - tentacleBones[i].transform.position).normalized;

                            // to avoid dividing by tiny numbers
                            if (r1.magnitude * r2.magnitude <= 0.001f)
                            {
                                // cos ? sin? 
                                _cos = 1.0f;
                                _sin = 0.0f;
                            }
                            else
                            {
                                // find the components using dot and cross product
                                float dot = Vector3.Dot(r1, r2);
                                _cos = dot;
                                Vector3 cross = Vector3.Cross(r1, r2);
                                _sin = cross.magnitude;
                            }


                            // The axis of rotation 
                            Vector3 axis = Vector3.Cross(r1, r2).normalized;

                            // find the angle between r1 and r2 (and clamp values if needed avoid errors)
                            _theta = Mathf.Acos(Mathf.Clamp(_cos, -1f, 1f));

                            //Optional. correct angles if needed, depending on angles invert angle if sin component is negative
                            if (_sin < 0.0f)
                                _theta = -_theta;

                            if (_theta > 180.0f)
                            {
                                _theta = 180.0f - _theta;
                            }


                            // obtain an angle value between -pi and pi, and then convert to degrees
                            _theta = _theta * Mathf.Rad2Deg;

                            // rotate the ith joint along the axis by theta degrees in the world space.
                            tentacleBones[i].transform.rotation = Quaternion.AngleAxis(_theta, axis) * tentacleBones[i].transform.rotation;
                            ClampBoneRotation(tentacleBones[i].transform);

                            ++_tries[tentacleI];
                        }
                    }

                    // find the difference in the positions of the end effector and the target
                    Vector3 targetToEffector = tentacleBones[tentacleBones.Length - 1].transform.position - tentacleTargetPos;

                    // if target is within reach (within epsilon) then the process is done
                    if (targetToEffector.magnitude < _epsilon)
                    {
                        _done = true;
                    }
                    // if it isn't, then the process should be repeated
                    else
                    {
                        _done = false;
                    }

                    // the target has moved, reset tries to 0 and change tpos
                    if (tentacleTargetPos != tpos[tentacleI])
                    {
                        _tries[tentacleI] = 0;
                        tpos[tentacleI] = tentacleTargetPos;
                    }

                }

                _tentacles[tentacleI].EndEffectorSphere = tentacleBones[tentacleBones.Length - 1];

            }



        }

        private void ClampBoneRotation(Transform bone)
        {
            Quaternion swingLocalRotation = GetSwing(bone.localRotation, Vector3.up);

            Quaternion clampedLocalRotation = GetClampedQuaternion(swingLocalRotation, _clampedAnglesMin, _clampedAnglesMax);

            bone.localRotation = clampedLocalRotation;
        }

        private Quaternion GetTwist(Quaternion rotation, Vector3 twistAxis)
        {
            return new Quaternion(rotation.x * twistAxis.x, rotation.y * twistAxis.y, rotation.z * twistAxis.z, rotation.w);
        }

        private Quaternion GetSwing(Quaternion rotation, Vector3 twistAxis)
        {
            return rotation * Quaternion.Inverse(GetTwist(rotation, twistAxis));
        }

        private Quaternion GetClampedQuaternion(Quaternion q, Vector3 minBounds, Vector3 maxBounds)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
            angleX = Mathf.Clamp(angleX, minBounds.x, maxBounds.x);
            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
            angleY = Mathf.Clamp(angleY, minBounds.y, maxBounds.y);
            q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

            float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.z);
            angleZ = Mathf.Clamp(angleZ, minBounds.z, maxBounds.z);
            q.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

            return q;
        }


        internal float GetAngle(float theta)
        {
            if (theta > Mathf.PI) { theta -= Mathf.PI * 2; }
            if (theta < -Mathf.PI) { theta += Mathf.PI * 2; }

            return theta;
        }
        internal float LimitZRot(float theta)
        {
            theta *= Mathf.Rad2Deg;
            if(theta > 15.0f)
            {
                theta = 15;
            }
            else if(theta < -15)
            {
                theta = -15;
            }
            return theta;
        }
        #endregion
    }
}
