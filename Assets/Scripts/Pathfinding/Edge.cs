using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge {

    //-----VARIABLES-----

    //Vertices the edge exists between
    private Vertex[] vertices = new Vertex[2];
    public Vertex[] Vertices {
        get {return vertices; }
    }

    //Weight property
    public float weight;

    public Graph parentGraph;
    
    //-----METHODS-----

    //Constructed from two vertex's
    public Edge (Vertex vertexA, Vertex vertexB) {
        vertices[0] = vertexA;
        vertices[1] = vertexB;

        //Add the edge to the edge collections of each vertex
        vertexA.IncidentEdges.Add(this);
        vertexB.IncidentEdges.Add(this);

        //Throw an error if both vertices are the same
        if (vertexA.Equals(vertexB)) {
            Debug.LogError("Edge created where both vertices are the same");
        }
    }

    //Get the opposite edge to that passed
    public Vertex GetOppositeVertex (Vertex v) {
        if (v == vertices[0]) {
            return vertices[1];
        } else if (v == vertices[1]) {            
            return vertices[0];
        } else {
            return null;
        }
    }    

}
