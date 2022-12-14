using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyController : MonoBehaviour
{
    protected bool isDead;
    
    public abstract void TakeDamage(float damage);
    
    public abstract IEnumerator RecoverySequence();

    public bool IsDead()
    {
        return isDead;
    }

    protected static void ReduceEnemyCounter()
    {
        StateGameManager.NumOfEnemies -= 1;
        UIController.Instance.SetEnemyCounter(StateGameManager.NumOfEnemies);
    }

}
