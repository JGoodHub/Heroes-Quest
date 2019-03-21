using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterData))]
[RequireComponent(typeof(MovementController))]
public class CharacterController : MonoBehaviour {

    //-----VARIABLES-----
    
    //Reference to the objects character data script
    protected CharacterData characterData;
    public CharacterData CharacterData {
        get { return this.characterData; }
    }

    //Reference to the objects move controller script
    protected MovementController movementController;
    public MovementController MovementController {
        get { return this.movementController; }
    }

    //The characters highlight object
    public MeshRenderer selectionRing;

    //The ID's for each ability and a reference to its instance
    public string[] abilityIDs;
    protected Ability[] abilities;

    //-----METHODS-----

    //Setup Method
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
                abilities[i] = AbilityBook.instance.AbilityDict[abilityIDs[i]];
            } catch (Exception e) {
                //Throw an error if no ability is found that matches that ID
                Debug.LogError("Ability ID not found, check ability book definitions and spelling");
                Debug.LogError(e);
            }
        }

        //Deselect the character by default
        Unhighlight();
    }

    //Activate the characters highlight object
    public void Highlight() {
        selectionRing.enabled = true;
    }

    //Deactivate the characters highlight object
    public void Unhighlight() {
        selectionRing.enabled = false;
    }

    //Check if the character has the required resources to cast the ability
    public bool CharacterHasResourcesToCast (Ability ability) {
        bool costRequirementsMet = true;

        //Check that the hero has enough resources to cast the ability
        foreach (StatChange change in ability.statChanges) {
            if (characterData.GetResourceOfType(change.Resource) < change.Amount * -1) {
                costRequirementsMet = false;
            }
        }

        return costRequirementsMet;
    }

    //Cast one of the characters abilities
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

    public virtual void Die () {
        Debug.Log("Oh shit, i'm dead");     
        StartCoroutine(DieCoroutine());   
    }

    IEnumerator DieCoroutine () {
        yield return new WaitForSeconds(2f);
        PlayerManager.instance.HeroSet.Remove(this);
        PlayerManager.instance.SelectRandomHero();
        movementController.lockedDown = true;
        movementController.GraphObstacle.UnblockCurrentVertex();
        Unhighlight();
        GetComponentInChildren<Animator>().SetTrigger("Die");
        yield return new WaitForSeconds(1f);
        Destroy(gameObject, 10f);
        float counter = 0;
        while (counter < 3.87f || true) {
            //transform.Translate(-Vector3.down * 20f * Time.deltaTime);
            counter += Time.deltaTime;
            yield return null;
        }
        
    }

}
