using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QAI_Pathfinding
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Node", menuName = "Pathfinding/Node", order = 0)]
    public class NodeData : ScriptableObject
    {
        //title of the resource that needs to be loaded for the gridNode display
        public string title;
        public float costModifier;
    }

    public struct GridNode 
    {
        public Vector3 worldPosition;
        public Vector3Int gridPosition;
        public Bounds bounds;

        public float gCost;
        public float hCost;
        public float fCost { get { return gCost + hCost; } }
        public bool pathable;

        public Vector3Int parentPos;

        public GridNode(Vector3 _worldPos, Vector3Int _gridPosition, bool _pathable, Vector3 _nodeSize)
        {
            worldPosition = _worldPos;
            gridPosition = _gridPosition;
            pathable = _pathable;

            bounds = new Bounds(_worldPos, _nodeSize);

            gCost = 0;
            hCost = 0;
            parentPos = new Vector3Int(-999, -999, -999);
        }

        public void SetPathable(bool value) { pathable = value; }
        public void SetWorldPos(Vector3 value) { worldPosition = value; }

        public Vector3 worldPosYZero()
        {
            return new Vector3(worldPosition.x, 0, worldPosition.z);
        }
    }
}
