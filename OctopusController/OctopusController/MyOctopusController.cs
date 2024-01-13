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

        private MyTentacleController[] _tentacles = new MyTentacleController[4];

        private Transform _currentRegion;
        private Transform _target;
        private int _tentacleNear;

        private Transform[] _randomTargets;// = new Transform[4];


        private float _twistMin, _twistMax;
        private float _swingMin, _swingMax;

        private float _start, _end;
        private bool _isShooting;

        #region public methods

        public float TwistMin { set => _twistMin = value; }
        public float TwistMax { set => _twistMax = value; }
        public float SwingMin { set => _swingMin = value; }
        public float SwingMax { set => _swingMax = value; }

        float[] _theta, _sin, _cos;


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

        public void NotifyShoot()
        {
            Debug.Log("Shoot");
            _start = 0;
            _end = 3;
            _isShooting = true;
            //float temp = Vector3.Distance(_tentacles[0].Bones[_tentacles[0].Bones.Length - 1].position, _currentRegion.position);

            //for (int i = 0; i < _tentacles.Length; i++)
            //{
            //    if (Vector3.Distance(_tentacles[i].Bones[_tentacles[i].Bones.Length - 1].position, _currentRegion.position) < temp)
            //    {
            //        _tentacleNear = i;
            //    }
            //}

            if (_currentRegion.gameObject.transform.name == "region1")
            {
                _tentacleNear = 0;
            }
            else if (_currentRegion.gameObject.transform.name == "region2")
            {
                _tentacleNear = 1;
            }
            else if (_currentRegion.gameObject.transform.name == "region3")
            {
                _tentacleNear = 2;
            }
            else if (_currentRegion.gameObject.transform.name == "region4")
            {
                _tentacleNear = 3;
            }
        }


        public void UpdateTentacles()
        {
            update_ccd();

            if (_isShooting)
            {
                _start += Time.deltaTime;
                if (_start > _end)
                {
                    _start = 0;
                    _isShooting = false;
                }
            }
        }

        #endregion


        #region private and internal methods

        private Vector3 _r2;
        private void update_ccd()
        {
            foreach (var tentacle in _tentacles)
            {
                _theta = new float[tentacle.Bones.Length];
                _sin = new float[tentacle.Bones.Length];
                _cos = new float[tentacle.Bones.Length];
                {
                    for (int j = tentacle.Bones.Length - 2; j >= 0; j--)
                    {
                        Vector3 r1 = tentacle.Bones[tentacle.Bones.Length - 1].transform.position - tentacle.Bones[j].transform.position;

                        //Change target depending if it's shooting
                        if (_isShooting && _tentacleNear == Array.IndexOf(_tentacles, tentacle))
                        {
                            _r2 = _target.transform.position - _tentacles[_tentacleNear].Bones[j].transform.position;
                            
                        }
                        else if(_isShooting && _tentacleNear != Array.IndexOf(_tentacles, tentacle))
                        {
                            _r2 = _randomTargets[Array.IndexOf(_tentacles, tentacle)].transform.position - tentacle.Bones[j].transform.position;
                        }
                        else
                        {
                            _r2 = _randomTargets[Array.IndexOf(_tentacles, tentacle)].transform.position - tentacle.Bones[j].transform.position;
                        }

                        // avoid the division of small numbers
                        if (r1.magnitude * _r2.magnitude <= 0.001f)
                        {
                            _cos[j] = 1;
                            _sin[j] = 0;
                        }
                        else
                        {
                            _cos[j] = Vector3.Dot(r1, _r2) / (r1.magnitude * _r2.magnitude);
                            _sin[j] = Vector3.Cross(r1, _r2).magnitude / (r1.magnitude * _r2.magnitude);
                        }

                        Vector3 axis = Vector3.Cross(r1, _r2).normalized;
                        _theta[j] = Mathf.Acos(Mathf.Clamp(_cos[j], -1, 1));

                        if (_sin[j] < 0.0f)
                            _theta[j] *= -1.0f;

                        _theta[j] = GetAngle(_theta[j]);

                        //(not working i think)
                        _theta[j] = LimitZRot(_theta[j]);

                        tentacle.Bones[j].transform.Rotate(axis, _theta[j], Space.World);
                        Quaternion twist = new Quaternion(0, tentacle.Bones[j].transform.localRotation.y, 0, tentacle.Bones[j].transform.localRotation.w);
                        twist = twist.normalized;
                        Quaternion swing = tentacle.Bones[j].transform.localRotation * Quaternion.Inverse(twist);
                        tentacle.Bones[j].transform.localRotation = swing.normalized;

                    }
                }
            }
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
