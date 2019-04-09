using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : CharacterController {

    //-----VARIABLES-----

    //The hero the enemy is targeting
    private CharacterController targetHero;

    [HideInInspector]
    public EnemyEncounter encounterInstance;

    //-----METHODS-----

    //Override the CharacterController to parse the hero directly to the ability, rather than using the target system
    public override void CastAbility (int abilityIndex) {
        //Get a reference to the chosen ability
        Ability chosenAbility = abilities[abilityIndex];
        
        Debug.Log(abilities[abilityIndex].statChanges[0].Resource + ": " + abilities[abilityIndex].statChanges[0].Amount);
        Debug.Log(CharacterData.GetResourceOfType(ResourceType.ACTIONPOINTS));

        //If met begin the casting of the ability
        if (CharacterHasResourcesToCast(chosenAbility)) {
            Debug.Log("Casting ability");
            AbilityManager.instance.CastEnemyAbility(this, targetHero, abilities[abilityIndex]);
        } else {
            Debug.Log ("Not enough resources to cast ability");
        }
    }

    //Set the enemies target hero
    public void SetTarget (CharacterController newTargetHero) {
        targetHero = newTargetHero;
    }

}