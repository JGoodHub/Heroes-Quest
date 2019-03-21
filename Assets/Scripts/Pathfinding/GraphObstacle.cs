using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphObstacle : MonoBehaviour {

    private Vertex currentVertex;
    public Vertex CurrentVertex { get => currentVertex; set => currentVertex = value; }

    public void CalculateCurrentVertex () {
        currentVertex = PathManager.instance.GetClosestVertexToCoordinates(PathManager.TranslateWorldSpaceToGraphCoordinates(transform.position));
    }

    public void BlockCurrentVertex () {
        currentVertex.blocked = true;
    }

    public void UnblockCurrentVertex () {
        currentVertex.blocked = false;
    }

    public void SetCurrentVertex (Vertex newCurrentVertex) {
        currentVertex = newCurrentVertex;
    }

}
