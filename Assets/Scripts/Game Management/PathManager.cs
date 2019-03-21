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

    //The graph representing the map
    private List<Graph> graphs = new List<Graph>();
    public List<Graph> Graphs {
        get { return this.graphs; }
    }

    private HashSet<Vertex> interGraphVertices = new HashSet<Vertex>();
    private HashSet<Vertex> globalVertexCollection = new HashSet<Vertex>();

    private GraphObstacle[] graphObstacles;

    //-----METHODS-----

    //Setup method
    public void Initialise () {
        globalVertexCollection = GetAllVerticesInScene();

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

    //Create a graph from the tile grid
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
                    Vertex v = new Vertex(new Vector2Int (x, z), graph);
                    v.SetWorldPosition(TranslateGraphCoordinatesToWorldSpace(new Vector2Int(x, z), ((graphs.Count - 1) * 10) + walkableMap[x, z]));
                    graph.Vertices.Add(v);
                    v.parentGraph = graph;

                    if (walkableMap[x, z] >= 10) {
                        interGraphVertices.Add(v);
                    }

                    globalVertexCollection = GetAllVerticesInScene();

                    //Create edges between the current vertex and those above it
                    for (int xOffset = -1; xOffset < 2; xOffset++) {
                        if (x + xOffset >= 0 && x + xOffset < walkableMap.GetLength(1) && z > 0) {
                            //Check if the offset path map space is walkable and create and edge between it and the current vertex
                            if (walkableMap[x + xOffset, z - 1] >= 0) {
                                Edge e = new Edge(v, GetClosestVertexToCoordinates(new Vector2Int(x + xOffset, z - 1)));
                                graph.Edges.Add(e);
                                e.parentGraph = graph;
                            }
                        }
                    }

                    //Create an edge to the vertex left of the current one
                    if (x > 0 && walkableMap[x - 1, z] >= 0) {
                        Edge e = new Edge(v, GetClosestVertexToCoordinates(new Vector2Int(x - 1, z)));
                        graph.Edges.Add(e);
                        e.parentGraph = graph;
                    }
                }
            }
        }

        //Calculate the weights for each of the edges
        foreach (Edge e in graph.Edges) {
            e.weight = Vector2Int.Distance(e.Vertices[0].GraphCoordinates, e.Vertices[1].GraphCoordinates);
        }
        #endregion
        
        return graph;
    }

    public void FormInterGraphEdges () {
        foreach (Vertex v in interGraphVertices) {
            Graph targetGraph = GetClosestGraphToYPosition(v.WorldPosition.y);
            foreach (Vertex adjV in GetVerticesInRange(v, 1.5f, false)) {
                Edge e = new Edge(v, adjV);
                e.weight = Vector2Int.Distance(v.GraphCoordinates, adjV.GraphCoordinates);
                targetGraph.Edges.Add(e);
                e.parentGraph = targetGraph;                         
            }
            
            HashSet<Edge> edgesToRemove = new HashSet<Edge>();
            foreach (Edge e in v.IncidentEdges) {
                if (e.weight > 1) {
                    edgesToRemove.Add(e);
                }
            }

            foreach (Edge edge in edgesToRemove) {
                edge.Vertices[0].IncidentEdges.Remove(edge);
                edge.Vertices[1].IncidentEdges.Remove(edge);
                edge.parentGraph.Edges.Remove(edge);
            }
        }
    }

    public static Vector3 TranslateGraphCoordinatesToWorldSpace (Vector2Int coordinates, float yOffset) {
        return new Vector3(coordinates.x * 5f, yOffset, -coordinates.y * 5f);
    }

    public static Vector2Int TranslateWorldSpaceToGraphCoordinates (Vector3 worldPos) {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / 5f), Mathf.RoundToInt(-worldPos.z / 5f));
    }

    //Find the closest vertex to the coordinates passed
    public Vertex GetClosestVertexToCoordinates (Vector2Int coordinates) {
        Vertex closestVertex = null;
        float closestDistance = 0;
        
        //Test each vertex and return the one that's the least distance away
        foreach (Vertex v in globalVertexCollection) {
            float newDist = Vector2.Distance(coordinates, v.GraphCoordinates);
            if (closestVertex == null || newDist < closestDistance) {
                closestVertex = v;
                closestDistance = newDist;
            }            
        }
        return closestVertex;
    }


    public Graph GetClosestGraphToYPosition (float yPosition) {
        int graphIndex = Mathf.RoundToInt(Mathf.Clamp(yPosition / 10f, 0f, graphs.Count - 1));

        return graphs[graphIndex];
    }
    
    public HashSet<Vertex> GetVerticesInRange (Vertex start, float range, bool includeStart) {
		HashSet<Vertex> verticesWithinRange = new HashSet<Vertex>();

        foreach (Vertex v in globalVertexCollection) {
            if (Vector2Int.Distance(start.GraphCoordinates, v.GraphCoordinates) <= range) {
                verticesWithinRange.Add(v);
            }
        }

        if (includeStart == false) {
            verticesWithinRange.Remove(start);
        }
        
		return verticesWithinRange;
	}

    public HashSet<Vertex> GetAllVerticesInScene () {
        HashSet<Vertex> vertexCollection = new HashSet<Vertex>();
        foreach (Graph g in graphs) {
            vertexCollection.UnionWith(g.Vertices);
        }
        return vertexCollection;
    }

    //A* Dijkstra's Shortest Path Algorithm
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
            foreach (Vertex vertex in globalVertexCollection) {
                if (vertex == source || vertex == target || vertex.blocked == false) {
                    edgeDist.Add(vertex, 1000000);
                    euclideanDist.Add(vertex, Vector2Int.Distance(vertex.GraphCoordinates, target.GraphCoordinates));                
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

                unexploredVertices.Remove(currentVertex);

                //Iterate through each vertex that shares an edge with the current one
                //Calculate the distance of the neighbour vertex when going through the current one
                //If this new path is shorter set the current vertex as the previous one in the 
                //shortest path to the neighbour
                foreach (Edge edgeToNeighbour in currentVertex.IncidentEdges) {                    
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
                    path.Vertices.Insert(0, currentVertex);
                    currentVertex = previousVertexInPath[currentVertex];
                }

                if (currentVertex == source) {
                    path.Vertices.Insert(0, source);
                    path.CalculateEdges();
                    
                    if (target.blocked == true) {
                        path.TrimPath(1);
                    }   

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
    public bool drawGizmos;
    public bool drawVertices;
    public bool drawEdges;
    void OnDrawGizmos () {
        Gizmos.color = Color.white;
        if (drawGizmos && Application.isPlaying) {
            for (int i = 0; i < graphs.Count; i++) {
                Vector3 yOffset = new Vector3(0f, 0.1f, 0f);

                if (drawVertices) {
                    foreach (Vertex v in graphs[i].Vertices) {
                        Gizmos.DrawCube(v.WorldPosition + yOffset, Vector3.one * 0.5f);
                    }
                }

                if (drawEdges) {
                    Gizmos.color = Color.cyan;
                    foreach (Edge e in graphs[i].Edges) {
                        Gizmos.DrawLine(e.Vertices[0].WorldPosition + yOffset, e.Vertices[1].WorldPosition + yOffset);
                    }               
                }  
            }   
        }
    }
}
