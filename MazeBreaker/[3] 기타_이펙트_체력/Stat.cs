#define DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Stat : MonoBehaviour
{
    #region Initialize

    public int Level;
    public int TotalExp;


    //[SerializeField]
    protected float _moveSpeed = 3.0f;
    [SerializeField]
    protected float _dashSpeed;
    //[SerializeField]
    protected float _maxHp = 100.0f;
    //[SerializeField]
    protected float _Hp = 100.0f; // if _maxHp isn't static, this won't work b/c there aren't mem allocation, but.. "ONLY 1 VARIABLE per CLASS"

    protected float _maxStamina = 100.0f;
    protected float _Stamina = 100.0f;
    protected float _maxMental = 100.0f;
    protected float _Mental = 100.0f;


    protected int _exp = 0;
    protected int _gold = 0;

    private int _attack = 10;
    private int _defense = 0;
    #endregion

    #region Get_Set
    public float MoveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }
    public float DashSpeed { get { return _dashSpeed; } set { _dashSpeed = value; } }
    public float MaxHp { get { return _maxHp; } set { _maxHp = value; } }

    public float MaxStamina{ get { return _maxStamina; } set { _maxStamina = value; } }
    public float Stamina { get { return _Stamina; } set { _Stamina = value; } }
    public float MaxMental { get { return _maxMental; } set { _maxMental = value; } }
    public float Mental { get { return _Mental; } set { _Mental = value; } }

    public float Hp { get { return _Hp; } set { _Hp = value; } }
    public float Exp { get { return _Hp; } set { _Hp = value; } }
    public int Attack { get { return _attack; } set { _attack = value; } }
    public int Defense { get { return _defense; } set { _defense = value; } }

    public int Gold { get { return _gold; } set { _gold = value; } }
    #endregion

    #region Methods

    public virtual void OnDamaged(float damage)
    {
        damage = Mathf.Max(0, damage - this.Defense);
        //Debug.Log("current HP : " + _Hp);
        this._Hp -= (int)damage;
        //Debug.Log("damaged HP : " + _Hp);

        if (_Hp <= 0) {
            this._Hp = 0;
            //OnDeadByAttacker(null);
        }
    }

    public virtual void OnDamaged(Stat _attacker)
    {
        int damage = Mathf.Max(0, _attacker.Attack - this.Defense);
        this._Hp -= damage;

        if (_Hp <= 0) {
            this._Hp = 0;
            //OnDeadByAttacker(_attacker);
        }
    }

    public virtual void OnDamagedByDeBuff(int _attack)
    {
        int damage = Mathf.Max(0, _attack - this.Defense);
        this._Hp -= damage;

        if (_Hp <= 0) {
            this._Hp = 0;
            OnDeadByAttacker(null);
        }
    }

    protected virtual void OnDeadByAttacker(Stat _attacker)
    {
        Debug.Log("OnDead By Attacker .. null");
#nullable enable
        Stat? _playerStat = _attacker;
        _playerStat.Exp += 100; // ain't no addition if _attacker is NULL
#nullable disable
        //this.gameObject.GetComponent<MonsterController>().OnDead();
        //Managers.Resource.Destroy(this.gameObject);

    }


    #endregion


    #region BasicMethod_Overrides
    private void OnEnable()
    {
        _Hp = _maxHp;

#if DEBUG
        Debug.Assert(_Hp >= 0, "Initialized CurrentHP = Max, BUT HP is MINUS!");
#endif
    }
    #endregion
}
