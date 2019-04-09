using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;

//Generate the tile map from the csv provided
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
	private GameObject tileCursorInstance;

    [Header("File Paths of CSV's for Each Level")]
	//File paths for each of the level csv's
	public string[] CSVLevelFilePaths;

	[Header("Tiles For Map Instantiation")]
	//Tiles
	//0 - No Tile				
	//1 - Grass
	//2 - Cliff Straight
	//3 - Cliff Outer Corner
	//4 - Cliff Inner Corner
	//5 - River Straight
	//6 - River End
	//7 - River T-Junction
	//8 - Cliff With Ladder
	public GameObject[] tilePrefabs;

    //2D arrays used to hold the csv data and the tiles respectively
    private List<GameObject[,]> tileGridList = new List<GameObject[,]>();

	//-----METHODS------

	//Create the map at the start of the game
    /// <summary>
    /// 
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
    /// 
    /// </summary>
    public void EnableMapEditorMode() {
        tileCursorInstance = Instantiate(tileCursorPrefab, new Vector3(2.5f, 0f, -2.5f), Quaternion.identity);
        CameraManager.instance.SetTrackingTarget(tileCursorInstance);

        StartCoroutine(CursorUpdate());
    }

	/// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator CursorUpdate () {
        while (true) {
            if (Input.GetKeyDown(KeyCode.W)) {
                tileCursorInstance.transform.position += Vector3.forward * 10f;
            } else if (Input.GetKeyDown(KeyCode.S)) {
                tileCursorInstance.transform.position += Vector3.back * 10f;
            }

            if (Input.GetKeyDown(KeyCode.A)) {
                tileCursorInstance.transform.position += Vector3.left * 10f;
            } else if (Input.GetKeyDown(KeyCode.D)) {
                tileCursorInstance.transform.position += Vector3.right * 10f;
            }

			if (Input.GetKeyDown(KeyCode.Q)) {
                tileCursorInstance.transform.position += Vector3.up * 10f;
            } else if (Input.GetKeyDown(KeyCode.E) && tileCursorInstance.transform.position.y > 0f) {
                tileCursorInstance.transform.position += Vector3.down * 10f;
            }

            yield return null;
        }
    }

	/// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
	public void PlaceTileAtCursorLocation (int id) {		
		if (id <= tilePrefabs.Length) {
            RemoveTileAtCursorLocation(true);
            GameObject newTileInstance = Instantiate(tilePrefabs[id - 1], tileCursorInstance.transform.position, Quaternion.identity);

            Vector2Int coords = TranslateWorldSpaceToTileGridCoordinates(tileCursorInstance.transform.position);
            int mapLevel = Mathf.RoundToInt(tileCursorInstance.transform.position.y / 10f);

            if (CoordinatesInsideTileGridBounds(coords) == false) {
                while (MapLevelInTileGridBounds(mapLevel) == false) {
                    GameObject[,] tileObjectGrid = new GameObject[tileGridList[0].GetLength(0), tileGridList[0].GetLength(1)];
                    tileGridList.Add(tileObjectGrid);
                    mapLevel++;
                }

                foreach(GameObject[,] tileGrid in tileGridList) {
                    GameObject[,] newTileGrid;

                    if (coords.x < 0 || coords.y < 0) {
                        newTileGrid = new GameObject[tileGrid.GetLength(0) + Mathf.Abs(coords.x), tileGrid.GetLength(1) + Mathf.Abs(coords.y)];
                    } else {
                        newTileGrid = new GameObject[coords.x + 1, coords.y + 1];
                    }
                }
            }

            Debug.Log("Placed");

            mapLevel = Mathf.RoundToInt(tileCursorInstance.transform.position.y / 10f);
            tileGridList[mapLevel][coords.x, coords.y] = newTileInstance;
            RotateTilesToCorrectOrientation(tileGridList[mapLevel]);            
        } else {
			Debug.LogError("Invalid Tile Prefab ID, Check Button");
			return;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="removeAllLevels"></param>
	public void RemoveTileAtCursorLocation (bool removeAllLevels) {
		Vector2Int coords = TranslateWorldSpaceToTileGridCoordinates(tileCursorInstance.transform.position);

        if (removeAllLevels) {
            for (int mapLevel = 0; mapLevel < tileGridList.Count; mapLevel++) {
                if (MapLevelInTileGridBounds(mapLevel) && CoordinatesInsideTileGridBounds(coords) && tileGridList[mapLevel][coords.x, coords.y] != null) {
                    Destroy(tileGridList[mapLevel][coords.x, coords.y]);
                }
            }
        } else {
            int mapLevel = Mathf.RoundToInt(tileCursorInstance.transform.position.y / 10f);
            if (MapLevelInTileGridBounds(mapLevel) && CoordinatesInsideTileGridBounds(coords) && tileGridList[mapLevel][coords.x, coords.y] != null) {
                Destroy(tileGridList[mapLevel][coords.x, coords.y]);
            }
        }        
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public Vector2Int TranslateWorldSpaceToTileGridCoordinates(Vector3 worldPos) {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / 10f), Mathf.RoundToInt(-worldPos.z / 10f));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapLevel"></param>
    /// <returns></returns>
    private bool MapLevelInTileGridBounds (int mapLevel) {
        return mapLevel >= 0 && mapLevel < tileGridList.Count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    private bool CoordinatesInsideTileGridBounds (Vector2Int coords) {
        return coords.x >= 0 && coords.y >= 0 &&
               coords.x < tileGridList[0].GetLength(0) && coords.y < tileGridList[0].GetLength(1);
    }

    /// <summary>
    /// 
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
    /// <param name="csvPath"></param>
    /// <returns></returns>
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

	//Create a single level of the tile map based on the data from the csv map grid
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapGrid"></param>
    /// <param name="yOffset"></param>
    /// <returns></returns>
	private GameObject[,] InstantiateTileGridFromMapGrid (int[,] mapGrid, float yOffset) {
		//-----STAGE ONE-----
		//Instantiate the tiles based on the numbers found in the map grid passed as an argument

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
					tileObject.GetComponent<TileData>().Initialise(new Vector2(x, z));	
				}						
			}
		}

        RotateTilesToCorrectOrientation(tileGrid);

		return tileGrid;
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tileGrid"></param>
    /// <returns></returns>
    private GameObject[,] RotateTilesToCorrectOrientation (GameObject[,] tileGrid) {
        //-----STAGE TWO-----
        //Tiles are instantiated with a default rotation, using the connection nodes and directions of each tile rotate
        //them so that each is in the correct orientation, e.g. walls lines up
        //Step One - have all nodes be in a valid orientation, i.e. all connections nodes are linked with adjacent tiles
        for (int z = 0; z < tileGrid.GetLength(1); z++) {
            for (int x = 0; x < tileGrid.GetLength(0); x++) {
                if (tileGrid[x, z] != null) {
                    TileData tileData = tileGrid[x, z].GetComponent<TileData>();
                    tileData.AdjacentTilesWithNodes = GetAdjacentTilesWithNodes(tileData, tileGrid);

                    //Tiles are instantiated with a default rotation, using the connection nodes and directions of each tile 
                    //rotate them so that each is in the correct alignment, e.g. walls lines up
                    //If a tile doesn't have any connector nodes rotation isn't unnecessary, i.e. grass is the same no matter how you rotate it				
                    if (tileData.NumberOfConnectorNodes() > 0) {
                        List<TileData> lockedDownAdjacentTiles = new List<TileData>();

                        //Iterate through each of the local tiles, if necessary each is rotated until they match with the current tile
                        foreach (TileData adjacentTile in tileData.AdjacentTilesWithNodes) {
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
                        foreach (TileData adjacentTile in tileData.AdjacentTilesWithNodes) {
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

    //Find and return all the nodes around passed one that have connection nodes, others can be ignored as they are static
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="tileObjectGrid"></param>
    /// <returns></returns>
    private HashSet<TileData> GetAdjacentTilesWithNodes (TileData tile, GameObject[,] tileObjectGrid) {
		HashSet<TileData> adjacentTiles = new HashSet<TileData>();
		
		if (tile.GridY > 0 && tileObjectGrid[tile.GridX, tile.GridY - 1] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.GridX, tile.GridY - 1].GetComponent<TileData>());
		}

		if (tile.GridX < tileObjectGrid.GetLength(0) - 1 && tileObjectGrid[tile.GridX + 1, tile.GridY] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.GridX + 1, tile.GridY].GetComponent<TileData>());
		}

		if (tile.GridY < tileObjectGrid.GetLength(1) - 1 && tileObjectGrid[tile.GridX, tile.GridY + 1] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.GridX, tile.GridY + 1].GetComponent<TileData>());
		}

		if (tile.GridX > 0 && tileObjectGrid[tile.GridX - 1, tile.GridY] != null) {
			adjacentTiles.Add(tileObjectGrid[tile.GridX - 1, tile.GridY].GetComponent<TileData>());
		}

		adjacentTiles.RemoveWhere(NoConnectorNodes);

		return adjacentTiles;
	}

    private bool NoConnectorNodes(TileData tD) {
        return tD.NumberOfConnectorNodes() == 0;
    }

    //-----GIZMOS-----
    //public bool drawGimzmos;
    void OnDrawGizmos () {
	}


}
