using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class TileManager : MonoBehaviour {

	//-----SINGLETON SETUP-----

	public static TileManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    [Header("Map Editor Mode Settings")]
    public bool mapEditorModeEnabled;
    public GameObject tileCursorPrefab;
	private GameObject tileCursorObject;

    [Header("File Paths of CSV's for Each Level")]
	//File paths for each of the level csvs
	public string[] CSVLevelFilePaths;

	[Header("Tiles For Map Instantiation")]
	public GameObject[] tilePrefabs;

    //2D arrays used to hold the csv data and the tiles respectively
    private List<GameObject[,]> tileGridList = new List<GameObject[,]>();

    //-----METHODS------

    /// <summary>
    /// Create the map at the start of the game
    /// </summary>
    public void Initialise() {
		for (int i = 0; i < CSVLevelFilePaths.Length; i++) {
			int[,] csvMapGrid = ParseCSVtoMapGrid(CSVLevelFilePaths[i]);
			tileGridList.Add(InstantiateTileGridFromMapGrid(csvMapGrid, i * 10f));				
		}

		for (int i = 0; i < tileGridList.Count; i++) {
			PathManager.instance.AddGraphFromTileGrid(tileGridList[i]);
		}

		PathManager.instance.FormInterGraphEdges();
	}

    #region Tile Editor Methods
    /// <summary>
    /// Enable the map editor mode
    /// </summary>
    public void EnableMapEditorMode() {
        tileCursorObject = Instantiate(tileCursorPrefab, new Vector3(2.5f, 0f, -2.5f), Quaternion.identity);
        CameraManager.instance.SetTrackingTarget(tileCursorObject);

        StartCoroutine(CursorUpdate());
    }

	/// <summary>
    /// Update the cursors position in the world base don user input
    /// </summary>
    /// <returns></returns>
    IEnumerator CursorUpdate () {
        while (true) {
            if (Input.GetKeyDown(KeyCode.W) && tileCursorObject.transform.position.z < -2.5f) {
                tileCursorObject.transform.position += Vector3.forward * 10f;
            } else if (Input.GetKeyDown(KeyCode.S)) {
                tileCursorObject.transform.position += Vector3.back * 10f;
            }

            if (Input.GetKeyDown(KeyCode.A) && tileCursorObject.transform.position.x > 2.5f) {
                tileCursorObject.transform.position += Vector3.left * 10f;
            } else if (Input.GetKeyDown(KeyCode.D)) {
                tileCursorObject.transform.position += Vector3.right * 10f;
            }

			if (Input.GetKeyDown(KeyCode.Q) && tileCursorObject.transform.position.y < 20f) {
                tileCursorObject.transform.position += Vector3.up * 10f;
            } else if (Input.GetKeyDown(KeyCode.E) && tileCursorObject.transform.position.y > 0f) {
                tileCursorObject.transform.position += Vector3.down * 10f;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Place a tile at the cursors location
    /// </summary>
    /// <param name="id">The tile ID to place</param>
    public void PlaceTileAtCursorLocation (int id) {	
		if (id <= tilePrefabs.Length) {
            RemoveTileAtCursorLocation(true);
            Vector2Int coords = TranslateWorldSpaceToTileGridCoordinates(tileCursorObject.transform.position);
            GameObject tileObject = Instantiate(tilePrefabs[id - 1], tileCursorObject.transform.position, Quaternion.identity);
            tileObject.transform.SetParent(this.transform);

            TileData tData = tileObject.GetComponent<TileData>();
            tData.Initialise(coords);
            tData.tileInstanceID = id;

            int mapLevel = Mathf.RoundToInt(tileCursorObject.transform.position.y / 10f);

            if (CoordinatesInsideTileGridBounds(coords) == false) {
                Debug.Log("Expanding Map");
                for (int i = 0; i < tileGridList.Count; i++) {
                    GameObject[,] oldTileGrid = tileGridList[i];
                    GameObject[,] newTileGrid;
                    newTileGrid = new GameObject[Mathf.Max(coords.x + 5, oldTileGrid.GetLength(0)), Mathf.Max(coords.y + 5, oldTileGrid.GetLength(1))];

                    for (int z = 0; z < oldTileGrid.GetLength(1); z++) {
                        for (int x = 0; x < oldTileGrid.GetLength(0); x++) {
                            newTileGrid[x, z] = oldTileGrid[x, z];
                        }
                    }

                    tileGridList[i] = newTileGrid;
                }
            }

            mapLevel = Mathf.RoundToInt(tileCursorObject.transform.position.y / 10f);
            tileGridList[mapLevel][coords.x, coords.y] = tileObject;
            RotateTilesToCorrectOrientation(tileGridList[mapLevel]);            
        } else {
			Debug.LogError("Invalid Tile Prefab ID, Check Button");
			return;
		}
	}

    /// <summary>
    /// Remove tile(s) at the cursors position
    /// </summary>
    /// <param name="removeAllLevels">Remove all tiles in the vertical stack</param>
	public void RemoveTileAtCursorLocation (bool removeAllLevels) {
		Vector2Int coords = TranslateWorldSpaceToTileGridCoordinates(tileCursorObject.transform.position);

        if (removeAllLevels) {
            for (int mapLevel = 0; mapLevel < tileGridList.Count; mapLevel++) {
                if (CoordinatesInsideTileGridBounds(coords) && tileGridList[mapLevel][coords.x, coords.y] != null) {
                    Destroy(tileGridList[mapLevel][coords.x, coords.y]);
                }
            }
        } else {
            int mapLevel = Mathf.RoundToInt(tileCursorObject.transform.position.y / 10f);
            if (CoordinatesInsideTileGridBounds(coords) && tileGridList[mapLevel][coords.x, coords.y] != null) {
                Destroy(tileGridList[mapLevel][coords.x, coords.y]);
            }
        }        
	}

    /// <summary>
    /// Convert world space positions into grid coordinates
    /// </summary>
    /// <param name="worldPos">The world position to convert</param>
    /// <returns>The Vector2Int grid coordinates</returns>
    public Vector2Int TranslateWorldSpaceToTileGridCoordinates(Vector3 worldPos) {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / 10f), Mathf.RoundToInt(-worldPos.z / 10f));
    }

    /// <summary>
    /// Checks if a set of coordinates is inside the map bounds
    /// </summary>
    /// <param name="coords">Coordinates to check</param>
    /// <returns>True if inside the bounds</returns>
    private bool CoordinatesInsideTileGridBounds (Vector2Int coords) {
        return coords.x >= 0 && coords.y >= 0 &&
               coords.x < tileGridList[0].GetLength(0) && coords.y < tileGridList[0].GetLength(1);
    }

    /// <summary>
    /// Saves the current tile grid to the same CSV's that it generate them from
    /// </summary>
    public void SaveTileLayoutToCSV() {
        String[] newCSVLevelFilePaths = new String[tileGridList.Count];
        for (int i = 0; i < tileGridList.Count; i++) {
            try {
                using (StreamWriter sw = new StreamWriter(CSVLevelFilePaths[i], false)) {
                    for (int z = 0; z < tileGridList[i].GetLength(1); z++) {
                        StringBuilder csvLineBuilder = new StringBuilder();
                        for (int x = 0; x < tileGridList[i].GetLength(0); x++) {
                            int tileID = 0;
                            if (tileGridList[i][x, z] != null) {
                                tileID = tileGridList[i][x, z].GetComponent<TileData>().tileInstanceID;                                
                            }

                            csvLineBuilder.Append(tileID);
                            if (x < tileGridList[i].GetLength(0) - 1) {
                                csvLineBuilder.Append(',');
                            }
                        }

                        sw.WriteLine(csvLineBuilder.ToString());
                    }
                }
            } catch (Exception e) {
                Debug.LogError("Error Reading Map CSV File");
                Debug.LogError(e);
                return;
            }
        }
    }
    #endregion

    /// <summary>
    /// Convert the string data from a csv into a map structure that the game can understand
    /// </summary>
    /// <param name="csvPath">The file path of the csv</param>
    /// <returns>A 2D array of integers representing tile ID's</returns>
    private int[,] ParseCSVtoMapGrid (string csvPath) {
		int[,] mapGrid;
		List<String> csvLines = new List<String>();

		//Read the lines from the csv file in a list
		try {
			using (StreamReader sr = new StreamReader(csvPath)) {
				string csvLine = "";
				while ((csvLine = sr.ReadLine()) != null) {			
					csvLines.Add(csvLine);
				}				
			}
		} catch (Exception e) {			
			Debug.LogError("Error Reading Map CSV File");
			Debug.LogError(e);
			return null;		
		}

		//Take each line and split it into tokens, parse each token to a number and store that in the 2D map array
		mapGrid = new int[csvLines[0].Split(',').Length, csvLines.Count];
		for (int i = 0; i < csvLines.Count; i++) {
			string[] splitLine = csvLines[i].Split(',');
			for (int j = 0; j < splitLine.Length; j++) {
				try {
					mapGrid[j, i] = Int32.Parse(splitLine[j]);
				} catch (Exception e) {
					Debug.LogError("Invalid Character in Map CSV");
					Debug.LogError(e);
					return null;
				}
			}
		}

		return mapGrid;
	}

    /// <summary>
    /// Create a single level of the tile map based on the data from the csv map grid
    /// Instantiate the tiles based on the numbers found in the map grid passed as an argument
    /// </summary>
    /// <param name="mapGrid">The 2D grid of integer ID's</param>
    /// <param name="yOffset">The yOffset at which to create the tiles</param>
    /// <returns>A 2D array of tile game objects</returns>
    private GameObject[,] InstantiateTileGridFromMapGrid (int[,] mapGrid, float yOffset) {
		GameObject[,] tileGrid = new GameObject[mapGrid.GetLength(0), mapGrid.GetLength(1)];

		for (int z = 0; z < mapGrid.GetLength(1); z++) {
			for (int x = 0; x < mapGrid.GetLength(0); x++) {
				GameObject tileObject = null;
				if (mapGrid[x, z] == 0) {
                    tileGrid[x, z] = null;
                } else if (mapGrid[x, z] >= tilePrefabs.Length + 1) {
					Debug.LogError("No Tile Matching Grid Number, Check CSV or Prefab Array");
					return null;
				} else {					
					tileObject = Instantiate(tilePrefabs[mapGrid[x, z] - 1], new Vector3(x * 10 + 2.5f, yOffset, z * -10 - 2.5f), Quaternion.identity);
					tileObject.transform.SetParent(this.transform);
					tileGrid[x, z] = tileObject;

                    TileData tData = tileObject.GetComponent<TileData>();
                    tData.Initialise(new Vector2Int(x, z));
                    tData.tileInstanceID = mapGrid[x, z];
				}						
			}
		}

        RotateTilesToCorrectOrientation(tileGrid);

		return tileGrid;
	}

    /// <summary>
    /// Rotates the tiles in the tile grid to the correct orientation
    /// </summary>
    /// <param name="tileGrid">The grid of all tile objects</param>
    /// <returns>The same grid but all tiles rotated correctly</returns>
    private GameObject[,] RotateTilesToCorrectOrientation (GameObject[,] tileGrid) {
        //Tiles are instantiated with a default rotation, using the connection nodes and directions of each tile rotate
        //them so that each is in the correct orientation, e.g. walls lines up
        //Step One - have all nodes be in a valid orientation, i.e. all connections nodes are linked with adjacent tiles
        for (int z = 0; z < tileGrid.GetLength(1); z++) {
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                if (tileGrid[x, z] != null) {
                    TileData tileData = tileGrid[x, z].GetComponent<TileData>();
                    tileData.adjacentTilesWithNodes = GetAdjacentTilesWithNodes(tileData, tileGrid);

                    //Tiles are instantiated with a default rotation, using the connection nodes and directions of each tile 
                    //rotate them so that each is in the correct alignment, e.g. walls lines up
                    //If a tile doesn't have any connector nodes rotation isn't unnecessary, i.e. grass is the same no matter how you rotate it				
                    if (tileData.NumberOfConnectorNodes() > 0) {
                        List<TileData> lockedDownAdjacentTiles = new List<TileData>();

                        //Iterate through each of the local tiles, if necessary each is rotated until they match with the current tile
                        foreach (TileData adjacentTile in tileData.adjacentTilesWithNodes) {
                            int degreesRotated = 0;
                            int adjacentTileDegreesRotated = 0;
                            bool correctRotationFound = false;

                            //Check all combinations of rotations between the current tile and the adjacent tile
                            while (adjacentTileDegreesRotated < 360 && correctRotationFound == false) {
                                while (degreesRotated < 360 && correctRotationFound == false) {
                                    if (tileData.NumberOfActiveConnections() == tileData.NumberOfConnectorNodes()) {
                                        //All nodes have an active connection so the part is in a valid rotation
                                        correctRotationFound = true;
                                    }

                                    //As each rotation combination is tried lock down tiles that form an active connection with the current tile
                                    //These tiles are in a semi-valid rotation so shouldn't be rotated further
                                    foreach (TileData adjacentActiveTile in tileData.ListOfAdjacentTilesWithActiveConnectionNodes()) {
                                        if (lockedDownAdjacentTiles.Contains(adjacentActiveTile) == false) {
                                            lockedDownAdjacentTiles.Add(adjacentActiveTile);
                                        }
                                    }

                                    //Rotate the tile and begin the check again
                                    if (correctRotationFound == false) {
                                        tileData.RotateTile();
                                        degreesRotated += 90;
                                    }
                                }

                                degreesRotated = 0;

                                //Rotate the adjacent tile by one step as no rotation of the adjacent tile forms a valid rotation
                                if (correctRotationFound == false) {
                                    if (lockedDownAdjacentTiles.Contains(adjacentTile)) {
                                        adjacentTileDegreesRotated = 360;
                                    }
                                    else {
                                        adjacentTile.RotateTile();
                                        adjacentTileDegreesRotated += 90;
                                    }
                                }
                            }

                            //Break out of the adjacent tile loop as the valid rotation has been found so the other tiles don't need checking
                            //AVOID USING BREAKS, REPLACE WITH WHILE LOOP <-------- TODO
                            if (correctRotationFound) {
                                break;
                            }
                        }
                    }
                }
            }
        }
        //End Step One

        //Step Two  - iterate through each tile and if necessary reorientate it to its correct rotation
        for (int z = 0; z < tileGrid.GetLength(1); z++) {
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                if (tileGrid[x, z] != null) {
                    TileData tileData = tileGrid[x, z].GetComponent<TileData>();

                    //If the start or end node of this tile is equal to that of any adjacent tile the 
                    //node is incorrectly rotated and needs to be rotated by 180 degrees
                    if (tileData.NumberOfConnectorNodes() > 0 && tileData.ConnectorNodesFormLine()) {
                        foreach (TileData adjacentTile in tileData.adjacentTilesWithNodes) {
                            if (Vector3.Distance(tileData.GetWorldPositionOfConnectorNode(tileData.startNodeIndex), adjacentTile.GetWorldPositionOfConnectorNode(adjacentTile.startNodeIndex)) < 1f ||
                                Vector3.Distance(tileData.GetWorldPositionOfConnectorNode(tileData.endNodeIndex), adjacentTile.GetWorldPositionOfConnectorNode(adjacentTile.endNodeIndex)) < 1f) {
                                tileData.RotateTile();
                                tileData.RotateTile();
                                break;
                            }
                        }
                    }
                }
            }
        }
        //End Step Two

        return tileGrid;
    }

    /// <summary>
    /// Find and return all the nodes around passed one that have connection nodes, others can be ignored as they are static
    /// </summary>
    /// <param name="tile">The tile to check</param>
    /// <param name="tileObjectGrid">The grid of all tile objects</param>
    /// <returns>A set of tiles surrounding the source that have nodes</returns>
    private HashSet<TileData> GetAdjacentTilesWithNodes (TileData tile, GameObject[,] tileObjectGrid) {
		HashSet<TileData> adjacentTiles = new HashSet<TileData>();
		
		if (tile.gridY > 0 && tileObjectGrid[tile.gridX, tile.gridY - 1] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.gridX, tile.gridY - 1].GetComponent<TileData>());
		}

		if (tile.gridX < tileObjectGrid.GetLength(0) - 1 && tileObjectGrid[tile.gridX + 1, tile.gridY] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.gridX + 1, tile.gridY].GetComponent<TileData>());
		}

		if (tile.gridY < tileObjectGrid.GetLength(1) - 1 && tileObjectGrid[tile.gridX, tile.gridY + 1] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.gridX, tile.gridY + 1].GetComponent<TileData>());
		}

		if (tile.gridX > 0 && tileObjectGrid[tile.gridX - 1, tile.gridY] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.gridX - 1, tile.gridY].GetComponent<TileData>());
		}

		adjacentTiles.RemoveWhere(NoConnectorNodes);

		return adjacentTiles;
	}

    /// <summary>
    /// Check if a tile has no connector nodes
    /// </summary>
    /// <param name="tD">The tile to check</param>
    /// <returns>True if the tile has no nodes</returns>
    private bool NoConnectorNodes(TileData tD) {
        return tD.NumberOfConnectorNodes() == 0;
    }

}
