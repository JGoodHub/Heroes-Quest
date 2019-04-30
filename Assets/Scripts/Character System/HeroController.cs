using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroController : CharacterController {
    
    //-----METHODS-----

    /// <summary>
    /// Calls the hero specific death coroutine
    /// </summary>
    public override void Die () {
        Debug.Log("Oh shit, i'm dead");     
        StartCoroutine(HeroDieCoroutine());  
    }

    /// <summary>
    /// Coroutine for hero death
    /// </summary>
    /// <returns></returns>
    IEnumerator HeroDieCoroutine () {
        yield return new WaitForSeconds(0.5f);
        PlayerManager.instance.HeroSet.Remove(this);
        PlayerManager.instance.SelectRandomHero();
        movementController.disableMovement = true;
        movementController.GraphObstacle.UnblockCurrentVertex();
        Unhighlight();
        GetComponentInChildren<Animator>().SetTrigger("Die");
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject, 10f);
        while (true) {
            transform.Translate(Vector3.down * 5f * Time.deltaTime);
            yield return null;
        }
        
    }

}
