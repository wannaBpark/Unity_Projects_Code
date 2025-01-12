using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Define;
using Unity.VisualScripting;

enum Hand {
    Left = 0,
    Right = 1,
}
public class Beast : MonsterController
{
    string _pathEffect = "Effect/Monster/Field/Beast/";
    GameObject[] _goHand = new GameObject[2];
    CapsuleCollider[] _collider = new CapsuleCollider[2]; 
    TrailRenderer[] _swingTrails = new TrailRenderer[2];
    AnimationClip[] _animClips;
    Coroutine _coSkill1 = null;
    Coroutine _coSkill2 = null;
    Coroutine _coJump = null;
    float _rotationSpeed = 5.0f;
    float _rangeSwing1 = 5.5f;
    float _rangeSwing2 = 5.5f;
    float _rangeJump = 3.0f;
    float _intervalSwing1 = 8.0f;
    float _intervalSwing2 = 10.0f;
    float _intervalJump = 5.0f;
    float _rangeForward = 2.0f;
    float _forwardSpeed = 10.0f;

    bool b_canHurt = true;
    bool b_canUseSwing1 = true;
    bool b_canUseSwing2 = true;
    bool b_canJump = true;


    protected override void Awake()
    {
        base.Awake();
        _animClips = _animator.runtimeAnimatorController.animationClips;
        _goHand[(uint)Hand.Left] = Util.FindChild(gameObject, "LeftHand", true);
        _goHand[(uint)Hand.Right] = Util.FindChild(gameObject, "RightHand", true);
        for(int idx = 0; idx < 2; ++idx) { 
            _swingTrails[idx] = _goHand[idx].GetComponent<TrailRenderer>();
            _collider[idx] = _goHand[idx].GetComponent<CapsuleCollider>();
        }
        this.AddBeastAnimationEvents();
    }
    protected override void UpdateAnimation()
    {
        
        base.UpdateAnimation();
        if (_state == CreatureState.Skill)
        {
            if (_skillInUse != SkillType.SKILL_NONE) {
                string _animName = (_skillInUse == SkillType.SKILL_SWING1) ? "ATTACK_V1" : "ATTACK_V2";
                _animator.CrossFade(_animName, 0.1f, -1, 0);
                //Debug.Log(_skillInUse);
                //Debug.Log("skill 1 running ? : " + _coSkill1 != null);
                //Debug.Log("skill 2 running ? : " + _coSkill2 != null);
            } else{
                _animator.CrossFade("IDLE", 0.0f, -1, 0);
            }
        } else if (_state == CreatureState.Jump) {
            _animator.CrossFade("JUMP", 0.1f, -1, 0);
        }
    }
    protected override void Init()
    {
        base.Init();
        _stat.MaxHp = 100000.0f;
        _stat.Hp = _stat.MaxHp;
        _speed = 3.0f;
        _runSpeed = 6.0f;
        _skillInUse = SkillType.SKILL_NONE;
    }

    protected override void UpdateController()
    {
        base.UpdateController();

    }

