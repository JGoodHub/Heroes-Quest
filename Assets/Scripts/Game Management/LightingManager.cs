using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static LightingManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

	//-----VARIABLES-----

    [Header("Sun Controls")]
    public Transform sunPivot;    
    public float dayLengthInMinutes;
    private float degreesPerSecond;
    public bool pauseCycle;
    [Range(0, 24)]
    public float manualPositionControl;

    //-----METHODS-----    

    public void Initialise () {
        degreesPerSecond = 360f / (dayLengthInMinutes * 60);
    }

    void Update() {
        if (pauseCycle == false) {
            sunPivot.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime);
        } else {
            Vector3 sunPivotNewRotation = sunPivot.transform.localEulerAngles;
            sunPivotNewRotation.y = manualPositionControl * 15f;
            sunPivot.transform.localEulerAngles = sunPivotNewRotation;

        }

    }

}
