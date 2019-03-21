using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXEffect : ScriptableObject {

    //-----ENUMS-----

    public enum EffectType {ANIMATION, PROJECTILE, PARTICLE_SYSTEM};

    //-----VARIABLES-----

    private EffectType vfxType;
    private string effectID;

    private float dominoVFXEffectDelay;
    
    private CharacterController source;
    private Vector3 startOffset;
    private CharacterController target;

    private Ability.Effect effect;
    private VFXEffect dominoVFXEffect;

    //-----METHODS-----

    public VFXEffect (EffectType _vfxType, string _effectID, float _dominoVFXEffectDelay, Ability.Effect _effect, VFXEffect _dominoVFXEffect) {
        this.vfxType = _vfxType;        
        this.effectID = _effectID;
        this.dominoVFXEffectDelay = _dominoVFXEffectDelay;
        this.effect = _effect;
        this.dominoVFXEffect = _dominoVFXEffect;
    }

    public void SetStartOffset (Vector3 offset) {
        if (vfxType == EffectType.PROJECTILE) {
            startOffset = offset;
        }
    }
    
    public void StartVisualEffect (CharacterController _source, CharacterController _target) {
        this.source = _source;
        this.target = _target;

        //Create the effect prefab at the given position and rotation
        switch (vfxType) {
            case EffectType.ANIMATION:
                //Trigger the animation on the casters animator controller
                source.GetComponentInChildren<Animator>().SetTrigger(effectID);
                AbilityManager.instance.StartExternalCoroutine(OnFinish());
                break;
            case EffectType.PROJECTILE:
                //Create the projectile and have it fly towards the enemy, when it reaches them destroy it
                GameObject projectilePrefabInstance = Instantiate(Resources.Load<GameObject>("Visual Effects/Projectiles/" + effectID), source.transform.position, Quaternion.identity);
                
                projectilePrefabInstance.transform.forward = target.transform.position - source.transform.position;

                projectilePrefabInstance.transform.Translate(Vector3.right * startOffset.x);
                projectilePrefabInstance.transform.Translate(Vector3.up * startOffset.y);
                projectilePrefabInstance.transform.Translate(Vector3.forward * startOffset.z);                

                AbilityManager.instance.StartExternalCoroutine(ProjectileCoroutine(projectilePrefabInstance));
                break;
            case EffectType.PARTICLE_SYSTEM:
                //Create the particle system at the source and have it face in the direction of the target
                GameObject particlePrefabInstance = Instantiate(Resources.Load<GameObject>("Visual Effects/Particle Systems/" + effectID), target.transform.position, Quaternion.identity);
                particlePrefabInstance.transform.forward = source.transform.position - target.transform.position;
                Destroy(particlePrefabInstance, 10f);
                AbilityManager.instance.StartExternalCoroutine(OnFinish());              
                break;
            default:
                Debug.LogError("Invalid effect state");
                return;
        } 
    }

    public IEnumerator ProjectileCoroutine (GameObject projectileObject) {
        while (Vector3.Distance(projectileObject.transform.position, target.transform.position + new Vector3(0, 3.5f, 0)) > 0.1f) {
            projectileObject.transform.position = Vector3.MoveTowards(projectileObject.transform.position, target.transform.position + new Vector3(0, 3.5f, 0), dominoVFXEffectDelay * Time.deltaTime);
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

        //Wait for all the particles to disperse
        Destroy(projectileObject, 5f);
        AbilityManager.instance.StartExternalCoroutine(OnFinish());

    }
    
    public IEnumerator OnFinish () {
        //Don't wait if its a projectile since theres no need
        if (vfxType != EffectType.PROJECTILE) {
            yield return new WaitForSeconds(dominoVFXEffectDelay);
        }

        //Apply the effect to the targets character data
        if (effect != null) {
            StatChange effectAsCost = new StatChange(ResourceType.HEALTH, effect.Amount);
            if (effect.EffectType == Ability.EffectType.RESTORE) {
                effectAsCost.Amount *= -1;
            }
            target.CharacterData.ApplyChangeToData(effectAsCost);
        }
        
        //Start the follow on vfx effect from this one
        if (dominoVFXEffect != null) {
            dominoVFXEffect.StartVisualEffect(source, target);
        } else {
            AbilityManager.instance.abilityRunning = false;
        }
    }



    
}
