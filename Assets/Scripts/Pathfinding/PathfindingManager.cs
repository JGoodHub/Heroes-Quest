using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingManager : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static PathfindingManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    //The graph representing the map
    private Graph graph;
    public Graph Graph {
        get { return this.graph; }
    }

    //-----METHODS-----

    //Create a graph from the tile grid
    public void CreateGraphFromTileGrid (GameObject[,] tileObjectGrid) {
        //Create a path map from the tile object grid
        //This is a 2D array where a 1 represents a walkable space and a 0 not walkable
        #region 2D Array Creation
            int[,] pathMap = new int[tileObjectGrid.GetLength(0) * 2, tileObjectGrid.GetLength(1) * 2];
            for (int z = 0; z < tileObjectGrid.GetLength(1); z++) {
                for (int x = 0; x < tileObjectGrid.GetLength(0); x++) {
                    if (tileObjectGrid[x, z] != null) {
                        //From each time grab the 2D array represention of which of its space are walkable accouting for rotation
                        TileData tileData = tileObjectGrid[x, z].GetComponent<TileData>();
                        int[,] tileGridMatrix = tileData.ConvertGridSpaceBoolsTo2DArray();

                        //Insert the tiles space array into the maps 2D array
                        int mapX = x * 2;
                        int mapZ = z *2;
                        pathMap[mapX, mapZ] = tileGridMatrix[0, 0];
                        pathMap[mapX + 1, mapZ] = tileGridMatrix[1, 0];
                        pathMap[mapX, mapZ + 1] = tileGridMatrix[0, 1];
                        pathMap[mapX + 1, mapZ + 1] = tileGridMatrix[1, 1];
                    }
                }
            }
        #endregion

        #region Graph Creation
            //The new graph object
            graph = new Graph();

            //Using the path map create a graph with a vertex at each 1 element and edges to adjacent walkable spaces
            for (int z = 0; z < pathMap.GetLength(1); z++) {
                for (int x = 0; x < pathMap.GetLength(0); x++) {
                    if (pathMap[x, z] == 1) {
                        //Create a new vertex and add it to the graph
                        Vertex v = new Vertex(new Vector2Int (x, z), TranslateGraphCoordinatesToWorldSpace(new Vector2Int(x, z), 0f));
                        graph.AddVertex(v);

                        //Create edges between the current vertex and those above it
                        for (int xOffset = -1; xOffset < 2; xOffset++) {
                            if (x + xOffset > -1 && x + xOffset < pathMap.GetLength(1) && z > 0) {
                                //Check if the offset path map space is walkable and create and edge between it and the current vertex
                                if (pathMap[x + xOffset, z - 1] == 1) {
                                    Edge e = new Edge(v, graph.GetClosestVertexToCoordinates(new Vector2Int(x + xOffset, z - 1)));
                                    graph.AddEdge(e);
                                }
                            }
                        }

                        //Create an edge to the vertex left of the current one
                        if (x > 0 && pathMap[x - 1, z] == 1) {
                            Edge e = new Edge(v, graph.GetClosestVertexToCoordinates(new Vector2Int(x - 1, z)));
                            graph.AddEdge(e);
                        }
                    }
                }
            }

            //Calculate the weights for each of the edges
            foreach (Edge e in graph.Edges) {
                e.weight = Vector2Int.Distance(e.Vertices[0].GraphCoordinates, e.Vertices[1].GraphCoordinates);
            }
        #endregion

    }

    //A* / Dijkstra's Shortest Path Algorithm
    public Path FindShortestPathBetween (Vertex source, Vertex target) {
        //Check that the prerequisites of the algorithm are not null 
        if (graph == null || source == null || target == null) {
            if (graph == null) {
                Debug.LogError("Attempting to find path when the graph is null");
                return null;
            } else {
                Debug.LogError("Attempting to find path where either vertex is null");
                return null;
            }
        } else if (source == target) {
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
            foreach (Vertex vertex in graph.Vertices) {
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
                foreach (Edge edgeToNeighbour in currentVertex.IncidentEdges) {
                    //Calculate the distance of the neighbour vertex when going through the current one
                    //If this new path is shorter set the current vertex as the previous one in the 
                    //shortest path to the neighbour
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
                while (currentVertex != source) {
                    path.Vertices.Insert(0, currentVertex);
                    currentVertex = previousVertexInPath[currentVertex];
                }
                path.Vertices.Insert(0, source);
                path.CalculateEdges();
                
                if (target.blocked == true) {
                    path.TrimPath(1);
                }                
                return path;
            } else {
                return null;
            }
        }
    }

    public static Vector3 TranslateGraphCoordinatesToWorldSpace (Vector2Int coordinates, float yOffset) {
        return new Vector3((coordinates.x * 5f), yOffset, (-coordinates.y * 5f));
    }

    public static Vector2Int TranslateWorldSpaceToGraphCoordinates (Vector3 worldPos) {
        return new Vector2Int(Mathf.RoundToInt((worldPos.x) / 5f), Mathf.RoundToInt((-worldPos.z) / 5f));
    }

    //-----GIZMOS-----
    [Header("Gizmo Toggles")]
    public bool drawGizmos;
    public bool drawVertices;
    public bool drawEdges;
    void OnDrawGizmos () {
        Gizmos.color = Color.white;
        if (drawGizmos && Application.isPlaying) {
            if (drawVertices) {
                foreach (Vertex v in graph.Vertices) {
                    Gizmos.DrawCube(v.WorldPosition, Vector3.one * 0.5f);
                }
            }

            if (drawEdges) {
                foreach (Edge e in graph.Edges) {
                    Gizmos.DrawLine(e.Vertices[0].WorldPosition, e.Vertices[1].WorldPosition);
                }               
            }     
        }
    }
}
