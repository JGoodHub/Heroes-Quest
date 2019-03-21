using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex {

    //-----VARIABLES-----

    private Vector2Int graphCoordinates;
    public Vector2Int GraphCoordinates {
        get { return graphCoordinates; }
    }

    private Vector3 worldPosition;
    public Vector3 WorldPosition {
        get { return worldPosition; }
    }
    
    private HashSet<Edge> incidentEdges = new HashSet<Edge>();
    public HashSet<Edge> IncidentEdges {
        get {return this.incidentEdges; }
    }

    public Graph parentGraph;

    public bool blocked;

    //-----METHODS-----

    public Vertex (Vector2Int _coordinates, Graph _parentGraph) {
        this.graphCoordinates = _coordinates;
        this.parentGraph = _parentGraph;
    }

    public void SetWorldPosition (Vector3 _worldPosition) {
        this.worldPosition = _worldPosition;
    }

    public HashSet<Vertex> GetNeighbouringVertices () {
        HashSet<Vertex> neighboringVertices = new HashSet<Vertex>();
        foreach (Edge e in incidentEdges) {
            neighboringVertices.Add(e.GetOppositeVertex(this));
        }
        return neighboringVertices;
    }

    public override string ToString () {
        return "(" + graphCoordinates.x + ", " + graphCoordinates.y + ")";
    }    

}
