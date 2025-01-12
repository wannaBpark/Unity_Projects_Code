using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Define;

public enum ShootStep{
    NONE,
    READY,
    SHOOT,
    RETURN,
}
public class Bear : MonsterController
{
    int _layerAttack = 1 << (int)SkillType.SKILL_HITDOWN | 1 << (int)SkillType.SKILL_SWING | 1 << (int)SkillType.SKILL_SHOOT;
    string _pathEffect = "Effect/Monster/Boss/Bear/";
    GameObject go_weapon = null;
    TrailRenderer _swingTrail;
    AnimationClip[] _animClips;

    Coroutine _coSkillSwing = null;
    Coroutine _coSkillShoot = null;
    Coroutine _coSkillHitDown = null;

    ShootStep _shootStep = ShootStep.NONE;

    float _rangeForward = 1.0f;
    float _forwardSpeed = 5.0f; 
    float _rotationSpeed = 5.0f;
    float _rangeSwing = 4.0f;
    float _rangeShoot = 10.0f;
    float _rangeHitDown = 15.0f;
    float _rangeMinChase = 3.0f;

    float _intervalSwing = 8.0f;
    float _intervalShoot = 10.0f;
    float _intervalHitDown = 10.0f;
    
    bool b_canUseSwing   = true;
    bool b_canUseShoot   = true;
    bool b_canUseHitDown = true;
    bool b_isShootingWait = false;

    int _cntShoot = 0;
    int _maxShoot = 0;

