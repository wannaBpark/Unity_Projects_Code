using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
public class WizardMarble : MonoBehaviour
{
    GameObject _eExplosion = null;
    string _pathExplosion = "Effect/Monster/Boss/Bear/Explosion";
    float _timer;
    float _speed = 10.0f;
    bool b_Throw = false;
    bool b_Destroyable = false;
    bool b_eExplosionSpawned = false;
    bool b_Collision = false;
    Vector3 _targetPosition = Vector3.zero;
    Vector3 _startPos;
    ParticleSystem _particleSystem;

    public Vector3 ThrowDir { get; set; }

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        //Debug.Log(transform.position + " marble Position");
        if (this.transform.position.y < 0 && !b_eExplosionSpawned) {
            EExplosion();
        }
        if (b_eExplosionSpawned && _eExplosion != null) {
            Destroy(this.gameObject);
        }
        if (!b_Throw) return;
        if (b_Throw == true) {
            StartCoroutine("MarbleMove");
            b_Throw = false;
        }
        
        
        //Debug.Log("My marble pos Y : " + transform.position.y);

    }

    public void Init(Vector3 _startPosition, Vector3 _targetPosition)
    {
        _startPos = _startPosition;
        this.transform.position = _startPos;
        this._targetPosition = _targetPosition;
        b_Throw = true;
    }

    protected static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
    {
        Func<float, float> f = x => -4 * height * x * x + 4 * height * x;
        var mid = Vector3.Lerp(start, end, t);
        return new Vector3(mid.x, f(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
    }

    protected IEnumerator MarbleMove()
    {
        _timer = 0;
        b_Destroyable = true;
        while (transform.position.y >= _startPos.y)
        {
            _timer += Time.deltaTime;
            Vector3 tempPos = Parabola(_startPos, _targetPosition, 5, _timer);
            transform.position = tempPos;
            yield return new WaitForEndOfFrame();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(transform.position + " marble Position");
        //Debug.Log(other.gameObject.layer + " to collison " + other.gameObject.name);
        if (!b_Destroyable) return;
        Debug.Assert(b_Destroyable == true, " bomb can be destroyed");
        int otherLayer = 1 << other.gameObject.layer;
        int checkLayer = (1 << (int)Layer.Ground) | (1 << (int)Layer.Player) | (1 << (int)Layer.Block) | (1 << 11); // check if layer is player or ground
        if ( (otherLayer & checkLayer) != 0 || other.gameObject.layer != (int)Layer.Monster)
        {
            Debug.Log(other.gameObject.layer + " real layer");
            //Debug.Log((otherLayer & checkLayer) + " layermask result");
            EExplosion();
           
        }
    }

    private void EExplosion()
    {
        Stat bombStat = new Stat();
        bombStat.Attack = 10;
        Managers.Object.ExplosionToCreature(transform.position, 3f, bombStat);
        _eExplosion = Managers.Resource.Instantiate(_pathExplosion, transform.position, Quaternion.identity, null);
        b_eExplosionSpawned = true;
    }

}
