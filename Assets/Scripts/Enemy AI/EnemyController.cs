using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : CharacterController {

    //-----VARIABLES-----

    //The hero the enemy is targeting
    private CharacterController targetHero;

    //-----METHODS-----

    //Override the CharacterController to parse the hero directly to the ability, rather than using the target system
    public override void CastAbility (int abilityIndex) {
        //Get a reference to the chosen ability
        Ability chosenAbility = abilities[abilityIndex];
        
        //If met begin the casting of the ability
        if (CharacterHasResourcesToCast(chosenAbility)) {
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