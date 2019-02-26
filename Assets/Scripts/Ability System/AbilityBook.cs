using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityBook : MonoBehaviour {

    //-----SINGLETON SETUP-----

	public static AbilityBook instance = null;
	
	void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy(gameObject);
		}
	}

    //-----VARIABLES-----

    private Dictionary<string, Ability> abilityDict = new Dictionary<string, Ability>();
	public Dictionary<string, Ability> AbilityDict {
		get { return this.abilityDict; }
	}

    //-----METHODS-----

    //Create all the abilities and store them under their ids
    public void Initialise () {
		#region Slash Ability
			VFXEffect slashBloodHit = new VFXEffect(VFXEffect.EffectType.PARTICLE_SYSTEM, "Slash", 1f, null, null);
			VFXEffect slashAnim = new VFXEffect(
				VFXEffect.EffectType.ANIMATION, 
				"Swing Sword Diagonally", 
				0.5f, 
				new Ability.Effect(Ability.EffectType.DAMAGE, -10), 
				slashBloodHit);
			
			Ability slashAbility = new Ability (
				"Slash",
				"A powerful strike with the sword should do it",
				new Ability.TargetingMode(Ability.TargetType.SINGLE, 1.5f, 0),
				new StatChange[]{new StatChange(ResourceType.ACTIONPOINTS, -2)},
				slashAnim);

			abilityDict.Add(slashAbility.name, slashAbility);
		#endregion

		#region Medium Fireball Ability
			VFXEffect mediumExplosion = new VFXEffect(VFXEffect.EffectType.PARTICLE_SYSTEM, "Medium Explosion", 1.5f, null, null);
			VFXEffect fireBall = new VFXEffect(
				VFXEffect.EffectType.PROJECTILE,
				"Fireball",
				75f,
				new Ability.Effect(Ability.EffectType.DAMAGE, -10),
				mediumExplosion);
			fireBall.SetStartOffset(new Vector3(0.5f, 4.2f, 3f));
			VFXEffect throwMagicAnim = new VFXEffect(VFXEffect.EffectType.ANIMATION, "Cast Projectile", 0.6f, null, fireBall);
			
			Ability mediumFireballAbility = new Ability (
				"Medium Fireball",
				"The power of the sun in my hands",
				new Ability.TargetingMode(Ability.TargetType.SINGLE, 7f, 1.5f),
				new StatChange[]{new StatChange(ResourceType.MANA, -5), new StatChange(ResourceType.ACTIONPOINTS, -2)},
				throwMagicAnim);

			abilityDict.Add(mediumFireballAbility.name, mediumFireballAbility);
		#endregion

		#region Bash Ability
			VFXEffect bashAnim = new VFXEffect(
				VFXEffect.EffectType.ANIMATION, 
				"Swing Fist", 
				0.5f, 
				new Ability.Effect(Ability.EffectType.DAMAGE, -10), 
				slashBloodHit);
			
			Ability bashAbility = new Ability (
				"Bash",
				"Who needs a weapon to kill",
				new Ability.TargetingMode(Ability.TargetType.SINGLE, 1.5f, 0f),
				new StatChange[]{new StatChange(ResourceType.ACTIONPOINTS, -1)},
				bashAnim);

			abilityDict.Add(bashAbility.name, bashAbility);
		#endregion
		
		//... Ability

    }


}
