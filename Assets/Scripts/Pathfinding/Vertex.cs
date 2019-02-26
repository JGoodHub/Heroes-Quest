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

    public bool blocked;

    //-----METHODS-----

    public Vertex (Vector2Int _coordinates) {
        this.graphCoordinates = _coordinates;
    }

    public Vertex (Vector2Int _coordinates, Vector3 _worldPosition) {
        this.graphCoordinates = _coordinates;
        this.worldPosition = _worldPosition;
    }

    public void AddIncidentEdges (Edge e) {
        incidentEdges.Add(e);
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
