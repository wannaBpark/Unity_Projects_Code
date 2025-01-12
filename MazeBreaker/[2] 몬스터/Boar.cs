using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class Boar : MonsterController
{
    Coroutine _coIdleV3 = null;
    GameObject _eRunGO = null;
    ParticleSystem _runParticleSystem = null;
    Vector3 _rushDir;
    string _pathEffect = "Effect/Monster/General/Boar/";
    float _skillRushRange = 10.0f;
    float _rotationSpeed = 3.0f;
    float _rushInterval = 5.0f;
    float _rushMoreRange = 10.0f;
    float _probIdleV3 = 5f;
    float _idleV3Interval = 3.0f;
    float _walkActivationDist = 0.5f;

    bool _canUseSkillRun = true;
    bool b_playIdleV3 = false;
    bool b_canDamagePlayer;
    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();
        if (_state == CreatureState.Idle)
        {
            b_playIdleV3 = Random.Range(0, 10) < _probIdleV3 ? true : false; Debug.Log("can IdleV3 : " +b_playIdleV3);

            if ((b_playIdleV3 && _coIdleV3 == null)) {
                _animator.CrossFade("IDLE_V3", 0.1f);
                _coIdleV3 = StartCoroutine("CoIdleV3");
            }
        }

        if (_state == CreatureState.Skill)
        {
            if (_skillInUse == SkillType.SKILL_RUSH)
                _animator.CrossFade("SKILL_RUSH", 0.1f, -1, 0);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _eRunGO = Util.FindChild(this.gameObject, "ERun", true);
        _runParticleSystem = _eRunGO.GetComponent<ParticleSystem>();
        DisableERun();
    }
    protected override void Init()
    {
        base.Init();

        _speed = 3.0f;
        _runSpeed = 10.0f;
        _searchRange = 15.0f;

        _layerObstacle = 1 << (int)Layer.Block;
    }
    
    protected override void UpdateIdle()
    {
        if (CanUseSkill()) {
            State = CreatureState.Skill;          // chase the Player
            return;
        }

        float distance = (_destPos - transform.position).magnitude;// Debug.Log("idle dist : " + distance);
        if (distance > _walkActivationDist)
        {
            State = CreatureState.Walk;         // Walk around random Area
            return;
        }

        if (_coPatrol == null && _coIdleV3 == null) // no Partol && non playing idle v3
            _coPatrol = StartCoroutine("CoPatrol");
        if (_coSearch == null)
            _coSearch = StartCoroutine("CoSearch");

    }
    protected override void UpdateWalk()
    {
        if (CanUseSkill()) {
            State = CreatureState.Skill;
            return;
        }

        Vector3 dir = _destPos - transform.position; //Debug.Log("walk dist : " + dir.magnitude);
        if (dir.magnitude < _walkActivationDist)
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
        // Idle V3 중 스킬 진입 시 코루틴 멈춰 줌
        if (_coIdleV3 != null) {
            StopCoroutine("CoIdleV3");
        }

        // 스킬 사용
        if (CanUseSkill()) {
            UseSkill();
        }
        if (_skillInUse == SkillType.SKILL_RUSH) {
            float dist = (DestPos- transform.position).magnitude;
            // 멀 땐 가속, 가까울 땐 감속해 줌
            if (dist <3.0f) {
                _nma.speed -= Time.deltaTime * 0.5f;
            } else {
                _nma.speed += Time.deltaTime * 1f;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_destPos - transform.position), 20 * Time.deltaTime);
        }
    }

    protected override void UpdateDead()
    {
        base.UpdateDead();
    }

    protected override bool CanUseSkill()
    {
        if (_target == null || _coSkill != null)
            return false;

        Vector3 dir = _target.transform.position - transform.position;
        
        if (!_canUseSkillRun || dir.magnitude > _skillRushRange) {
            return false;
        }
        
        bool b_hasObstacle = Physics.Raycast(transform.position, dir, dir.magnitude, _layerObstacle);
        if (b_hasObstacle) {
            return false;
        }

        return true;
    }

    protected override void UseSkill()
    {
        if (_target == null || _coSkill != null)
            return;

        Vector3 dir = _target.transform.position - transform.position;
        if (_canUseSkillRun && dir.magnitude <= _skillRushRange)
            _coSkill = StartCoroutine("CoSkillRun");
    }

    IEnumerator CoIdleV3()
    {
        float randInterval = Random.Range(0f, 2f);
        float totIdleV3Interval = _idleV3Interval + randInterval;
        yield return new WaitForSeconds(totIdleV3Interval);
        _coIdleV3 = null;
    }

    IEnumerator CoSkillRun()
    {
        b_canDamagePlayer = true;
        EnableERun();
        _canUseSkillRun = false;

        
        _destPos = _target.transform.position;
        _rushDir = (_destPos - transform.position).normalized;
        _destPos += _rushDir * _rushMoreRange;

        //_destPos = Vector3.Lerp(transform.position, _destPos, 10 * Time.deltaTime);
        _nma.SetDestination(_destPos);
        _nma.speed = _runSpeed;
        
        _skillInUse = SkillType.SKILL_RUSH;
        UpdateAnimation();

        // 피격 판정

        yield return new WaitForSeconds(2.0f);
        DisableERun();

        //_centerPos = transform.position; // to renew centerpos in _coPatrol
        _target = null;                  // to renew target in _coSearch
        State = CreatureState.Idle;
        _skillInUse = SkillType.SKILL_NONE;
        _nma.speed = _speed;

        yield return new WaitForSeconds(_rushInterval);
        _coSkill = null;
        _canUseSkillRun = true;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (!b_canDamagePlayer) { return; }
        if(1 << other.gameObject.layer == 1 << (int)Layer.Player) {
            if (_skillInUse != SkillType.SKILL_RUSH) return;
            b_canDamagePlayer = false;
            Managers.Object.TakeDamageToPlayer(this.transform.position, _stat);
            GameObject go = Managers.Resource.Instantiate(_pathEffect + "Hit", other.gameObject.transform.position, Quaternion.identity);
        }
    }

    protected override IEnumerator CoPatrol()
    {
        int waitSeconds = Random.Range(1, 4);
        yield return new WaitForSeconds(waitSeconds);

        for (int i = 0; i < 10; i++)
        {
            Vector3 randDir = Random.insideUnitSphere * _patrolRange;
            randDir.y = 0;
            Vector3 randPos = _centerPos + randDir;

            // 다음 위치의 최소 이동거리 정의 (Idle 갇힘 방지)
            if ((randPos - transform.position).magnitude <= _walkActivationDist) {
                continue;
            }
            NavMeshPath path = new NavMeshPath();
            _nma.CalculatePath(randPos, path);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                Debug.Log("nxt dist : " + (randPos - transform.position).magnitude);
                _destPos = randPos;
                _coPatrol = null;
                yield break;
            }
        }

        _coPatrol = null;
    }

    private void EnableERun()
    {
        Debug.Log("Enable Run ! ");
        var a = _runParticleSystem.main;
        a.loop = true;
        _runParticleSystem.Play();
        //_eRunGO.SetActive(true);
    }
    private void DisableERun()
    {
        Debug.Log("Enable Run ! ");
        //_eRunGO.SetActive(false);
        var main = _runParticleSystem.main;
        main.loop = false;
        _runParticleSystem.Play();

    }
}
