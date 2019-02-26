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

    public void Initialise () {
        foreach (GameObject interactableGameObject in GameObject.FindGameObjectsWithTag("Interactable")) {
            try {
                IInteractable interactableInstance = interactableGameObject.GetComponent<IInteractable>();
                interactableInstance.Initialise();
                interactableObjects.Add(interactableInstance);
            } catch (Exception e) {
                Debug.LogError("Object tagged as interactable but no IInterable child found, check tag and scripts applied");
                Debug.Log(e);
            }
        }


    }


}
