using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractablesManager : MonoBehaviour {
    
    //-----SINGLETON SETUP-----

	public static InteractablesManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    private HashSet<IInteractable> interactableObjects = new HashSet<IInteractable>();

    //-----METHODS-----

    /// <summary>
    /// Get reference to all interactable object in the scene
    /// </summary>
    public void Initialise () {
        foreach (GameObject interactableGameObject in GameObject.FindGameObjectsWithTag("Interactable")) {
            try {
                IInteractable interactableInstance = interactableGameObject.GetComponent<IInteractable>();
                interactableInstance.Initialise();
                interactableInstance.UnhighlightObject();
                interactableObjects.Add(interactableInstance);
            } catch (Exception e) {
                Debug.LogError("\"" + interactableGameObject.name + "\" tagged as Interactable but no IInterable component found, check tag and scripts applied");
                Debug.LogError(e);
            }
        }


    }


}
