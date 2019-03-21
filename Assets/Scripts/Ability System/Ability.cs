using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability {

    //-----ENUMS-----

    public enum TargetType {SINGLE, AOE_TARGETED};
    public enum EffectType {DAMAGE, RESTORE};

    //-----STRUCTS-----

    //Targeting Mode Struct
    public struct TargetingMode {
        public TargetType TargetType;
        public float Range;
        public float AOEDiameter;

        public TargetingMode (TargetType _TargetType, float _Range, float _AOEDiameter) {
            TargetType = _TargetType;
            Range = _Range;
            AOEDiameter = _AOEDiameter;
        }
    }

    //Effect Struct(kinda)
    public class Effect {
        public EffectType EffectType;
        public int Amount;

        public Effect (EffectType _EffectType, int _Amount) {
            EffectType = _EffectType;
            Amount = _Amount;
        }
    }

    //-----VARIABLES-----

    public string name;
    public string description;

    public TargetingMode targetingMode;
    public StatChange[] statChanges;
    public VFXEffect vfxEffect;

    //-----METHODS-----

    public Ability (string _name, string _description, TargetingMode _targetingMode, StatChange[] _changes, VFXEffect _vfxEffect) {
        this.name = _name;
        this.description = _description;
        this.targetingMode = _targetingMode;
        this.statChanges = _changes;
        this.vfxEffect = _vfxEffect;
    }

    public void StartAbility (CharacterController source, CharacterController target) {
        //Turn the characters to face each other
        source.transform.forward = new Vector3(target.transform.position.x, source.transform.position.y, target.transform.position.z) - source.transform.position;
        target.transform.forward = source.transform.forward * -1;

        //Apply the cost of the ability to the characters stats
        foreach (StatChange change in statChanges) {
            source.CharacterData.ApplyChangeToData(change);
        }

        vfxEffect.StartVisualEffect(source, target);
    }


}
