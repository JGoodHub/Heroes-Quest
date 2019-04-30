using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {

    //-----SINGLETON SETUP-----

    public static PlayerManager instance = null;

    void Awake() {
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

    [HideInInspector] public bool playersTurn;
	private bool actionRunning;
	public bool ActionRunning { set => actionRunning = value; }

    [Header("Line Drawing Prefabs")]
	public GameObject lineSegmentPrefab;
	public GameObject lineDotPrefab;
	public GameObject groundMarkerPrefab;

    //-----METHODS-----

    /// <summary>
    /// Setup all the heroes in the scene
    /// </summary>
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
			Debug.LogError("No hero's in the hero's set, check tags and game objects in the scene");
		}

    }

    /// <summary>
    /// Checks for new selection each frame
    /// </summary>
    void Update () {
		if (Input.GetMouseButtonDown(0)) {
			CheckForNewCharacterSelection();
		}
	}

    /// <summary>
    /// Select a random hero
    /// </summary>
	public void SelectRandomHero () {
		CharacterController[] heroArrayTemp = heroSet.ToArray();
		selectedHero = heroArrayTemp[Mathf.RoundToInt(Random.Range(0, HeroSet.Count - 1))];
		selectedHero.Highlight();
	}

    /// <summary>
    /// Select a hero by name
    /// </summary>
    /// <param name="characterName">The name of the hero to select</param>
    public void SelectHeroByName (string characterName) {
        StartCoroutine(SelectHeroBynameCoroutine(characterName));
    }

    /// <summary>
    /// Coroutine for selecting a hero by name
    /// </summary>
    /// <param name="characterName">The name of the hero to select</param>
    /// <returns></returns>
    IEnumerator SelectHeroBynameCoroutine (string characterName) {
        foreach (CharacterController heroController in heroSet) {
            if (heroController.CharacterData.characterName == characterName) {
                selectedHero.Unhighlight();
                selectedHero = heroController;
                selectedHero.Highlight();

                CameraManager.instance.SetTrackingTarget(selectedHero.gameObject);
                while (CameraManager.instance.TrackingTargetReached == false) {
                    yield return null;
                }
                CameraManager.instance.ClearTrackingTarget();
            }
        }
    }
    	
    /// <summary>
    /// 
    /// </summary>
	public void BeginTurn () {
		playersTurn = true;

		foreach (CharacterController h in heroSet) {
            h.MovementController.ResetDistanceMoved();
            h.CharacterData.actionAvailable = true;
			h.MovementController.disableMovement = false;

            if (EnemyAIManager.instance.combatActive) {
                h.MovementController.infinityMovement = false;
            } else {
                h.MovementController.infinityMovement = true;
            }
		}

        UIManager.instance.UpdateHeroStatusBar();

    }

    /// <summary>
    /// End the players turn and lock down all their characters
    /// </summary>
    public void EndTurn () {
        if (playersTurn) {
            playersTurn = false;

            foreach (CharacterController h in heroSet) {
                h.MovementController.disableMovement = true;
            }
        }
	}

    /// <summary>
    /// Check if the player has selected a new hero and set that as the primary hero
    /// </summary>
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

    /// <summary>
    /// Cast the specified ability on the selected hero
    /// </summary>
    /// <param name="abilityIndex">The index of the ability to cast</param>
    public void CastSelectedCharacterAbility(int abilityIndex) {
        if (playersTurn) {
            selectedHero.CastAbility(abilityIndex);
        }
    }

    /// <summary>
    /// If its the players turn trigger the coroutine
    /// </summary>
    public void TriggerMoveAction () {
		if (playersTurn) {
			StartCoroutine (TriggerMoveActionCoroutine());
		}
	}

    /// <summary>
    /// Coroutine for targeting the players move action and starting the hero's movement
    /// </summary>
    /// <returns></returns>
	IEnumerator TriggerMoveActionCoroutine () {
        //Disable the UI to avoid mis clicks
		UIManager.instance.DisablePlayerControls();

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
        Vertex source = selectedHero.MovementController.GraphObstacle.currentVertex;
		Path path = null;
		while (stillSearchingForTarget) {
			#region Path Selection
			//Check if the mouse is over a tile object
			RaycastHit rayHit = CameraManager.instance.FrameRayHit;
			if (rayHit.collider != null && rayHit.collider.CompareTag("Tile")) {
				//Calculate a path to that tile
                if (target != PathManager.instance.GetClosestVertexToCoordinates(PathManager.TranslateWorldSpaceToGraphCoordinates(rayHit.point))) {
                    target = PathManager.instance.GetClosestVertexToCoordinates(PathManager.TranslateWorldSpaceToGraphCoordinates(rayHit.point));
                    path = PathManager.instance.FindShortestPathBetween(source, target);
                }
                
                #region Path Drawing
                if (path != null) {
                    //If the object pool doesn't have enough dots create more
                    while (lineDots.Count < path.vertices.Count) {
                        GameObject dotInstance = Instantiate(lineDotPrefab, Vector3.zero, Quaternion.identity);
                        dotInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
                        lineDots.Add(dotInstance);
                    }

                    //Move all dots to an out of game location
                    foreach (GameObject dot in lineDots) {
                        dot.transform.position = unusedElementOffset;
                    }

                    //Move the required number of dots into position
                    for (int i = 1; i < path.vertices.Count - 1; i++) {
                        lineDots[i - 1].transform.position = path.vertices[i].worldPosition;
                    }

                    //If the object pool doesn't have enough lines create more
                    while (lineSegments.Count < path.vertices.Count) {
                        GameObject lineInstance = Instantiate(lineSegmentPrefab, Vector3.zero, Quaternion.identity);
                        lineInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
                        lineSegments.Add(lineInstance);
                    }

                    //Move the line segments into place and rotate in the required direction
                    for (int i = 0; i < path.vertices.Count - 1; i++) {
                        lineSegments[i].transform.position = (path.vertices[i].worldPosition + path.vertices[i + 1].worldPosition) / 2;
                        lineSegments[i].transform.forward = (path.vertices[i + 1].worldPosition - path.vertices[i].worldPosition).normalized;

                        if ((Mathf.Abs(lineSegments[i].transform.forward.x) < 1f && Mathf.Abs(lineSegments[i].transform.forward.x) > 0f)) {
                            lineSegments[i].transform.localScale = new Vector3(1, 1, 1.5f);
                        } else {
                            lineSegments[i].transform.localScale = new Vector3(1, 1, 1);
                        }
                    }

                    //Move the rest to an out of game location
                    for (int j = path.vertices.Count - 1; j < lineSegments.Count; j++) {
                        lineSegments[j].transform.position = unusedElementOffset;
                    }

                    //Move the ground marker to the final vertex in the path
                    groundMarkerInstance.transform.position = path.vertices[path.vertices.Count - 1].worldPosition;

                    //Move and update the UI feet text element
                    if (selectedHero.MovementController.infinityMovement == true) {
                        UIManager.instance.UpdateFeetCostText(groundMarkerInstance.transform.position, Mathf.RoundToInt(path.length) + 1000);
                    } else {
                        UIManager.instance.UpdateFeetCostText(groundMarkerInstance.transform.position, Mathf.RoundToInt(path.length));
                    }
                    
                    if (selectedHero.MovementController.CanMoveDistance(path.length)) {
                        UIManager.instance.SetSpeedCostToValid();
                    } else {
                        UIManager.instance.SetSpeedCostToInvalid();
                    }
                }
                #endregion
                
                #region Confirm Path Movement
                //Set the relevent flags depending of which button is pressed
                if (Input.GetMouseButtonDown(0)) {
                    if (path != null && selectedHero.MovementController.CanMoveDistance(path.length)) {
                        stillSearchingForTarget = false;

                        //If the characters has the AP required move along the path
                        path = selectedHero.MovementController.CheckForEncounterAndTrimPath(path);
                        selectedHero.MovementController.MoveCharacter(path);
                    }
                } else if (Input.GetMouseButtonDown(1)) {
                    stillSearchingForTarget = false;
                    actionRunning = false;

                    UIManager.instance.EnablePlayerControls();
                }
                #endregion

            }
            #endregion

            yield return null;
		}

		//Clear the path overlay
		Destroy(groundMarkerInstance);
		for (int j = 0; j < lineDots.Count; j++) {
			Destroy(lineDots[j]);
			Destroy(lineSegments[j]);
		}
        UIManager.instance.HideFeetCostText();

        while (actionRunning) {
            yield return null;
        }

        //Check if the character is in enemy territory and trigger their turn if so
        if (EnemyAIManager.instance.GetAllEncounterVertices().Contains(selectedHero.MovementController.GraphObstacle.currentVertex)) {
            EnemyAIManager.instance.GetEncounterThatVertexIsPartOf(selectedHero.MovementController.GraphObstacle.currentVertex).encounterActive = true;

            GameManager.instance.EndPlayersTurn();
        } else {
            UIManager.instance.EnablePlayerControls();

            if (EnemyAIManager.instance.combatActive == false) {
                selectedHero.MovementController.ResetDistanceMoved();
            }
        }
        

    }

    /// <summary>
    /// Trigger the interact action coroutine
    /// </summary>
	public void TriggerInteractAction () {
        if (playersTurn == true) {
            if (EnemyAIManager.instance.combatActive == false) {
                StartCoroutine(TriggerInteractActionCoroutine());
            } else {
                Debug.Log("Cannot use the interaction action while in combat");
            }
		}
	}

    /// <summary>
    /// Interact coroutine to handle when the player clicks on an interactive object
    /// </summary>
    /// <returns></returns>
	IEnumerator TriggerInteractActionCoroutine () {
		UIManager.instance.DisablePlayerControls();
		actionRunning = true;

		bool awaitingSelection = true;
        IInteractable previousHoverItem = null;
		while (awaitingSelection) {
			RaycastHit rayHit = CameraManager.instance.FrameRayHit;

            if (rayHit.collider != null && rayHit.collider.CompareTag("Interactable")) {
                IInteractable interactableObject = rayHit.collider.GetComponent<IInteractable>();
                interactableObject.HighlightObject();

                if (previousHoverItem != null && previousHoverItem.Equals(interactableObject) == false) {
                    previousHoverItem.UnhighlightObject();                    
                    previousHoverItem = interactableObject;
                } else {
                    previousHoverItem = interactableObject;
                }
            } else {
                if (previousHoverItem != null) {
                    previousHoverItem.UnhighlightObject();
                }
            }

            if (Input.GetMouseButtonDown(0)) {
                if (rayHit.collider != null && rayHit.collider.CompareTag("Interactable")) {
				    IInteractable interactableObject = rayHit.collider.GetComponent<IInteractable>();
				    GraphObstacle objectObstacleComponent = rayHit.collider.GetComponent<GraphObstacle>();
                    NPCController npcController = rayHit.collider.GetComponent<NPCController>();
                    
                    //Is the selected hero next to the interactable object
                    if (objectObstacleComponent.currentVertex.GetNeighbouringVertices().Contains(selectedHero.MovementController.GraphObstacle.currentVertex)) {
					    selectedHero.MovementController.LookAtVector(objectObstacleComponent.currentVertex.worldPosition);
                        if (npcController != null) {
                            npcController.MovementController.LookAtVector(selectedHero.transform.position);
                        }

                        interactableObject.TriggerInteraction();

					    awaitingSelection = false;
				    }

                    if (previousHoverItem != null) {
                        previousHoverItem.UnhighlightObject();
                    }
                }                
            } else if (Input.GetMouseButtonDown(1)) {
				awaitingSelection = false;

                if (previousHoverItem != null) {
                    previousHoverItem.UnhighlightObject();
                }
            }

			yield return null;
		}

		UIManager.instance.EnablePlayerControls();
		actionRunning = false;
	}
    
}