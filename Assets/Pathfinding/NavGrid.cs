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
        internal List<GridNode> grid = null;

        internal Vector3Int gridSize;

        internal Vector3 currentNodeWorldPoint;

        internal Vector3 worldBottomLeft;
        private Vector3 worldTopRight;
        private Bounds bounds;
        

        [Space]
        public bool CREATE_GRID = false;

        public void Start()
        {
            gridWorldSize = topRightPoint.localPosition - bottomLeftPoint.localPosition;
            worldBottomLeft = bottomLeftPoint.position;
            worldTopRight = topRightPoint.position;

            gridSize.x = (int)(gridWorldSize.x / nodeSize.x) + 1;
            gridSize.y = (int)(gridWorldSize.y / nodeSize.y) + 1;
            gridSize.z = (int)(gridWorldSize.z / nodeSize.z) + 1;

            CreateGrid();
        }

        

        void CreateGrid()
        {
            Debug.Log("Creating Grid");
            grid = new List<GridNode>();

            bounds = new Bounds(transform.position, topRightPoint.position - bottomLeftPoint.position);

            //worldBottomLeft = new Vector3(
            //    transform.localPosition.x - (gridWorldSize.x / 2),
            //    transform.localPosition.y - (gridWorldSize.y / 2),
            //    transform.localPosition.z - (gridWorldSize.z / 2));

            //worldTopRight = new Vector3(
            //    transform.localPosition.x + (gridWorldSize.x / 2),
            //    transform.localPosition.y + (gridWorldSize.y / 2),
            //    transform.localPosition.z + (gridWorldSize.z / 2));


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

                        GridNode newNode = new GridNode(currentNodeWorldPoint, new Vector3Int(worldX, worldY, worldZ), false, nodeSize);
                        CheckNodePathable(newNode);
                        grid.Add(newNode);
                    }
                }
            }
        }

        public void UpdateGrid()
        {
            Debug.Log(grid.Count);
            foreach (GridNode node in grid)
            {
                //currentNodeWorldPoint = transform.parent.TransformPoint(worldBottomLeft +
                //    new Vector3(node.gridPosition.x * nodeSize.x, node.gridPosition.y * nodeSize.y, node.gridPosition.z * nodeSize.z));

                currentNodeWorldPoint = worldBottomLeft +
                    new Vector3(node.gridPosition.x * nodeSize.x, node.gridPosition.y * nodeSize.y, node.gridPosition.z * nodeSize.z);

                node.SetWorldPos(currentNodeWorldPoint);
                CheckNodePathable(node);
            }
        }

        public void CheckNodePathable(GridNode node)
        {
            bool pathable = false;

            RaycastHit hit;

            Debug.DrawRay(node.worldPosition + (Vector3.up * (nodeSize.y / 2)), Vector3.down * nodeSize.y, Color.green, 15);

            if (Physics.Raycast(node.worldPosition + (Vector3.up * (nodeSize.y/2)), Vector3.down, out hit, nodeSize.y /*, 1 << LayerMask.NameToLayer("Floor")*/))
            {
                //node.worldPosition.y = hit.point.y;

                //int layerMask = 1 << LayerMask.NameToLayer("Obstacle");
                //layerMask += 1 << LayerMask.NameToLayer("Floor");

                Vector3 boxSize = (nodeSize/2) * minimumExcess;

                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                if (!Physics.CheckBox(node.worldPosition, boxSize, rotation, 1 << LayerMask.NameToLayer("Obstacle")))
                {
                    pathable = true;
                }
                else 
                {
                    Debug.Log("Hit Obstacle " + node.gridPosition);
                }
            }
            else 
            {
                Debug.Log("Didn't Hit Floor " + node.gridPosition);
            }

            node.SetPathable(pathable);
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
            if (grid != null && grid.Count > 0)
            {
                Vector3 size = (nodeSize * 0.9f);
                size.Scale(transform.lossyScale);
                foreach (GridNode item in grid)
                {
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

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(worldBottomLeft, 0.1f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(worldTopRight, 0.1f);

            }
        }

        private void OnValidate()
        {
            if (CREATE_GRID)
            {
                CREATE_GRID = false;
                Start();
            }
        }
    }
}
