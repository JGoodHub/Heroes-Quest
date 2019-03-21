using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Graph {
    
    //-----VARIABLES-----

    //Collection of vertices
    private HashSet<Vertex> vertices = new HashSet<Vertex>();
    public HashSet<Vertex> Vertices {
        get { return vertices; }
    }

    //Collection of edges
    private HashSet<Edge> edges = new HashSet<Edge>();
    public HashSet<Edge> Edges {
        get { return edges; }
    }

    //-----METHODS-----

    

}
