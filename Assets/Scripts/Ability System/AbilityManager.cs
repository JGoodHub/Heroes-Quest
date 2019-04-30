using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour {
    
    //-----SINGLETON SETUP-----

	public static AbilityManager instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	} 

    //-----VARIABLES-----

    public GameObject tileHighlightPrefab;

	[HideInInspector] public bool abilityRunning = false;

    //-----METHODS-----
	
    /// <summary>
    /// Get all characters positions onto of the vertices to be searched
    /// </summary>
    /// <param name="verticesToSearch">Set of vertices to search</param>
    /// <returns>A set of characters standing onto of the vertices to be searched</returns>
	public HashSet<CharacterController> GetCharactersOnVertices (HashSet<Vertex> verticesToSearch) {
		HashSet<CharacterController> charactersWithinRange = new HashSet<CharacterController>();
		foreach (Vertex v in verticesToSearch) {
			RaycastHit vRayHit;
			Ray vCheckRay = new Ray(v.worldPosition + new Vector3(0, 50, 0), Vector3.down);
			if (Physics.Raycast(vCheckRay, out vRayHit, 100f)) {
				if (vRayHit.collider.CompareTag("Hero") || vRayHit.collider.CompareTag("Enemy")) {
					charactersWithinRange.Add(vRayHit.collider.GetComponent<CharacterController>());
				}
			}
		}

		return charactersWithinRange;
	}

    #region Hero Ability Casting
    /// <summary>
    /// Starts the CastHeroAbilityCoroutine which handles the targeting and casting of a hero's ability
    /// </summary>
    /// <param name="caster">Character casting the ability</param>
    /// <param name="ability">Ability instance the character is casting</param>
    public void CastHeroAbility (CharacterController caster, Ability ability) {		
		StartCoroutine(CastHeroAbilityCoroutine(caster, ability));
	}

    /// <summary>
    /// Handles the targeting and casting of a hero's ability
    /// </summary>
    /// <param name="caster">Character casting the ability</param>
    /// <param name="ability">Ability instance the character is casting</param>
    /// <returns></returns>
	IEnumerator CastHeroAbilityCoroutine (CharacterController caster, Ability ability) {
		if (abilityRunning || caster.MovementController.CurrentlyMoving) {
			yield break;
		} else {
			abilityRunning = true;
		}
				
		//-----TARGETING-----
		//Based on the targeting mode setup in the ability, chose a valid target from those available
		CharacterController chosenTarget = null;
		List<GameObject> tileHighlights = new List<GameObject>();
		switch (ability.targetingMode.TargetType) {
			case Ability.TargetType.SINGLE:
				HashSet<Vertex> verticesInRange = PathManager.instance.GetVerticesInRange(caster.MovementController.GraphObstacle.currentVertex, ability.targetingMode.Range, false);

				foreach (Vertex v in verticesInRange) {
					GameObject tileHighlightInstance = Instantiate(tileHighlightPrefab, v.worldPosition, Quaternion.identity);
					tileHighlightInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
					tileHighlights.Add(tileHighlightInstance);
				}

				HashSet<CharacterController> charactersInRange = GetCharactersOnVertices(verticesInRange);
				foreach (CharacterController charController in charactersInRange) {
					charController.Highlight();
				}

				//Wait for the player to pick their desired target
				while (chosenTarget == null) {
                    UIManager.instance.DisableEndTurnButton();
					UIManager.instance.DisablePlayerControls();

					if (Input.GetMouseButtonDown(0)) {
						RaycastHit hit = CameraManager.instance.FireRaycastFromMousePosition();
						if (hit.collider != null && (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Hero"))) {
							CharacterController possibleTarget = hit.collider.GetComponent<CharacterController>();
							if (charactersInRange.Contains(possibleTarget)) {
								chosenTarget = possibleTarget;
							}
						}
					} else if (Input.GetMouseButtonDown(1)) {
                        UIManager.instance.EnableEndTurnButton();
						UIManager.instance.EnablePlayerControls();

						foreach(CharacterController e in charactersInRange) {
							e.Unhighlight();
						}
						
						abilityRunning = false;
						foreach (GameObject tileHighlight in tileHighlights) {
							Destroy(tileHighlight);
						}
						yield break;
					}

                    yield return null;
				}

				foreach(CharacterController e in charactersInRange) {
					e.Unhighlight();
				}
			break;
		}		

		foreach (GameObject tileHighlight in tileHighlights) {
			Destroy(tileHighlight);
		}

        caster.CharacterData.actionAvailable = false;
        UIManager.instance.DisableActionPoint();

		ability.StartAbility(caster, chosenTarget);
		while (abilityRunning) {
			yield return null;
		}

        UIManager.instance.EnableEndTurnButton();
        UIManager.instance.EnablePlayerControls();

        if (chosenTarget.gameObject.CompareTag("Enemy")) {
            EnemyController targetEnemy = chosenTarget.gameObject.GetComponent<EnemyController>();
            if (targetEnemy.encounterInstance.encounterActive == false) {
                targetEnemy.encounterInstance.encounterActive = true;
                GameManager.instance.EndPlayersTurn();
            }    
        }        

    }
    #endregion

    #region Enemy Ability Casting
    /// <summary>
    /// Starts the CastEnemyAbilityCoroutine which casts the enemies ability, the target is pre-determined so it's passed as a parameter
    /// </summary>
    /// <param name="caster">The enemy casting the ability</param>
    /// <param name="targetHero">The determined target hero</param>
    /// <param name="ability">The ability instance being cast</param>
    public void CastEnemyAbility (CharacterController caster, CharacterController targetHero, Ability ability) {
		StartCoroutine(CastEnemyAbilityCoroutine(caster, targetHero, ability));
	}

    /// <summary>
    /// Cast the enemies ability, the target is pre-determined so it's passed as a parameter
    /// </summary>
    /// <param name="caster"></param>
    /// <param name="targetHero"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    IEnumerator CastEnemyAbilityCoroutine (CharacterController caster, CharacterController targetHero, Ability ability) {
		abilityRunning = true;

		HashSet<Vertex> verticesInRange = PathManager.instance.GetVerticesInRange(caster.MovementController.GraphObstacle.currentVertex, ability.targetingMode.Range, false);
		HashSet<CharacterController> charactersInRange = GetCharactersOnVertices(verticesInRange);

		if (charactersInRange.Contains(targetHero)) {
            Debug.Log("Character in range");
            caster.CharacterData.actionAvailable = false;
			ability.StartAbility(caster, targetHero);
			while (abilityRunning) {
				yield return null;
			}
		} else {
			Debug.Log("Failed to cast, target not in range");
			abilityRunning = false;
		}		
	}
	#endregion

    /// <summary>
    /// Start a coroutine located in another script
    /// </summary>
    /// <param name="coroutine">The coroutine to run</param>
	public void StartRemoteCoroutine (IEnumerator coroutine) {
		StartCoroutine(coroutine);
	}

    /// <summary>
    /// Instantiates an object into the scene from another script
    /// </summary>
    /// <param name="prefab">The object to instantiate</param>
    /// <param name="position">The position of the object</param>
    /// <param name="rotation">The rotation of the object</param>
    /// <returns></returns>
    public static GameObject ExternalInstantiation (GameObject prefab, Vector3 position, Quaternion rotation) {
        return Instantiate(prefab, position, rotation);
    }

    /// <summary>
    /// Destroy an object from another script
    /// </summary>
    /// <param name="obj">The game object to destroy</param>
    /// <param name="delay">The delay before destroy said object</param>
    public static void ExternalDestroyCall (GameObject obj, float delay) {
        Destroy(obj, delay);
    }

}
