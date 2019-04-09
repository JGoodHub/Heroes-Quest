using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour { 

    //-----SINGLETON SETUP-----

	public static PlayerManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----
	
	private HashSet<CharacterController> heroSet = new HashSet<CharacterController>();
	public HashSet<CharacterController> HeroSet { get => heroSet; }
	
    private CharacterController selectedHero;
	public CharacterController SelectedHero { get => selectedHero; }

	private bool playersTurn;
	private bool actionRunning;
	public bool ActionRunning { set => actionRunning = value; }

    [Header("Line Drawing Prefabs")]
	public GameObject lineSegmentPrefab;
	public GameObject lineDotPrefab;
	public GameObject groundMarkerPrefab;

    //-----METHODS-----

    public void Initialise () {
		//Find and initialise all the characters in the game
		foreach (GameObject gameObj in GameObject.FindGameObjectsWithTag("Hero")) {
			CharacterController heroScript = gameObj.GetComponent<CharacterController>();
			heroSet.Add(heroScript);
			heroScript.Initialise();
		}

		//Log an error if there aren't any characters, this shouldn't be a valid case
		if (heroSet.Count > 0) {
			SelectRandomHero();
		} else {
			Debug.LogError("No hero's in the hero's set, check tags and gameobjects in the scene");
		}

    }

	//Checks for new selection each frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			CheckForNewCharacterSelection();
		}
	}

	public void SelectRandomHero () {
		CharacterController[] heroArrayTemp = heroSet.ToArray();
		selectedHero = heroArrayTemp[Mathf.RoundToInt(Random.Range(0, HeroSet.Count - 1))];
		selectedHero.Highlight();
	}

	//Begin the players turn and unlock all their characters
	public void BeginTurn () {
		playersTurn = true;

		UIManager.instance.UpdateHeroStatusBar();

		foreach (CharacterController h in heroSet) {
            h.MovementController.ResetDistanceMoved();
			h.MovementController.lockedDown = false;
		}
	}

	//End the players turn and lock down all their characters
	public void EndTurn () {
		playersTurn = false;

		foreach (CharacterController h in heroSet) {
			h.MovementController.lockedDown = true;
		}
	}

	//Cast the specified ability on the selected hero
	public void CastSelectedCharacterAbility (int abilityIndex) {
		if (playersTurn == true) {
			selectedHero.CastAbility(abilityIndex);
		}		
	}
	
	//Check if the player has selected a new hero and set that as the primary hero
	public void CheckForNewCharacterSelection () {
		RaycastHit rayHit = CameraManager.instance.FrameRayHit;
		if (playersTurn) {
			if (rayHit.collider!= null && rayHit.collider.tag == "Hero") {
				if (AbilityManager.instance.abilityRunning == false && actionRunning == false) {
					CharacterController newSelectedHero = rayHit.collider.gameObject.GetComponent<CharacterController>();
					if (HeroSet.Contains(newSelectedHero)) {
						selectedHero.Unhighlight();
						newSelectedHero.Highlight();

						selectedHero = newSelectedHero;

						UIManager.instance.UpdateHeroStatusBar();
					}
				}
			}
		}
	}

	//If its the players turn trigger the coroutine
	public void TriggerMoveAction () {
		if (playersTurn) {
			StartCoroutine (TriggerMoveActionCoroutine());
		}
	}

	//Object pools used to create the line	
	IEnumerator TriggerMoveActionCoroutine () {
		//Disable the UI to avoid mis clicks
		UIManager.instance.DisableGameUI();
		actionRunning = true;
		Vector3 unusedElementOffset = new Vector3(999, 999, 999);

		//Collections for storing the parts of the line display
		List<GameObject> lineDots = new List<GameObject>();
		List<GameObject> lineSegments = new List<GameObject>();

		//Set up the ground marker for the move action
		GameObject groundMarkerInstance = null;
		groundMarkerInstance = Instantiate(groundMarkerPrefab, Vector3.zero, Quaternion.identity);
		groundMarkerInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
		groundMarkerInstance.transform.position = unusedElementOffset;

		//Set up variables for the routine
		bool stillSearchingForTarget = true;

        Vertex target = null;
        Vertex source = null;
		Path path = null;
		while (stillSearchingForTarget) {
			#region Path Selection
			//Check if the mouse is over a tile object
			RaycastHit rayHit = CameraManager.instance.FrameRayHit;
			if (rayHit.collider != null && rayHit.collider.CompareTag("Tile")) {
				//Calculate a path to that tile
				target = PathManager.instance.GetClosestVertexToCoordinates(PathManager.TranslateWorldSpaceToGraphCoordinates(rayHit.point));
				source = selectedHero.MovementController.GraphObstacle.CurrentVertex;
				path = PathManager.instance.FindShortestPathBetween(source, target);

                #region Draw Path
                if (path != null) {
                    //If the object pool doesn't have enough dots create more
                    while (lineDots.Count < path.Vertices.Count) {
                        GameObject dotInstance = Instantiate(lineDotPrefab, Vector3.zero, Quaternion.identity);
                        dotInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
                        lineDots.Add(dotInstance);
                    }

                    //Move all dots to an out of game location
                    foreach (GameObject dot in lineDots) {
                        dot.transform.position = unusedElementOffset;
                    }

                    //Move the required number of dots into position
                    for (int i = 1; i < path.Vertices.Count - 1; i++) {
                        lineDots[i - 1].transform.position = path.Vertices[i].WorldPosition;
                    }

                    //If the object pool doesn't have enough lines create more
                    while (lineSegments.Count < path.Vertices.Count) {
                        GameObject lineInstance = Instantiate(lineSegmentPrefab, Vector3.zero, Quaternion.identity);
                        lineInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
                        lineSegments.Add(lineInstance);
                    }

                    //Move the line segments into place and rotate in the required direction
                    for (int i = 0; i < path.Vertices.Count - 1; i++) {
                        lineSegments[i].transform.position = (path.Vertices[i].WorldPosition + path.Vertices[i + 1].WorldPosition) / 2;
                        lineSegments[i].transform.forward = (path.Vertices[i + 1].WorldPosition - path.Vertices[i].WorldPosition).normalized;

                        if ((Mathf.Abs(lineSegments[i].transform.forward.x) < 1f && Mathf.Abs(lineSegments[i].transform.forward.x) > 0f)) {
                            lineSegments[i].transform.localScale = new Vector3(1, 1, 1.5f);
                        } else {
                            lineSegments[i].transform.localScale = new Vector3(1, 1, 1);
                        }
                    }

                    //Move the rest to an out of game location
                    for (int j = path.Vertices.Count - 1; j < lineSegments.Count; j++) {
                        lineSegments[j].transform.position = unusedElementOffset;
                    }

                    //Move the ground marker to the final vertex in the path
                    groundMarkerInstance.transform.position = path.Vertices[path.Vertices.Count - 1].WorldPosition;

                    //Move the UI AP text element
                    UIManager.instance.UpdateFeetCostText(groundMarkerInstance.transform.position, Mathf.RoundToInt(path.Length));
                    if (selectedHero.MovementController.CanMoveDistance(path.Length)) {
                        UIManager.instance.SetSpeedCostToValid();
                    } else {
                        UIManager.instance.SetSpeedCostToInvalid();
                    }
                }
                #endregion

                #region Execute Path Movement
                //Set the relevent flags depending of which button is pressed
                if (Input.GetMouseButtonDown(0)) {
                    if (path != null && selectedHero.MovementController.CanMoveDistance(path.Length)) {
                        stillSearchingForTarget = false;

                        //If the characters has the AP required move along the path
                        path = selectedHero.MovementController.CheckForEncounterAndTrimPath(path);
                        selectedHero.MovementController.MoveCharacter(path, 0f);
                    }
                } else if (Input.GetMouseButtonDown(1)) {
                    stillSearchingForTarget = false;

                    actionRunning = false;
                    UIManager.instance.EnableGameUI();
                }
                #endregion

            }
            #endregion

            yield return null;
		}

		//Move all the line elements to an out of game location
		Destroy(groundMarkerInstance);
		for (int j = 0; j < lineDots.Count; j++) {
			Destroy(lineDots[j]);
			Destroy(lineSegments[j]);
		}

		//Hide the UI text
		UIManager.instance.HideFeetCostText();
	}

	public void TriggerInteractAction () {
		if (playersTurn == true && EnemyAIManager.instance.combatActive == false) {
			StartCoroutine(TriggerInteractActionCoroutine());
		}
	}

	IEnumerator TriggerInteractActionCoroutine () {
		UIManager.instance.ChangeCursorTo(UIManager.CursorType.INTERACT);
		UIManager.instance.DisableGameUI();
		actionRunning = true;

		bool awaitingSelection = true;
		while (awaitingSelection == true) {
			RaycastHit rayHit = CameraManager.instance.FrameRayHit;
            if (Input.GetMouseButtonDown(0)) {
                if (rayHit.collider != null && rayHit.collider.CompareTag("Interactable")) {
					IInteractable interactableObject = rayHit.collider.GetComponent<IInteractable>();
					GraphObstacle objectObstacleComponent = rayHit.collider.GetComponent<GraphObstacle>();
					if (objectObstacleComponent.CurrentVertex.GetNeighbouringVertices().Contains(selectedHero.MovementController.GraphObstacle.CurrentVertex)) {
						selectedHero.MovementController.LookAtVector(objectObstacleComponent.CurrentVertex.WorldPosition);
						interactableObject.TriggerInteraction();

                        if (rayHit.collider.GetComponent<NPCController>() != null) {
                            rayHit.collider.GetComponent<NPCController>().MovementController.LookAtVector(selectedHero.transform.position);
                        }

						awaitingSelection = false;
					}
				}
			} else if (Input.GetMouseButtonDown(1)) {
				awaitingSelection = false;
				actionRunning = false;
			}

			yield return null;
		}


		UIManager.instance.ChangeCursorTo(UIManager.CursorType.DEFAULT);
		UIManager.instance.EnableGameUI();
        UIManager.instance.DisableEndTurnButton();
		actionRunning = false;
	}

	//-----GIZMOS-----
	public bool drawGizmos;
	void OnDrawGizmos () {
		
	}

}