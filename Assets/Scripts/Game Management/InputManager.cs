using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static InputManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    //Dictionary of actions linked to mouse presses
    private Dictionary<int, Action> mouseDownActions = new Dictionary<int, Action>();
    public Dictionary<int, Action> MouseDownActions {
        get { return mouseDownActions; }
    }

    //Dictionary of actions linked to key presses
    private Dictionary<KeyCode, Action> keyDownActions = new Dictionary<KeyCode, Action>();
    public Dictionary<KeyCode, Action> KeyDownActions {
        get { return keyDownActions; }
    }

    //-----METHODS-----

    void Update () {
        //Call each of the actions for each mouse button key
        foreach (int mouseButton in mouseDownActions.Keys) {
            if (mouseButton <= 2 && Input.GetMouseButtonDown(mouseButton)) {
                mouseDownActions[mouseButton]();
            }
        }

        //Call each of the actions for each keycode key
        foreach (KeyCode kCode in keyDownActions.Keys) {
            if (Input.GetKeyDown(kCode)) {
                keyDownActions[kCode]();
            }
        }
    }






}
