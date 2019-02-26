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

	[HideInInspector]
	public bool abilityRunning = false;

    //-----METHODS-----

	public void Initialise () {

	}
	
	public HashSet<CharacterController> GetCharactersOnVertices (HashSet<Vertex> verticesToSearch) {
		HashSet<CharacterController> charactersWithinRange = new HashSet<CharacterController>();
		foreach (Vertex v in verticesToSearch) {
			RaycastHit vRayHit;
			Ray vCheckRay = new Ray(v.WorldPosition + new Vector3(0, 50, 0), Vector3.down);
			if (Physics.Raycast(vCheckRay, out vRayHit, 100f)) {
				if (vRayHit.collider.CompareTag("Hero") || vRayHit.collider.CompareTag("Enemy")) {
					charactersWithinRange.Add(vRayHit.collider.GetComponent<CharacterController>());
				}
			}
		}

		return charactersWithinRange;
	}

	public void CastHeroAbility (CharacterController caster, Ability ability) {
		StartCoroutine(CastHeroAbilityCoroutine(caster, ability));
	}

	IEnumerator CastHeroAbilityCoroutine (CharacterController caster, Ability ability) {
		if (abilityRunning || caster.MovementController.CurrentlyMoving) {
			yield break;
		} else {
			abilityRunning = true;
		}
				
		//-----TARGETING-----
		//Based on the targetting mode setup in the ability, chose a valid target from those available
		CharacterController chosenTarget = null;
		List<GameObject> tileHighlights = new List<GameObject>();
		switch (ability.targetingMode.TargetType) {
			case Ability.TargetType.SINGLE:
				HashSet<Vertex> verticesInRange = PathfindingManager.instance.Graph.GetVerticesInRange(caster.MovementController.CurrentVertex, ability.targetingMode.Range, false);

				foreach (Vertex v in verticesInRange) {
					GameObject tileHighlightInstance = Instantiate(tileHighlightPrefab, v.WorldPosition, Quaternion.identity);
					tileHighlightInstance.transform.SetParent(GameManager.instance.runtimeObjectsPool.transform);
					tileHighlights.Add(tileHighlightInstance);
				}

				HashSet<CharacterController> charactersInRange = GetCharactersOnVertices(verticesInRange);
				foreach (CharacterController charController in charactersInRange) {
					charController.Highlight();
				}

				//Wait for the player to pick their desired target
				while (chosenTarget == null) {					
					UIManager.instance.DisableAbilityButtons();
					UIManager.instance.DisableEndTurnButton();

					if (Input.GetMouseButtonDown(0)) {
						RaycastHit hit = CameraManager.instance.FireRaycastFromMousePosition();
						if (hit.collider != null && (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Hero"))) {
							CharacterController possibleTarget = hit.collider.GetComponent<CharacterController>();
							if (charactersInRange.Contains(possibleTarget)) {
								chosenTarget = possibleTarget;
							}
						}
					} else if (Input.GetMouseButtonDown(1)) {
						//Cancel the casting
						UIManager.instance.EnableAbilityButtons();
						UIManager.instance.EnableEndTurnButton();

						foreach(CharacterController e in charactersInRange) {
							e.Unhighlight();
						}
						
						abilityRunning = false;
						foreach (GameObject tileHighlight in tileHighlights) {
							Destroy(tileHighlight);
						}
						yield break;
					}
					//Wait for the next frame
					yield return null;
				}

				foreach(CharacterController e in charactersInRange) {
					e.Unhighlight();
				}
			break;
			case Ability.TargetType.AOE_TARGETED:
				//TODO				
			break;
		}		

		foreach (GameObject tileHighlight in tileHighlights) {
			Destroy(tileHighlight);
		}

		ability.StartAbility(caster, chosenTarget);
		while (abilityRunning) {
			//Wait for the next frame
			yield return null;
		}

		//unlock the UI
		UIManager.instance.EnableAbilityButtons();
		UIManager.instance.EnableEndTurnButton();
	}

	//Cast the enemies ability, the target is pre-determined so it's passed as a parameter
	public void CastEnemyAbility (CharacterController caster, CharacterController targetHero, Ability ability) {
		StartCoroutine(CastEnemyAbilityCoroutine(caster, targetHero, ability));
	}

	//Coroutine for the enemy ability casting
	IEnumerator CastEnemyAbilityCoroutine (CharacterController caster, CharacterController targetHero, Ability ability) {
		abilityRunning = true;

		ability.StartAbility(caster, targetHero);		
		while (abilityRunning) {
			yield return null;
		}
	}

	public void StartExternalCoroutine (IEnumerator coroutine) {
		StartCoroutine(coroutine);
	}

	//-----GIZMOS-----

	public bool drawGizmos;
	void OnDrawGizmos () {

	}

}
