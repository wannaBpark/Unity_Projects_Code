//#define DEBUG
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;


/*
public class BossSkillDelay
{
    float timeWalk = 5.0f;
    float timeScream = 10.0f;
    float timeDive = 10.0f;
    float timeThrow = 20.0f;
    float timeSpin = 0.0f;
    float timeChase = 5.0f;

    public BossSkillDelay()
    {
        this.timeWalk = 5.0f;
        this.timeScream = 0.0f;
        this.timeDive = 10.0f;
        this.timeThrow = 20.0f;
        this.timeSpin = 30.0f;
    }
}*/

public enum SkillType
{
    None,
    Scream,
    Chase,
    Dive,
    GetUp,
    Throw,
    Spin,
}

public class BossController : CreatureController
{
    GameObject _cuboid;
    GameObject _diveCollider;
    GameObject _goDialogue = null;
    BulletSpawner bulletSpawner;
    GameObject _map;
    Coroutine _coScream;
    Coroutine _coDive;
    Coroutine _coThrow;
    Coroutine _coSpin;
    Coroutine _coChase;
    Coroutine _coStunned;
    Coroutine _coMoveTo;
    Coroutine _coSpawnBullets;

    public Material[] bossColor;
    SkinnedMeshRenderer _smd;

    SkillType _skill = SkillType.Dive;
    public Transform _rightArm;
    public Transform _diveEffectPos;
    public Transform _screamEffectPos;
    private Transform _dialoguePos;
    private Vector3 moveDir;
    //float timeWalk = 5.0f;
    float timeScream = 3.0f;
    float timeDive = 10.0f;
    float timeThrow = 20.0f;
    float timeSpin = 15.0f;
    float timeStunned = 15.0f;

    float timeGetUp = 3.0f;
    float timeChase = 5.0f;
    float timeSpinAnim = 6.0f;
    //[SerializeField] CharacterController controller;
    [SerializeField] 
    Transform target;
    //bool isReachedDest = true;
    bool _isChasing = false;
    bool _isSkillUsing = false;
    bool _isSpinUsing = false;
    bool _isCrazy = false;
    bool _isNxtCrazy = false;
    bool _isDecreasingGroggy = false;
    float _coeffCrazy = 1.0f;

    //float _walkSpeed = 2.0f;
    float _chaseSpeed = 8.0f;
    float distOffSet = 6.0f;
    float chaseOffset = 3.0f;
    float _screamSpeed = 15.0f;

    int _nxtTargetIdx = 0;

    ParticleSystem _debuffEffect;
    
    protected BossState _bossState = BossState.Walk;
    public BossState BState
    {
        get { return _bossState; }
        set
        {
            if (_bossState == value)
                return;

            _bossState = value;
            UpdateAnimation();
        }
    }
    protected override void Init()
    {
        base.Init();
        State = CreatureState.Start;

        _cuboid = this.transform.GetChild(0).gameObject;
        _diveCollider = this.transform.GetChild(1).gameObject;
        _smd = this.transform.GetChild(5).gameObject.GetComponent<SkinnedMeshRenderer>();

        _rightArm = this.transform.Find("RightArmPos");
        _diveEffectPos = this.transform.Find("DiveEffectPos");
        _screamEffectPos = this.transform.Find("ScreamEffectPos");
        _dialoguePos = this.transform.Find("DialoguePos");
        bulletSpawner = this.GetComponent<BulletSpawner>();
        
        _map = GameObject.FindGameObjectWithTag("Map");
        _debuffEffect = GetComponentInChildren<ParticleSystem>();

        bossColor = Resources.LoadAll<Material>("Models/Boss/Materials");


        InvokeRepeating("SwitchToCrazy", 90f, 90f);

        // Set Stat
        _stat.Hp = 10000;
        _stat.MaxHp = 10000;
        _stat.Attack = 50;
        _stat.Defense = 20;
        _stat.Groggy = 0;
        _stat.MaxGroggy = 50;
        _stat.MoveSpeed = 5.0f;
    }

