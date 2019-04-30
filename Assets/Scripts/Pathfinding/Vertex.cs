using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex {

    //-----VARIABLES-----

    public Vector2Int graphCoordinates;
    public Vector3 worldPosition;
    public HashSet<Edge> incidentEdges = new HashSet<Edge>();
    
    public Graph parentGraph;
    public bool blocked;

    //-----METHODS-----

    /// <summary>
    /// Set up the coordinates and graph reference of the vertex
    /// </summary>
    /// <param name="graphCoordinates">The coordinates of the vertex</param>
    /// <param name="parentGraph">The graph the vertex belongs to</param>
    public Vertex (Vector2Int graphCoordinates, Graph parentGraph) {
        this.graphCoordinates = graphCoordinates;
        this.parentGraph = parentGraph;
    }

    /// <summary>
    /// Set the position of th vertex in world space
    /// </summary>
    /// <param name="worldPosition"></param>
    public void SetWorldPosition (Vector3 worldPosition) {
        this.worldPosition = worldPosition;
    }

    /// <summary>
    /// Get the neighboring vertices of the vertex
    /// </summary>
    /// <returns>A set of neighboring vertices</returns>
    public HashSet<Vertex> GetNeighbouringVertices () {
        HashSet<Vertex> neighboringVertices = new HashSet<Vertex>();
        foreach (Edge e in incidentEdges) {
            neighboringVertices.Add(e.GetOppositeVertex(this));
        }
        return neighboringVertices;
    }

    /// <summary>
    /// Get the vertex as a string
    /// </summary>
    /// <returns>The vertex as a string</returns>
    public override string ToString () {
        return "(" + graphCoordinates.x + ", " + graphCoordinates.y + ")";
    }    

}
