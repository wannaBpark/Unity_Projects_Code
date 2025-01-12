using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class CreatureController : MonoBehaviour
{
    protected Stat _stat;
    public Stat Stat { get { return _stat; } }

    public Vector3 DestPos { get; set; } = Vector3.zero;
    protected Animator _animator;

    [SerializeField]
    protected CreatureState _state = CreatureState.Idle;
    public virtual CreatureState State
    {
        get { return _state; }
        set
        {
            if (_state == value)
                return;

            _state = value;
            UpdateAnimation();
        }
    }


    protected virtual void UpdateAnimation()
    {

    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        UpdateController();
    }

    protected virtual void Init()
    {
        _animator = GetComponent<Animator>();
        transform.position = DestPos;
        _stat = gameObject.GetOrAddComponent<Stat>();
    }

    protected virtual void UpdateController()
    {
        switch (State)
        {
            case CreatureState.Idle:
                UpdateIdle();
                break;
            case CreatureState.Moving:
                UpdateMoving();
                break;
            case CreatureState.Skill:
                UpdateSkill();
                break;
            case CreatureState.Damaged:
                UpdateDamaged();
                break;
            case CreatureState.Dead:
                UpdateDead();
                break;
        }
    }

    protected virtual void UpdateIdle()
    {

    }

    protected virtual void UpdateMoving()
    {

    }

    protected virtual void UpdateSkill()
    {

    }

    protected virtual void UpdateDamaged()
    {

    }

    protected virtual void UpdateStunned()
    {

    }

    protected virtual void UpdateDead()
    {

    }

    public virtual void OnDamaged(Stat attacker, HitType hitType)
    {

    }

    public virtual void OnDead()
    {

    }
}
