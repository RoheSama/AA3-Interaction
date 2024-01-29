using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OctopusController
{
    public enum TentacleMode { LEG, TAIL, TENTACLE };

    public class MyOctopusController
    {

        // Tentacle-related variables
        private MyTentacleController[] _tentacles = new MyTentacleController[4];
        private Transform[] _randomTargets; // = new Transform[4];
        private int _tentacleToTargetIndex = -1; // start at 0
        private bool _interceptShotBall;
        private int[] _tries;

        // Target and region variables
        private Transform _currentRegion;
        private Transform _target;
        private Dictionary<Transform, int> regionToTentacleIndex;

        // Angle calculation variables
        private float _theta;
        private float _sin;
        private float _cos;

        // Max number of tries before the system gives up (Maybe 10 is too high?)
        private int _mtries = 10;

        // Range within which the target will be assumed to be reached
        private readonly float _epsilon = 0.1f;

        // To check if the target is reached at any point
        private bool _done = false;

        // To store the position of the target
        private Vector3[] tpos;

        // Target movement variables
        private readonly float _targetDuration = 3f;
        private float _targetTimer = 0f;
        private readonly float _moveToTargetDuration = 0.75f;
        private float _moveToTargetTimer = 0f;

        // Clamped angle limits
        private Vector3 _clampedAnglesMin = new Vector3(-20, 0, -3);
        private Vector3 _clampedAnglesMax = new Vector3(20, 0, 3);


        float _twistMin, _twistMax;
        float _swingMin, _swingMax;

        #region public methods
        //DO NOT CHANGE THE PUBLIC METHODS!!

        public float TwistMin { set => _twistMin = value; }
        public float TwistMax { set => _twistMax = value; }
        public float SwingMin { set => _swingMin = value; }
        public float SwingMax { set => _swingMax = value; }

        public void Init(Transform[] tentacleRoots, Transform[] randomTargets)
        {
            _randomTargets = randomTargets;

            _tentacles = new MyTentacleController[tentacleRoots.Length];
            tpos = new Vector3[tentacleRoots.Length];
            _tries = new int[tentacleRoots.Length];
            regionToTentacleIndex = new Dictionary<Transform, int>();


            // foreach (Transform t in tentacleRoots)
            for (int i = 0; i < tentacleRoots.Length; i++)
            {

                _tentacles[i] = new MyTentacleController();
                _tentacles[i].LoadTentacleJoints(tentacleRoots[i], TentacleMode.TENTACLE);

                //TODO: initialize any variables needed in ccd
                tpos[i] = randomTargets[i].position;
                _tries[i] = 0;

                //TODO: use the regions however you need to make sure each tentacle stays in its region
                regionToTentacleIndex.Add(randomTargets[i].parent, i);
            }

            _tentacleToTargetIndex = -1;
            _interceptShotBall = false;
            _targetTimer = 0f;
            _moveToTargetTimer = 0f;
        }


        public void NotifyTarget(Transform target, Transform region)
        {
            if (!_interceptShotBall || _targetTimer >= _targetDuration) return;

            _currentRegion = region;
            _target = target;

            if (regionToTentacleIndex.ContainsKey(region))
            {
                _tentacleToTargetIndex = regionToTentacleIndex[region];
            }

        }

        public void NotifyShoot(bool interceptShotBall)
        {
            Debug.Log("Shoot");

            _interceptShotBall = interceptShotBall;
            if (interceptShotBall)
            {
                _targetTimer = 0f;
            }
        }


        public void UpdateTentacles()
        {
            UpdateCCD();

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

        void UpdateCCD()
        {
            for (int tentacleIndex = 0; tentacleIndex < _tentacles.Length; ++tentacleIndex)
            {
                Transform[] tentacleBones = _tentacles[tentacleIndex].Bones;
                Vector3 tentacleTargetPos = (_interceptShotBall && tentacleIndex == _tentacleToTargetIndex) ?
                                            Vector3.Lerp(_randomTargets[tentacleIndex].position, _target.position, _moveToTargetTimer / _moveToTargetDuration) :
                                            _randomTargets[tentacleIndex].position;

                bool done = false;
                int triesCount = _tries[tentacleIndex];

                if (triesCount <= _mtries)
                {
                    for (int boneIndex = tentacleBones.Length - 2; boneIndex >= 0; boneIndex--)
                    {
                        Vector3 r1 = (tentacleBones[tentacleBones.Length - 1].position - tentacleBones[boneIndex].position).normalized;
                        Vector3 r2 = (tentacleTargetPos - tentacleBones[boneIndex].position).normalized;

                        if (r1.magnitude * r2.magnitude <= 0.001f)
                        {
                            _cos = 1.0f;
                            _sin = 0.0f;
                        }
                        else
                        {
                            _cos = Vector3.Dot(r1, r2);
                            _sin = Vector3.Cross(r1, r2).magnitude;
                        }

                        Vector3 axis = Vector3.Cross(r1, r2).normalized;
                        _theta = Mathf.Acos(Mathf.Clamp(_cos, -1f, 1f)) * Mathf.Rad2Deg;

                        if (_sin < 0.0f)
                            _theta = -_theta;

                        if (_theta > 180.0f)
                            _theta = 180.0f - _theta;

                        tentacleBones[boneIndex].rotation = Quaternion.AngleAxis(_theta, axis) * tentacleBones[boneIndex].rotation;
                        ClampBoneRotation(tentacleBones[boneIndex]);

                        ++_tries[tentacleIndex];
                    }
                }

                Vector3 targetToEffector = tentacleBones[tentacleBones.Length - 1].position - tentacleTargetPos;

                done = targetToEffector.magnitude < _epsilon;

                if (tentacleTargetPos != tpos[tentacleIndex])
                {
                    _tries[tentacleIndex] = 0;
                    tpos[tentacleIndex] = tentacleTargetPos;
                }

                _tentacles[tentacleIndex].EndEffectorSphere = tentacleBones[tentacleBones.Length - 1];
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

        #endregion

    }
}
