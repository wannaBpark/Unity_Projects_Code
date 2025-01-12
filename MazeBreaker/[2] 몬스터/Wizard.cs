using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class Wizard : MonsterController
{
    Coroutine _coFindValidPath = null;
    AnimationClip _walkAnimClip;
    private HashSet<Collider> _collidedObjects = new HashSet<Collider>();

    bool b_canUseSkillThrow = true;
    bool b_findNewPath = true;
    bool b_isJumping = false;
    float _walkAnimLength;

    float _rotationSpeed = 10.0f;
    float _skillThrowRange = 8.0f;
    float _idleRange = 6.0f;
    float _skillInterval = 5.0f;
    float _pathInterval = 2.0f;

    float _walkActivationDist = 1.0f;

    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        if (_state == CreatureState.Skill)
        {
            if (_skillInUse == SkillType.SKILL_THROW) {
                float _skillProb = Random.Range(0f, 1f);
                string _animName = _skillProb < 0.5f ? "SKILL_THROW1" : "SKILL_THROW2";
               _animator.CrossFade(_animName, 0.1f, -1, 0);
            }
        }
    }

    protected override void Init()
    {
        base.Init();

        AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips) {
            if (clip.name == "Jump") {
                _walkAnimClip = clip;
            }
        }
        _walkAnimLength = _walkAnimClip.length;
        _speed = 6.0f;
        _runSpeed = 6.0f;
        _nma.acceleration = 100f;
        _nma.autoBraking = false;
        _layerObstacle = 1 << (int)Layer.Block;
    }

    protected override void UpdateIdle()
    {
        if (_target != null) {
            if (!CanUseSkill()) {
                State = CreatureState.Walk;
                return;
            } else {
                State = CreatureState.Skill;
                return;
            }
        }

        float distance = (_destPos - transform.position).magnitude;
        if (distance > _walkActivationDist) {
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
        if (_target != null) { // target is close to wizard
            if (!CanUseSkill() && !b_isJumping) { // but wizard skill's coolTime && can find new Path
                Vector3 farDir= (transform.position - _target.transform.position).normalized;
                farDir.y = 0;
                SetValidDestPos(farDir, _target.transform.position);
                //_coFindValidPath = StartCoroutine("CoFindValidPath");
            } else {
                if (!b_isJumping) {
                    State = CreatureState.Skill;
                    return;
                }
            }
            
        }
        Vector3 dir = _destPos - transform.position;
        dir.y = 0;
        //Debug.Log("dist : " + dir.magnitude);
        if (dir.magnitude <= _walkActivationDist && _target != null) {
            _nma.SetDestination(transform.position);
            State = CreatureState.Idle;
            return;
        }

        if (_coSearch == null)
            _coSearch = StartCoroutine("CoSearch");

        _nma.speed = _speed;
        if (b_isJumping) {
            _nma.SetDestination(_destPos);
        } else {
            _nma.SetDestination(this.transform.position);
        }
        

        if (b_isJumping) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 20 * Time.deltaTime);
        }

    }

    protected override void UpdateSkill()
    {
        if (CanUseSkill()){
            UseSkill();
        }

        if (_skillInUse == SkillType.SKILL_THROW) {
            Quaternion targetRotation = Quaternion.LookRotation(_target.transform.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

    }

    protected override void UpdateDamaged()
    {
        _nma.SetDestination(transform.position);
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
        if (b_canUseSkillThrow /*&& dir.magnitude <= _skillThrowRange*/)
            return true;

        return false;
    }

    protected override void UseSkill()
    {
        if (_target == null || _coSkill != null)
            return;

        Vector3 dir = _target.transform.position - transform.position;
        if (b_canUseSkillThrow)
            _coSkill = StartCoroutine("CoSkillThrow");
    }

    IEnumerator CoSkillThrow()
    {
        _nma.SetDestination(transform.position); // Don't move while using skill
        //Debug.Log("fix position 1");
        b_canUseSkillThrow = false;
        _skillInUse = SkillType.SKILL_THROW;
        UpdateAnimation();

        // 피격 판정

        yield return new WaitForSeconds(0.4f); // temp interval for throw animation
        Vector3 _targetPos = _target.transform.position;
        GameObject go = Managers.Resource.Instantiate("Objects/WizardMarble");
        go.GetComponent<WizardMarble>().Init(transform.position, _targetPos);

        yield return new WaitForSeconds(0.6f);
        _target = null; //
        State = CreatureState.Idle;
        _skillInUse = SkillType.SKILL_NONE;
        yield return new WaitForSeconds(_skillInterval);
        
        _coSkill = null;
        b_canUseSkillThrow = true;
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

            float dist = (randPos - transform.position).magnitude;
            if (dist <= _walkActivationDist) continue;

            NavMeshPath path = new NavMeshPath();
            _nma.CalculatePath(randPos, path);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                _destPos = randPos;
                _coPatrol = null;
                yield break;
            }
        }
        Debug.Log("Couldn't find Adaptive path");
        _coPatrol = null;
    }

    private IEnumerator CoFindValidPath()
    {
        b_findNewPath = false;
        State = CreatureState.Walk;
        yield return new WaitForSeconds(_pathInterval);
        _coFindValidPath = null;
        b_findNewPath = true;
    }

    private void SetValidDestPos(Vector3 farDir, Vector3 playerPos)
    {
        Vector3 vNxt = new Vector3();
        Vector3 vResult = new Vector3();
        NavMeshPath path = new NavMeshPath();
        float mxDist = 0;

        for (int i =0; i <36; ++i) {
            vNxt = transform.position + Quaternion.Euler(0, -10* i, 0) * farDir * (_skillThrowRange + 1f);
            Vector3 dir = vNxt - transform.position;
            dir.y = 10f; // no vector right above the ground
            //Debug.Log("vNxt : " + vNxt);
            bool b_hasObstacle = Physics.Raycast(transform.position, dir, dir.magnitude, _layerObstacle);
            //Debug.Log("reachable? : " + b_hasObstacle + "destpos : " + vNxt);
            if (b_hasObstacle) continue;
            _nma.CalculatePath(vNxt, path);
            if (path.status == NavMeshPathStatus.PathComplete && mxDist < dir.magnitude) { // find path maximize dist from player
                mxDist = (vNxt- playerPos).magnitude; 
                vResult = vNxt;
            }
        }
        _destPos = vResult;
    }
    protected override void OnTriggerEnter(Collider other)
    {
        if (_collidedObjects.Contains(other)) {
            return;
        }
        _collidedObjects.Add(other);

        if (other.gameObject.tag == "Skill") {
            string prevSkill = other.gameObject.name;
            this._stat.OnDamaged(1f);
            Debug.Log("cur HP :" + _stat.Hp);
            if (this._stat.Hp <= 0) {
                this.OnDead();
            } else if ((_skillInUse == SkillType.SKILL_NONE)) { // state is not skill || skill but not in use yet
                b_isJumping = false; // fix rotation
                State = CreatureState.Damaged; // added Animation event in the end of HURT : SetStateIdle()
                UpdateAnimation();
                Debug.Log("Hurt State :" + State);
            }
        }
    }

    public void SetJumpingTrue()
    {
        b_isJumping = true;
    }
    public void SetJumpingFalse()
    {
        b_isJumping = false;
    }
}
