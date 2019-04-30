using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterData))]
[RequireComponent(typeof(MovementController))]
public class CharacterController : MonoBehaviour {

    //-----VARIABLES-----
    
    protected CharacterData characterData;
    public CharacterData CharacterData { get => characterData; }

    protected MovementController movementController;
    public MovementController MovementController { get => movementController; }

    public GameObject selectionHightlight;

    public string[] abilityIDs;
    protected Ability[] abilities;

    //-----METHODS-----

    /// <summary>
    /// Sets up the component references
    /// </summary>
    public virtual void Initialise () {
        //Grab the reference to the other scripts on this object
        characterData = GetComponent<CharacterData>();
        characterData.Initialise();
        characterData.characterController = this;

        movementController = GetComponent<MovementController>();
        movementController.Initialise();

        //Using the ability ID's grab the references to their objects from the ability book
        abilities = new Ability[abilityIDs.Length];
        for (int i = 0; i < abilityIDs.Length; i++) {
            try {
                abilities[i] = AbilityCollection.instance.AbilityDict[abilityIDs[i]];
            } catch (Exception e) {
                //Throw an error if no ability is found that matches that ID
                Debug.LogError("Ability ID not found, check ability book definitions and spelling");
                Debug.LogError(e);
            }
        }

        //De-select the character by default
        Unhighlight();
    }

    /// <summary>
    /// Activate the characters highlight object
    /// </summary>
    public void Highlight() {
        selectionHightlight.SetActive(true);
    }

    /// <summary>
    /// Deactivate the characters highlight object
    /// </summary>
    public void Unhighlight() {
        selectionHightlight.SetActive(false);
    }

    /// <summary>
    /// Check if the character has the required resources to cast the ability
    /// </summary>
    /// <param name="ability">The ability the character wants to cast</param>
    /// <returns>A boolean of whether the character can cast the ability</returns>
    public bool CharacterHasResourcesToCast (Ability ability) {
        bool costRequirementsMet = true;

        //Check that the hero has enough resources to cast the ability
        foreach (StatChange change in ability.resourceCosts) {
            if (characterData.GetResourceOfType(change.Resource) < change.Amount * -1) {
                costRequirementsMet = false;
            }
        }

        if (characterData.actionAvailable == false) {
            costRequirementsMet = false;
        }

        return costRequirementsMet;
    }

    /// <summary>
    /// Cast one of the characters abilities
    /// </summary>
    /// <param name="abilityIndex">The index of the ability to cast</param>
    public virtual void CastAbility (int abilityIndex) {
        //Get a reference to the chosen ability
        Ability chosenAbility = abilities[abilityIndex];
        
        //If met begin the casting of the ability
        if (CharacterHasResourcesToCast(chosenAbility)) {
            AbilityManager.instance.CastHeroAbility(this, abilities[abilityIndex]);
        } else {
            Debug.Log ("Not enough resources to cast ability");
        }
    }

    /// <summary>
    /// Start the death coroutine
    /// </summary>
    public virtual void Die () {
        Debug.Log("Oh shit, i'm dead");     
        StartCoroutine(DieCoroutine());  
    }

    /// <summary>
    /// Handles character death
    /// </summary>
    /// <returns></returns>
    IEnumerator DieCoroutine () {
        yield return new WaitForSeconds(0.5f);
        movementController.disableMovement = true;
        movementController.GraphObstacle.UnblockCurrentVertex();
        GetComponentInChildren<Animator>().SetTrigger("Die");
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject, 10f);
        while (true) {
            transform.Translate(Vector3.down * 5f * Time.deltaTime);
            yield return null;
        }
        
    }

}
