using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class Cutty : MonsterController
{
    AnimationClip[] _animClips;
    GameObject _eRunGO;
    protected Coroutine _coIdle;
    protected Coroutine _coStop;

    bool _canUseSkillRun = true;
    float _skillRunRange = 10.0f; // define running range
    Vector3 _runDir = Vector3.zero;

    #region IDLE Parameters
    protected float _idleInterval;
    protected float _idleTime;

    float _minIdleInterval = 10f;
    float _maxIdleInterval = 15f;
    float _minIdleTime = 3f;
    float _maxIdleTime = 5f;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        _eRunGO = Util.FindChild(this.gameObject, "ERun", true);
        _eRunGO.SetActive(false);
        Debug.Log( (_eRunGO == null) + " is ERUN NULL?");
        this.AddCuttyAnimationEvents();
    }
    protected override void UpdateController()
    {
        base.UpdateController();

        if (_state != CreatureState.Skill && _coIdle == null && _coStop == null)
        {
            SetIdleIntervalTime();
            _coStop = StartCoroutine("CoStop");
        }
    }
    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        if (_state == CreatureState.Skill)
        {
            _animator.CrossFade("SKILL_RUN", 0.1f, -1, 0);
        }
        else if (_state == CreatureState.Jump)
        {
            _animator.CrossFade("JUMP", 0.1f);

            _runDir = (transform.position - _target.transform.position).normalized;
            _runDir.y = 0;
            SetValidDestPos(_runDir);

            _target = null;
        }
    }

    protected override void Init()
    {
        base.Init();
        SetIdleIntervalTime();

        _speed = 3.0f;
        _runSpeed = 10.0f; // RUN SPEED
        _layerObstacle = 1 << (int)Layer.Block;
    }

    protected override void UpdateIdle()
    {
        // If 3 ~ 5 second Stop is activating, DO NOT Search or Patrol
        if (_coStop != null) return;

        if (_target != null)
        {
            State = CreatureState.Walk;
            return;
        }

        float distance = (_destPos - transform.position).magnitude;
        if (distance > 1.0f)
        {
            State = CreatureState.Walk;
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
            _nma.SetDestination(transform.position);
            State = CreatureState.Jump;
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


    protected override void UpdateSkill()
    {
        _canUseSkillRun = false;
        _nma.SetDestination(_destPos);
        //Debug.Log("destpos : " + _destPos);
        _nma.speed = _runSpeed;

        Vector3 dir = _destPos - transform.position;
        // can slow down too (_speed -= 1.0f * Time.deltaTime);
        if (dir.magnitude > 4.0f) {
            float _nxtSpeed = _nma.speed - 10.0f * Time.deltaTime;
            _nma.speed = _nxtSpeed;
        }
        if (dir.magnitude < 0.5f)
        {
            _nma.SetDestination(transform.position);
            State = CreatureState.Idle;

            _nma.speed = _speed;
            _canUseSkillRun = true;
            DisableERun();
            _target = null;
            return;
        }
        _runDir = _destPos - transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_runDir), 20 * Time.deltaTime);
    }

    protected override void UpdateDead()
    {
        base.UpdateDead();
    }

    //protected override void UseSkill()
    //{
    //    if (_target == null || _coSkill != null)
    //        return;

    //    Vector3 dir = transform.position - _target.transform.position;
    //    _destPos = dir * _skillRunRange;

    //    if (_canUseSkillRun && dir.magnitude <= _skillRunRange)
    //        _coSkill = StartCoroutine("CoSkillRun");
    //}

    //IEnumerator CoSkillRun()
    //{
    //    _canUseSkillRun = false;
    //    _skillInUse = SkillType.SKILL_RUN;
    //    //UpdateAnimation();

    //    // 피격 판정

    //    yield return new WaitForSeconds(1.5f);
    //    State = CreatureState.Walk;
    //    _skillInUse = SkillType.SKILL_NONE;
    //    _coSkill = null;

    //    _canUseSkillRun = true;
    //}

    IEnumerator CoStop()
    {
        _nma.SetDestination(transform.position);
        State = CreatureState.Idle;
        yield return new WaitForSeconds(_idleTime);
        State = CreatureState.Idle;

        _coIdle = StartCoroutine("CoIdle"); // after stopping 3 ~ 5 seconds, measure idle coolTime 
        _coStop = null;

    }

    IEnumerator CoIdle()
    {
        yield return new WaitForSeconds(_idleTime);
        _coIdle = null;
    }
    protected void SetIdleIntervalTime()
    {
        _idleInterval = Random.Range(_minIdleInterval, _maxIdleInterval);
        _idleTime = Random.Range(_minIdleTime, _maxIdleTime);
    }

    private void AddCuttyAnimationEvents()
    {
        _animClips = _animator.runtimeAnimatorController.animationClips;
        AnimationClip runAnimClip = new AnimationClip();
        foreach (var animClip in _animClips)
        {
            switch (animClip.name)
            {
                case "Run": runAnimClip = animClip; break;
            }
        }
        runAnimClip.AddEvent(new AnimationEvent()
        {
            time = 0f,
            functionName = "EnableERun"
        });
        //runAnimClip.AddEvent(new AnimationEvent()
        //{
        //    time = runAnimClip.length,
        //    functionName = "DisableERun"
        //});
    }

    private void SetValidDestPos(Vector3 farDir)
    {
        Vector3 vNxt = new Vector3();
        NavMeshPath path = new NavMeshPath();
        for (int i = 0; i < 12; ++i)
        {
            vNxt = transform.position + Quaternion.Euler(0, 0, -30 * i) * farDir * _skillRunRange;
            float nxtDist = (vNxt - _target.transform.position).magnitude;
            if (nxtDist <= _searchRange) {
                vNxt = transform.position + Quaternion.Euler(0, 0, -30 * i) * farDir * _skillRunRange * 2.0f;
                
            }
            vNxt.y = 0;
            Vector3 dir = vNxt - transform.position;
            dir.y = 0;
            bool b_hasObstacle = Physics.Raycast(transform.position, dir, dir.magnitude, _layerObstacle);
            Debug.Log("reachable? : " + b_hasObstacle);
            if (b_hasObstacle) continue;
            
            _nma.CalculatePath(vNxt, path);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                break;
            }
        }
        _destPos = vNxt;
    }


    private void EnableERun() 
    {
        Debug.Log("Enable Run ! ");
        if (State != CreatureState.Skill) return;
        _eRunGO.SetActive(true);
    }
    private void DisableERun() 
    {
        Debug.Log("Diable Run ! ");
        _eRunGO.SetActive(false);
    }
}
