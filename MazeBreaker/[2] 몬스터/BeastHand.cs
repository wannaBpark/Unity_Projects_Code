using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class BeastHand : MonoBehaviour
{
    private Stat _stat;
    private void Awake()
    {
        _stat = gameObject.GetOrAddComponent<Stat>();
        _stat.Attack = 5;
    }
    protected void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == (int)Layer.Player)
        {
            // take damage only once
            //Debug.Log("calling takedamage to player: ");
            Managers.Object.TakeDamageToPlayer(this.transform.position, _stat); // beast 왼손 오른손 충돌. 
        }
    }
}
