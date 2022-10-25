using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace QAI_Pathfinding 
{
    public class GridManager : MonoBehaviour
    {
        public float updateInterval;
    
        internal List<NavGrid> grids = new List<NavGrid>();
        internal List<JobHandle> jobs = new List<JobHandle>();
    
        private float updateTimer;
        private int nextUpdateIndex = 0;

        private bool canUpdate = true;
    
        // Start is called before the first frame update
        void Start()
        {
            grids.AddRange(GetComponentsInChildren<NavGrid>());
        }
    
        // Update is called once per frame
        void Update()
        {
            if (updateTimer > 0)
            {
                updateTimer -= Time.deltaTime;
            }
    
            if (updateTimer <= 0)
            {
                updateTimer = updateInterval;
    
                if (grids[nextUpdateIndex].gameObject.activeInHierarchy)
                {
                    grids[nextUpdateIndex].UpdateGrid();
                    Debug.Log("updating Grid");
                }
    
                nextUpdateIndex++;
                if (nextUpdateIndex >= grids.Count)
                {
                    nextUpdateIndex = 0;
                }
            }
        }

        IEnumerator UpdateRoutine()
        {

            yield return null;
        }
    }
}
