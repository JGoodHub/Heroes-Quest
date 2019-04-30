using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterData : MonoBehaviour {

    //-----VARIABLES-----  

    public string characterName;

    public int maxHealth;
    private int health;

    public int maxMana;
    private int mana;

    public bool actionAvailable = true;

    public int level = 0;
    public int experience = 0;

    [HideInInspector] public CharacterController characterController;

    //-----METHODS-----

    /// <summary>
    /// Setup the health and mana values
    /// </summary>
    public void Initialise () {
        health = maxHealth;
        mana = maxMana;
    }

    /// <summary>
    /// Return the current amount of the specified resource
    /// </summary>
    /// <param name="type">The resource type to get</param>
    /// <returns>The amount of that resource</returns>
    public int GetResourceOfType (ResourceType type) {
        switch (type) {
            case ResourceType.HEALTH:
                return health;
            case ResourceType.MANA:
                return mana;
            case ResourceType.EXPERIENCE:
                return experience;
            default:
                Debug.LogError("Attempting to get a resource that doesn't exist");
                return 0;
        }
    }

    /// <summary>
    /// Apply a change to the specified resource by the amount in the change
    /// </summary>
    /// <param name="change">The resource to change and amount to change it by</param>
    public void ApplyChangeToData (StatChange change) {
        switch (change.Resource) {
            case ResourceType.HEALTH:
                health = (int) Mathf.Clamp(health + change.Amount, 0, maxHealth);
                if (health <= 0) {
                    TriggerCharacterDeath();
                }
                break;
            case ResourceType.MANA:
                mana = (int) Mathf.Clamp(mana + change.Amount, 0, maxMana);                
                break;
            case ResourceType.EXPERIENCE:
                experience += change.Amount;
                while (experience >= LevelToExperience(level + 1)) {
                    experience -= LevelToExperience(level);
                    level++;                    
                }
                break;
            default:
                Debug.LogError("Attempting to change a resource that doesn't exist");
                break;
        }

        //Update the UI to reflect the changes
        UIManager.instance.UpdateHeroStatusBar();
    }

    /// <summary>
    /// Trigger the characters death if their at 0 health
    /// </summary>
    public void TriggerCharacterDeath () {
        characterController.Die();
    } 

    /// <summary>
    /// Convert the  level into pure experience
    /// </summary>
    /// <param name="level">The level to convert</param>
    /// <returns>The experience point equivalent</returns>
    public int LevelToExperience (int level) {
        return Mathf.RoundToInt(Mathf.Pow(level, 1.5f) * 100);
    }

}

//-----RELATED DATA STRUCTURES-----

public enum ResourceType {HEALTH, MANA, EXPERIENCE};

public struct StatChange {
    public ResourceType Resource;
    public int Amount;

    public StatChange (ResourceType resource, int amount) {
        Resource = resource;
        Amount = amount;
    }
}
