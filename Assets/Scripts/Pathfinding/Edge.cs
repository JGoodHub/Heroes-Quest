using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge {

    //-----VARIABLES-----

    public Vertex[] vertices = new Vertex[2];
    public float weight;
    public Graph parentGraph;

    //-----METHODS-----

    /// <summary>
    /// Constructed from two vertex's
    /// </summary>
    /// <param name="vertexA">Vertex A</param>
    /// <param name="vertexB">Vertex B</param>
    public Edge (Vertex vertexA, Vertex vertexB) {
        vertices[0] = vertexA;
        vertices[1] = vertexB;

        //Add the edge to the edge collections of each vertex
        vertexA.incidentEdges.Add(this);
        vertexB.incidentEdges.Add(this);

        //Throw an error if both vertices are the same
        if (vertexA.Equals(vertexB)) {
            Debug.LogError("Edge created where both vertices are the same");
        }
    }

    /// <summary>
    /// Get the opposite edge to that passed
    /// </summary>
    /// <param name="v">The vertex to get the opposite of</param>
    /// <returns>The opposite vertex or null if none</returns>
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
