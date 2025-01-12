using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public class ViolinController : PlayerController
{
    protected override void Init()
    {
        base.Init();

        Managers.UI.MakeWorldSpaceUI<UI_HPBar>(transform, "UI_ViolinHPBar");
    }

    bool _playAnimFlag = false;
    void UpdatePlayAnimation()
    {
        if (_playAnimFlag)
            _animator.CrossFade("DOWN", 0.1f, 1);
        else
            _animator.CrossFade("UP", 0.1f, 1);
        _playAnimFlag = !_playAnimFlag;
    }

    protected override void GetInput()
    {
        // move input
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.UpArrow))
            moveDir.z += 1.0f;
        if (Input.GetKey(KeyCode.DownArrow))
            moveDir.z -= 1.0f;
        if (Input.GetKey(KeyCode.LeftArrow))
            moveDir.x -= 1.0f;
        if (Input.GetKey(KeyCode.RightArrow))
            moveDir.x += 1.0f;

        if (!_isDashing)
            DestPos = transform.position + moveDir;

        // Dash
        if (Input.GetKeyDown(KeyCode.Period) && Managers.Note.Combo >= 3)
        {
            Managers.Note.Combo -= 3;
            DestPos = transform.position + transform.forward * 4.0f;
            _isDashing = true;
            invincibilityTime = 0.25f;
            // TEMP
            GameObject effect = Managers.Resource.Instantiate("Effect/Dash_Violin", transform);
            effect.transform.SetParent(null);
            effect.transform.position = DestPos + new Vector3(0.0f, 0.5f, 0.0f);
        }

        if (DestPos.x < -15 || 15 < DestPos.x || DestPos.z < -27 || 9 < DestPos.z)
            DestPos = transform.position;

        // Hit Note
        if (Input.GetKeyDown(KeyCode.Slash))
            HitNote();

        // Change BGM
        if (Input.GetKeyDown(KeyCode.Semicolon))
            Managers.Note.PlayMusic(MusicType.Aggressive);
        if (Input.GetKeyDown(KeyCode.T))
            Managers.Note.PlayMusic(MusicType.Defensive);
    }

    void HitNote()
    {
        bool success = Managers.Note.CheckTiming(Define.NoteType.Violin);
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
            if (Managers.Note.CurrentMusic == MusicType.Defensive)
            {
                effect = Managers.Resource.Instantiate("Effect/Groggy_Projectile");
                effect.transform.position = transform.position + Vector3.up * 0.8f;
                effect.GetOrAddComponent<Projectile>().SetTarget(enemy.transform, _stat, HitType.Groggy);
            }
            else
            {
                effect = Managers.Resource.Instantiate("Effect/Damage_Projectile");
                effect.transform.position = transform.position + Vector3.up * 0.8f;
                effect.GetOrAddComponent<Projectile>().SetTarget(enemy.transform, _stat, HitType.Damage);
            }
        }
        
    }
}
