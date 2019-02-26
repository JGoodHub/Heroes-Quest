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

    //Add a new vertex
    public void AddVertex (Vertex newVertex) {
        vertices.Add(newVertex);
    }

    //Remove an old vertex
    public void RemoveVertex (Vertex oldVertex) {
        vertices.Remove(oldVertex);
    }

    //Add a new edge
    public void AddEdge (Edge newEdge) {
        edges.Add(newEdge);        
    }

    //Remove an old edge
    public void RemoveEdge (Edge oldEdge) {
        edges.Remove(oldEdge);
    }

    //Find the closest vertex to the coordinates passed
    public Vertex GetClosestVertexToCoordinates (Vector2Int coordinates) {
        Vertex closestVertex = null;
        float closestDistance = 0;
        
        //Test each vertex and return the one that's the least distance away
        foreach (Vertex v in vertices) {
            float newDist = Vector2.Distance(coordinates, v.GraphCoordinates);
            if (closestVertex == null || newDist < closestDistance) {
                closestVertex = v;
                closestDistance = newDist;
            }            
        }
        return closestVertex;
    }

    public HashSet<Vertex> GetVerticesInRange (Vertex start, float tileRange, bool includeStartVertex) {
		HashSet<Vertex> verticesWithinRange = new HashSet<Vertex>();

        foreach (Vertex v in vertices) {
            if (Vector2Int.Distance(start.GraphCoordinates, v.GraphCoordinates) <= tileRange) {
                verticesWithinRange.Add(v);
            }
        }

        if (includeStartVertex == false) {
            verticesWithinRange.Remove(start);
        }
        
		return verticesWithinRange;
	}

}
