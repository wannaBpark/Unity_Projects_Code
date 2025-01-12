//#define _DEBUG  
//#define _DEBUG_UPDATE

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using static Define;
using static UnityEngine.Rendering.DebugUI;
using MonsterLove.StateMachine;



public class Driver
{
    StateEvent Update;
    StateEvent<Collider> OnCollisionEnter;
    StateEvent<Collider> OnTriggerEnter;
}

public class PlayerController : CreatureController
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    private StateMachine<CreatureState, StateDriverUnity> _fsm;

    Vector2Int _curGridPos;
    public Vector2Int CurGridPos
    {
        get { return _curGridPos; }
        set
        {
            if (_curGridPos == value)
                return;
            _curGridPos = value;
            //Managers.Map.UpdateCurGridPos(_curGridPos);
        }
    }
    protected NavMeshAgent _nma;

    protected bool _isDashing = false;
    protected GameObject _hitPoint;

    [SerializeField]
    protected GameObject[] _weapons = new GameObject[3];
    protected GameObject _curWeapon;
    private WeaponType _weaponType = WeaponType.Sword;

    protected String[,] _animName = new String[3, 2] { { "SWORD_Basic", "SWORD_Swing" }, { "AXE_Basic", "AXE_Swing" }, { "WAND_Basic", "WAND_Swing" } };
    private int _skillType = 0;

    protected float invincibilityTime = 0.0f;

    #region Initialization 
    protected override void Init_Awake()
    {
        base.Init_Awake();

        Managers.Input.KeyAction -= OnKeyBoard; // Subscribe! OnKeyBoard is a func when KeyAction gets Invoked
        Managers.Input.KeyAction += OnKeyBoard;

        //_hitPoint = Util.FindChild(gameObject, "HitPoint");
        _nma = GetComponent<NavMeshAgent>();
    }

    protected override void Init_Start()
    {
        base.Init_Start();
        Managers.Object.Add(this.gameObject); // to FindPlayer in ObjectManger

        _fsm = new StateMachine<CreatureState, StateDriverUnity>(this); // fsm memory alloc + Initial State = IDLE
        _fsm.ChangeState(CreatureState.Idle);

        _weapons[0] = Util.FindChild(gameObject, "WP_Sword", true); // weapon go's find + DeActivate
        _weapons[1] = Util.FindChild(gameObject, "WP_Axe", true);
        _weapons[2] = Util.FindChild(gameObject, "WP_Wand", true);
        foreach (GameObject w in _weapons)
            w.SetActive(false);

        ChangeWeapon(0);

        // Set Stat
        _stat = new Stat();
        _stat.MoveSpeed = 6.0f;
        _stat.DashSpeed = 30.0f;

        // HP Bar Addition
        Managers.UI.MakeWorldSpaceUI<UI_PlayerStatBar>(transform, "UI_PlayerStatBar");
        //Managers.UI.MakeWorldSpaceUI<UI_StaminaBar>(transform, "UI_StaminaBar");
    }
    #endregion

    /*protected override void UpdateAnimation()
    {
        switch (State)
        {
            case CreatureState.Idle:
                _animator.CrossFade("IDLE", 0.5f, -1, 0.0f);
                break;
            case CreatureState.Moving:
                _animator.CrossFade("MOVE", 0.1f);
                break;
            case CreatureState.Skill:
                _animator.CrossFade( _animName[(int)_weaponType,_skillType], 0.1f, -1, 0.0f);
                break;
            case CreatureState.Damaged:

                break;
            case CreatureState.Dead:

                break;
        }
    }*/

    protected override void UpdateController()
    {
        //base.UpdateController();
        _fsm.Driver.Update.Invoke();
    }

    #region Input_KeyBoard
    protected override void OnKeyBoard()
    {
        Vector3 forwardDir = Camera.main.transform.forward;
        forwardDir.y = 0;
        Vector3 rightDir = Camera.main.transform.right;
        rightDir.y = 0;

        // move input
        Vector3 moveDir = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) // ↑ 
            moveDir += forwardDir.normalized;
        if (Input.GetKey(KeyCode.S)) // ↓ 
            moveDir -= forwardDir.normalized;
        if (Input.GetKey(KeyCode.A)) // ← 
            moveDir -= rightDir.normalized;
        if (Input.GetKey(KeyCode.D)) // →
            moveDir += rightDir.normalized;

        if (!_isDashing)
            DestPos = transform.position + moveDir;

        // Dashing
        if (Input.GetKey(KeyCode.LeftShift) && /* Can Dash Condition */ true) {
            DestPos = transform.position + transform.forward * _stat.DashSpeed;
            _isDashing = true;
        }

        if (Input.GetKey(KeyCode.Q)) {
            _skillType = (int)Skilltype.Basic;
            //State = CreatureState.Skill;
            _fsm.ChangeState(CreatureState.Skill);
        } else if (Input.GetKey(KeyCode.E)) {
            _skillType = (int)Skilltype.Swing;
            _fsm.ChangeState(CreatureState.Skill);
        }

        // This code above needs to be divided into 2 parts : Input, State Handling

        


        #region UI_PlayerStat
        if (Input.GetKey(KeyCode.H)) {
            this.ApplyBuff(new DeBuffDamageOverTime(10f, this.gameObject, 2, 2f, "Hp"));
        } else if(Input.GetKey(KeyCode.J)) {
            this.ApplyBuff(new DeBuffDamageOverTime(10f, this.gameObject, 2, 2f, "Stamina"));
        } else if (Input.GetKey(KeyCode.K)) {
            this.ApplyBuff(new DeBuffDamageOverTime(10f, this.gameObject, 2, 2f, "Mental"));
        }

        #endregion

        DestPos = transform.position + moveDir.normalized * 1.0f;
    }
    #endregion

    #region Legacy_State_Methods
    protected override void UpdateIdle()
    {
        Vector3 moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist > _stat.MoveSpeed * Time.deltaTime)
        {
            State = CreatureState.Moving;
            return;
        }

        _nma.SetDestination(transform.position);
    }

    protected override void UpdateMoving()
    {
        // 도착 여부 체크
        Vector3 moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist < _stat.MoveSpeed * Time.deltaTime || (_isDashing && dist < _stat.DashSpeed * Time.deltaTime))
        {
            transform.position = DestPos;
            State = CreatureState.Idle;
            if (_isDashing)
                _isDashing = false;
            return;
        }

        // translation
        //transform.position += moveDir.normalized * _stat.MoveSpeed * Time.deltaTime;
        _nma.SetDestination(DestPos);
        _nma.speed = _stat.MoveSpeed;

        // rotation
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);
    }

    protected override void UpdateSkill()
    {
        _nma.SetDestination(transform.position);
    }
    #endregion

    #region Handling_Weapons
    public void ChangeWeapon(int _dst)
    {
        Debug.Assert(_dst != -1, " Destination Weapon == -1 ");

        if (_curWeapon != null)
            _curWeapon.SetActive(false);
        _weapons[_dst].SetActive(true);
        _curWeapon = _weapons[_dst];
        
        _weaponType = (WeaponType)_dst;

    }
    #endregion

    #region Animation_Key_Events
    public void SetStateIdle()
    {
        _fsm.ChangeState(CreatureState.Idle);
    }

    int _mask = (1 << (int)Define.Layer.Ground);
    public void OnSkill(string skillName)
    {
        if (skillName == "MeteorsAOE")
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 100.0f, _mask);

            Vector3 dir = hit.point - transform.position;
            dir.y = 0;
            transform.rotation = Quaternion.LookRotation(dir);

            GameObject go = Managers.Resource.Instantiate("Effect/MeteorsAOE");
            go.transform.position = hit.point;
        }
        else if (skillName == "RedEnergyExplosion")
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 100.0f, _mask);

            Vector3 dir = hit.point - transform.position;
            dir.y = 0;
            transform.rotation = Quaternion.LookRotation(dir);

            GameObject go = Managers.Resource.Instantiate("Effect/RedEnergyExplosion");
            go.transform.position = hit.point;
        }
        else if (skillName == "ElectroSlash")
        {
            GameObject go = Managers.Resource.Instantiate("Effect/ElectroSlash");
            go.transform.position = transform.position + transform.forward * 0.8f + transform.up * 1.6f;
            go.transform.rotation = transform.rotation * go.transform.rotation;
        }
        else if (skillName == "StoneSlash")
        {
            GameObject go = Managers.Resource.Instantiate("Effect/StoneSlash");
            go.transform.position = transform.position + transform.forward * 0.8f + transform.up * 1.6f;
            go.transform.rotation = transform.rotation * go.transform.rotation;
        }
        else if (skillName == "ContinuousSlashBlue")
        {
            GameObject go = Managers.Resource.Instantiate("Effect/ContinuousSlashBlue");
            go.transform.position = transform.position + transform.up * 1.5f;
            go.transform.rotation = transform.rotation * go.transform.rotation;
        }
        else if (skillName == "ContinuousSlashRed")
        {
            GameObject go = Managers.Resource.Instantiate("Effect/ContinuousSlashRed");
            go.transform.position = transform.position + transform.up * 1.5f;
            go.transform.rotation = transform.rotation * go.transform.rotation;
        }
    }

    #endregion

    #region Idle_Methods
    private void Idle_Enter()
    {
#if _DEBUG
        Debug.Log("Idle_Enter");
#endif
        _animator.CrossFade("IDLE", 0.5f, -1, 0.0f);

    }
    private void Idle_Update()
    {
#if _DEBUG_UPDATE
        Debug.Log("Idle : Update");
#endif
        Vector3 moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist > _stat.MoveSpeed * Time.deltaTime)
        {
            _fsm.ChangeState(CreatureState.Moving);
        }
        _nma.SetDestination(transform.position);
        _nma.speed = _stat.MoveSpeed;
    }
    private void Idle_Exit()
    {
#if _DEBUG
        Debug.Log("Idle - Exit");
#endif
    }

    private void Idle_Finally()
    {

    }
    #endregion

    #region Moving_Methods
    private void Moving_Enter()
    {
#if _DEBUG
        Debug.Log("Moving_Enter");
#endif
        _animator.CrossFade("MOVE", 0.1f);

    }
    private void Moving_Update()
    {
        // 도착 여부 체크
        Vector3 moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude;
        if (dist < _stat.MoveSpeed * Time.deltaTime || (_isDashing && dist < _stat.DashSpeed * Time.deltaTime))
        {
            //_nma.enabled = false;
            transform.position = DestPos;
            //_nma.enabled = true;

            _fsm.ChangeState(CreatureState.Idle);
            if (_isDashing)
                _isDashing = false;
        }

        // translation
        //transform.position += moveDir.normalized * _stat.MoveSpeed * Time.deltaTime;
        _nma.SetDestination(DestPos);
        _nma.speed = _stat.MoveSpeed;

        // rotation
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);
    }
    private void Moving_Exit()
    {

    }

    private void Moving_Finally()
    {

    }
    #endregion

    // Testing Bear Weapon
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "WEAPON") {
            Debug.Log(other.gameObject.name + " collision weapon!");
        }
    }

    #region Skill_Methods
    private void Skill_Enter()
    {
#if _DEBUG
        Debug.Log("Skill_Enter");
#endif
        _animator.CrossFade(_animName[(int)_weaponType, _skillType], 0.1f, -1, 0.0f);
    }
    private void Skill_Update()
    {
#if _DEBUG_UPDATE
        Debug.Log("\ SKill _ Update ... /");
#endif
    }
    private void Skill_Exit()
    {
#if _DEBUG
        Debug.Log("-> SKill_Exit");
#endif

    }

    private void Skill_Finally()
    {

    }
    #endregion


    #region StatusEffect_Methods
    public void ApplyBuff(StatusEffect effect)
    {
        effect.ApplyEffect();
        activeEffects.Add(effect);
        StartCoroutine(RemoveEffectAfterDuration(effect));
    }

    private IEnumerator RemoveEffectAfterDuration(StatusEffect effect)
    {
        yield return new WaitForSeconds(effect.Duration);
        effect.RemoveEffect();
        activeEffects.Remove(effect);
    }

    public void RemoveAllEffects()
    {
        foreach (StatusEffect effect in activeEffects) {
            effect.RemoveEffect();
        }
        activeEffects.Clear();
    }
    #endregion
}
