using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public class PlayerController : CreatureController
{
    protected bool _isDashing = false;
    protected GameObject _hitPoint;
    
    protected Shield _shield;
    public bool IsShieldOn { get { return _shield.IsOn; } set { _shield.IsOn = value; } }

    protected float invincibilityTime = 0.0f;

    protected override void Init()
    {
        base.Init();

        _hitPoint = Util.FindChild(gameObject, "HitPoint");
        _shield = GetComponentInChildren<Shield>();

        // Set Stat
        _stat.Hp = 200;
        _stat.MaxHp = 200;
        _stat.Attack = 30;
        _stat.Defense = 5;
        _stat.MoveSpeed = 5.0f;
        _stat.DashSpeed = 30.0f;
    }

    protected override void UpdateAnimation()
    {
        switch (State)
        {
            case CreatureState.Idle:
                _animator.CrossFade("IDLE", 0.5f);
                break;
            case CreatureState.Moving:
                _animator.CrossFade("MOVE", 0.1f);
                break;
            case CreatureState.Damaged:

                break;
            case CreatureState.Dead:
                _animator.CrossFade("DEAD", 0.1f);
                break;
        }
    }

    protected override void UpdateController()
    {
        if (!Managers.Game.IsGameOver)
            GetInput();
        base.UpdateController();
        invincibilityTime -= Time.deltaTime;
    }

    protected virtual void GetInput()
    {

    }

    protected override void UpdateIdle()
    {
        Vector3 moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist > _stat.MoveSpeed * Time.deltaTime)
        {
            State = CreatureState.Moving;
            return;
        }
    }

    protected override void UpdateMoving()
    {
        // 도착 여부 체크
        Vector3 moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist < _stat.MoveSpeed * Time.deltaTime || (_isDashing && dist < _stat.DashSpeed * Time.deltaTime))
        {
            transform.position = DestPos;
            State = CreatureState.Idle;
            if (_isDashing)
                _isDashing = false;
            return;
        }

        // translation
        if (!_isDashing)
            transform.position += moveDir.normalized * _stat.MoveSpeed * Time.deltaTime;
        else
            transform.position += moveDir.normalized * _stat.DashSpeed * Time.deltaTime;

        // rotation
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);
    }

    public override void OnDamaged(Stat attacker, HitType hitType)
    {
        if (IsShieldOn || invincibilityTime > 0.0f)
            return;

        int damage = Mathf.Max(0, attacker.Attack - _stat.Defense);
        _stat.Hp -= damage;
        if (_stat.Hp <= 0)
        {
            _stat.Hp = 0;
            OnDead();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Projectile")
        {
            Stat attacker = other.GetComponent<Stat>();
            if (attacker)
            {
                OnDamaged(attacker, HitType.Damage);
            }
        }
    }

    public override void OnDead()
    {
        State = CreatureState.Dead;
        Managers.Game.GameOver();
    }
}
