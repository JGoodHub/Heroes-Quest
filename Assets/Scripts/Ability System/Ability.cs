using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability {

    //-----ENUMS-----

    public enum TargetType {SINGLE};
    public enum EffectType {DAMAGE, RESTORE};

    //-----STRUCTS-----

    //Targeting Mode Struct
    public struct TargetingMode {
        public TargetType TargetType;
        public float Range;

        public TargetingMode (TargetType TargetType, float Range) {
            this.TargetType = TargetType;
            this.Range = Range;
        }
    }

    //Effect Class (To allow for null instances)
    public class EffectOnTarget {
        public EffectType EffectType;
        public int Amount;

        public EffectOnTarget (EffectType EffectType, int Amount) {
            this.EffectType = EffectType;
            this.Amount = Amount;
        }
    }

    //-----VARIABLES-----

    public string name;
    public string description;

    public TargetingMode targetingMode;
    public StatChange[] resourceCosts;
    public EffectComponent startEffect;

    //-----METHODS-----

    /// <summary>
    /// Setup the Ability instance
    /// </summary>
    /// <param name="name">Name of the ability</param>
    /// <param name="description">Description of the ability</param>
    /// <param name="targetingMode">The abilities targeting type</param>
    /// <param name="changes">The stat costs for the caster</param>
    /// <param name="startEffect">The first effect component to trigger</param>
    public Ability(string name, string description, TargetingMode targetingMode, StatChange[] resourceCosts, EffectComponent startEffect) {
        this.name = name;
        this.description = description;
        this.targetingMode = targetingMode;
        this.resourceCosts = resourceCosts;
        this.startEffect = startEffect;
    }

    /// <summary>
    /// Triggers the start of the ability
    /// </summary>
    /// <param name="source">The source character caster</param>
    /// <param name="target">The target enemy or hero of the ability</param>
    public void StartAbility (CharacterController source, CharacterController target) {
        //Turn the characters to face each other
        source.transform.forward = new Vector3(target.transform.position.x, source.transform.position.y, target.transform.position.z) - source.transform.position;
        target.transform.forward = source.transform.forward * -1;

        //Apply the cost of the ability to the characters stats
        foreach (StatChange change in resourceCosts) {
            source.CharacterData.ApplyChangeToData(change);
        }

        //Start the chain of effect components
        startEffect.StartVisualEffect(source, target);
    }


}
