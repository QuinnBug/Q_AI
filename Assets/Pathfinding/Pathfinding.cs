using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace QAI_Pathfinding 
{
    public class Pathfinding : MonoBehaviour
    {
        #region Pathfinding Variables
    public enum Facing
    {
        NONE = 0,
        LEFT = 1,
        RIGHT = 2,
        UP = 3,
        DOWN = 4
    }

    public float pathUpdateRate = 0.05f;
    [Space]
    public float minDistanceToNode = 0.05f;
    public float endDistanceToTarget = 0.05f;
    public float maxStepDistance = 0.1f;
    public float maxStraightLineDistance = 3;
    public float testBoxWidth = 0.05f;
    [Space]
    public NavGrid navGrid;
    public BoxCollider testBox;
    [Space]
    public GameObject targetObj;

    private Facing direction;

    private bool findingPath = false;
    private List<GridNode> currentRoute = new List<GridNode>();
    private Vector3Int currentGridPosition;

    [SerializeField]
    internal Vector3 target;
    internal Vector3 posToMoveTo;
    internal bool canSeeTarget;
    [SerializeField]
    internal bool targetObjOnGrid;
    internal float distanceToTarget;

    #endregion
    
        public void Start()
        {
            //targetObj = GameObject.FindGameObjectWithTag("Player");
            target = transform.position;
            findingPath = false;
        }
    
        public void ValuesUpdate()
        {
            //define the target here
            if (targetObj != null && IsPosOnNavGrid(targetObj.transform.position))
            {
                target = navGrid.NodeFromWorld(targetObj.transform.position).worldPosition;
                targetObjOnGrid = true;
            }
            else
            {
                targetObjOnGrid = false;
            }
    
            canSeeTarget = CheckLineToTarget(target);
            distanceToTarget = Vector3.Distance(transform.position, target);
        }
    
        public void PathingUpdate()
        {
            currentGridPosition = navGrid.NodeFromWorld(transform.position).gridPosition;
            UpdateRoute(target);

            transform.rotation = UpdateDirection(currentRoute[0].worldPosition);
        }
    
        public bool IsPosOnNavGrid(Vector3 pos)
        {
            //return navGrid.NodeFromWorld(pos) != null;
            return  true;
        }
    
        private void UpdateRoute(Vector3 targetPosition)
        {
            if (!targetObjOnGrid)
            {
                return;
            }
    
            if (canSeeTarget && distanceToTarget < maxStraightLineDistance)
            {
                posToMoveTo = targetPosition;
                currentRoute.Clear();
            }
            else if (!findingPath)
            {
                StartCoroutine(FindPath(navGrid.NodeFromWorld(targetPosition).gridPosition));
            }
    
            if (currentRoute.Count > 1) //2 or more points
            {
                if (Vector3.Distance(transform.position, currentRoute[0].worldPosition) <= minDistanceToNode)
                {
                    currentRoute.RemoveAt(0);
                }
    
                posToMoveTo = currentRoute[0].worldPosition;
            }
            else
            {
                posToMoveTo = transform.position;
            }
        }
    
        internal Quaternion UpdateDirection(Vector3 targetPosition)
        {
            Vector3 posYZero = new Vector3(transform.position.x, 0, transform.position.z);
    
            if (targetObj != null)
            {
                posYZero.y = targetObj.transform.position.y;
                return Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetObj.transform.position - posYZero, Vector3.up), 5 * Time.deltaTime);
            }
            else if (currentRoute.Count > 0)
            {
                return Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentRoute[0].worldPosYZero() - posYZero, Vector3.up), 5 * Time.deltaTime);
            }
    
            return transform.rotation;
        }
    
        private float DistanceFrom(Vector3Int start, Vector3Int end)
        {
            int dstX = Mathf.Abs(end.x - start.x);
            int dstY = Mathf.Abs(end.y - start.y);
            int dstZ = Mathf.Abs(end.z - start.z);
    
            return Mathf.Sqrt((dstX * dstX) + (dstY * dstY) + (dstZ * dstZ));
        }
    
        IEnumerator FindPath(Vector3Int targetV3)
        {
            int groundMask = 1 << LayerMask.NameToLayer("Floor");
            groundMask = groundMask & 1 << LayerMask.NameToLayer("Obstacle");
    
            findingPath = true;
    
            List<GridNode> open = new List<GridNode>();
            HashSet<GridNode> closed = new HashSet<GridNode>();
    
            GridNode targetNode = navGrid.NodeFromGridSpace(targetV3);
            GridNode startNode = navGrid.NodeFromGridSpace(currentGridPosition);
            GridNode closestNode = navGrid.NodeFromGridSpace(currentGridPosition);
            GridNode currentNode = navGrid.NodeFromGridSpace(currentGridPosition);
    
            List<GridNode> neighbours = new List<GridNode>();
            float newMovementCostToNeighbour = 0;
    
            open.Add(startNode);
    
            while (open.Count > 0)
            {
                currentNode = open[0];
    
                for (int i = 0; i < open.Count; i++)
                {
                    if (open[i].fCost <= currentNode.fCost && open[i].hCost < currentNode.hCost)
                    {
                        currentNode = open[i];
                    }
                }
    
                if (currentNode.hCost < closestNode.hCost)
                {
                    closestNode = currentNode;
                }
    
                closed.Add(currentNode);
                open.Remove(currentNode);
    
                if (currentNode.gridPosition == targetNode.gridPosition)
                {
                    RetracePath(startNode, targetNode);
                    yield return new WaitForSeconds(pathUpdateRate);
                    findingPath = false;
                    yield break;
                }
    
                GetNeighbouringGridSpaces(currentNode, neighbours);

                for (int i = 0; i < neighbours.Count; i++)
                {
                    GridNode neighbourNode = neighbours[i];

                    if (closed.Contains(neighbourNode) ||
                        neighbourNode.pathable == false ||
                        neighbourNode.worldPosition.y > currentNode.worldPosition.y + maxStepDistance ||
                        Physics.Raycast(currentNode.worldPosition, neighbourNode.worldPosition - currentNode.worldPosition,
                            Vector3.Distance(currentNode.worldPosition, neighbourNode.worldPosition)))
                    {
                        if (neighbourNode.gridPosition != targetNode.gridPosition)
                        {
                            continue;
                        }
                    }
    
                    #region
                //if (currentNode.worldPosition.y > neighbourNode.worldPosition.y)
                //{
                //    RaycastHit hit;
                //    bool notSeeNode = Physics.Raycast(currentNode.worldPosition, neighbourNode.worldPosition - currentNode.worldPosition, out hit,
                //        Vector3.Distance(currentNode.worldPosition, neighbourNode.worldPosition));

                //    if (notSeeNode)
                //    {
                //        Debug.DrawLine(currentNode.worldPosition, hit.point, Color.green, 2);
                //    }
                //    else
                //    {
                //        Debug.DrawLine(currentNode.worldPosition, neighbourNode.worldPosition, Color.red, 2);
                //    }

                //}

                //Debug.DrawLine(currentNode.worldPosition, neighbourNode.worldPosition, Color.red, 2);
                #endregion
    
                    newMovementCostToNeighbour = currentNode.gCost + DistanceFrom(currentNode.gridPosition, neighbourNode.gridPosition);
    
                    //if XZ diagonal or higher, add cost to movement
                    if ((neighbourNode.gridPosition.x != currentNode.gridPosition.x && neighbourNode.gridPosition.z != currentNode.gridPosition.z)
                        || neighbourNode.worldPosition.y > currentNode.worldPosition.y)
                    {
                        newMovementCostToNeighbour += 0.5f;
                    }
    
                    newMovementCostToNeighbour += 0.2f * NonPathAdjacentCount(neighbourNode);
    
                    if (newMovementCostToNeighbour < neighbourNode.gCost || !open.Contains(neighbourNode))
                    {
                        neighbourNode.gCost = newMovementCostToNeighbour;
                        neighbourNode.hCost = DistanceFrom(neighbourNode.gridPosition, targetV3);
                        neighbourNode.parentPos = currentNode.gridPosition;
    
                        if (!open.Contains(neighbourNode))
                        {
                            open.Add(neighbourNode);
                        }
    
                        if (neighbourNode.gridPosition == targetNode.gridPosition)
                        {
                            RetracePath(startNode, targetNode);
                            yield return new WaitForSeconds(pathUpdateRate);
                            findingPath = false;
                            yield break;
                        }
                    }
                }
            }
    
            if (Vector3.Distance(closestNode.worldPosition, targetNode.worldPosition) <= 10 /*0.1f*/)
            {
                Debug.Log(Vector3.Distance(closestNode.worldPosition, target) + " closest point distance");
                RetracePath(startNode, closestNode);
            }
            else
            {
                Debug.Log("can't reach position");
                currentRoute.Clear();
            }
    
    
            yield return new WaitForSeconds(pathUpdateRate);
            findingPath = false;
            yield break;
        }
    
        private List<GridNode> adjNeighbours = new List<GridNode>();
        private int NonPathAdjacentCount(GridNode node)
        {
            int adjacent = 0;
            GetNeighbouringGridSpaces(node, adjNeighbours);
            foreach (GridNode neighbour in adjNeighbours)
            {
                if (!neighbour.pathable)
                {
                    adjacent++;
                }
            }
    
            return adjacent;
        }
    
        private void RetracePath(GridNode startNode, GridNode targetNode)
        {
            GridNode currentNode = targetNode;
            currentRoute.Clear();
            while (currentNode.gridPosition != startNode.gridPosition)
            {
                currentRoute.Add(currentNode);
                currentNode = navGrid.NodeFromGridSpace(currentNode.parentPos);
            }
    
            currentRoute.Reverse();
            //SimplifyPath();
        }
    
        private void SimplifyPath()
        {
            int nodeCount = currentRoute.Count;
    
            if (nodeCount <= 1)
            {
                return;
            }
    
            bool hitSomething = false;
            bool cleanPath = true;
            int j = 1;
    
            RaycastHit hit;
            //int layerMask = ~(1 << LayerMask.NameToLayer("Player"));
            //layerMask = layerMask & ~(1 << LayerMask.NameToLayer("Ai"));
            int layerMask = 1 << LayerMask.NameToLayer("Floor");
            layerMask = layerMask & 1 << LayerMask.NameToLayer("Obstacle");
    
            while (!hitSomething && j < nodeCount - 1)
            {
                if (currentRoute[j].worldPosition.y >= transform.position.y + maxStepDistance || currentRoute[j].worldPosition.y <= transform.position.y - maxStepDistance)
                {
                    Debug.Log("oof");
                    j--;
                    break;
                }
    
                if (Physics.SphereCast(transform.position, testBoxWidth, currentRoute[j].worldPosition - transform.position, out hit, Vector3.Distance(transform.position, currentRoute[j].worldPosition), layerMask))
                {
                    hitSomething = true;
                    j--;
                    break;
                }
    
                j++;
            }
    
            Debug.Log(hitSomething);
    
            if (!hitSomething)
            {
                return;
            }
    
            //while the testBox contains the points of non pathable areas and we aren't on the next point in the room
            while (!cleanPath && j > 0)
            {
                cleanPath = true;
                foreach (GridNode node in navGrid.grid)
                {
                    Vector3 start = navGrid.NodeFromGridSpace(currentGridPosition).worldPosition;
                    Vector3 target = currentRoute[j].worldPosition;
    
                    Vector3 dir_1 = (target - start);
    
                    testBox.transform.position = start + (dir_1/2);
    
                    testBox.transform.rotation = Quaternion.LookRotation(dir_1, transform.up);
    
                    testBox.size = new Vector3(testBoxWidth, testBoxWidth, Vector3.Distance(testBox.transform.InverseTransformPoint(start), testBox.transform.InverseTransformPoint(target)));
    
                    //Debug.DrawLine(start, target, Color.green, 1.0f);
    
                    //if node is not pathable but it is in the bounding box then we go one node closer to the player and confirm that the path is not clean.
                    if (!node.pathable && PointInOABB(node, testBox))
                    {
                        j--;
                        cleanPath = false;
                        break;
                    }
                }
            }
    
            if (cleanPath)
            {
                Debug.Log("cleaning path " + currentRoute.Count);
                currentRoute.RemoveRange(0, j);
                Debug.Log(currentRoute.Count);
            }
            //Debug.Break();
    
        }
    
        bool PointInOABB(GridNode point, BoxCollider box)
        {
            return point.bounds.Intersects(box.bounds);
        }
    
        private void GetNeighbouringGridSpaces(GridNode node, List<GridNode> neighbours)
        {
            neighbours.Clear();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        //The diagonals - != for only straights, == 3 for only basic diagonals
                        if (Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z) == 3)
                        {
                            continue;
                        }
    
                        int checkX = node.gridPosition.x + x;
                        int checkY = node.gridPosition.y + y;
                        int checkZ = node.gridPosition.z + z;
    
                        // check if the grid space is on the grid
                        if (checkX >= 0 && checkX < navGrid.gridSize.x &&
                            checkY >= 0 && checkY < navGrid.gridSize.y &&
                            checkZ >= 0 && checkZ < navGrid.gridSize.z)
                        {
                            neighbours.Add(navGrid.Node(checkX, checkY, checkZ));
                        }
                    }
                }
            }
        }
    
        private bool CheckLineToTarget(Vector3 position)
        {
            int layerMask = 1 << LayerMask.NameToLayer("Obstacle");
    
            RaycastHit2D hit;
    
            if (hit = Physics2D.CircleCast(transform.position, testBoxWidth, position - transform.position, Vector3.Distance(transform.position, position), layerMask))
            {
                return false;
            }
    
            return true;
        }
    
        public void OnDrawGizmosSelected()
        {
            if (currentRoute == null)
            {
                return;
            }
    
            if (currentRoute.Count > 0)
            {
                foreach (GridNode node in currentRoute)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(node.worldPosition, 0.01f);
                }
            }
        }
    }
}
