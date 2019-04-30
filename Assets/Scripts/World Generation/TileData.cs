using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData : MonoBehaviour {
    
    //-----VARIABLES-----
    
    [Header("Connector Node Toggles")]
    //Node direction toggles
    public bool forwardNodeEnabled;
    public bool rightNodeEnabled;
    public bool backwardNodeEnabled;
    public bool leftNodeEnabled;

    /*
    Stores whether that direction has a connector node attached to it, booleans state which direction has a node, directions don't change with rotation
    0 - forward
    1 - right
    2 - backward
    3 - left
    */
    private bool[] connectorNodes = new bool[4];

    //Walkable grid space toggles
    [Header("Walkable Space Toggles")]    
    public bool gridA;
    public bool gridB;
    public bool gridC;
    public bool gridD;
    
    [Header("Walkable Space Displacements")]
    public TileSpaceDisplacement[] tileDisplacements = new TileSpaceDisplacement[0];
    [Serializable]
    public struct TileSpaceDisplacement {
        public Char spaceID;
        public float verticalDisplacement;
        public bool interGraphVertex;
    }
    
    //Aid in calculating the direction a tile should be rotated
    [Header("Direction of Tile")]
    public int startNodeIndex;
    public int endNodeIndex;

    //The coordinates of the tile in the map grid
    public int gridX;
    public int gridY;
    private int absoluteRotation = 0;
    public int tileMapLevel;
    
    [HideInInspector] public int tileInstanceID;

    //The tiles adjacent to this one that have connector nodes
    public HashSet<TileData> adjacentTilesWithNodes;

    //-----METHODS-----

    /// <summary>
    /// Initialises the tile by storing its coordinates in the map grid and converting the node booleans to an array
    /// </summary>
    /// <param name="gridPos">The grid position of the tile</param>
    public void Initialise (Vector2Int gridPos) {
        gridX = gridPos.x;
        gridY = gridPos.y;

        connectorNodes[0] = forwardNodeEnabled;
        connectorNodes[1] = rightNodeEnabled;
        connectorNodes[2] = backwardNodeEnabled;
        connectorNodes[3] = leftNodeEnabled;

        SetupColliders();
    }

    /// <summary>
    /// Set up the colliders on each walkable space
    /// </summary>
    private void SetupColliders () {
        for (int i = 0, id = 'A'; i < 4; i++, id++) {
            if (IsWalkableSpaceActive((Char) id) == true) {
                BoxCollider gridSpaceCollider = gameObject.AddComponent(typeof(BoxCollider)) as BoxCollider;
                gridSpaceCollider.center = GetWorldPositionOfWalkableSpace((Char) id) - transform.position + new Vector3(0, GetWalkableSpaceDisplacement((Char) id), 0);
                gridSpaceCollider.size = new Vector3(5, 0.5f, 5);
            }
        }
    }

    /// <summary>
    /// Does the tile have a node in a given direction
    /// </summary>
    /// <param name="direction">The direction to check</param>
    /// <returns>A boolean of whether there exists a connector node in that direction</returns>
    public bool HasConnectorNodeInDirection (int direction) {
        return connectorNodes[direction] == true;
    }

    /// <summary>
    /// Get the number of nodes this tile has
    /// </summary>
    /// <returns>The number of connector nodes</returns>
    public int NumberOfConnectorNodes () {
        int count = 0;
        foreach (bool nodeEnabled in connectorNodes) {
            if (nodeEnabled) {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Number of adjacent tiles that have nodes that are actively connected with this tile
    /// </summary>
    /// <returns>Number of active connections this tile has with those surrounding it</returns>
    public int NumberOfActiveConnections () {
        return ListOfAdjacentTilesWithActiveConnectionNodes().Count;
    }

    /// <summary>
    /// Get a list of all tiles which share an active node connection with this tile
    /// </summary>
    /// <returns>A list of tiles</returns>
    public List<TileData> ListOfAdjacentTilesWithActiveConnectionNodes () {
        List<TileData> tilesWithActiveConnections = new List<TileData>();

        //Compare the position of each node on this tile with each node on each of the adjacent tiles
        //If they share the same position then those two tiles share an active connection
        foreach (TileData adjacentTile in adjacentTilesWithNodes) {
            foreach (Vector3 currentNodePosition in GetWorldPositionOfAllConnectorNodes()) {
                foreach (Vector3 adjacentNodePosition in adjacentTile.GetWorldPositionOfAllConnectorNodes()) {
                    if (Vector3.Distance(currentNodePosition, adjacentNodePosition) < 1f && tilesWithActiveConnections.Contains(adjacentTile) == false) {
                        tilesWithActiveConnections.Add(adjacentTile);
                    }
                }
            }
        }
        return tilesWithActiveConnections;
    }

    /// <summary>
    /// Get the position of a given node in reference to the world instead of the local transform
    /// Each node occupies a different offset to its transform, invalid nodes are returned as zero vectors
    /// </summary>
    /// <param name="direction">The direction of the node to get</param>
    /// <returns>The world position of that node</returns>
    public Vector3 GetWorldPositionOfConnectorNode (int direction) {
        if (direction == 0 && HasConnectorNodeInDirection(direction)) {
            return (transform.position + (transform.forward * 5));
        }
        
        if (direction == 1 && HasConnectorNodeInDirection(direction)) {
            return (transform.position + (transform.right * 5));
        }

        if (direction == 2 && HasConnectorNodeInDirection(direction)) {
            return (transform.position + (-transform.forward * 5));
        }

        if (direction == 3 && HasConnectorNodeInDirection(direction)) {
            return (transform.position + (-transform.right * 5));
        }

        return Vector3.zero;
    }

    //
    /// <summary>
    /// Get the world position of all nodes
    /// </summary>
    /// <returns>A list of vectors</returns>
    public List<Vector3> GetWorldPositionOfAllConnectorNodes () {
        List<Vector3> nodeWorldPositions = new List<Vector3>();

        //Get position of all nodes, only return those that are valid
        for(int i = 0; i < 4; i++) {
            Vector3 nodeWorldPosition = GetWorldPositionOfConnectorNode(i);
            if (nodeWorldPosition.Equals(Vector3.zero) == false) {
                nodeWorldPositions.Add(nodeWorldPosition);
            }
        }
        
        return nodeWorldPositions;
    }


    /// <summary>
    /// Are the nodes for the tile paced on opposite sides of the tile
    /// i.e. this results in two possible valid rotations at 0 and 180 degrees
    /// </summary>
    /// <returns>Boolean of whether they form a line</returns>
    public bool ConnectorNodesFormLine () {
        return (NumberOfConnectorNodes() == 2) && ((HasConnectorNodeInDirection(0) && HasConnectorNodeInDirection(2)) || (HasConnectorNodeInDirection(1) && HasConnectorNodeInDirection(3)));
    }

    /// <summary>
    /// Rotate the tile by one step/90 degrees, keeping track of the absolute rotation of the tile
    /// </summary>
    public void RotateTile () {
        transform.Rotate(new Vector3(0, 90, 0));
        absoluteRotation = (absoluteRotation + 90) % 360;
    }

    /// <summary>
    /// Checks whether a given grid space is walkable
    /// </summary>
    /// <param name="spaceID">Space to check</param>
    /// <returns>Boolean of whether it's walkable</returns>
    public bool IsWalkableSpaceActive (Char spaceID) {
        switch (spaceID) {
            case 'A': return gridA;
            case 'B': return gridB;
            case 'C': return gridC;
            case 'D': return gridD;
            default : return false;
        }
    }

    /// <summary>
    /// Checks if a space is an inter graph connection
    /// </summary>
    /// <param name="spaceID">The space to check</param>
    /// <returns>Boolean of whether it's an inter graph vertex</returns>
    public bool IsWalkableSpaceInterGraphConnecting (Char spaceID) {
        foreach (TileSpaceDisplacement tileDisplacement in tileDisplacements) {
            if (tileDisplacement.spaceID == spaceID && tileDisplacement.interGraphVertex == true) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get the displacement of a walkable space
    /// </summary>
    /// <param name="spaceID">Space to get</param>
    /// <returns>The y float of the displacement, 0 if not found</returns>
    private float GetWalkableSpaceDisplacement (Char spaceID) {
        float displacementTotal = 0f;
        foreach (TileSpaceDisplacement tileDisplacement in tileDisplacements) {
            if (tileDisplacement.spaceID == spaceID) {
                displacementTotal += tileDisplacement.verticalDisplacement;
            }
        }

        return displacementTotal;
    }

    /// <summary>
    /// Returns the world position of a given grid space
    /// </summary>
    /// <param name="spaceID">The space to get</param>
    /// <returns>World position of  walkable space</returns>
    public Vector3 GetWorldPositionOfWalkableSpace (Char spaceID) {
        switch (spaceID) {
            case 'A':
                if (gridA == true) {
                    return transform.position + (((transform.forward + -transform.right) / 2f) * 5f);
                }
            break;
            case 'B':
                if (gridB == true) {
                    return transform.position + (((transform.forward + transform.right) / 2f) * 5f);
                }
            break;
            case 'C':
                if (gridC == true) {
                    return transform.position + (((-transform.forward + -transform.right) / 2f) * 5f);
                }
            break;
            case 'D':
                if (gridD == true) {
                    return transform.position + (((-transform.forward + transform.right) / 2f) * 5f);
                }
            break;
        }

        return Vector3.zero;        
    }

    
    /// <summary>
    /// Convert the walkable space toggles into a 2D array for easier iteration
    /// </summary>
    /// <returns>"2D array representing the walkable spaces of the tile</returns>
    public int[,] ConvertWalkableSpacesTo2DDisplacementArray () {
        int[,] gridSpaceMatrix = new int[2, 2];
        Char correspondingID = 'A';

        for (int z = 0; z <= 1; z++) {
            for (int x = 0; x <= 1; x++) {
                if (IsWalkableSpaceActive(correspondingID)) {
                    if (IsWalkableSpaceInterGraphConnecting(correspondingID)) {
                        gridSpaceMatrix[x, z] = 10;
                    }  else {
                        gridSpaceMatrix[x, z] = Mathf.RoundToInt(GetWalkableSpaceDisplacement(correspondingID));
                    }
                } else {
                    gridSpaceMatrix[x, z] = -1;
                }
                
                correspondingID++;
            }
        }

        //Rotate the grid space array by 90 degrees at a time
        for (int matrixRotation = 0; matrixRotation < absoluteRotation; matrixRotation += 90) {
            int temp = gridSpaceMatrix[0, 0];
            gridSpaceMatrix[0, 0] = gridSpaceMatrix[0, 1];
            gridSpaceMatrix[0, 1] = gridSpaceMatrix[1, 1];
            gridSpaceMatrix[1, 1] = gridSpaceMatrix[1, 0];
            gridSpaceMatrix[1, 0] = temp;            
        }

        return gridSpaceMatrix;
    }

    //-----GIZMOS-----

    [Header("Gizmo Toggles")]
    public bool drawConnectorNodes;
    public bool drawWalkableToggles;
    public bool drawTileDirection;
    public bool drawTileBounds;
    /// <summary>
    /// Draw the tiles attributes as gizmos in the editor window
    /// </summary>
    void OnDrawGizmos () {
        if (drawConnectorNodes) {
            //Draw spheres at each node position
            Gizmos.color = Color.cyan;
            connectorNodes[0] = forwardNodeEnabled;
            connectorNodes[1] = rightNodeEnabled;
            connectorNodes[2] = backwardNodeEnabled;
            connectorNodes[3] = leftNodeEnabled;
            foreach(Vector3 nodePosition in GetWorldPositionOfAllConnectorNodes()) {
                Gizmos.DrawSphere(nodePosition, 0.5f);
            }
        }

        if (drawWalkableToggles) {
            //Draw cubes on each grid space that's marked as walkable
            for (Char id = 'A'; id != 'E'; id++) {
                if (IsWalkableSpaceActive(id)) {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(GetWorldPositionOfWalkableSpace(id) + new Vector3(0, GetWalkableSpaceDisplacement(id), 0), Vector3.one);
                        
                }
            }
        }

        if (drawTileDirection) {
            //Draw a line between the start and end node
            Gizmos.color = Color.red;
            GizmosDrawArrowedLine(GetWorldPositionOfConnectorNode(startNodeIndex), GetWorldPositionOfConnectorNode(endNodeIndex));   
        }

        if (drawTileBounds) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(10, 5, 10));
        }
    }

    /// <summary>
    /// Draw an arrowed line between two vectors
    /// </summary>
    /// <param name="start">Arrow start in world space</param>
    /// <param name="end">Arrow end in world space</param>
    private void GizmosDrawArrowedLine (Vector3 start, Vector3 end) {
        Vector3 reverseDirection = (start - end).normalized;
        Vector3 tangentDirection = Quaternion.AngleAxis(90, Vector3.up) * reverseDirection;
        Vector3 midPoint = (start + end) / 2;

        //Draw base line
        Gizmos.DrawLine(start, end);

        //Draw first arrow
        Gizmos.DrawLine(((midPoint + start) / 2), ((midPoint + start) / 2) + (reverseDirection + tangentDirection));
        Gizmos.DrawLine(((midPoint + start) / 2), ((midPoint + start) / 2) + (reverseDirection + -tangentDirection));

        //Draw second arrow
        Gizmos.DrawLine(midPoint, midPoint + (reverseDirection + tangentDirection));
        Gizmos.DrawLine(midPoint, midPoint + (reverseDirection + -tangentDirection));

        //Draw third arrow
        Gizmos.DrawLine(((midPoint + end) / 2) , ((midPoint + end) / 2)  + (reverseDirection + tangentDirection));
        Gizmos.DrawLine(((midPoint + end) / 2) , ((midPoint + end) / 2)  + (reverseDirection + -tangentDirection));
    }
    

}