    protected override void UpdateRun()
    {
        if (CanUseSkill())
        {
            float dist = (_target.transform.position - transform.position).magnitude;
            
            if (dist > _rangeJump && b_canJump) {
                _nma.SetDestination(_target.transform.position);
                State = CreatureState.Jump;
                _coJump = StartCoroutine("CoJump");
                return;
            } else {
                _nma.SetDestination(transform.position);
                State = CreatureState.Skill;
                UseSkill();
            }
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
        if (CanUseSkill()){
            UseSkill();
        }

        Quaternion targetRotation = Quaternion.LookRotation(_target.transform.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    protected override void UpdateDead()
    {
        base.UpdateDead();
    }


    protected override bool CanUseSkill()
    {
        if (_target == null || _skillInUse != SkillType.SKILL_NONE)
            return false;

        Vector3 dir = _target.transform.position - transform.position;
        float dist = dir.magnitude;
        if (IsRangeSwing1(dist) || IsRangeSwing2(dist)) {
            return true;
        }
        return false;
    }

    protected override void UseSkill()
    {
        if (_target == null || _skillInUse != SkillType.SKILL_NONE/*|| (_coSkill1 != null && _coSkill2 != null)*/ )
            return;

        Vector3 dir = _target.transform.position - transform.position;
        float dist = dir.magnitude;

        if (IsRangeSwing1(dist) && b_canUseSwing1) {
            _coSkill1 = StartCoroutine("CoSkillSwing1");
        } else if (IsRangeSwing2(dist) && b_canUseSwing2 ) {
            _coSkill2 = StartCoroutine("CoSkillSwing2");
        } else {
            State = CreatureState.Idle;
            //SetStateIdle(); // 범위 안에 있지만, 모든 스킬이 쿨타임
        }
    }

    IEnumerator CoSkillSwing1()
    {
        b_canUseSwing1 = false;
        _skillInUse = SkillType.SKILL_SWING1;
        UpdateAnimation();

        // end of animation, skillInUse = SKILL_NONE

        //Debug.Log("Wait for Swing1");
        // 피격 판정
        yield return new WaitForSeconds(_intervalSwing1);
        //Debug.Log("Wait for Swing1 Complete!");
        _coSkill1 = null;
        b_canUseSwing1 = true;
    }

    IEnumerator CoSkillSwing2()
    {
        b_canUseSwing2 = false;
        _skillInUse = SkillType.SKILL_SWING2;
        UpdateAnimation();

        // end of animation, skillInUse = SKILL_NONE

        // 피격 판정
        //Debug.Log("Wait for Swing2");
        // 피격 판정
        yield return new WaitForSeconds(_intervalSwing2);
        //Debug.Log("Wait for Swing2 Complete!");
        _coSkill2 = null;
        b_canUseSwing2= true;
    }

    IEnumerator CoJump()
    {
        b_canJump = false;
        yield return new WaitForSeconds(2.6f);
        State = CreatureState.Run;
        yield return new WaitForSeconds(_intervalJump);
        b_canJump = true;
        _coJump = null;
    }

    private bool IsRangeSwing1(float dist)
    {
        return dist < _rangeSwing1;
    }

    private bool IsRangeSwing2(float dist)
    {
        return dist < _rangeSwing2;
    }

    public void SetSkillNone()
    {
        _skillInUse = SkillType.SKILL_NONE;
        if (State != CreatureState.Damaged) { // not damaged, but skill used finished
            State = CreatureState.Walk;
        }
        
        b_canHurt = true;
    }

    private void AddBeastAnimationEvents()
    {
        AnimationClip jumpAnimClip = new AnimationClip();
        AnimationClip walkAnimClip = new AnimationClip();
        AnimationClip[] swingAnimClips = new AnimationClip[2];
        
        foreach (AnimationClip clip in _animClips) {
            switch (clip.name){
                case "Jump": jumpAnimClip = clip;  break;
                case "Walk": walkAnimClip = clip; break; 
                case "Attack_v001": swingAnimClips[0] = clip; break;
                case "Attack_v002":  swingAnimClips[1] = clip; break;
            }
        }
        swingAnimClips[0].AddEvent(new AnimationEvent() { time = 48.0f / swingAnimClips[0].frameRate, functionName = "DisableLeftHand" });
        swingAnimClips[0].AddEvent(new AnimationEvent() { time = 65.0f / swingAnimClips[0].frameRate, functionName = "EnableRightHand" });
        swingAnimClips[0].AddEvent(new AnimationEvent() { time = 70.0f / swingAnimClips[0].frameRate, functionName = "DisableTrailColliders" });
        swingAnimClips[1].AddEvent(new AnimationEvent() { time = 47.0f / swingAnimClips[1].frameRate, functionName = "DisableTrailColliders" });
        // Move Forward
        swingAnimClips[0].AddEvent(new AnimationEvent() { time = 30.0f / swingAnimClips[0].frameRate, functionName = "MoveForward"});
        swingAnimClips[0].AddEvent(new AnimationEvent() { time = 50.0f / swingAnimClips[0].frameRate, functionName = "MoveForward"});
        swingAnimClips[1].AddEvent(new AnimationEvent() { time = 30.0f / swingAnimClips[1].frameRate, functionName = "MoveForward"});

        // Enable left hand
        swingAnimClips[0].AddEvent(new AnimationEvent() { time = 36.0f / swingAnimClips[0].frameRate, functionName = "EnableLeftHand" });
        swingAnimClips[1].AddEvent(new AnimationEvent() { time = 36.0f / swingAnimClips[0].frameRate, functionName = "EnableLeftHand" });

        jumpAnimClip.AddEvent(new AnimationEvent()
        {
            time = 0 / jumpAnimClip.frameRate,
            functionName = "EJumpStart"
        });
        jumpAnimClip.AddEvent(new AnimationEvent()
        {
            time = 1.0f,
            functionName = "EJumpEnd"
        });

        walkAnimClip.AddEvent(new AnimationEvent()
        {
            time = 13.0f / walkAnimClip.frameRate,
            functionName = "EWalk"
        });
        walkAnimClip.AddEvent(new AnimationEvent()
        {
            time = 33.0f / walkAnimClip.frameRate,
            functionName = "EWalk"
        });
    }

    private void EJumpStart()
    {
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "JumpStart", transform.position, transform.rotation ,null);
        go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y - 0.5f, go.transform.position.z);
        //Debug.Log("Jump Start Effect!\n");
    }
    private void EJumpEnd()
    {
        Vector3 spawnPos = transform.position + transform.forward * 3.0f;
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "JumpEnd", spawnPos, transform.rotation, null);
        Managers.Object.ExplosionToCreature(spawnPos, 8, _stat);
        //go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y - 0.5f, go.transform.position.z)
        //Debug.Log("Jump End Effect!\n");
    }

