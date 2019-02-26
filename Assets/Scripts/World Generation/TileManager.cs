using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	[Header("File Paths of CSV's for Each Level")]
	//File paths for each of the level csv's
	public string[] CSVLevelFilePaths;

	[Header("Tiles For Map Instantiation")]
	//Tiles
	public GameObject[] tilePrefabs;
	
	//2D arrays used to hold the csv data and the tiles respectively
	private List<GameObject[,]> tileObjectGridList = new List<GameObject[,]>();

	//-----METHODS------

	//Create the map at the start of the game
	public void Initialise () {
		for (int i = 0; i < CSVLevelFilePaths.Length; i++) {
			InstantiateTileGridFromMapGrid(ParseCSVtoMapGrid(CSVLevelFilePaths[i]));
			PathfindingManager.instance.CreateGraphFromTileGrid(tileObjectGridList[i]);
		}		
	}

	//Convert the string data from a csv into a map structure that the game can understand
	private int[,] ParseCSVtoMapGrid (string csvPath) {
		int[,] mapGrid;
		List<String> csvLines = new List<String>();

		//Read the lines from the csv file in a list
		try {
			using (StreamReader sr = new StreamReader(csvPath)){
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
	private void InstantiateTileGridFromMapGrid (int[,] mapGrid) {
		//-----STAGE ONE-----
		//Instantiate the tiles based on the numbers found in the map grid passed as an argument
		tileObjectGridList.Add(new GameObject[mapGrid.GetLength(0), mapGrid.GetLength(1)]);
		int mapLevel = tileObjectGridList.Count - 1;
		for (int z = 0; z < mapGrid.GetLength(1); z++) {
			for (int x = 0; x < mapGrid.GetLength(0); x++) {
				GameObject tileObject = null;
				if (mapGrid[x, z] == 0) {
					//Don't place a tile in this location
				} else if (mapGrid[x, z] >= tilePrefabs.Length + 1) {
					Debug.LogError("No Tile Matching Grid Number, Check CSV or Prefab Array");
					return;
				} else {
					//0 - No Tile				
					//1 - Grass
					//2 - Cliff Straight
					//3 - Cliff Outer Corner
					//4 - Cliff Inner Corner
					//5 - River Straight
					//6 - River End
					//7 - River T-Junction
					tileObject = Instantiate(tilePrefabs[mapGrid[x, z] - 1], new Vector3(x * 10 + 2.5f, mapLevel * 10, z * -10 - 2.5f), Quaternion.identity);

					tileObject.transform.SetParent(this.transform);
					tileObjectGridList[mapLevel][x, z] = tileObject;
					tileObject.GetComponent<TileData>().Init(new Vector2(x, z));	
				}						
			}
		}	


		//-----STAGE TWO-----
		//Tiles are instantiated with a default rotation, using the connection nodes and directions of each tile rotate
		//them so that each is in the correct orientation, e.g. walls lines up
		//Step One - have all nodes be in a valid oration, i.e. all connections nodes are linked with adjacent tiles
		for (int z = 0; z < tileObjectGridList[mapLevel].GetLength(1); z++) {
			for (int x = 0; x < tileObjectGridList[mapLevel].GetLength(0); x++) {
				if (tileObjectGridList[mapLevel][x, z] != null) {
					TileData tileData = tileObjectGridList[mapLevel][x, z].GetComponent<TileData>();
					tileData.AdjacentTilesWithNodes = GetAdjacentTilesWithNodes(tileData, mapLevel);
					
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
									} else {
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
		for (int z = 0; z < tileObjectGridList[mapLevel].GetLength(1); z++) {
			for (int x = 0; x < tileObjectGridList[mapLevel].GetLength(0); x++) {
				if (tileObjectGridList[mapLevel][x, z] != null) {
					TileData tileData = tileObjectGridList[mapLevel][x, z].GetComponent<TileData>();	

					//If the start or end node of this tile is equal to that of any adjacent tile the 
   				 	//node is incorrectly rotated and needs to be rotated by 180 degrees
					if (tileData.NumberOfConnectorNodes() > 0 && tileData.ConnectorNodesFormLine()) {
						foreach (TileData adjacentTile in tileData.AdjacentTilesWithNodes) {
							if (Vector3.Distance(tileData.GetWorldPositionOfConnectorNode(tileData.startNodeIndex), adjacentTile.GetWorldPositionOfConnectorNode(tileData.startNodeIndex)) < 1f || 
							    Vector3.Distance(tileData.GetWorldPositionOfConnectorNode(tileData.endNodeIndex), adjacentTile.GetWorldPositionOfConnectorNode(tileData.endNodeIndex)) < 1f) {
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
	}

	//Find and return all the nodes around passed one that have connection nodes, others can be ignored as they are static
	private HashSet<TileData> GetAdjacentTilesWithNodes (TileData tile, int mapLevel) {
		HashSet<TileData> adjacentTiles = new HashSet<TileData>();
		
		if (tile.GridY > 0 && tileObjectGridList[mapLevel][tile.GridX, tile.GridY - 1] != null) {
			adjacentTiles.Add(tileObjectGridList[mapLevel][tile.GridX, tile.GridY - 1].GetComponent<TileData>());
		}

		if (tile.GridX < tileObjectGridList[0].GetLength(0) - 1 && tileObjectGridList[mapLevel][tile.GridX + 1, tile.GridY] != null) {
			adjacentTiles.Add(tileObjectGridList[mapLevel][tile.GridX + 1, tile.GridY].GetComponent<TileData>());
		}

		if (tile.GridY < tileObjectGridList[0].GetLength(1) - 1 && tileObjectGridList[mapLevel][tile.GridX, tile.GridY + 1] != null) {
			adjacentTiles.Add(tileObjectGridList[mapLevel][tile.GridX, tile.GridY + 1].GetComponent<TileData>());
		}

		if (tile.GridX > 0 && tileObjectGridList[mapLevel][tile.GridX - 1, tile.GridY] != null) {
			adjacentTiles.Add(tileObjectGridList[mapLevel][tile.GridX - 1, tile.GridY].GetComponent<TileData>());
		}

		adjacentTiles.RemoveWhere(NoNodes);

		return adjacentTiles;

	}

	private bool NoNodes (TileData tD) { return tD.NumberOfConnectorNodes() == 0; }

	//-----GIZMOS-----

	void OnDrawGizmos () {


	}


}
