using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ES_Bullet : Enemy_State
{
    public Enemy_BulletPattern pattern;

    public void DebugPlayShot ()
    {  
        StartCoroutine(pattern.PlayShot(Enemy.playerReference.aimTarget, Enemy.playerReference.rb, e.gameObject));
    }
}