    protected void GetInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            //isDashActive = true;
            Managers.Effect.PlayDashEffect(transform);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            _stat.Groggy = _stat.MaxGroggy;
            Camera.main.GetComponent<CameraController>().ZoomInBoss = false;
            StopAllCoroutines(); // Stop all Coroutines, ESPECIALLY Skills
            SetAllCoroutinesToNull(); // RESET Coroutine variables INTO NULL
            GameObject eBStunned =
                    Managers.Effect.PlayEffectByName("bStunned", this.transform);
            State = CreatureState.Stunned;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            State = CreatureState.Crazy;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            DeActivateCrazy();
        }
    }

    private Transform SetUpTarget()
    {
        return transform;
    }

    private void FindNewTarget()
    {
        GameObject[] _gobjs = GameObject.FindGameObjectsWithTag("Player");
        Vector3 dist0 = _gobjs[0].transform.position - this.transform.position; 
        Vector3 dist1 = _gobjs[1].transform.position - this.transform.position;


        GameObject _target = dist0.magnitude <= dist1.magnitude ? _gobjs[0] : _gobjs[1];
        if (_target != null)
        {
            DestPos = _target.transform.position;
        }
    }

    protected override void UpdateAnimation()
    {
        DeSpawnDialogue();
        switch (State)
        {
            case CreatureState.Idle:
                _animator.CrossFade("WALK", 0f, -1, 0);
                Debug.Log("IDLE Animation Transition");
                break;
            case CreatureState.Moving:
                //_animator.CrossFade("MOVE", 0.1f);
                break;
            case CreatureState.Damaged:
                //_animator.CrossFade("GROGGY", 0.1f);
                break;
            case CreatureState.Stunned:
                this.transform.rotation = Quaternion.Euler(new Vector3(0f, -180f, 0f));
                //_goDialogue = Managers.Resource.Instantiate("UI/WorldSpace/Boss/Stunned", transform);
                UI_SpeechBubble ui_stunned = Managers.UI.MakeWorldSpaceUI<UI_SpeechBubble>(transform, "Boss/Stunned");
                ui_stunned.SetOffset(0.0f, 4.8f, 0.0f);
                _goDialogue = ui_stunned.gameObject;
                _animator.CrossFade("GROGGY", 0f, -1, 0);
                break;
            case CreatureState.Dead:
                this.transform.rotation = Quaternion.Euler(new Vector3(0f, -180f, 0f));
                Camera.main.GetComponent<CameraController>().ZoomInBoss = true;
                _animator.CrossFade("DEAD", 0f, -1, 0);
                break;
            case CreatureState.Crazy:
                this.transform.rotation = Quaternion.Euler(new Vector3(0f, -180f, 0f));
                _animator.CrossFade("CRAZY", 0f, -1, 0);
                break;
            case CreatureState.Start:
                //_goDialogue = Managers.Resource.Instantiate("UI/WorldSpace/Boss/Start", transform);
                UI_SpeechBubble ui_start = Managers.UI.MakeWorldSpaceUI<UI_SpeechBubble>(transform, "Boss/Start");
                ui_start.SetOffset(0.0f, 4.8f, 0.0f);
                _goDialogue = ui_start.gameObject;
                this.transform.rotation = Quaternion.Euler(new Vector3(0f, -180f, 0f));
                Camera.main.GetComponent<CameraController>().ZoomInBoss = true;
                _animator.CrossFade("START", 0f, -1, 0);
                break;
        }
        if (State == CreatureState.Skill)
        {
            
            switch (_skill)
            {
                case SkillType.Scream:
                    _animator.CrossFade("SKILL_SCREAM", 0f, -1,0);
                    Debug.Log("Scream Animation Transition");
                    break;
                case SkillType.Chase:
                    _animator.CrossFade("SKILL_CHASE", 0f, -1, 0);
                    break;
                case SkillType.Dive:
                    _animator.CrossFade("SKILL_DIVE", 0f, -1, 0);
                    break;
                case SkillType.GetUp:
                    _animator.CrossFade("SKILL_GETUP", 0f, -1, 0);
                    break;
                case SkillType.Throw:
                    _animator.CrossFade("SKILL_THROW", 0f, -1, 0);
                    break;
                case SkillType.Spin:
                    if (_isSpinUsing) {
                        _animator.CrossFade("SKILL_SPIN", 0f, -1, 0);
                    } 
                    break;
            }
        }

    }

    protected override void UpdateController()
    {
        GetInput();
        switch (State)
        {
            case CreatureState.Idle:
                UpdateIdle();
                break;
            case CreatureState.Moving:
                //_animator.CrossFade("MOVE", 0.1f);
                break;
            case CreatureState.Damaged:
                //_animator.CrossFade("GROGGY", 0.1f);
                break;
            case CreatureState.Stunned:
                UpdateStunned();
                break;
            case CreatureState.Dead:
                UpdateDead();
                break;
            case CreatureState.Skill:
                UpdateSkill();
                break;
        }
    }

    protected override void UpdateIdle()
    {
        FindNewTarget();
        moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude - distOffSet; // dist = real distance - boundary Offset(������)

        if (dist <= _stat.MoveSpeed * Time.deltaTime && !_isSkillUsing)
        {
            //Debug.Log("Before State changed! : " + State + "Skilltype : " + _skill);
            State = CreatureState.Idle;
            //transform.position = DestPos;
            _skill = UpdateSkillType();
            State = CreatureState.Skill;
            return;
        } else {
            //Debug.Log("ELSE Distance : " + dist);
        }

        // translation
        transform.position += (moveDir.normalized * _stat.MoveSpeed * Time.deltaTime) * _coeffCrazy;


        // rotation
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);
        //Debug.Log("Walk!!!");

        
    }
    protected void UpdateChasing()
    {
        FindNewTarget();
        // ���� ���� üũ
        moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude - chaseOffset; // dist = real distance - boundary Offset(������)
        
        if (dist <= _stat.MoveSpeed * Time.deltaTime)
        {
            //transform.position = DestPos;
            DestPos += moveDir.normalized * 3.5f;

            State = CreatureState.Idle;
            _skill = SkillType.Dive;
            State = CreatureState.Skill;
            _coDive = StartCoroutine("CoStartDive");
            return;
        } 
        // translation
        transform.position += (moveDir.normalized * _chaseSpeed * Time.deltaTime) * _coeffCrazy;

        // rotation
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);
    }

    protected void UpdateDiving()
    {
        moveDir = DestPos - transform.position;
        if (moveDir.magnitude > 0.1f) {
            transform.position += moveDir.normalized * _chaseSpeed * Time.deltaTime;
        }
    }
    protected void UpdateSpin()
    {
        // Set DestPos as the Map Center
        //DestPos = _map.transform.position;
        Vector3 DestPos = Vector3.zero;
        moveDir = DestPos - transform.position;
        float dist = moveDir.magnitude - chaseOffset; // dist = real distance - boundary Offset(������)

        // translation
        transform.position += (moveDir.normalized * _stat.MoveSpeed * Time.deltaTime) * _coeffCrazy;

        // rotation
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);

        if (moveDir.magnitude <= 0.1f)
        {
            _coMoveTo = null;
            _isSpinUsing = true;

            State = CreatureState.Idle;
            _skill = SkillType.Spin;
            State = CreatureState.Skill;
            _coSpin = StartCoroutine("CoStartSpin");
            return;
        }
    }

    protected override void UpdateSkill()
    {
        // If there is any COROUTINE available, don't use any skills
        //if (_coSpin != null || _coThrow != null || _coDive != null || _coScream != null)
        //    return;
        if (_coSpin == null && _skill == SkillType.Spin)
        {
            UpdateSpin();
        }
        else if (_coDive == null && _skill == SkillType.Chase) { // chase before dive
            _isChasing = true;
            UpdateChasing();
        } else if (_coDive != null && _skill == SkillType.Dive) { // dive coroutine executing 
            UpdateDiving();
        }
        //_coChase = StartCoroutine("CoStartChase");
        else if (_skill == SkillType.Scream && _coScream == null)
            _coScream = StartCoroutine("CoStartScream");
        
    }

    private SkillType UpdateSkillType()
    {
        _isSkillUsing = true;
        if (_coSpin == null)
            return SkillType.Spin;
        //else if (_coThrow == null)
        //    return SkillType.Throw;
        else if (_coDive == null)
            return SkillType.Chase;
        else
            return SkillType.Scream;
    }


    protected override void UpdateStunned()
    {
        _stat.Groggy -= Time.deltaTime * 2.0f;
        if (_stat.Groggy <= 0)
        {
            _stat.Groggy = 0;
            DeActivateCrazy();
        }
    }

    IEnumerator CoStartScream()
    {
        // Spawn the CUBOID that indicates Collider range
        FindNewTarget();
        Vector3 moveDir = DestPos - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0.0f, moveDir.z));
        transform.rotation = targetRotation;
        yield return new WaitForSeconds(timeScream); // Cause there is no Scream coolTime
        _coScream = null;
    }

    IEnumerator CoStartChase()
    {
        _isChasing = true;
        yield return new WaitForSeconds(timeChase);
        _isChasing = false;
    }
    IEnumerator CoStartDive()
    {
        State = CreatureState.Idle;
        _skill = SkillType.Dive;
        State = CreatureState.Skill;

        yield return new WaitForSeconds(timeGetUp / _coeffCrazy);
        State = CreatureState.Idle;
        _skill = SkillType.GetUp;
        State = CreatureState.Skill;

        //EndAnimation();
        yield return new WaitForSeconds(timeDive);
        _coChase = null;
        _coDive = null;
    }

    IEnumerator CoStartThrow()
    {
        //SpawnThrowBall(); // this will be called in the ANIMATION

        yield return new WaitForSeconds(timeThrow);
        _coThrow = null;
    }

    
    IEnumerator CoStartSpin()
    {
        // Notice : Go to Center position of the stage
       
        bulletSpawner.CalcBulletCount(timeSpinAnim);
        _coSpawnBullets = StartCoroutine(bulletSpawner.SpawnBullets());
        Debug.Log("Spawn Bullets!");
        

        yield return new WaitForSeconds(timeSpinAnim);
        EndAnimation();

        yield return new WaitForSeconds(timeSpin);
        _coSpin = null;
        _isSpinUsing = false;
    }


    IEnumerator CoStartStunned()
    {
        GameObject eBStunned = 
            Managers.Effect.PlayEffectByName("bStunned", this.transform);

        //yield return new WaitForSeconds(timeStunned);
        
        DeActivateCrazy();
        _coStunned = null;
        _stat.Groggy = 0;
        yield return null;
    }

    public void EndAnimation()
    {
        Debug.Log("EndAnimation");

        Camera.main.GetComponent<CameraController>().ZoomInBoss = false;
        _isSkillUsing = false;
        
        if (_isNxtCrazy == true) {
            SwitchToCrazy();
            _isNxtCrazy = false;
        } else {
            DeSpawnDialogue();

            State = CreatureState.Idle;
        }
        //_skill = SkillType.None;
    }

    public void EndScreamAnimation()
    {
        _cuboid.SetActive(false);
        State = CreatureState.Idle;
    }

    public void PlayDiveEffect()
    {
        // Needed Empty GameObject
        GameObject eDive
            = Managers.Effect.PlayEffectByName("bDive", _diveEffectPos);

        _diveCollider.SetActive(true);

    }

    public void EndDiveEffect()
    {
        _diveCollider.SetActive(false);
    }
    public void ScreamToWalk()
    {
        //Debug.Log("Lee hunwoo gae mung chung hae");
        State = CreatureState.Idle;
    }

    public void SpawnThrowBall()
    {
        GameObject _ball = Managers.Resource.Instantiate("Object/Ball");
        _ball.transform.SetPositionAndRotation(_rightArm.position, Quaternion.identity);
    }

    public void SpawnCuboid()
    {
        _cuboid.SetActive(true);
        GameObject eScream
            = Managers.Effect.PlayEffectByName("bScream", _screamEffectPos);
        
    }
    public void DeSpawnCuboid()
    {
        _cuboid.SetActive(false);
    }


    public void SwitchToCrazy()
    {
        // Don't Turn into Crazy Mode if it is ALREADY
        if (_coeffCrazy != 1.0f || State == CreatureState.Stunned) return;

        if (State != CreatureState.Skill || _isNxtCrazy == true)
        {
            //StopAllCoroutines();
            SetAllCoroutinesToNull();
            State = CreatureState.Crazy;
            Camera.main.GetComponent<CameraController>().ZoomInBoss = true;
        } else {
            _isNxtCrazy = true;
        }
    }
    public void ActivateCrazy()
    {
        _smd.GetComponent<SkinnedMeshRenderer>().material = bossColor[1];
        Camera.main.GetComponent<CameraController>().ZoomInBoss = false;
        _coeffCrazy = 2.0f;
        _animator.speed = 2.0f;
        bulletSpawner.ActivateCrazy(); // bullet speed, radius *= _coeffCrazy 2.0f
        EndAnimation();
    }
    public void DeActivateCrazy()
    {
        _smd.GetComponent<SkinnedMeshRenderer>().material = bossColor[2];
        _coeffCrazy = 1.0f;
        _animator.speed = 1.0f;
        bulletSpawner.DeActivateCrazy(); // bullet speed, radius *= _coeffCrazy 1.0f
        EndAnimation();
    }

    public void SpawnCrazyDialogue1()
    {
        //_goDialogue = Managers.Resource.Instantiate("UI/WorldSpace/Boss/Crazy1", transform);
        UI_SpeechBubble ui = Managers.UI.MakeWorldSpaceUI<UI_SpeechBubble>(transform, "Boss/Crazy1");
        ui.SetOffset(0.0f, 4.8f, 0.0f);
        _goDialogue = ui.gameObject;
    }
    public void SpawnCrazyDialogue2()
    {
        //_goDialogue = Managers.Resource.Instantiate("UI/WorldSpace/Boss/Crazy2", transform);
        UI_SpeechBubble ui = Managers.UI.MakeWorldSpaceUI<UI_SpeechBubble>(transform, "Boss/Crazy2");
        ui.SetOffset(0.0f, 4.8f, 0.0f);
        _goDialogue = ui.gameObject;
    }
    public void DeSpawnDialogue()
    {
        if (_goDialogue != null)
        {
            Destroy(_goDialogue);
            _goDialogue = null;
        }
    }

    int _debuffValue = 0;
    public void OnDebuff(bool value)
    {
        if (value)
        {
            if (!_debuffEffect.isPlaying)
                _debuffEffect.Play();
            _debuffValue = Stat.Defense;
        }
        else
        {
            if (_debuffEffect.isPlaying)
                _debuffEffect.Stop();
            _debuffValue = 0;
        }
    }

    public override void OnDamaged(Stat attacker, HitType hitType)
    {
        if (hitType == HitType.Damage)
        {
            GameObject effect = Managers.Resource.Instantiate("Effect/Damage_Hit", transform);
            effect.transform.localPosition = Vector3.up * 3.0f;

            int damage = Mathf.Max(0, attacker.Attack - _stat.Defense + _debuffValue);
            _stat.Hp -= damage;
#if DEBUG
            Debug.Log($"damage {damage} Hp {_stat.Hp}");
#endif
            if (_stat.Hp <= 0)
            {
                _stat.Hp = 0;
                OnDead();
            }

        }
        else if (hitType == HitType.Groggy)
        {
            GameObject effect = Managers.Resource.Instantiate("Effect/Groggy_Hit", transform);
            effect.transform.localPosition = Vector3.up * 3.0f;

            _stat.Groggy += 2.0f;
            if (_stat.Groggy >= _stat.MaxGroggy)
            {
                _stat.Groggy = _stat.MaxGroggy;
                // Change to groggy state
                Camera.main.GetComponent<CameraController>().ZoomInBoss = false;
                StopAllCoroutines(); // Stop all Coroutines, ESPECIALLY Skills
                SetAllCoroutinesToNull(); // RESET Coroutine variables INTO NULL
                State = CreatureState.Stunned;

                GameObject eBStunned =
                    Managers.Effect.PlayEffectByName("bStunned", this.transform);
            }
        }
        else if (hitType == HitType.Debuff) 
        {
            // Not in use
        }
    }

    public override void OnDead()
    {
        _animator.speed = 1.0f;
        State = CreatureState.Dead;
    }

    public void EndDead()
    {
        Camera.main.GetComponent<CameraController>().ZoomInBoss = false;
        Managers.Game.GameClear();
    }

    public void SetAllCoroutinesToNull()
    {
        _coScream       = null;
        _coDive         = null;
        _coThrow        = null;
        _coSpin         = null;
        _coChase        = null;
        _coStunned      = null;
        _coMoveTo       = null;
        _coSpawnBullets = null;
    }

    
}
