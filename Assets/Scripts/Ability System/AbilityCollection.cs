using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityCollection : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static AbilityCollection instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    private Dictionary<string, Ability> abilityDict = new Dictionary<string, Ability>();
	public Dictionary<string, Ability> AbilityDict { get => abilityDict; }

    //-----METHODS-----

    /// <summary>
    /// Create all the abilities and store them in a dictionary using their ID's as keys
    /// </summary>
    public void Initialise () {
		#region Slash Ability
		//-----EFFECT COMPONENTS-----
		EffectComponent slashBloodHit = new EffectComponent(
            EffectComponent.VFXType.PARTICLE_SYSTEM,
            "Slash", 1f, null, null);

		EffectComponent slashAnim = new EffectComponent(
			EffectComponent.VFXType.ANIMATION, "Swing Sword", 
			0.5f, new Ability.EffectOnTarget(Ability.EffectType.DAMAGE, -10), 
			slashBloodHit);
			
		//-----ABILITY-----
		Ability slashAbility = new Ability (
			"Slash", "A powerful strike with the sword should do it",
			new Ability.TargetingMode(Ability.TargetType.SINGLE, 1.5f),
			new StatChange[]{},	slashAnim);

		abilityDict.Add(slashAbility.name, slashAbility);
        #endregion

        #region Medium Fireball Ability
        //-----EFFECT COMPONENTS-----
        EffectComponent mediumExplosion = new EffectComponent(
            EffectComponent.VFXType.PARTICLE_SYSTEM,
            "Medium Explosion",
            1.5f,
            null,
            null);

		EffectComponent fireBall = new EffectComponent(
			EffectComponent.VFXType.PROJECTILE,
			"Fireball",
			75f,
			new Ability.EffectOnTarget(Ability.EffectType.DAMAGE, -6),
			mediumExplosion);
		fireBall.SetStartOffset(new Vector3(0.5f, 4.2f, 3f));

		EffectComponent throwMagicAnim = new EffectComponent(
            EffectComponent.VFXType.ANIMATION,
            "Cast Projectile",
            0.6f,
            null,
            fireBall);
			
		//-----ABILITY-----
		Ability mediumFireballAbility = new Ability(
			"Medium Fireball",
			"The power of the sun in my hands",
			new Ability.TargetingMode(Ability.TargetType.SINGLE, 9f),
			new StatChange[]{new StatChange(ResourceType.MANA, -5)},
			throwMagicAnim);

		abilityDict.Add(mediumFireballAbility.name, mediumFireballAbility);
		#endregion

		#region Bash Ability
		//-----EFFECTS-----
		EffectComponent bashAnim = new EffectComponent(
			EffectComponent.VFXType.ANIMATION, 
			"Swing Fist", 
			0.5f, 
			new Ability.EffectOnTarget(Ability.EffectType.DAMAGE, -5), 
			slashBloodHit);
			
		//-----ABILITY-----
		Ability bashAbility = new Ability (
			"Bash",
			"Who needs a weapon to kill",
			new Ability.TargetingMode(Ability.TargetType.SINGLE, 1.5f),
			new StatChange[]{},
			bashAnim);

		abilityDict.Add(bashAbility.name, bashAbility);
		#endregion

    }


}
