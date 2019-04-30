using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

	//-----SINGLETON SETUP-----

	public static CameraManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

	//-----VARIABLES-----

	public float cameraPanSpeed;
	public float panEdgeSize;

	private bool userPanEnabled;
	public bool globalPanEnabled;
	public bool useArrowKeysInstead;	

	private RaycastHit frameRayHit;
	public RaycastHit FrameRayHit { get => frameRayHit; }

	private string lastHoveredTag = "";

	private GameObject trackingTarget;
	public float trackingSnapToSpeed;
	private bool trackingTargetReached = true;
	public bool TrackingTargetReached { get => trackingTargetReached; }

	private float yLock;
	
	//-----METHODS-----

    /// <summary>
    /// Setup for the camera
    /// </summary>
	public void Initialise () {
		yLock = transform.position.y;
	}

    /// <summary>
    /// Get the ray from the camera is the direction of the mouse
    /// </summary>
    /// <returns></returns>
    public Ray GetCameraRay () {
		return GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
	}

    /// <summary>
    /// Fire a raycast using the camera ray and return what it hit, if anything
    /// </summary>
    /// <returns>The RaycastHit for the collider hit</returns>
    public RaycastHit FireRaycastFromMousePosition () {
		RaycastHit hit;
		Physics.Raycast(GetCameraRay(), out hit, 1000f);
		return hit;
	}
	
    /// <summary>
    /// Update called once per frame
    /// </summary>
	void Update () {
		//Check if the mouse is on the edge of the screen and move the camera in the corresponding direction
		#region Camera Pan
			//Find the mouse position on the screen and calculate the two vectors needed for translation
			Vector2 mousePosition = (Vector2) Input.mousePosition;
			Vector3 rightTranslationVector = transform.right;
			Vector3 forwardtranslationVector = Vector3.Scale(transform.forward, new Vector3(1, 0, 1)).normalized;

			if (userPanEnabled && globalPanEnabled) {
				//Check if panning conditions are met, then translate in that directions respectively
				if ((mousePosition.x < panEdgeSize && !useArrowKeysInstead) || (Input.GetKey(KeyCode.LeftArrow) && useArrowKeysInstead)) {
					transform.position += (-rightTranslationVector * cameraPanSpeed * Time.deltaTime);		
				} else if ((mousePosition.x > Screen.width - panEdgeSize && !useArrowKeysInstead) || (Input.GetKey(KeyCode.RightArrow) && useArrowKeysInstead)) {
					transform.position += (rightTranslationVector * cameraPanSpeed * Time.deltaTime);
				}

				if ((mousePosition.y < panEdgeSize && !useArrowKeysInstead) || (Input.GetKey(KeyCode.DownArrow) && useArrowKeysInstead)) {
					transform.position += (-forwardtranslationVector * cameraPanSpeed * Time.deltaTime);
				} else if ((mousePosition.y > Screen.height - panEdgeSize && !useArrowKeysInstead) || (Input.GetKey(KeyCode.UpArrow) && useArrowKeysInstead)) {
					transform.position += (forwardtranslationVector * cameraPanSpeed * Time.deltaTime);
				}
			}
		#endregion

		//Check if the mouse if hovering over a specific object that requires a UI action
		#region Mouse Hover Detection
			//Fire a new raycast each frame to see if the user has hovered over something
			frameRayHit = FireRaycastFromMousePosition();

			if (frameRayHit.collider != null) {
				//Take action on what's just been un-hovered
				if (frameRayHit.collider.tag != lastHoveredTag) {
					switch (lastHoveredTag) {
						case "Enemy":
							UIManager.instance.HideEnemyStatusBar();
						break;
					}
				}

				//Take action on what's just been hovered over
				switch (frameRayHit.collider.tag) {
					case "Enemy":
						UIManager.instance.ShowStatusBarForEnemy(frameRayHit.collider.GetComponent<CharacterController>());
						lastHoveredTag = "Enemy";
						break;
				}

				lastHoveredTag = frameRayHit.collider.tag;
			} else {
				//Switch to the default cursor and reset all UI elements to their default states
				//Debug.Log ("ray hit null, hide everthing, use default cursor");
			}			
		#endregion

		//Focus the camera on each enemy while their moving and attacking
		#region Pan Target Tracking
			if (trackingTarget != null) {
				userPanEnabled = false;
				
				//Interpolate the camera over to the target enemy and follow their movements
				Vector3 centreOnOffset = new Vector3 (45, 75, -45);
				Vector3 cameraPosition = transform.position;
				cameraPosition = Vector3.Lerp(cameraPosition, trackingTarget.transform.position + centreOnOffset, trackingSnapToSpeed * Time.deltaTime);
				cameraPosition.y = yLock;
				transform.position = cameraPosition;

                Vector2 targetPosition = new Vector2(trackingTarget.transform.position.x + centreOnOffset.x, trackingTarget.transform.position.z + centreOnOffset.z);
				if (Vector2.Distance(new Vector2(cameraPosition.x, cameraPosition.z), targetPosition) < 0.5f) {
					trackingTargetReached = true;
				}
			} else {
				trackingTargetReached = false;
				userPanEnabled = true;
			}
		#endregion		
	}

    /// <summary>
    /// Set the MovementController that the camera should focus on
    /// </summary>
    /// <param name="newTarget">The target to start tracking</param>
    public void SetTrackingTarget (GameObject newTarget) {
		trackingTarget = newTarget;
		trackingTargetReached = false;
	}

    /// <summary>
    /// Clear the focus target
    /// </summary>
    public void ClearTrackingTarget () {
		trackingTarget = null;
	}


}
