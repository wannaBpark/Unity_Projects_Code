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
        // ������ ������ ���� ���� ����
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
