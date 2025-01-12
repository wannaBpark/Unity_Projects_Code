using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;

public class SK_Archer : MonsterController
{
    bool _canUseSkillAuto = true;
    float _skillAutoRange = 10.0f;

    bool _canUseSkillProjectile = true;
    float _skillProjectileRange = 10.0f;

    bool _canUseSkillKiting = true;
    float _minDistance = 8.0f;
    bool _isKiting = false;

    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        if (_state == CreatureState.Skill)
        {
            if (_skillInUse == SkillType.SKILL_AUTO)
                _animator.CrossFade("SKILL_AUTO", 0.1f);
            else if (_skillInUse == SkillType.SKILL_PROJECTILE)
                _animator.CrossFade("SKILL_PROJECTILE", 0.1f, -1, 0);
            else if (_skillInUse == SkillType.SKILL_RUSH)
                _animator.CrossFade("SKILL_RUSH", 0.1f);
        }
    }

    protected override void Init()
    {
        base.Init();

        _searchRange = 15.0f;
        _speed = 3.0f;
        _runSpeed = 6.0f;
    }

    protected override void UpdateSkill()
    {
        if (_coSkill == null && !CanUseSkill())
        {
            State = CreatureState.Walk;
            return;
        }

        if (CanUseSkill())
            UseSkill();

        Vector3 dir = _target.transform.position - transform.position;
        if (_isKiting)
        {
            _destPos = _target.transform.position + -1 * (dir.normalized * (_minDistance + 0.5f));
            _nma.SetDestination(_destPos);
            _nma.speed = _runSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-dir), 20 * Time.deltaTime);
        }
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 20 * Time.deltaTime);
    }

    protected override void UpdateDead()
    {

    }

    protected override bool CanUseSkill()
    {
        if (_target == null || _coSkill != null)
            return false;

        Vector3 dir = _target.transform.position - transform.position;
        if (_canUseSkillAuto && dir.magnitude < _skillAutoRange)
            return true;
        if (_canUseSkillKiting && dir.magnitude < _minDistance)
            return true;
        if (_canUseSkillProjectile && dir.magnitude <= _skillProjectileRange)
            return true;

        return false;
    }

    protected override void UseSkill()
    {
        if (_target == null || _coSkill != null)
            return;

        Vector3 dir = _target.transform.position - transform.position;
        if (_canUseSkillProjectile && dir.magnitude <= _skillProjectileRange)
            _coSkill = StartCoroutine("CoSkillProjectile");
        else if (_canUseSkillKiting && dir.magnitude < _minDistance)
            _coSkill = StartCoroutine("CoSkillKiting");
        else if (_canUseSkillAuto && dir.magnitude <= _skillAutoRange)
            _coSkill = StartCoroutine("CoSkillAuto");
    }

    IEnumerator CoSkillAuto()
    {
        _canUseSkillAuto = false;
        _skillInUse = SkillType.SKILL_AUTO;
        UpdateAnimation();

        yield return new WaitForSeconds(0.5f);
        //State = CreatureState.Walk;
        _skillInUse = SkillType.SKILL_NONE;
        _coSkill = null;

        _canUseSkillAuto = true;
    }

    IEnumerator CoSkillProjectile()
    {
        _canUseSkillProjectile = false;
        _skillInUse = SkillType.SKILL_PROJECTILE;
        UpdateAnimation();

        yield return new WaitForSeconds(1.0f);

        GameObject go = Managers.Resource.Instantiate("Effect/Arrow");
        ArrowController arrow = go.GetComponent<ArrowController>();
        arrow.transform.position = transform.position + new Vector3(0.0f, 1.0f, 0.0f);
        arrow.Dir = transform.forward;

        yield return new WaitForSeconds(0.5f);

        //State = CreatureState.Walk;
        _skillInUse = SkillType.SKILL_NONE;
        _coSkill = null;

        yield return new WaitForSeconds(3.0f);

        _canUseSkillProjectile = true;
    }

    IEnumerator CoSkillKiting()
    {
        _canUseSkillKiting = false;
        _skillInUse = SkillType.SKILL_RUSH;
        UpdateAnimation();
        _isKiting = true;

        yield return new WaitForSeconds(0.8f);
        //State = CreatureState.Walk;
        _skillInUse = SkillType.SKILL_NONE;
        _coSkill = null;
        _isKiting = false;

        _canUseSkillKiting = true;
    }
}
