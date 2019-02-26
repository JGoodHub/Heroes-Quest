using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path {

    //-----VARIABLES-----
    
    private List<Vertex> vertices = new List<Vertex>();
    public List<Vertex> Vertices {
        get { return vertices; }
    }

    private List<Edge> edges = new List<Edge>();
    public List<Edge> Edges {
        get { return edges; }
    }
    
    //-----METHODS-----

    public void CalculateEdges () {
        edges.Clear();
        for (int i = 0; i < vertices.Count - 1; i++) {
            foreach (Edge e in vertices[i].IncidentEdges) {
                if (e.GetOppositeVertex(vertices[i]) == vertices[i + 1]) {
                    edges.Add(e);
                    break;
                }
            }
        }
    }

    //Reverse the order of the paths vertices
    public void ReversePath () {
        vertices.Reverse();
    }

    //Get the length of the path
    public float GetPathLength () {
        float weightCount = 0;
        foreach (Edge e in edges) {
            weightCount += e.weight;
        }
        return weightCount;
    }

    //Trim the path by a set number of vertices
    public void TrimPath (int verticesToRemove) {
        while (verticesToRemove > 0) {
            vertices.RemoveAt(vertices.Count - 1);
            edges.RemoveAt(edges.Count - 1);
            verticesToRemove--;
        }
    }



    
}
