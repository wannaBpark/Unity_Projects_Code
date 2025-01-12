using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PianoController : PlayerController
{
    protected override void Init()
    {
        base.Init();

        Managers.UI.MakeWorldSpaceUI<UI_HPBar>(transform, "UI_PianoHPBar");
    }

    void UpdatePlayAnimation()
    {
        switch (State)
        {
            case Define.CreatureState.Idle:
                _animator.CrossFade("IDLE_HIT", 0.1f, 2, 0.0f);
                break;
            case Define.CreatureState.Moving:
                _animator.CrossFade("RUN_HIT", 0.1f, 1, 0.0f);
                break;
        }
    }

    protected override void GetInput()
    {
        // move input
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            moveDir.z += 1.0f;
        if (Input.GetKey(KeyCode.S))
            moveDir.z -= 1.0f;
        if (Input.GetKey(KeyCode.A))
            moveDir.x -= 1.0f;
        if (Input.GetKey(KeyCode.D))
            moveDir.x += 1.0f;

        if (!_isDashing)
            DestPos = transform.position + moveDir;

        // Dash
        if (Input.GetKeyDown(KeyCode.G) && Managers.Note.Combo >= 3)
        {
            Managers.Note.Combo -= 3;
            DestPos = transform.position + transform.forward * 4.0f;
            _isDashing = true;
            invincibilityTime = 0.25f;
            // TEMP
            GameObject effect = Managers.Resource.Instantiate("Effect/Dash_Piano", transform);
            effect.transform.SetParent(null);
            effect.transform.position = DestPos + new Vector3(0.0f, 0.5f, 0.0f);
        }

        if (DestPos.x < -15 || 15 < DestPos.x || DestPos.z < -27 || 9 < DestPos.z)
            DestPos = transform.position;

        // Hit Note
        if (Input.GetKeyDown(KeyCode.F))
            HitNote();
    }

    void HitNote()
    {
        bool success = Managers.Note.CheckTiming(Define.NoteType.Piano);
        UpdatePlayAnimation();

        if (success == false) {
            return;
        }

        // Called only if when hitnote successful

        GameObject effect = Managers.Resource.Instantiate("Effect/HitNote", _hitPoint.transform);
        effect.transform.localPosition = Vector3.zero;
        GameObject enemy = Managers.Object.Boss.gameObject;
        if (enemy != null)
        {
            if (Managers.Note.CurrentMusic == MusicType.Aggressive)
            {
                enemy.GetComponent<BossController>().OnDebuff(true);
                effect = Managers.Resource.Instantiate("Effect/Debuff_Projectile");
                effect.transform.position = transform.position + Vector3.up * 0.8f;
                effect.GetOrAddComponent<Projectile>().SetTarget(enemy.transform, _stat, HitType.Debuff, 100.0f);
            }
            else if (Managers.Note.CurrentMusic == MusicType.Balance || Managers.Note.CurrentMusic == MusicType.Intro)
            {
                effect = Managers.Resource.Instantiate("Effect/Damage_Projectile");
                effect.transform.position = transform.position + Vector3.up * 0.8f;
                effect.GetOrAddComponent<Projectile>().SetTarget(enemy.transform, _stat, HitType.Damage);
            }
            else if (Managers.Note.CurrentMusic == MusicType.Defensive)
            {
                ViolinController violin = Managers.Object.ViolinPlayer;
                violin.IsShieldOn = true;
                IsShieldOn = true;
            }
        }
        
    }
}
