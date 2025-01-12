using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static Define;

public class Tree : MonsterController
{
    string _pathEffect = "Effect/Monster/General/Tree/";

    uint _maxSpit = 3;
    bool b_canUseSkillSpit = true;
    bool b_jumpRotated = false;
    float _rotationSpeed = 20.0f;
    float _skillSpitRange = 10.0f;
    float _spitInterval = 1.5f;
    float _spitForce = 2f;

    Vector3 _spitDir = Vector3.zero;

    protected override void UpdateController()
    {
        base.UpdateController();

        if (_state == CreatureState.Jump) {
            this.UpdateJump();  
        }
    }

    // MonsterController의 _patrolRange로 조절
    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        if (_state == CreatureState.Skill) {
            //Debug.Log("Play Spit Animation " + _skillInUse);
            if (_skillInUse == SkillType.SKILL_SPIT) {
                _animator.CrossFade("SKILL_SPIT", 0.1f, -1, 0);
            }
        } else if (_state == CreatureState.Jump) {
            _animator.CrossFade("JUMP", 0.1f);
        }

    }

    protected override void Init()
    {
        base.Init();

        _speed = 3.0f;
        _runSpeed = 6.0f;
    }

    protected override void UpdateIdle()
    {
        //Debug.Log("Is target is null? : " + _target == null);
        if (_target != null)
        {
            State = CreatureState.Jump;          // Turn around to player direction
            //SetSpitDirection();
            return;
        }


        //if (_coPatrol == null)
        //    _coPatrol = StartCoroutine("CoPatrol");
        if (_coSearch == null)
            _coSearch = StartCoroutine("CoSearch");

    }

    protected override void OnDamaged()
    {
        State = CreatureState.Damaged;
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "Hit", transform);
        UpdateAnimation();
    }

    protected override void UpdateSkill()
    {
        if (CanUseSkill()) {
            UseSkill();
        }

        if (_skillInUse == SkillType.SKILL_SPIT){
            Quaternion targetRotation = Quaternion.LookRotation(_target.transform.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    protected override void UpdateDead()
    {
        base.UpdateDead();
    }

    private void UpdateJump()
    {
        //if (!b_jumpRotated) {
        Quaternion targetRotation = Quaternion.LookRotation(_target.transform.position - this.transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        b_jumpRotated = true;
        //}
    }

    protected override bool CanUseSkill()
    {
        // If skill is using ( Coroutine is running) , return false
        if (_coSkill != null)
            return false;

        if (b_canUseSkillSpit)
            return true;

        return false;
    }

    protected override void UseSkill()
    {
        if (_coSkill != null)
            return;

        if (b_canUseSkillSpit)
            _coSkill = StartCoroutine("CoSkillSpit");
    }

    IEnumerator CoSkillSpit()
    {
        uint cntSpit = 0;
        
        //Debug.Log("Coroutine spit skill");
        b_canUseSkillSpit = false;
        while (cntSpit++ <= _maxSpit) {
            _skillInUse = SkillType.SKILL_SPIT;
            UpdateAnimation();
            yield return new WaitForSeconds(0.4f); // spit action keyframe starts after 0.4
            SetSpitDirection();
            //_target = null;

            GameObject eMuzzle = Managers.Resource.Instantiate(_pathEffect + "Muzzle", transform);

            GameObject go = Managers.Resource.Instantiate("Objects/TreeSpit");
            go.transform.position = transform.position;
            go.GetComponent<TreeSpit>().Dir = _spitDir;

            yield return new WaitForSeconds(0.3f);
        }
        _skillInUse = SkillType.SKILL_NONE;
        yield return new WaitForSeconds(_spitInterval);
        State = CreatureState.Idle;
        _target = null;
        _coSkill = null;
        b_canUseSkillSpit = true;
    }

    private void SetSpitDirection()
    {
        _spitDir = (_target.transform.position - transform.position).normalized; // Set spit direction to CURRENT Player Position
        _spitDir.y = 0;

    }

    public override void SetStateSkill()
    {
        b_jumpRotated = false;
        GameObject go = Managers.Resource.Instantiate(_pathEffect + "Jump", this.transform);
        base.SetStateSkill();
    }
}
