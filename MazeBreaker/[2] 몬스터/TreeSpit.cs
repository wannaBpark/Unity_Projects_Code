using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using static Define;

public class TreeSpit : MonoBehaviour
{
    string _pathEHit = "Effect/Monster/General/Tree/Hit";
    GameObject _eHit = null;
    bool b_eHitDestroyed = false;
    bool b_eHitSpawned = false;
    int _damage = 5;
    float _speed = 10.0f;
    Vector3 _startPos;
    public Vector3 Dir { get; set; }
    Stat _stat;
    void Start()
    {
        _startPos = transform.position;
        _stat = gameObject.GetOrAddComponent<Stat>();
        _stat.Attack = 5;
        //transform.position += new Vector3(0, .4f, 0);
    }

    void Update()
    {
        Vector3 dir = transform.position - _startPos;
        if (dir.magnitude > 20.0f) // TODO
        {
            Managers.Resource.Destroy(gameObject);
        }

        if (transform.position.y != 1.0f)
            transform.position = new Vector3(transform.position.x, 1.0f, transform.position.z);

        transform.position += (Dir * _speed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(Dir);
        //Debug.Log("direction : " + Dir);

        if (b_eHitSpawned) {
            //Debug.Log(_eHit + " effect Hit is null");
            // check if Effect Hit has spawned, if so Destroy this BULLET
            if (_eHit != null) {
                Destroy(this.gameObject);
            }
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == (int)Layer.Player) {
            // take damage only once
            Managers.Object.TakeDamageToPlayer(this.transform.position, _stat);
            _eHit = Managers.Resource.Instantiate(_pathEHit, transform.position, Quaternion.identity, null);
            b_eHitSpawned = true;
        }
    }
}
