using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static PathManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    private List<Graph> graphs = new List<Graph>();

    private HashSet<Vertex> interGraphVertices = new HashSet<Vertex>();
    private Dictionary<Vector2Int, Vertex> globalVertexDictionary = new Dictionary<Vector2Int, Vertex>();

    //-----METHODS-----

    /// <summary>
    /// Setup method to initialise the PathManager object
    /// </summary>
    public void Initialise () {
        globalVertexDictionary.Clear();
        foreach (Vertex v in GetAllVerticesInScene()) {
            globalVertexDictionary.Add(v.graphCoordinates, v);
        }

		//Block all obstacle vertices on the graph
		foreach (GameObject obstacleObject in FindObjectsOfType<GameObject>()) {
            //Check if the object is on the graph obstacles layer
			if (obstacleObject.layer == 10) {
                try {
                    GraphObstacle obstacleInstance = obstacleObject.GetComponent<GraphObstacle>();
                    obstacleInstance.CalculateCurrentVertex();
                    obstacleInstance.BlockCurrentVertex();
                } catch (Exception e) {
                    Debug.LogError("\"" + obstacleObject.name + "\" tagged as an Obstacle but no component found, check tag and scripts applied");
                    Debug.LogError(e);
                }
            }
		}
    }

    /// <summary>
    /// Creates a graph from a 2D array of tile game objects
    /// </summary>
    /// <param name="tileObjectGrid">2D Array of tile game objects</param>
    /// <returns>A Graph representing the walkable spaces in the tile grid</returns>
    public Graph AddGraphFromTileGrid (GameObject[,] tileObjectGrid) {
        //The new graph object
        Graph graph = new Graph();
        graphs.Add(graph);

        //Create a path map from the tile object grid
        //This is a 2D array where a 1 represents a walkable space and a 0 not walkable
        #region 2D Array Creation
        int[,] walkableMap = new int[tileObjectGrid.GetLength(0) * 2, tileObjectGrid.GetLength(1) * 2];
        for (int z = 0; z < tileObjectGrid.GetLength(1); z++) {
            for (int x = 0; x < tileObjectGrid.GetLength(0); x++) {
                int mapX = x * 2;
                int mapZ = z * 2;

                if (tileObjectGrid[x, z] != null) {
                    //From each time grab the 2D array represention of which of its space are walkable accouting for rotation
                    TileData tileData = tileObjectGrid[x, z].GetComponent<TileData>();
                    int[,] tileGridMatrix = tileData.ConvertWalkableSpacesTo2DDisplacementArray();

                    //Insert the tiles space array into the maps 2D array
                    for (int tileZ = 0; tileZ <= 1; tileZ++) {
                        for (int tileX = 0; tileX <= 1; tileX++) {
                            walkableMap[mapX + tileX, mapZ + tileZ] = tileGridMatrix[tileX, tileZ];
                        }
                    }
                } else {
                    for (int tileZ = 0; tileZ <= 1; tileZ++) {
                        for (int tileX = 0; tileX <= 1; tileX++) {
                            walkableMap[mapX + tileX, mapZ + tileZ] = -1;
                        }
                    }
                }
            }
        }
        #endregion

        #region Graph Creation
        //Using the path map create a graph with a vertex at each 1 element and edges to adjacent walkable spaces
        for (int z = 0; z < walkableMap.GetLength(1); z++) {
            for (int x = 0; x < walkableMap.GetLength(0); x++) {
                if (walkableMap[x, z] >= 0) {
                    //Create a new vertex and add it to the graph
                    Vertex newVertex = new Vertex(new Vector2Int (x, z), graph);
                    newVertex.SetWorldPosition(TranslateGraphCoordinatesToWorldSpace(new Vector2Int(x, z), ((graphs.Count - 1) * 10) + walkableMap[x, z]));
                    graph.vertices.Add(newVertex);
                    newVertex.parentGraph = graph;

                    if (walkableMap[x, z] >= 10) {
                        interGraphVertices.Add(newVertex);
                    }

                    foreach (Vertex vertex in GetAllVerticesInScene()) {
                        if (globalVertexDictionary.ContainsKey(vertex.graphCoordinates) == false) {
                            globalVertexDictionary.Add(newVertex.graphCoordinates, newVertex);
                        }
                    }

                    //Create edges between the current vertex and those above it
                    for (int xOffset = -1; xOffset < 2; xOffset++) {
                        if (x + xOffset >= 0 && x + xOffset < walkableMap.GetLength(0) && z > 0) {
                            //Check if the offset path map space is walkable and create and edge between it and the current vertex
                            if (walkableMap[x + xOffset, z - 1] >= 0) {
                                Edge e = new Edge(newVertex, GetClosestVertexToCoordinates(new Vector2Int(x + xOffset, z - 1)));
                                graph.edges.Add(e);
                                e.parentGraph = graph;
                            }
                        }
                    }

                    //Create an edge to the vertex left of the current one
                    if (x > 0 && walkableMap[x - 1, z] >= 0) {
                        Edge newEdge = new Edge(newVertex, GetClosestVertexToCoordinates(new Vector2Int(x - 1, z)));
                        graph.edges.Add(newEdge);
                        newEdge.parentGraph = graph;
                    }
                }
            }
        }

        //Calculate the normalised weights for each of the edges, then times by 5 to represent 5 feet per tile
        foreach (Edge edge in graph.edges) {
            edge.weight = Vector2Int.Distance(edge.vertices[0].graphCoordinates, edge.vertices[1].graphCoordinates) * 5f;
        }
        #endregion
        
        return graph;
    }

    /// <summary>
    /// Connects the different level graphs using the inter graph vertices on certain tiles
    /// </summary>
    public void FormInterGraphEdges () {
        foreach (Vertex v in interGraphVertices) {
            Graph targetGraph = GetClosestGraphToYPosition(v.worldPosition.y);
            foreach (Vertex adjV in GetVerticesInRange(v, 1.5f, false)) {
                Edge e = new Edge(v, adjV);
                e.weight = Vector2Int.Distance(v.graphCoordinates, adjV.graphCoordinates) * 5f;
                targetGraph.edges.Add(e);
                e.parentGraph = targetGraph;                         
            }
            
            HashSet<Edge> edgesToRemove = new HashSet<Edge>();
            foreach (Edge e in v.incidentEdges) {
                if (e.weight > 5) {
                    edgesToRemove.Add(e);
                }
            }

            foreach (Edge edge in edgesToRemove) {
                edge.vertices[0].incidentEdges.Remove(edge);
                edge.vertices[1].incidentEdges.Remove(edge);
                edge.parentGraph.edges.Remove(edge);
            }
        }
    }

    /// <summary>
    /// Converts graph coordinates into world space coordinates
    /// </summary>
    /// <param name="coordinates">The graph coordinates to convert</param>
    /// <param name="yOffset">Y offset to set for the resulting Vector3</param>
    /// <returns>Vector3 representing the coordinate in world space</returns>
    public static Vector3 TranslateGraphCoordinatesToWorldSpace (Vector2Int coordinates, float yOffset) {
        return new Vector3(coordinates.x * 5f, yOffset, -coordinates.y * 5f);
    }

    /// <summary>
    /// Converts world space coordinates into graph coordinates
    /// </summary>
    /// <param name="worldPos">The world position to convert</param>
    /// <returns>A Vector2Int representing the x and y graph coordinates</returns>
    public static Vector2Int TranslateWorldSpaceToGraphCoordinates (Vector3 worldPos) {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / 5f), Mathf.RoundToInt(-worldPos.z / 5f));
    }

    /// <summary>
    /// Find the closest vertex to the coordinates passed
    /// </summary>
    /// <param name="coordinates">Coordinates to search from</param>
    /// <returns>The nearest Vertex to the coordinates passed</returns>
    public Vertex GetClosestVertexToCoordinates (Vector2Int coordinates) {
        Vertex closestVertex = null;
        float closestDistance = 0;

        //Return the Vertex as the coordinate position
        if (globalVertexDictionary.ContainsKey(coordinates)) {
            return globalVertexDictionary[coordinates];
        } else {
            //Test each vertex and return the one that's the least distance away
            foreach (Vertex vertex in globalVertexDictionary.Values) {
                float newDist = Vector2.Distance(coordinates, vertex.graphCoordinates);
                if (closestVertex == null || newDist < closestDistance) {
                    closestVertex = vertex;
                    closestDistance = newDist;
                }            
            }

            return closestVertex;
        }
    }

    /// <summary>
    /// Find the closest level graph to a given Y value
    /// </summary>
    /// <param name="yPosition">The Y value to search</param>
    /// <returns>The nearest Graph to the Y value</returns>
    public Graph GetClosestGraphToYPosition (float yPosition) {
        int graphIndex = Mathf.RoundToInt(Mathf.Clamp(yPosition / 10f, 0f, graphs.Count - 1));

        return graphs[graphIndex];
    }
    
    /// <summary>
    /// Get all the vertices within the Euclidean range of the start Vertex's coordinates
    /// </summary>
    /// <param name="start">The starting Vertex to search from</param>
    /// <param name="range">The distance range to check in</param>
    /// <param name="includeStart">Boolean of whether the start Vertex should be included in the returned collection</param>
    /// <returns>A HashSet of vertices containing all vertices within range of the start</returns>
    public HashSet<Vertex> GetVerticesInRange (Vertex start, float range, bool includeStart) {
		HashSet<Vertex> verticesWithinRange = new HashSet<Vertex>();

        foreach (Vertex v in globalVertexDictionary.Values) {
            if (Vector2Int.Distance(start.graphCoordinates, v.graphCoordinates) <= range) {
                verticesWithinRange.Add(v);
            }
        }

        if (includeStart == false) {
            verticesWithinRange.Remove(start);
        }
        
		return verticesWithinRange;
	}

    /// <summary>
    /// Get all vertices across all graphs
    /// </summary>
    /// <returns>A collection containing all vertices in the scene</returns>
    public HashSet<Vertex> GetAllVerticesInScene () {
        HashSet<Vertex> vertexCollection = new HashSet<Vertex>();
        foreach (Graph g in graphs) {
            vertexCollection.UnionWith(g.vertices);
        }
        return vertexCollection;
    }

    /// <summary>
    /// Calculate the shortest weighted path between a source and target vertex using the A* search algorithm
    /// </summary>
    /// <param name="source">The source Vertex</param>
    /// <param name="target">the target Vertex</param>
    /// <returns>A Path object representing the shortest path or null if no path exists</returns>
    public Path FindShortestPathBetween (Vertex source, Vertex target) {
        //Check that the prerequisites of the algorithm are not null 
        if (graphs == null || source == null || target == null) {
            if (graphs == null) {
                Debug.LogError("Attempting to find path when the graph is null");
                return null;
            } else {
                Debug.LogError("Attempting to find path where either vertex is null");
                return null;
            }
        } else if (source == target) {
            //Debug.LogError("Attempting to find a path where source and target are the same");
            return null;
        } else {
            Path path = new Path();
            HashSet<Vertex> unexploredVertices = new HashSet<Vertex>();

            //The previous vertex (value) in the shortest path to reach that vertex (key)
            Dictionary<Vertex, Vertex> previousVertexInPath = new Dictionary<Vertex, Vertex>();

            //Stores the edge distance from the source to that vertex and the euclidean distance from that vertex to the target
            Dictionary<Vertex, float> edgeDist = new Dictionary<Vertex, float>();
            Dictionary<Vertex, float> euclideanDist = new Dictionary<Vertex, float>();
            
            //Set each vertex to unexplored with an infinite distance and calculate it's euclidean distance to the target
            foreach (Vertex vertex in globalVertexDictionary.Values) {
                if (vertex == source || vertex == target || vertex.blocked == false) {
                    edgeDist.Add(vertex, 1000000);
                    euclideanDist.Add(vertex, Vector2Int.Distance(vertex.graphCoordinates, target.graphCoordinates));                
                    unexploredVertices.Add(vertex);
                }             
            }

            //Source to source distance is zero
            edgeDist[source] = 0;
            
            Vertex currentVertex = source;
            while (unexploredVertices.Count > 0 && currentVertex != target) {
                //Find the unexplored vertex that has the lowest combined edge and euclidean distance
                currentVertex = null;
                foreach (Vertex v in unexploredVertices) {
                    if (currentVertex == null || (edgeDist[v] + euclideanDist[v] < edgeDist[currentVertex] + euclideanDist[currentVertex])) {
                        currentVertex = v;
                    }
                }

                //Performence TODO ^, try checking the vertices disocvered on the last loop rather than searching through every vertex to find the best ^

                unexploredVertices.Remove(currentVertex);

                //Iterate through each vertex that shares an edge with the current one
                //Calculate the distance of the neighbour vertex when going through the current one
                //If this new path is shorter set the current vertex as the previous one in the 
                //shortest path to the neighbour
                foreach (Edge edgeToNeighbour in currentVertex.incidentEdges) {
                    Vertex neighbourVertex = edgeToNeighbour.GetOppositeVertex(currentVertex);
                    if (neighbourVertex == target || neighbourVertex.blocked == false) {
                        float alternatePathToNeighbour = edgeDist[currentVertex] + edgeToNeighbour.weight;
                        if (alternatePathToNeighbour < edgeDist[neighbourVertex]) {
                            edgeDist[neighbourVertex] = alternatePathToNeighbour;
                            if (previousVertexInPath.ContainsKey(neighbourVertex)) {
                                previousVertexInPath[neighbourVertex] = currentVertex;
                            } else {
                                previousVertexInPath.Add(neighbourVertex, currentVertex);
                            }
                        }
                    }
                }
            }
            
            if (currentVertex == target) {
                //Backtrack using the previousVertexInPath to find the shortest path from target to source
                while (currentVertex != source && previousVertexInPath.ContainsKey(currentVertex)) {
                    path.vertices.Insert(0, currentVertex);
                    currentVertex = previousVertexInPath[currentVertex];
                }

                if (currentVertex == source) {
                    path.vertices.Insert(0, source);
                                        
                    //If the final vertex is blocked, remove it from the path
                    if (target.blocked == true) {
                        path.TrimPath(1);
                    }

                    path.CalculateEdges();
                    path.CalculatePathLength();
                    return path;
                } else {
                    Debug.Log("Not path could be found");
                    return null;
                }
            } else {
                return null;
            }
        }
    }

    //-----GIZMOS-----
    [Header("Gizmo Toggles")]
    public bool drawVertices;
    public bool drawEdges;
    /// <summary>
    /// Draw the path managers graphs as gizmos in the editor window
    /// </summary>
    void OnDrawGizmos () {
        Gizmos.color = Color.white;
        if (Application.isPlaying) {
            for (int i = 0; i < graphs.Count; i++) {
                Vector3 yOffset = new Vector3(0f, 0.1f, 0f);

                if (drawEdges) {
                    Gizmos.color = Color.blue;
                    foreach (Edge e in graphs[i].edges) {
                        Gizmos.DrawLine(e.vertices[0].worldPosition + yOffset, e.vertices[1].worldPosition + yOffset);
                    }               
                }

                if (drawVertices) {
                    Gizmos.color = Color.red;
                    foreach (Vertex v in graphs[i].vertices) {
                        Gizmos.DrawCube(v.worldPosition + yOffset, Vector3.one * 0.5f);
                    }
                }
            }
        }
    }

}
