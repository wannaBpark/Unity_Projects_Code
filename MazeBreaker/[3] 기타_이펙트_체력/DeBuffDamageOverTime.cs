#define DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeBuffDamageOverTime: StatusEffect // StatusEffect Inherits "MonoBehaviour"
{
    private readonly int _damagePerSecond;
    private readonly float _damagePeriod;
    private readonly string _nameOfField;
    private Coroutine _damageCoroutine;

    private readonly float _reduceRate = 1f;

    public DeBuffDamageOverTime(float duration, GameObject target, int damagePerSecond, float damagePeriod, string nameOfField) : base(duration, target)
    {
        this._damagePerSecond = damagePerSecond;
        this._damagePeriod = damagePeriod;
        this._nameOfField = nameOfField;

        Debug.Assert(target != null, "Target GO is NULL!");
    }

    public override void ApplyEffect()
    {
        // Begins periodically applying damage

        //해당 코루틴은 Effect를 적용할 개체에 추가하며, 개체의 생명주기와 같다
        //아래 코드는 임시로 플레이어라고 가정한 상태이며, 추후 CoroutineHelper가 필요하다.
        //GameObject _testGameObject = GameObject.FindGameObjectWithTag("PLAYER");
        //if (_testGameObject != null && _testGameObject.GetComponent<DeBuffDamageOverTime>() == null)
        //{
        //    _testGameObject.AddComponent<DeBuffDamageOverTime>();
        //}

        //_damageCoroutine = _testGameObject.GetComponent<DeBuffDamageOverTime>().StartCoroutine(InflictDamageOverTime()); 

        if (_nameOfField.Equals("Hp")) {
            _damageCoroutine = CoroutineHelper.StartCoroutine(InflictDamageOverTime());
        } else if (_nameOfField.Equals("Stamina")) {
            _damageCoroutine = CoroutineHelper.StartCoroutine(InflictStaminaOverTime());
        } else if (_nameOfField.Equals("Mental")){
            _damageCoroutine = CoroutineHelper.StartCoroutine(InflictMentalOverTime());
        }

#if DEBUG
        if (_damageCoroutine != null) {
            Debug.Log("Dot Damage Applied : " + _damagePerSecond + " damage per sec");
        }
#endif
    }



    public override void RemoveEffect()
    {
        // remove Effect, stop Coroutine
        if ( _damageCoroutine != null )
        {
            CoroutineHelper.StopCoroutine(_damageCoroutine);
        }
#if DEBUG
        Debug.Log("Dot damage Removed");
#endif
    }

    private IEnumerator InflictDamageOverTime()
    {
#nullable enable
        Stat? _targetStat = _target.GetComponent<PlayerController>().Stat;
        //Debug.Log(_targetStat.MaxHp.ToString() + " : MAxHP");

        //float? _targetField = (float)_targetStat.GetType().GetField(_nameOfField).GetValue(_targetStat);
        //Debug.Log(_targetField.ToString() + " : _targetField");

        //Debug.Assert(_targetField >= 0, "Loaded Field is NULL");
#nullable disable

        while (_duration > 0)
        {
            // 1 : Inflict Damage Using Stat.OnDamage()
            //_targetStat.OnDamagedByDeBuff(_damagePerSecond); //assuming it's Component (Stat)

            // 2 : GetValue by Reflection
            //_targetField -= _damagePerSecond;

            _targetStat.Hp -= _damagePerSecond;
            // wait "period" seconds
            yield return new WaitForSeconds(_damagePeriod);
            _duration -= _reduceRate;
        }
    }

    private IEnumerator InflictStaminaOverTime()
    {
#nullable enable
        Stat? _targetStat = _target.GetComponent<PlayerController>().Stat;
        Debug.Assert(_targetStat != null, "Loaded Stat is NULL");
#nullable disable

        while (_duration > 0) {
            _targetStat.Stamina -= _damagePerSecond;
            yield return new WaitForSeconds(_damagePeriod);
            _duration -= _reduceRate;
        }
    }

    private IEnumerator InflictMentalOverTime()
    {
#nullable enable
        Stat? _targetStat = _target.GetComponent<PlayerController>().Stat;
        Debug.Assert(_targetStat != null, "Loaded Stat is NULL");
#nullable disable

        while (_duration > 0) {
            _targetStat.Mental -= _damagePerSecond;
            yield return new WaitForSeconds(_damagePeriod);
            //_duration -= _reduceRate;
        }
    }
}
