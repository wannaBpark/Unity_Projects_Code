using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class Projectile : MonoBehaviour
{
    float _speed = 0.0f;          // ź��
    float _rotationSpeed = 500.0f; // ȸ�� �ӵ�

    Transform _target;
    Stat _stat;
    HitType _hitType;

    public void SetTarget(Transform target, Stat attacker, HitType hitType, float speed = 10.0f)
    {
        _target = target;
        _stat = attacker;
        _hitType = hitType;
        _speed = speed;
    }

    void Start()
    {
        // ������� �ٶ󺸵���
        Vector3 destPos = _target.position + new Vector3(0.0f, 2.0f, 0.0f);
        Vector3 direction = (destPos - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void Update()
    {
        if (_target == null)
        {
            // ����� ������ ��Ȱ��ȭ
            gameObject.SetActive(false);
            return;
        }

        Vector3 destPos = _target.position + new Vector3(0.0f, 2.0f, 0.0f);


        // ���� ���� üũ
        Vector3 moveDir = destPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist < _speed * Time.deltaTime)
        {
            CreatureController cc = _target.GetComponent<CreatureController>();
            if (cc != null)
                cc.OnDamaged(_stat, _hitType);
            if (_hitType == HitType.Debuff)
                GameObject.Destroy(gameObject, 0.5f);
            else
                Managers.Resource.Destroy(gameObject);
            return;
        }

        // ��� �������� ȸ��
        Vector3 direction = (destPos - transform.position).normalized;
        Quaternion toRotation = Quaternion.LookRotation(direction);
        if (_hitType == HitType.Debuff)
            transform.rotation = toRotation;
        else
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, _rotationSpeed * Time.deltaTime);

        // ź �ӵ��� �̵�
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }
}
