using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static Define;
using static Unity.Burst.Intrinsics.X86;

public class MonsterController : CreatureController
{
    protected GameObject go_eDeath = null;
    protected Coroutine _coSkill;
    protected Coroutine _coPatrol;
    protected Coroutine _coSearch;

    protected NavMeshAgent _nma;
    protected Vector3 _centerPos;
    protected Vector3 _destPos;
    protected GameObject _target;
    protected float _patrolRange = 10.0f;
    protected float _searchRange = 10.0f;
    protected int _layerObstacle;

    // Stat
    protected int _hp = 100;
    protected float _speed = 3.0f;
    protected float _runSpeed = 6.0f;
    protected SkillType _skillInUse;
    public int Hp { get { return _hp; } set { _hp = value; } }

    bool b_isDead = false;
    public override CreatureState State
    {
        get { return _state; }
        set
        {
            if (State != CreatureState.Damaged && _state == value)
                return;

            base.State = value;

            if (_coPatrol != null) {
                StopCoroutine(_coPatrol);
                _coPatrol = null;
            }

            if (_coSearch != null) {
                StopCoroutine(_coSearch);
                _coSearch = null;
            }
        }
    }

    protected override void UpdateAnimation()
    {
        switch (_state)
        {
            case CreatureState.Idle:
                _animator.CrossFade("IDLE", 0.1f);
                break;
            case CreatureState.Walk:
                _animator.CrossFade("WALK", 0.1f);
                break;
            case CreatureState.Run:
                _animator.CrossFade("RUN", 0.1f);
                break;
            case CreatureState.Damaged:
                _animator.Play("HURT",0,0);
                break;
            case CreatureState.Dead:
                _animator.CrossFade("DEAD", 0.1f);
                break;
        }
    }

    protected override void Init()
    {
        base.Init();
        _stat.Attack = (int)5;
        _layer = 1 << (int)Layer.Monster;

        this.gameObject.GetComponent<Rigidbody>().freezeRotation = true;
        State = CreatureState.Idle;
        _nma = GetComponent<NavMeshAgent>();
        _centerPos = transform.position;
        _destPos = transform.position;
    }

    protected override void UpdateIdle()
    {
        if (_target != null)
        {
            State = CreatureState.Run;          // chase the Player
            return;
        }

        float distance = (_destPos - transform.position).magnitude;
        if (distance > 1.0f)
        {
            State = CreatureState.Walk;         // Walk around random Area
            return;
        }

        if (_coPatrol == null)
            _coPatrol = StartCoroutine("CoPatrol");
        if (_coSearch == null)
            _coSearch = StartCoroutine("CoSearch");
    }

    protected override void UpdateWalk()
    {
        if (_target != null)
        {
            State = CreatureState.Run;
            return;
        }

        Vector3 dir = _destPos - transform.position;
        if (dir.magnitude < 0.5f)
        {
            _nma.SetDestination(transform.position);
            State = CreatureState.Idle;
            return;
        }

        if (_coSearch == null)
            _coSearch = StartCoroutine("CoSearch");

        _nma.SetDestination(_destPos);
        _nma.speed = _speed;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 20 * Time.deltaTime);
    }

    protected override void UpdateRun()
    {
        if (CanUseSkill())
        {
            _nma.SetDestination(transform.position);
            State = CreatureState.Skill;
            UseSkill();
            return; 
        }

        _destPos = _target.transform.position;
        Vector3 dir = _destPos - transform.position;
        if (dir.magnitude > _searchRange)
        {
            _nma.SetDestination(transform.position);
            _target = null;
            _destPos = transform.position;
            State = CreatureState.Idle;
            return;
        }

        _nma.SetDestination(_destPos);
        _nma.speed = _runSpeed;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 20 * Time.deltaTime);
    }

    protected override void UpdateSkill()
    {

    }

    protected override void UpdateDamaged()
    {
        _nma.SetDestination(transform.position);
    }

    protected override void UpdateDead()
    {
        _nma.SetDestination(transform.position);
        // if go_eDeath destroyed after duration, also destroys monster itself
        Debug.Log((go_eDeath == null) + " dead effect null?");
        if (go_eDeath == null) {
            Managers.Resource.Destroy(this.gameObject);
        }
    }

    protected virtual IEnumerator CoPatrol()
    {
        int waitSeconds = Random.Range(1, 4);
        yield return new WaitForSeconds(waitSeconds);

        for (int i = 0; i < 10; i++)
        {
            Vector3 randDir = Random.insideUnitSphere * _patrolRange;
            randDir.y = 0;
            Vector3 randPos = _centerPos + randDir;

            NavMeshPath path = new NavMeshPath();
            _nma.CalculatePath(randPos, path);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                _destPos = randPos;
                _coPatrol = null;
                yield break;
            }
        }

        _coPatrol = null;
    }

    protected virtual IEnumerator CoSearch()
    {
#nullable enable
        GameObject? player = Managers.Object.GetPlayer().gameObject;
#nullable disable
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (_target != null)
                continue;

            // TODO
            
            Vector3 dir = (player.transform.position - transform.position);
            if (dir.magnitude <= _searchRange)
            {
                _target = player;
            }
        }
    }

    protected virtual bool CanUseSkill()
    {
        return false;
    }

    protected virtual void UseSkill()
    {

    }

    public void SetStateIdle()
    {
        State = CreatureState.Idle;
    }

    public virtual void SetStateSkill()
    {
        State = CreatureState.Skill;
    }

    public override void OnDead()
    {
        Debug.Log("OnDead() ");
        if (b_isDead) return;
        b_isDead = true;

        StopAllCoroutines();
        base.OnDead();
        Debug.Log("Current State : " + State);
        Vector3 spawnPos = transform.position;
        spawnPos.y = 1f;
        go_eDeath = Managers.Resource.Instantiate("Effect/Monster/General/Tree/Hit", spawnPos, Quaternion.identity, null);
    }

    protected virtual void OnDamaged()
    {
        //State = CreatureState.Damaged;
        //Debug.Log("Damaged");

        //// TODO
        //Hp = 0;
        //gameObject.SetActive(false);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Skill") {
            //Debug.Log("Monster Current HP" + this._stat.Hp);
            this._stat.OnDamaged(1f);            
            if (this._stat.Hp <= 0) { 
                this.OnDead();
            } else if ( (_skillInUse == SkillType.SKILL_NONE) ) { // state is not skill || skill but not in use yet
                State = CreatureState.Damaged; // added Animation event in the end of HURT : SetStateIdle()
                UpdateAnimation();
                Debug.Log("Hurt State :" + State);
            }
        }
    }

    public void SetPosition(Vector3 pos)
    {
        NavMeshAgent nma = GetComponent<NavMeshAgent>();
        if (nma != null)
            nma.Warp(pos);
        else
            transform.position = pos;

        _centerPos = pos;
        _destPos = pos;
        State = CreatureState.Idle;
    }

    void OnDisable()
    {
        if (_coPatrol != null)
        {
            StopCoroutine(_coPatrol);
            _coPatrol = null;
        }

        if (_coSearch != null)
        {
            StopCoroutine(_coSearch);
            _coSearch = null;
        }

        if (_coSkill != null)
        {
            StopCoroutine(_coSkill);
            _coSkill = null;
        }
    }
}
