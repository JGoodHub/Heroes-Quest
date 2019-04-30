using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphObstacle : MonoBehaviour {

    //-----VARIABLES-----

    public Vertex currentVertex;

    //-----METHODS-----

    /// <summary>
    /// Calculate the current vertex the obstacle is on top of
    /// </summary>
    public void CalculateCurrentVertex () {
        currentVertex = PathManager.instance.GetClosestVertexToCoordinates(PathManager.TranslateWorldSpaceToGraphCoordinates(transform.position));
    }

    /// <summary>
    /// Mark the current vertex as blocked
    /// </summary>
    public void BlockCurrentVertex () {
        currentVertex.blocked = true;
    }

    /// <summary>
    /// Unblock the current vertex
    /// </summary>
    public void UnblockCurrentVertex () {
        currentVertex.blocked = false;
    }
    
    /// <summary>
    /// Manually set the current vertex
    /// </summary>
    /// <param name="newCurrentVertex">The next current vertex</param>
    public void SetCurrentVertex (Vertex newCurrentVertex) {
        currentVertex = newCurrentVertex;
    }

}
