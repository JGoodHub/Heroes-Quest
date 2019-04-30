using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : CharacterController {

    //-----VARIABLES-----

    private CharacterController targetHero;

    [HideInInspector] public EncounterController encounterInstance;

    //-----METHODS-----

    /// <summary>
    /// Override the CharacterController to parse the hero directly to the ability, rather than using the target system
    /// </summary>
    /// <param name="abilityIndex">The index of the ability to cast</param>
    public override void CastAbility (int abilityIndex) {
        //Get a reference to the chosen ability
        Ability chosenAbility = abilities[abilityIndex];
        
        //If met begin the casting of the ability
        if (CharacterHasResourcesToCast(chosenAbility)) {
            Debug.Log("Casting ability controller");
            AbilityManager.instance.CastEnemyAbility(this, targetHero, abilities[abilityIndex]);
        } else {
            Debug.Log ("Not enough resources to cast ability");
        }
    }

    /// <summary>
    /// Call the base death method and remove the enemies from the encounter
    /// </summary>
    public override void Die () {
        base.Die();
        encounterInstance.enemies.Remove(this);
    }

    /// <summary>
    /// Set the enemies target hero
    /// </summary>
    /// <param name="newTargetHero">The new hero to target</param>
    public void SetTarget (CharacterController newTargetHero) {
        targetHero = newTargetHero;
    }

}