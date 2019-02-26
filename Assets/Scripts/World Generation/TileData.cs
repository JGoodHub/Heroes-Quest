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

    [Header("Grid Space Walkable Toggles")]
    //Walkable grid space toggles
    public bool gridA;
    public bool gridB;
    public bool gridC;
    public bool gridD;
    
    //Aid in calculating the direction a tile should be rotated
    [Header("Direction of Tile")]
    public int startNodeIndex;
    public int endNodeIndex;

    //The corrdinates of the tile in the map grid
    private int gridX;
    public int GridX {
        get { return this.gridX; }
    }

    private int gridY;
    public int GridY {
        get { return this.gridY; }
    }    

    private int absoluteRotation = 0;

    private int tileMapLevel;
    public int TileMapLevel {
        get { return this.tileMapLevel; }
        set { this.tileMapLevel = value; }
    }

    //The tiles adjacent to this one that have connector nodes
    private HashSet<TileData> adjacentTilesWithNodes;
    public HashSet<TileData> AdjacentTilesWithNodes {
        get { return this.adjacentTilesWithNodes; }
        set { this.adjacentTilesWithNodes = value; }
    }

    //-----METHODS-----
    //Initialises the tile by storing its coordinates in the map grid and converting the node bools to an array
    public void Init (Vector2 gridPos) {
        gridX = (int) gridPos.x;
        gridY = (int) gridPos.y;

        connectorNodes[0] = forwardNodeEnabled;
        connectorNodes[1] = rightNodeEnabled;
        connectorNodes[2] = backwardNodeEnabled;
        connectorNodes[3] = leftNodeEnabled;

        SetupColliders();
    }

    private void SetupColliders () {
        for (int i = 0, c = 'A'; i < 4; i++, c++) {
            if (IsGridSpaceWalkable((Char) c) == true) {
                BoxCollider gridSpaceCollider = gameObject.AddComponent(typeof(BoxCollider)) as BoxCollider;
                gridSpaceCollider.center = GetWorldPositionOfWalkableGridSpace((Char) c) - transform.position;
                gridSpaceCollider.size = new Vector3(5, 1, 5);
            }
        }
    }
    
    //Does the tile have a node in a given direction
    public bool HasConnectorNodeInDirection (int direction) {
        return connectorNodes[direction] == true;        
    }

    //Number of nodes this tile has
    public int NumberOfConnectorNodes () {
        int count = 0;
        foreach (bool nodeEnabled in connectorNodes) {
            if (nodeEnabled) {
                count++;
            }
        }
        return count;
    }

    //Number of adjacent tiles that have nodes that are actively connected with this tile
    public int NumberOfActiveConnections () {
        return ListOfAdjacentTilesWithActiveConnectionNodes().Count;
    }

    //Return a list of adjacent tiles that share an active connection with this tile
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
    
    //Get the position of a given node in reference to the world instead of the local transform
    //Each node occupies a different offset to its transform, invalid nodes are returned as zero vectors
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

    //Get the world position of all nodes, return them in list of vectors
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

    //Are the nodes for the tile paced on opposite sides of the tile
    //i.e. this results in two possible valid rotations at 0 and 180 degrees
    public bool ConnectorNodesFormLine () {
        return (NumberOfConnectorNodes() == 2) && ((HasConnectorNodeInDirection(0) && HasConnectorNodeInDirection(2)) || (HasConnectorNodeInDirection(1) && HasConnectorNodeInDirection(3)));
    }

    //Rotate the tile by one step/90 degrees, keeping track of the absolute rotation of the tile
    public void RotateTile () {
        transform.Rotate(new Vector3(0, 90, 0));
        absoluteRotation = (absoluteRotation + 90) % 360;
    }    

    //Checks whether a given grid space is walkable
    public bool IsGridSpaceWalkable (Char spaceID) {
        switch (spaceID) {
            case 'A': return gridA;
            case 'B': return gridB;
            case 'C': return gridC;
            case 'D': return gridD;
        }
        return false;
    }

    //Returns the world position of a given grid space
    //Mostly used for debugging and gizmos at the moment
    public Vector3 GetWorldPositionOfWalkableGridSpace (Char spaceID) {
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

    public int[,] ConvertGridSpaceBoolsTo2DArray () {
        int[,] gridSpaceMatrix = new int[2, 2];
        gridSpaceMatrix[0, 0] = gridA ? 1 : 0;
        gridSpaceMatrix[1, 0] = gridB ? 1 : 0;
        gridSpaceMatrix[0, 1] = gridC ? 1 : 0;
        gridSpaceMatrix[1, 1] = gridD ? 1 : 0;
        
        //Rotate the grid space array by 90 degrees at a time
        for (int matrixRotation = 0; matrixRotation < absoluteRotation; matrixRotation += 90) {
            int temp = gridSpaceMatrix[0,0];
            gridSpaceMatrix[0, 0] = gridSpaceMatrix[0, 1];
            gridSpaceMatrix[0, 1] = gridSpaceMatrix[1, 1];
            gridSpaceMatrix[1, 1] = gridSpaceMatrix[1, 0];
            gridSpaceMatrix[1, 0] = temp;            
        }

        return gridSpaceMatrix;
    }

    //-----GIZMOS-----

    [Header("Gizmo Toggles")]
    public bool drawGizmos;
    void OnDrawGizmos () {
        if (drawGizmos) {
            //Draw spheres at each node position
            Gizmos.color = Color.cyan;
            connectorNodes[0] = forwardNodeEnabled;
            connectorNodes[1] = rightNodeEnabled;
            connectorNodes[2] = backwardNodeEnabled;
            connectorNodes[3] = leftNodeEnabled;
            foreach(Vector3 nodePosition in GetWorldPositionOfAllConnectorNodes()) {
                Gizmos.DrawSphere(nodePosition, 0.5f);
            }

            //Draw cubes on each grid space that's marked as walkable
            Gizmos.color = Color.green;
            for (Char id = 'A'; id != 'E'; id++) {
                if (IsGridSpaceWalkable(id)) {
                    Gizmos.DrawCube(GetWorldPositionOfWalkableGridSpace(id), Vector3.one);
                }
            }


            //Draw a line between the start and end node
            Gizmos.color = Color.red;
            GizmosDrawArrow(GetWorldPositionOfConnectorNode(startNodeIndex), GetWorldPositionOfConnectorNode(endNodeIndex));   
        }
    }

    //Draw an arrowed line between two vectors
    private void GizmosDrawArrow (Vector3 start, Vector3 end) {
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
