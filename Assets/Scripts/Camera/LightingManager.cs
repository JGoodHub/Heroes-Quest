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

    private HashSet<ParticleSystem> lightParticleSystems;
    private HashSet<Light> lightSources;

    //-----METHODS-----    

    /// <summary>
    /// Setup the manager and get references to all dynamic light sources in the world
    /// </summary>
    public void Initialise () {
        degreesPerSecond = 360f / (dayLengthInMinutes * 60);

        lightParticleSystems = new HashSet<ParticleSystem>();
        lightSources = new HashSet<Light>();

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("World Light")) {
            ParticleSystem lightParticle = obj.GetComponent<ParticleSystem>();
            Light lightSource = obj.GetComponent<Light>();

            if (lightParticle != null) {
                lightParticle.Stop();
                lightParticleSystems.Add(lightParticle);
            }

            if (lightSource != null) {
                lightSource.gameObject.SetActive(false);
                lightSources.Add(lightSource);
            }
        }
    }

    /// <summary>
    /// Rotates the sun over the course of the time period specified
    /// </summary>
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
