using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VolumetricFogAndMist2;

public class ObjectManager
{
    //CreatureController _player;
    PlayerControllerV1 _player;

    List<GameObject> _objects = new List<GameObject>();

    public VolumetricFog FogVolume { get; private set; }

    public void Add(GameObject go)
    {
        //CreatureController pc = go.GetComponent<CreatureController>();
        PlayerControllerV1 pc = go.GetComponent<PlayerControllerV1>();
        if (pc != null)
        {
            _player = pc;
            return;
        }

        VolumetricFog fog = go.GetComponent<VolumetricFog>();
        if (fog != null)
        {
            FogVolume = fog;
            return;
        }

        _objects.Add(go);
    }

    public PlayerControllerV1 GetPlayer()
    {
        return _player;
    }

    public GameObject Find(Func<GameObject, bool> condition)
    {
        foreach (GameObject obj in _objects)
        {
            if (obj != null && condition.Invoke(obj))
                return obj;
        }

        return null;
    }

    public List<GameObject> FindAll(Func<GameObject, bool> condition)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (GameObject obj in _objects)
        {
            if (obj != null && condition.Invoke(obj))
                list.Add(obj);
        }

        return list;
    }

    public void Remove(GameObject go)
    {
        _objects.Remove(go);
        Managers.Resource.Destroy(go);
    }

    public void RemoveAll(Func<GameObject, bool> condition)
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            GameObject go = _objects[i];
            if (go == null || condition.Invoke(go))
            {
                _objects.RemoveAt(i);
                Managers.Resource.Destroy(go);
            }
        }
    }

    public void Clear()
    {
        _player = null;
    }

    // Called when Collision ASSURED -> Take Damage to player
    public void TakeDamageToPlayer(int damage)
    {
        Debug.Assert(_player != null);
        if (_player != null) {
            _player.Stat.OnDamaged(damage);
        }
    }
    public void TakeDamageToPlayer(Stat _attacker)
    {
        Debug.Assert(_attacker != null);
        Debug.Assert(_player != null);
        if (_player != null) {
            _player.Stat.OnDamaged(_attacker);
        }
    }

    public void TakeDamageToPlayer(Vector3 monsterPos, Stat _attacker)
    {
        Debug.Assert(_attacker != null);
        Debug.Log("attakcer? : " + _attacker.Attack);
        Debug.Assert(_player != null);
        if (_player != null)
        {
            //Debug.Log("Take damage to player : " + monsterPos);
            //Debug.Log($" default state null ? : {_player.gameObject.GetComponent<PlayerDefaultState>() == null}");
            _player.gameObject.GetComponent<PlayerControllerV1>().HandleDamage(monsterPos, _attacker); 
        }
    }



    public void ExplosionToCreature(Vector3 position, float radius, Stat _attacker)
    {
        // current example : only apply damage for player
        float dist = (position - _player.transform.position).magnitude;
        float damage = _attacker.Attack;

        if (dist > radius) return;

        _player.gameObject.GetComponent<PlayerControllerV1>().HandleDamage(position, _attacker);
    }
}
