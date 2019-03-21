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
	public HashSet<CharacterController> HeroSet {
		get { return heroSet; }
	}
	
    private CharacterController selectedHero;
	public CharacterController SelectedHero {
		get { return selectedHero; }
	}

	private bool playersTurn;
	private bool actionRunning;
	public bool ActionRunning { set => actionRunning = value; }
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
		} else if(Input.GetKeyDown(KeyCode.Equals)) {
			Debug.LogError("ERROR");
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
			h.CharacterData.ApplyChangeToData(new StatChange(ResourceType.ACTIONPOINTS, 3));
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
		UIManager.instance.DisableUI();
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
		bool heroHasRequiredActionPoints = false;

		Path path = null;
		
		while (stillSearchingForTarget) {
			#region Path Selection
			//Check if the mouse is over a tile object
			RaycastHit rayHit = CameraManager.instance.FrameRayHit;
			if (rayHit.collider != null && rayHit.collider.CompareTag("Tile")) {
				//Calculate a path to that tile
				Vertex target = PathManager.instance.GetClosestVertexToCoordinates(PathManager.TranslateWorldSpaceToGraphCoordinates(rayHit.point));
				Vertex source = selectedHero.MovementController.GraphObstacle.CurrentVertex;
				path = PathManager.instance.FindShortestPathBetween(source, target);
				
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
					UIManager.instance.UpdateAPCostText(groundMarkerInstance.transform.position, selectedHero.MovementController.APCostOfMovement(path));
					if (selectedHero.MovementController.APCostOfMovement(path) <= selectedHero.CharacterData.GetResourceOfType(ResourceType.ACTIONPOINTS)) {
						UIManager.instance.SetAPCostToValid();	
						heroHasRequiredActionPoints = true;				
					} else {
						UIManager.instance.SetAPCostToInvalid();
						heroHasRequiredActionPoints = false;
					}
				}	
			}
			#endregion

			#region Path Conformation
			//Set the relevent flags depending of which button is pressed
			if (Input.GetMouseButtonDown(0) && path != null && heroHasRequiredActionPoints) {
				stillSearchingForTarget = false;

				//If the characters has the AP required move along the path
				path = selectedHero.MovementController.CheckForEncounterAndTrimPath(path);
				selectedHero.MovementController.MoveCharacter(path, 0f);
			} else if (Input.GetMouseButtonDown(1)) {
				stillSearchingForTarget = false;
				
				actionRunning = false;
				UIManager.instance.EnableUI();
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
		UIManager.instance.HideAPCostText();
	}

	public void TriggerInteractAction () {
		if (playersTurn == true) {
			StartCoroutine(TriggerInteractActionCoroutine());
		}
	}

	IEnumerator TriggerInteractActionCoroutine () {
		UIManager.instance.ChangeCursorTo(UIManager.CursorType.INTERACT);
		UIManager.instance.DisableUI();
		actionRunning = true;

		bool awaitingSelection = true;
		while (awaitingSelection == true) {
			RaycastHit rayHit = CameraManager.instance.FrameRayHit;
			if (rayHit.collider != null && rayHit.collider.CompareTag("Interactable")) {
				if (Input.GetMouseButtonDown(0)) {
					IInteractable interactableObject = rayHit.collider.GetComponent<IInteractable>();
					GraphObstacle objectObstacleComponent = interactableObject.GraphObstacleComponent;
					if (objectObstacleComponent.CurrentVertex.GetNeighbouringVertices().Contains(selectedHero.MovementController.GraphObstacle.CurrentVertex)) {
						selectedHero.MovementController.LookAtVector(objectObstacleComponent.CurrentVertex.WorldPosition);
						interactableObject.TriggerInteraction();
						awaitingSelection = false;
					}
				}  
			}

			if (Input.GetMouseButtonDown(1)) {
				awaitingSelection = false;
				actionRunning = false;
			}

			yield return null;
		}


		UIManager.instance.ChangeCursorTo(UIManager.CursorType.DEFAULT);
		UIManager.instance.EnableUI();
		actionRunning = false;
	}

	//-----GIZMOS-----
	public bool drawGizmos;
	void OnDrawGizmos () {
		
	}

}