    protected override void UpdateAnimation()
    {

        base.UpdateAnimation();
        if (_state == CreatureState.Skill)
        {
            if (_skillInUse == SkillType.SKILL_SWING) {
                _animator.CrossFade("ATTACK_V1", 0.1f, -1, 0);
            } else if (_skillInUse == SkillType.SKILL_HITDOWN) {
                _animator.CrossFade("ATTACK_V3", 0.1f, -1, 0);
            } else if (_skillInUse == SkillType.SKILL_SHOOT) {
                if (_shootStep == ShootStep.READY){
                    _animator.CrossFade("ATTACK_V2_READY", 0.1f, -1, 0);
                } else if (_shootStep == ShootStep.SHOOT) {
                    _animator.CrossFade("ATTACK_V2_SHOOT", 0.1f, -1, 0);
                } else if (_shootStep == ShootStep.RETURN) {
                    _animator.CrossFade("ATTACK_V2_RETURN", 0.1f, -1, 0);
                }
            } else {
                _animator.CrossFade("IDLE", 0.1f);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _animClips = _animator.runtimeAnimatorController.animationClips;
        go_weapon = Util.FindChild(gameObject, "Bone_Weapon", true);
        _swingTrail = Util.FindChild(gameObject, "TrailPos", true).GetComponent<TrailRenderer>();
        AddBearAnimationEvents();
    }
    protected override void Init()
    {
        base.Init();

        _speed = 3.0f;
        _runSpeed = 6.0f;
        _skillInUse = SkillType.SKILL_NONE;
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
        float dist = dir.magnitude; 
        if (dist <= _rangeMinChase) {
            _nma.SetDestination(transform.position);
            _destPos = transform.position;
            _target = null;
            State = CreatureState.Idle;
            return;
        } else if (dist> _searchRange) {
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
        if (CanUseSkill()){
            UseSkill();
        }
        //Debug.Log("cur skill : " + _skillInUse);
        if (b_isShootingWait) return;
        Quaternion targetRotation = Quaternion.LookRotation(_target.transform.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    protected override bool CanUseSkill()
    {
        // ALREADY using SKILLS -> DONT USE SKILL
        if (_target == null || _skillInUse != SkillType.SKILL_NONE)
            return false;

        Vector3 dir = _target.transform.position - transform.position;
        float dist = dir.magnitude;
        if ( (IsRangeHitDown(dist) && b_canUseHitDown) || (IsRangeSwing(dist) && b_canUseSwing) 
            || (IsRangeShoot(dist) && b_canUseShoot)) {
            return true;
        }

        return false;
    }

    protected override void UseSkill()
    {
        if (_target == null || _skillInUse != SkillType.SKILL_NONE )
            return;

        Vector3 dir = _target.transform.position - transform.position;
        float dist = dir.magnitude;

        if (IsRangeHitDown(dist) && b_canUseHitDown) {
            _coSkillHitDown = StartCoroutine("CoSkillHitDown");
        } else if (IsRangeSwing(dist) && b_canUseSwing) {
            _coSkillSwing = StartCoroutine("CoSkillSwing");
        } else if (IsRangeShoot(dist) && b_canUseShoot) {
            _coSkillShoot = StartCoroutine("CoSkillShoot");
        }
    }

    IEnumerator CoSkillSwing()
    {
        b_canUseSwing = false;
        _skillInUse = SkillType.SKILL_SWING;
        UpdateAnimation();

        // end of animation, skillInUse = SKILL_NONE

        //Debug.Log("Wait for Swing");
        // 피격 판정
        yield return new WaitForSeconds(3.6f);
        _skillInUse = SkillType.SKILL_NONE;
        _nma.speed = _speed;
        State = CreatureState.Walk;
        yield return new WaitForSeconds(_intervalSwing);
        //Debug.Log("Complete! : Swing ");
        b_canUseSwing = true;
        _coSkillSwing = null;
    }

    IEnumerator CoSkillShoot()
    {
        b_canUseShoot = false;
        _skillInUse = SkillType.SKILL_SHOOT;
        ShootStepForward();
        UpdateAnimation();

        _maxShoot = Random.Range(2, 4);
        //Debug.Log("Wait for Shoot");

        yield return new WaitForSeconds(1.767f); // Interval between READY / SHOOT
        
        while (_cntShoot <= _maxShoot){
            yield return new WaitForSeconds(0.2f);
            
            UpdateAnimation();
            EShoot();
            Shoot(_target.transform.position);
            b_isShootingWait = true;
            yield return new WaitForSeconds(1.0f);
            b_isShootingWait = false;
            ++_cntShoot;
            
        }
        ShootStepForward();
        UpdateAnimation();
        yield return new WaitForSeconds(0.73f); // Interval between RETURN / NONE
        // end of animation, skillInUse = SKILL_NONE
        _cntShoot = 0;
        _skillInUse = SkillType.SKILL_NONE;
        _shootStep = ShootStep.NONE;
        State = CreatureState.Walk;
        // 피격 판정
        yield return new WaitForSeconds(_intervalShoot);
        //Debug.Log("Complete! : Shoot");
        _coSkillShoot = null;
        b_canUseShoot = true;
    }

    IEnumerator CoSkillHitDown()
    {
        b_canUseHitDown = false;
        _skillInUse = SkillType.SKILL_HITDOWN;
        UpdateAnimation();

        //Debug.Log("Wait for HitDown");
        yield return new WaitForSeconds(2.6f);
        _skillInUse = SkillType.SKILL_NONE;
        State = CreatureState.Walk;
        // 피격 판정
        yield return new WaitForSeconds(_intervalHitDown);
        //Debug.Log("Complete! : HitDown ");
        _coSkillHitDown = null;
        b_canUseHitDown = true;
    }




    private bool IsRangeSwing(float dist)
    {
        return dist <= _rangeSwing;
    }

    private bool IsRangeShoot(float dist)
    {
        return dist <= _rangeShoot;
    }
    private bool IsRangeHitDown(float dist)
    {
        return dist <= _rangeHitDown;
    }

    public void MoveForward()
    {
        Vector3 nxtPosition = transform.position + transform.forward * _rangeForward;
        _nma.speed = _forwardSpeed * 2.0f;
        _nma.SetDestination(nxtPosition);
        //Debug.Log("Move Forward : " + nxtPosition);
        //transform.position = Vector3.Lerp(transform.position, nxtPosition, _forwardSpeed * Time.deltaTime);
    }

    public void Shoot(Vector3 _targetPos)
    {
        GameObject go = Managers.Resource.Instantiate("Objects/BearMarble",  null);
        go.GetComponent<WizardMarble>().Init(transform.position, _targetPos);
    }
    public void AddShootCount()
    {
        ++_cntShoot;
    }

    public void ShootStepForward()
    {
        _shootStep += 1;
    }

    private void AddBearAnimationEvents()
    {
        AnimationClip hitdownAnimClip = new AnimationClip();
        AnimationClip swingAnimClip = new AnimationClip();

        foreach (AnimationClip clip in _animClips) {
            switch (clip.name) {
                case "Bear_Attack1": swingAnimClip = clip; break;
                case "Bear_Attack3": hitdownAnimClip = clip; break;
            }
        }
        hitdownAnimClip.AddEvent(new AnimationEvent() { time = 48.0f / hitdownAnimClip.frameRate, functionName = "EHitDown" });
        swingAnimClip.AddEvent(new AnimationEvent() { time = 29.0f / swingAnimClip.frameRate, functionName = "EnableTrailCollider" });
        swingAnimClip.AddEvent(new AnimationEvent() { time = 72.0f / swingAnimClip.frameRate, functionName = "DisableTrailCollider" });
        #region commented: swingEffectEvent
        /*
        swingAnimClip.AddEvent(new AnimationEvent() {
            time = 26.0f / swingAnimClip.frameRate,
            functionName = "ESwing"
        });
        swingAnimClip.AddEvent(new AnimationEvent() {
            time = 46.0f / swingAnimClip.frameRate,
            functionName = "ESwing"
        });
        swingAnimClip.AddEvent(new AnimationEvent() {
            time = 71.0f / swingAnimClip.frameRate,
            functionName = "ESwing"
        });
        */
        #endregion
    }
    
    private void EHitDown()
    {
        Vector3 spawnPos = transform.position;
        spawnPos += transform.forward * 3.0f;
        Managers.Object.ExplosionToCreature(transform.position, _rangeHitDown, _stat);
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "Explosion", spawnPos, Quaternion.identity, null);
        //go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y +5.0f, go.transform.position.z);
    }
    private void ESwing()
    {
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "Swing", this.transform);
        //Destroy(go, 1.0f);
        //go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y - 0.5f, go.transform.position.z)
    }
    private void EShoot()
    {
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "Shoot", go_weapon.transform.position, Quaternion.identity, null);
        // Offset
        //go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y + 1.0f, go.transform.position.z);
    }

    private void EnableTrailCollider()
    {
        go_weapon.GetComponent<CapsuleCollider>().enabled = true;
        _swingTrail.enabled = true;
        Debug.Assert(go_weapon != null, "couldn't find bear weapon");
    }

    private void DisableTrailCollider()
    {
        go_weapon.GetComponent<CapsuleCollider>().enabled = false;
        _swingTrail.enabled = false;
        Debug.Assert(go_weapon != null, "couldn't find bear weapon");
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.gameObject.layer == (int)Layer.Player)
        {
            // checking if bear is using any skills : Layer And Operation
            int _usingAnySkill =  1 << (int)_skillInUse & _layerAttack;
            if (_usingAnySkill == 0) return;

            if (_skillInUse == SkillType.SKILL_SWING) {
                Managers.Object.TakeDamageToPlayer(this.transform.position, _stat);
            }
            //GameObject go = Managers.Resource.Instantiate(_pathEffect + "Hit", other.gameObject.transform.position, Quaternion.identity);
        }
    }

}
