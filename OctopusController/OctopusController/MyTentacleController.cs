using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OctopusController
{
    internal class MyTentacleController
    {

        TentacleMode tentacleMode;
        Transform[] _bones;
        Transform _endEffectorSphere;

        public Transform[] Bones { get => _bones; }

        public Transform[] LoadTentacleJoints(Transform root, TentacleMode mode)
        {
            tentacleMode = mode;

            List<Transform> joints = new List<Transform>();
            
            switch (tentacleMode){
                case TentacleMode.LEG:

                    root = root.GetChild(0);
                    joints.Add(root);
                    while(root.transform.childCount != 0)
                    {
                        root = root.GetChild(1);
                        joints.Add(root);
                    }

                    //Reference base of the leg
                    _endEffectorSphere = joints[0];
                    break;
                case TentacleMode.TAIL:

                    joints.Add(root);
                    while(root.transform.childCount != 0)
                    {
                        root = root.GetChild(1);
                        joints.Add(root);
                    }

                    //Reference to red sphere
                    _endEffectorSphere = joints[joints.Count - 1];
                    
                    break;
                case TentacleMode.TENTACLE:

                    root = root.GetChild(0).transform.GetChild(0);
                    while (root.transform.childCount != 0)
                    {
                        root = root.GetChild(0);
                        joints.Add(root);
                    }

                    //Reference to sphere with collider
                    _endEffectorSphere = joints[joints.Count - 1];
                    break;
            }

            _bones = joints.ToArray();

            return Bones;
        }
    }
}
