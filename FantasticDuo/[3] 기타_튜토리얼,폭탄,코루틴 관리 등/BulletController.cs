using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        // 보스가 스스로 맞은 경우는 무시
        string tagName = other.gameObject.tag;
        if (tagName == "Map" || tagName == "Player")
        {
            if (tagName == "Player")
            {
                GameObject eCollision = Managers.Resource.Instantiate("Effect/bDive_ButHit",
                this.transform.position, Quaternion.identity);
            }
            Destroy(this.gameObject);
        }
    }
}
