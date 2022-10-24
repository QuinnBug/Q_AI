using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace QAI_Pathfinding 
{
    [System.Serializable]
    public class NavGrid : MonoBehaviour
    {
        public Transform bottomLeftPoint;
        public Transform topRightPoint;
        [Space]
        public float minimumExcess;
        [Space]
        public bool displayPathable = false;
        public bool displayNonPathable = false;
        [Space]
        public Vector3 nodeSize;
        public float unitHeight;

        internal Vector3 gridWorldSize;
        //internal GridNode[,,] grid = null;
        public NativeList<GridNode> grid;

        internal Vector3Int gridSize;

        private Vector3 currentNodeWorldPoint;

        private Vector3 worldBottomLeft;
        private Vector3 worldTopRight;
        private Bounds bounds;

        public GridUpdate_Job updateJob = new GridUpdate_Job();
        

        [Space]
        public bool CREATE_GRID = false;
        public bool DESTROY_GRID = false;
        [Space]
        public bool UPDATE_TRP_TO_BLP = true;

        public void Start()
        {
            gridWorldSize = topRightPoint.localPosition - bottomLeftPoint.localPosition;

            gridSize.x = (int)(gridWorldSize.x / nodeSize.x);
            gridSize.y = (int)(gridWorldSize.y / nodeSize.y);
            gridSize.z = (int)(gridWorldSize.z / nodeSize.z);

            CreateGrid();
        }

        public void OnDestroy()
        {
            grid.Dispose();
        }

        void CreateGrid()
        {
            grid = new NativeList<GridNode>();

            bounds = new Bounds(transform.position, topRightPoint.position - bottomLeftPoint.position);

            worldBottomLeft = new Vector3(
                transform.localPosition.x - (gridWorldSize.x / 2),
                transform.localPosition.y - (gridWorldSize.y / 2),
                transform.localPosition.z - (gridWorldSize.z / 2));

            worldTopRight = new Vector3(
                transform.localPosition.x + (gridWorldSize.x / 2),
                transform.localPosition.y + (gridWorldSize.y / 2),
                transform.localPosition.z + (gridWorldSize.z / 2));


            for (int worldZ = 0; worldZ < gridSize.z; worldZ++)
            {
                for (int worldY = 0; worldY < gridSize.y; worldY++)
                {
                    for (int worldX = 0; worldX < gridSize.x; worldX++)
                    {
                        currentNodeWorldPoint = transform.parent.TransformPoint(worldBottomLeft + new Vector3(worldX * nodeSize.x, worldY * nodeSize.y, worldZ * nodeSize.z));

                        currentNodeWorldPoint = new Vector3();
                        currentNodeWorldPoint = worldBottomLeft + new Vector3(worldX * nodeSize.x, worldY * nodeSize.y, worldZ * nodeSize.z);
                        currentNodeWorldPoint = transform.parent.TransformPoint(currentNodeWorldPoint);
                        bool isPathable = CheckNodePathable();

                        grid.Add( new GridNode(currentNodeWorldPoint, new Vector3Int(worldX, worldY, worldZ), isPathable, nodeSize));
                    }
                }
            }
        }

        //this is probably the part that I want to set up as a job
        public void UpdateGrid()
        {
            foreach (GridNode node in grid)
            {
                currentNodeWorldPoint = transform.parent.TransformPoint(worldBottomLeft +
                    new Vector3(node.gridPosition.x * nodeSize.x, node.gridPosition.y * nodeSize.y, node.gridPosition.z * nodeSize.z));

                node.SetPathable(CheckNodePathable());
                node.SetWorldPos(currentNodeWorldPoint);
            }
        }

        public bool CheckNodePathable()
        {
            bool pathable = false;

            RaycastHit hit;
            if (Physics.Raycast(currentNodeWorldPoint + (Vector3.up * (nodeSize.y * 0.25f)), Vector3.down, out hit, nodeSize.y * 0.5f, 1 << LayerMask.NameToLayer("Floor")) && hit.collider.tag != "DeathZone")
            {
                currentNodeWorldPoint.y = hit.point.y + unitHeight / 2;

                //int layerMask = 1 << LayerMask.NameToLayer("Obstacle");
                //layerMask += 1 << LayerMask.NameToLayer("Floor");

                Vector3 boxSize = new Vector3((nodeSize.x / 2) * (minimumExcess / 2), unitHeight / 3, (nodeSize.z / 2) * (minimumExcess / 2));

                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                if (!Physics.CheckBox(currentNodeWorldPoint, boxSize, rotation, 1 << LayerMask.NameToLayer("Obstacle")))
                {
                    pathable = true;
                }
            }

            return pathable;
        }

        internal GridNode NodeFromGridSpace(Vector3Int gridSpace)
        {
            return Node(gridSpace.x, gridSpace.y, gridSpace.z);
        }

        public GridNode NodeFromWorld(Vector3 worldPos)
        {
            if (!bounds.Contains(worldPos))
            {
                Debug.LogError("position is out of bounds");
                return grid[0];
            }

            float shortest = nodeSize.magnitude;
            int returnIdx = -1;
            int i = 0;
            foreach (GridNode node in grid)
            {
                float distance = Vector3.Distance(worldPos, node.worldPosition);
                if (distance < shortest)
                {
                    returnIdx = i;
                    shortest = distance;
                }

                i++;
            }

            if (returnIdx != -1)
            {
                return grid[i];
            }

            return grid[0];
        }

        public GridNode Node(int x, int y, int z = 0) 
        {
            return grid[x + (y * gridSize.x) + (z * gridSize.x * gridSize.y)];
        }

        private void OnDrawGizmos()
        {
            if (grid != null)
            {
                Vector3 size = (nodeSize * 0.25f);
                size.Scale(transform.lossyScale);
                foreach (GridNode item in grid)
                {
                    if (item == null)
                    {
                        Debug.Log("broke");
                        displayPathable = false;
                        displayNonPathable = false;
                    }

                    if (item.pathable && displayPathable)
                    {
                        Gizmos.color = new Color(0, 0.2f, 0.8f, 0.6f);
                        Gizmos.DrawCube(item.worldPosition, size);
                    }
                    else if (!item.pathable && displayNonPathable)
                    {
                        Gizmos.color = new Color(1, 0, 0, 0.2f);
                        Gizmos.DrawCube(item.worldPosition, size);
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (UPDATE_TRP_TO_BLP)
            {
                topRightPoint.localPosition = bottomLeftPoint.localPosition * -1;
            }

            if (CREATE_GRID)
            {
                CREATE_GRID = false;
                Start();
            }

            if (DESTROY_GRID)
            {
                DESTROY_GRID = false;
                grid = null;
            }
        }
    }

    public struct GridUpdate_Job : IJob
    {
        public NativeList<GridNode> grid;

        public void Execute() 
        {
            Debug.Log(grid.Length);
        }

    }
}