    private void EWalk()
    {
        //Renderer[] renderers = GetComponentsInChildren<Renderer>();
        //foreach (Renderer renderer in renderers)
        //{
        //    if (!renderer.isVisible)
        //    {
        //        return;
        //    }
        //}
            
        //GameObject go = Managers.Resource.Instantiate(_pathEffect + "Walk", transform.position, Quaternion.LookRotation(new Vector3(0,90,0)),null);
        //go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y - 0.5f, go.transform.position.z)
        //Debug.Log("Jump End Effect!\n");
    }

    private void EnableTrail()
    {
        foreach (var trail in _swingTrails) {
            trail.gameObject.SetActive(true);
            trail.enabled= true;
        }
    }

    private void DisableTrailColliders()
    {
        for(int i = 0; i < 2; ++i) {
            _swingTrails[i].enabled = false;
            _collider[i].enabled = false;
        }
    }

    private void EnableRightHand()
    {
        _swingTrails[(int)Hand.Right].enabled = true;
        _collider[(int)Hand.Right].enabled = true;
    }

    private void EnableLeftHand()
    {
        _collider[(uint)Hand.Left].enabled = true; // enable only LeftHand Collider and trail at first
        _swingTrails[(int)Hand.Left].enabled = true;
    }
    private void DisableLeftHand()
    {
        _swingTrails[(int)Hand.Left].enabled = false;
        _collider[(int)Hand.Left].enabled = false;
    }


    private void DisableColliders()
    {
        for (uint i = 0; i < 2; i++) {
            _collider[i].enabled = false;
        }
    }

    public void MoveForward()
    {
        Vector3 nxtPosition = transform.position + transform.forward * _rangeForward;
        _nma.speed = _forwardSpeed;
        _nma.SetDestination(nxtPosition);
        //Debug.Log("Move Forward : " + nxtPosition);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Skill") {
            this._stat.OnDamaged(25f);
            if (this._stat.Hp <= 0) {
                this.OnDead();
            } else if ((_skillInUse == SkillType.SKILL_NONE)) { // state is not skill || skill but not in use yet
                State = CreatureState.Damaged; // added Animation event in the end of HURT : SetStateIdle()
                SetSkillNone();
                DisableColliders();
                
            }
        }
        // Collision with Player
        //Debug.Log("other.layer : " + other.gameObject.layer);
        //Debug.Log("skills : " + _skillInUse);
        if (other.gameObject.layer == (int)Layer.Player) {
            if (_skillInUse != SkillType.SKILL_SWING1 && _skillInUse != SkillType.SKILL_SWING2) return;
            //if (!b_canHurt) return;

            // take damage only once
            //Debug.Log("calling takedamage to player: ");
            //Managers.Object.TakeDamageToPlayer(this.transform.position, _stat); // beast 왼손 오른손 충돌. 
            b_canHurt = false;
        }
    }

}
