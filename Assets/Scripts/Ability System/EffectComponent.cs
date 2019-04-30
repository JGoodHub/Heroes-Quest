using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectComponent {

    //-----ENUMS-----

    public enum VFXType {ANIMATION, PROJECTILE, PARTICLE_SYSTEM};

    //-----VARIABLES-----

    private VFXType vfxType;
    private string effectID;

    private float dominoEffectDelay;
    
    private CharacterController source;
    private Vector3 startOffset;
    private CharacterController target;

    private Ability.EffectOnTarget effectOnTarget;
    private EffectComponent dominoEffect;

    //-----METHODS-----

    /// <summary>
    /// Setup for the effects variables
    /// </summary>
    /// <param name="vfxType">The type of visual effect</param>
    /// <param name="effectID">The effect ID, such as animation name</param>
    /// <param name="dominoEffectDelay">The delay in seconds before tarting the domino effect</param>
    /// <param name="effectOnTarget">The effect on the target</param>
    /// <param name="dominoEffect">The follow on effect when this one finishes</param>
    public EffectComponent (VFXType vfxType, string effectID, float dominoEffectDelay, Ability.EffectOnTarget effectOnTarget, EffectComponent dominoEffect) {
        this.vfxType = vfxType;        
        this.effectID = effectID;
        this.dominoEffectDelay = dominoEffectDelay;
        this.effectOnTarget = effectOnTarget;
        this.dominoEffect = dominoEffect;
    }

    /// <summary>
    /// Set the starting offset for the projectile
    /// </summary>
    /// <param name="offset"></param>
    public void SetStartOffset (Vector3 offset) {
        if (vfxType == VFXType.PROJECTILE) {
            startOffset = offset;
        }
    }
    
    /// <summary>
    /// Start the visual effect of the effect
    /// </summary>
    /// <param name="source">The source CharacterController</param>
    /// <param name="target">The target CharacterController</param>
    public void StartVisualEffect (CharacterController source, CharacterController target) {
        this.source = source;
        this.target = target;

        //Create the effect prefab at the given position and rotation
        switch (vfxType) {
            case VFXType.ANIMATION:
                //Trigger the animation on the casters animator controller
                source.GetComponentInChildren<Animator>().SetTrigger(effectID);

                AbilityManager.instance.StartRemoteCoroutine(OnFinish());
                break;
            case VFXType.PROJECTILE:
                //Create the projectile and have it fly towards the enemy, when it reaches them destroy it
                GameObject projectileInstance = AbilityManager.ExternalInstantiation(Resources.Load<GameObject>("Visual Effects/Projectiles/" + effectID), source.transform.position, Quaternion.identity);
                
                projectileInstance.transform.forward = target.transform.position - source.transform.position;

                projectileInstance.transform.Translate(Vector3.right * startOffset.x);
                projectileInstance.transform.Translate(Vector3.up * startOffset.y);
                projectileInstance.transform.Translate(Vector3.forward * startOffset.z);                

                AbilityManager.instance.StartRemoteCoroutine(ProjectileCoroutine(projectileInstance));
                break;
            case VFXType.PARTICLE_SYSTEM:
                //Create the particle system at the source and have it face in the direction of the target
                GameObject particleInstance = AbilityManager.ExternalInstantiation(Resources.Load<GameObject>("Visual Effects/Particle Systems/" + effectID), target.transform.position, Quaternion.identity);

                particleInstance.transform.forward = source.transform.position - target.transform.position;

                AbilityManager.ExternalDestroyCall(particleInstance, 10f);
                AbilityManager.instance.StartRemoteCoroutine(OnFinish());              
                break;
            default:
                Debug.LogError("Invalid effect state");
                return;
        } 
    }

    /// <summary>
    /// Coroutine for moving the projectile across the world
    /// </summary>
    /// <param name="projectileObject">The projectile game object</param>
    /// <returns></returns>
    public IEnumerator ProjectileCoroutine (GameObject projectileObject) {
        //Move the projectile forwards towards the target
        while (Vector3.Distance(projectileObject.transform.position, target.transform.position + new Vector3(0, 3.5f, 0)) > 0.1f) {
            projectileObject.transform.position = Vector3.MoveTowards(projectileObject.transform.position, target.transform.position + new Vector3(0, 3.5f, 0), dominoEffectDelay * Time.deltaTime);
            yield return null;
        }

        //Find all meshes in the projectile and disable them
        foreach (MeshRenderer meshRen in projectileObject.GetComponentsInChildren<MeshRenderer>()) {
            meshRen.enabled = false;
        }

        //If the projectile has particle systems, stop them
        foreach (ParticleSystem partSys in projectileObject.GetComponentsInChildren<ParticleSystem>()) {
            partSys.Stop();
        }

        //If the projectile has particle systems, stop them
        foreach (Light light in projectileObject.GetComponentsInChildren<Light>()) {
            light.enabled = false;
        }

        //Wait for all the particles to disperse then destory the object
        AbilityManager.ExternalDestroyCall(projectileObject, 5f);
        AbilityManager.instance.StartRemoteCoroutine(OnFinish());
    }
    
    /// <summary>
    /// Apply effect effects and trigger follow on effects if necessary
    /// </summary>
    /// <returns></returns>
    public IEnumerator OnFinish () {
        //Don't wait if its a projectile since theres no need
        if (vfxType != VFXType.PROJECTILE) {
            yield return new WaitForSeconds(dominoEffectDelay);
        }

        //Apply the effect to the targets character data
        if (effectOnTarget != null) {
            StatChange effectAsCost = new StatChange(ResourceType.HEALTH, effectOnTarget.Amount);
            if (effectOnTarget.EffectType == Ability.EffectType.RESTORE) {
                effectAsCost.Amount *= -1;
            }
            target.CharacterData.ApplyChangeToData(effectAsCost);
        }
        
        //Start the follow on vfx effect from this one
        if (dominoEffect != null) {
            dominoEffect.StartVisualEffect(source, target);
        } else {
            AbilityManager.instance.abilityRunning = false;
        }
    }



    
}
