using System.Collections;
using System.Collections.Generic;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace QAI_Pathfinding
{
    public class Pather : MonoBehaviour 
    {
        NativeList<Vector3Int> path;

        public void Start()
        {
            path = new NativeList<Vector3Int>(1, Allocator.Persistent);
        }

        public void OnDestroy()
        {
            path.Dispose();
        }
    }

    public struct AStar_Job : IJob
    {
        public void Execute()
        {
            //Find the most optimal path from point A to point B
        }
    }
}
