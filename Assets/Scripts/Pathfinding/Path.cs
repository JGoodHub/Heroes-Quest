using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path {

    //-----VARIABLES-----
    
    public List<Vertex> vertices = new List<Vertex>();
    public List<Edge> edges = new List<Edge>();

    public float length;
    
    //-----METHODS-----

    /// <summary>
    /// Calculate all the edges using the vertices
    /// </summary>
    public void CalculateEdges () {
        edges.Clear();
        for (int i = 0; i < vertices.Count - 1; i++) {
            foreach (Edge e in vertices[i].incidentEdges) {
                if (e.GetOppositeVertex(vertices[i]) == vertices[i + 1]) {
                    edges.Add(e);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Reverse the order of the paths vertices
    /// </summary>
    public void ReversePath () {
        vertices.Reverse();
    }

    /// <summary>
    /// Get the length of the path
    /// </summary>
    public void CalculatePathLength () {
        length = 0;
        foreach (Edge e in edges) {
            length += e.weight;
        }
    }

    /// <summary>
    /// Trim the path by a set number of vertices
    /// </summary>
    /// <param name="verticesToRemove"></param>
    public void TrimPath (int verticesToRemove) {
        while (vertices.Count > 0 && verticesToRemove > 0) {
            vertices.RemoveAt(vertices.Count - 1);            
            verticesToRemove--;
        }

        CalculateEdges();
        CalculatePathLength();
    }
    
}
