using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path {

    //-----VARIABLES-----
    
    private List<Vertex> vertices = new List<Vertex>();
    public List<Vertex> Vertices { get => vertices;  }

    private List<Edge> edges = new List<Edge>();
    public List<Edge> Edges { get => edges; }

    private float length;
    public float Length { get => length; }
    
    //-----METHODS-----

    /// <summary>
    /// 
    /// </summary>
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
    /// <summary>
    /// 
    /// </summary>
    public void ReversePath () {
        vertices.Reverse();
    }

    //Get the length of the path
    /// <summary>
    /// 
    /// </summary>
    public void CalculatePathLength () {
        length = 0;
        foreach (Edge e in edges) {
            length += e.weight;
        }
    }

    //Trim the path by a set number of vertices
    /// <summary>
    /// 
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
