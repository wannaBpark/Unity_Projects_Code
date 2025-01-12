using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using static Define;


public enum DefaultState
{
    Dash,
    Parry,
    Move,
    ComboAttack1,
    ComboAttack2,
    ComboAttack3,
    SpecialAttak,
    Idle,
    Hook,
    Air,
    Blown,
    Hurt,
    Null
}

public class PlayerDefaultState : PlayerStrategy
{
    private Rigidbody rb;
    private GameObject currentCollider;
    //���� ����
    public DefaultState state = DefaultState.Idle;
    Stat playerStat;

    #region move_variable
    [Header("Move")]
    public float moveSpeed = 5f; // �⺻ �̵� �ӵ�
    private bool isMoving = false;
    //�������� ���� ���ΰ�?
    private bool isMoveBlocked = false;
    private float moveBlockTimer;
    #endregion

    private float _powerKnockBack = 8.0f;

    #region dash_variable
    [Header("Dash")]
    public AnimationCurve velocityCurve;
    private bool isDashing = false;
    public float dashSpeed = 40;
    private float dashStartTime; // �뽬 ���� �ð�
    #endregion

    #region effect_variable
    [Header("Effect")]
    //����Ʈ ���� ����
    public Transform effectTrash;
    public Transform effectPos;
    public Transform specialEffectPos;
    public GameObject attack1Effect;
    public GameObject attack2Effect;
    public GameObject attack3Effect;
    public GameObject specialAttackEffect;
    public Transform blockEffectPos;
    public GameObject blockEffect;
    public GameObject parryEffect;
    public GameObject hurtEffect;
    #endregion

    #region special_gage_variable
    //����Ʈ���� Collider�� �޾Ƽ� OnTriggerEnterüũ�� �� ��쿡 ��Ʈ�� �����ߴٴ� ������ �÷��̾�� ����,���� ���� �޾��� ���
    //�÷��̾�� �� ��ų�� ������ ��ġ��ŭ ������ ����
    //������ ���� ����
    [Header("SpecialGage")]
    public float specialGageBarValue;
    public float gageAmount = 0;
    public bool isHit = false;
    public float gageMaintainTime = 1;
    private float gagetTimer = 0;
    #endregion

    #region attack_variable
    [Header("Attack")]
    // �Է� ������
    public float inputDelay = 0.5f;
    // ���� ������ �����ϴ� ������
    public float comboResetDelay = 1f;
    // ���� �Է� ������ Ÿ�̸�
    private float inputDelayTimer = 0f;
    // ���� ���� ���� Ÿ�̸�
    private float comboResetTimer = 0f;
    // ���� ���� ����
    public int attackStep = 0;
    private bool isAttacking = false;
    #endregion

    #region special_attack_variable
    [Header("SpecialAttack")]
    private float specialAttackInputDelayTimer = 0f;
    public float specialAttackInputDelay = 0.5f;
    private bool isSpecialAttacking = false;
    #endregion

    #region hurt_variable
    public bool isHurt = false;
    #endregion

    #region blocking_variable
    [Header("Blocking")]
    public float parryingSuccessTime = 0.2f;//�и� ���� ���� �ð�
    private float parryingTimer = 0;//
    private bool isBlocking = false;
    private bool isParryingSuccess = false;
    Quaternion parry_target_rotation;

    private bool isBlockDamaged = false;//만약 블락킹 상태에서 데미지를 입었을 경우
    public float blockDamagedTime = 0.5f; // 0.5초 동안 막기 상태가 유지 된다.
    private float blockDamagedTimer = 0;
    #endregion

    #region above_wall_variable
    [Header("Blown")]
    public bool isBeigngBlown = false;
    public Vector3 blownVector;
    public float timeOnWall = 0;
    private float timeOnWallTimer = 0;

    #endregion

    #region etc_variable
    [Header("ETC")]
    //�׽�Ʈ�� ����
    public GrapplingGun gg;//�׷��ø� ���� ����ϴ� �����̱� ������ ���
    public MeshTrailTut mt;//�뽬�Ҷ� Ʈ���� �����Ϸ���
    public Inventory ivt;
    public UIManagerV2 um;
    #endregion

    #region air_variable
    //���� ���߿� �ִ°�?
    private bool isAir = false;
    private bool isCollided = true;
    private float collisionCheckDelay = 0.1f; // üũ ������
    private float lastCollisionTime;
    #endregion

    #region interaction_variable
    public float interactionRange = 3f; // ��ȣ�ۿ� �Ÿ�
    public LayerMask interactableLayer; // ��ȣ�ۿ� ������ ���̾�
    #endregion

    public float hookMaxGage = 100.0f;
    public float hookGage = 100.0f;
    public float HookGageBarValue { get { return hookGage / hookMaxGage; } }

    // Start is called before the first frame update
    void Start()
    {
        lastCollisionTime = Time.time;
        specialAttackInputDelayTimer = specialAttackInputDelay;
        rb = GetComponent<Rigidbody>();
        playerStat = this.gameObject.GetComponent<Stat>();
    }


    // ��ǲ �����̳� ������ �ο��� �ؾ��ϴ� ������ ��� ó��
    public override void ExecuteUpdate()        
    {
        //�÷��̾ ���߿� �ִ� �Ǵ��ϴ� ����
        if (Time.time > lastCollisionTime + collisionCheckDelay && !isCollided && !isAir)
        {
            isAir = true;
        }

        BeingBlown();
        Blocking();
        SpecialAttack();
        ComboAttack();

        

        if (isDashing || isBlocking || isAttacking || isSpecialAttacking || isBeigngBlown || isHurt || um.currentUIManager!= UIManagerType.None)//�뽬���̰ų� �и������� ���� �ƹ��� �Է��� ���� ����
            return;

        
        //공격후 움직이면 안움직이는 문제 발생 : 인풋을 막는 부분이있을거임

        

        //�������� ���� ��ȯ
        if (Input.GetKeyDown(KeyCode.LeftShift) && currentCollider.gameObject.layer != LayerMask.NameToLayer("Block") && !isAir)
        {
            strategyState = StrategyState.HookMoveState;
        }

        /*
        if (Input.GetKeyDown("c"))//Hurt test logic (맞을 때, 막기상태X)
        {
            HandleBlockCollision(new Vector3(0, 0, 0)); // dummy data
        }

        if (Input.GetKeyDown("z")) //BlockDamaged test logic 패링실패 (막기상태)
        {
            HandleParryingCollision(new Vector3(0, 0, 0)); // dummy data
        }
        */
        //�ӽ� �׽�Ʈ�� ����(���ѱ�)*************

        if (currentCollider != null && currentCollider.gameObject.layer == LayerMask.NameToLayer("Block") && !isAir)
        {

        }
        else
        {
            gg.Execute();
            if (gg.IsGrappling())
            {
                isMoving = false;
                state = DefaultState.Hook;
                Vector3 targetPoint = gg.GetGrapplePoint() + new Vector3(0, 3, 0);
                Vector3 direction = targetPoint - transform.position;

                //�����Ÿ��� 1���� Ŭ�������� �ش� ������ �ٶ����
                if (Vector3.Distance(transform.position, targetPoint) > 1f)
                {
                    Vector3 playerDirection = direction;
                    playerDirection.y = 0;
                    transform.rotation = Quaternion.LookRotation(playerDirection, Vector3.up);
                }

                //�Ź������� �̵�
                rb.velocity = direction.normalized * 20f; // 이부분 rb.velocity 주의


                //0.3f�̳��� �Ÿ��� ���ð�� ���� ���߰� ����
                if (Vector3.Distance(transform.position, targetPoint) < 0.3f)
                {
                    gg.StopGrapple();
                    rb.velocity = Vector3.zero;
                    rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
                    //isMoveBlocked = true;
                    //moveBlockTimer = 0.5f;

                }

                return;
            }
            else if (isAir)
            {
                state = DefaultState.Air;
                isMoving = false;
                return;
            }
        }

        //�׷��ø� ���� �ƴϸ鼭 ���߿� ���� ���

        //*******************************


        if (Input.GetMouseButtonDown(1)) // �и� ����
        {
            
            rb.velocity = Vector3.zero;
            isBlocking = true;
            // ���̸� ���콺 ��ġ�� �߻�
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // ����ĳ��Ʈ�� ���� �浹 �˻�
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // �÷��̾ ��Ʈ ����Ʈ�� �ٶ󺸵��� ȸ����Ŵ
                Vector3 direction = hit.point - transform.position;
                direction.y = 0f; // ���� �������θ� ȸ��
                parry_target_rotation = Quaternion.LookRotation(direction);
                transform.rotation = parry_target_rotation;
            }
        }

        //�Ϲ� ����
        if (Input.GetMouseButtonDown(0))
        {
            rb.velocity = Vector3.zero;
            isAttacking = true;
        }

        //����� ����
        
        if (gageAmount == 100 && Input.GetKeyDown("c"))
        {
            specialAttackInputDelayTimer = specialAttackInputDelay;
            isSpecialAttacking = true;
        }

        //�뽬����
        Dash();

        if (isMoveBlocked)
        {
            MoveBlocking();
        }
        else
        {
            //������ ����
            if(Input.GetKey("w") || Input.GetKey("a") || Input.GetKey("s") || Input.GetKey("d"))
                isMoving = true;
        }

        // ������Ʈ ��ȣ�ۿ�
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F Key");
            Interact();
        }

    }

    void Interact()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * interactionRange, Color.red, 1.0f);
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, interactionRange, interactableLayer))
        {
            Debug.Log("Interact");
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.Interact(gameObject.GetComponent<PlayerControllerV1>());
            }
        }
    }

    //�̵� ���� ���� ó��(������ �����Ӹ��� ������ ó���ؾ��ϴ� ���)
    override public void ExecuteFixedUpdate()
    {
        #region test_logic
        /*
        //���� ���� ó��
        switch (state)
        {
            case DefaultState.Dash:
                break;

            case DefaultState.ComboAttack:
                break;

            case DefaultState.Parry:

                break;

            case DefaultState.Move:
                Move();
                break;

            case DefaultState.SpecialAttak:

                break;

            case DefaultState.Idle:
                break;

        }
        */
        #endregion

        Dashing();
        Move();

        //������ õõ�� �پ��
        //�� �Ѵ�� ��Ʈ������ �� 1�ʵ��� �پ���� ����
        if (isHit)
        {
            gagetTimer -= Time.deltaTime;
            if (gagetTimer <= 0)
            {
                isHit = false;
            }
        }
        else
        {
            gageAmount -= Time.deltaTime * 10;
            specialGageBarValue = gageAmount / 100f;
        }

    }


    #region move_related_logic
    private void Move()
    {
        if (isMoving)
        {
            ThrowAwayEffects();
            // WASD Ű �Է��� �����Ͽ� �������� ó��
            float horizontalInput, verticalInput;

            horizontalInput = 0;
            verticalInput = 0;

            if (Input.GetKey("w"))
            {
                verticalInput = 1;
            }
            else if (Input.GetKey("s"))
            {
                verticalInput = -1;
            }

            if (Input.GetKey("a"))
            {
                horizontalInput = -1;
            }
            else if (Input.GetKey("d"))
            {
                horizontalInput = 1;
            }
            Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized; // �̵� ���� ����

            // ������ ���͸� ī�޶��� �������� ��ȯ�Ͽ� ���� ������ ����
            moveDirection = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f) * moveDirection;
            moveDirection.y = 0;

            // Rigidbody�� �ӵ� ���� ����
            rb.velocity = moveDirection * moveSpeed + Vector3.up * rb.velocity.y;

            // ĳ���Ͱ� �̵��ϴ� �������� ȸ��
            if (moveDirection != Vector3.zero)
            {
                state = DefaultState.Move;
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }
            else
            {
                state = DefaultState.Idle;
                isMoving = false;
            }
        }
    }

    
    private void Dash()
    {
        // Shift Ű�� �� �� ������ �� �뽬
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //�뽬�� �� �� �ִ� �������� ���� üũ �ʿ�
            //�޸��� Ʈ���� ����
            mt.isRun = true;
            mt.meshRefreshRate = 0.03f;
            StartCoroutine(mt.ActivateRunTrail());

            ThrowAwayEffects();
            isDashing = true;
            dashStartTime = Time.time;
        }
    }

    void Dashing()
    {
        if (isDashing)
        {
            isMoving = false;
            state = DefaultState.Dash;
            // ���� ��� �ð� ���
            float elapsedTime = Time.time - dashStartTime;
            if (elapsedTime > 0.2f)
            {
                
                //�뽬 ��
                mt.meshRefreshRate = 0.05f;
                mt.isRun = false;
                isDashing = false;
                state = DefaultState.Idle;
            }

            // �ӵ� �׷����� ���� ���� �ð��� �ش��ϴ� �ӵ� ���� ������
            float speedFactor = velocityCurve.Evaluate(elapsedTime);
            // �ӵ� ���


            Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;


            // ������ ���͸� ī�޶��� �������� ��ȯ�Ͽ� ���� ������ ����
            moveDirection = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f) * moveDirection;

            if (moveDirection == Vector3.zero)
                moveDirection = transform.forward;

            Vector3 velocity = moveDirection * dashSpeed * speedFactor;

            // Rigidbody�� �ӵ� ����
            rb.velocity = velocity;

            // ĳ���Ͱ� �̵��ϴ� �������� ȸ��
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

        }
    }


    void MoveBlocking()
    {
        moveBlockTimer -= Time.deltaTime;
        if (moveBlockTimer <= 0f)
            isMoveBlocked = false;
    }

    #endregion

    void ThrowAwayEffects()
    {
        // ��� �ڽ� ������Ʈ ��������
        // Transform[] children = effectPos.GetComponentsInChildren<Transform>();
        // �� �ڽ� ������Ʈ�� �θ� �����Ͽ� ���ο� �θ� ������Ʈ�� �ڽ����� ����
        foreach (Transform child in effectPos.transform)
        {
            // �ڽ��� Transform�� �����ϰ� �θ� ����
            if (child != transform)
            {
                child.parent = effectTrash.transform;
            }
        }
    }

    Vector3 normalizedDirection;

    void BeingBlown()
    {
        if (isBeigngBlown)
        {
            state = DefaultState.Blown;
            float horizontalInput, verticalInput;

            horizontalInput = 0;
            verticalInput = 0;

            if (Input.GetKey("w"))
            {
                verticalInput = 1;
            }
            else if (Input.GetKey("s"))
            {
                verticalInput = -1;
            }

            if (Input.GetKey("a"))
            {
                horizontalInput = -1;
            }
            else if (Input.GetKey("d"))
            {
                horizontalInput = 1;
            }

            Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized; // �̵� ���� ����
            moveDirection = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f) * moveDirection;
            //���� �̵� ����� �ٶ󺸴� ������ �ޱ��� ����ؼ� ���� ����� �ޱ۷� ȸ��
            float angle = Vector3.SignedAngle(moveDirection, transform.forward, Vector3.up);
            if (moveDirection != Vector3.zero)
            {
                if (angle < 0.1f && angle > -0.1f)
                {

                }
                if (angle >= 0.1f)
                {
                    normalizedDirection = Quaternion.Euler(0, -20f * Time.deltaTime, 0) * normalizedDirection;
                }
                else if (angle <= -0.1f)
                {
                    normalizedDirection = Quaternion.Euler(0, 20f * Time.deltaTime, 0) * normalizedDirection;
                }
            }

            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
            transform.rotation = targetRotation;

            rb.velocity = transform.forward * 20 + Vector3.up * rb.velocity.y;
        }
        if(currentCollider != null && LayerMask.LayerToName(currentCollider.gameObject.layer) == "Block" && !isAir && !isBeigngBlown && transform.position.y >= 7)
        {
            timeOnWallTimer += Time.deltaTime;
            if(timeOnWall <= timeOnWallTimer)
            {
                if (!isDashing)
                {
                    normalizedDirection = blownVector;
                    isBeigngBlown = true;
                    isMoving = false;
                }
            }
        }
        if(transform.position.y<7)
        {
            isBeigngBlown = false;
            timeOnWallTimer = 0f;
        }
    }

    #region attack_related_logic

    Quaternion target_rotation_combo_attack;

    void ComboAttack()
    {

        // ���콺 ���� Ŭ�� �Է� �ޱ�
        if (isAttacking)
        {
            isMoving = false;
            // �Է� �����̰� �������� Ȯ��
            if (inputDelayTimer <= 0)
            {
                // ���̸� ���콺 ��ġ�� �߻�
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // ����ĳ��Ʈ�� ���� �浹 �˻�
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    // �÷��̾ ��Ʈ ����Ʈ�� �ٶ󺸵��� ȸ����Ŵ
                    Vector3 direction = hit.point - transform.position;
                    direction.y = 0f; // ���� �������θ� ȸ��
                    target_rotation_combo_attack = Quaternion.LookRotation(direction);
                    transform.rotation = target_rotation_combo_attack;

                    // ���� ����
                    ComboAttack_Attack();

                    // �Է� ������ Ÿ�̸� �ʱ�ȭ
                    inputDelayTimer = inputDelay;
                    // ���� ���� ���� Ÿ�̸� ����
                    comboResetTimer = comboResetDelay;
                }
            }
        }
        if (inputDelayTimer == 0)
        {
        }
        else
        {
            // �Է� ������ Ÿ�̸� ����
            inputDelayTimer -= Time.deltaTime;
            transform.rotation = target_rotation_combo_attack;
            if (inputDelayTimer < 0)
            {
                //���� ���� �ִϸ��̼��� ��������� isAttacking = false, state= idle
                //��ǲ �����̰� ������ ��� isAttacking�� False
                //state = DefaultState.Idle;
                isAttacking = false;
                inputDelayTimer = 0;
            }
        }



        if (comboResetTimer == 0)
        {
        }
        else
        {
            // ���� ���� ���� Ÿ�̸� ����
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer < 0)
            {
                comboResetTimer = 0;
            }

            // ���� ���� ����
            if (comboResetTimer <= 0)
            {
                // 1�� ���� ���� ���� �Է��� ������ 0�� �������� ���ư�
                attackStep = 0;
            }
        }
    }

    public void AttackAnimationEnd()
    {
        state = DefaultState.Idle;
        isAttacking = false;
    }

    void ComboAttack_Attack()
    {
        isMoveBlocked = true;
        //moveBlockTimer = 0.5f;
        
        switch (attackStep)
        {
            case 0:

                state = DefaultState.ComboAttack1;
                //����Ʈ ����
                GameObject a = Instantiate(attack3Effect, effectPos.position, effectPos.rotation);
                a.transform.parent = effectPos;
                a.transform.localScale *= 1.25f;
                a.transform.localScale = new Vector3(a.transform.localScale.x, a.transform.localScale.y, -a.transform.localScale.z);



                //�Է� ������
                inputDelay = 0.3f;

                //������ ���� �ð�
                gagetTimer = gageMaintainTime;

                //���ݽ� �󸶳� ������ ������
                rb.velocity = Vector3.zero;
                rb.AddForce(transform.forward * 4, ForceMode.Impulse);
                break;

            case 1:
                state = DefaultState.ComboAttack2;

                GameObject b = Instantiate(attack3Effect, effectPos.position, effectPos.rotation);
                b.transform.parent = effectPos;
                b.transform.localScale *= 1.25f;

                b.transform.localScale = new Vector3(b.transform.localScale.x, -b.transform.localScale.y, b.transform.localScale.z);


                inputDelay = 0.3f;
                gagetTimer = gageMaintainTime;
                rb.velocity = Vector3.zero;
                rb.AddForce(transform.forward * 4, ForceMode.Impulse);
                break;

            case 2:
                state = DefaultState.ComboAttack3;
                inputDelay = 0.5f;
                Invoke("SpawnAttack2Effect", .13f);

                /*
                GameObject c = Instantiate(attack3Effect, transform.position, effectPos.rotation);
                c.transform.parent = effectPos;
                
                inputDelay = 0.3f;
                gagetTimer = gageMaintainTime;
                rb.velocity = Vector3.zero;
                rb.AddForce(transform.forward * 4, ForceMode.Impulse);
                */
                break;
        }

        // ���� ���� �������� �̵�
        attackStep++;
        if (attackStep > 2)
        {
            attackStep = 0;
        }
    }

    void SpawnAttack2Effect()
    {
        // ����Ʈ ����
        GameObject c = Instantiate(attack2Effect, transform.position, effectPos.rotation);
        c.transform.parent = effectPos;
        c.transform.localScale *= 1.5f;
        
        gagetTimer = gageMaintainTime;
        rb.velocity = Vector3.zero;
        rb.AddForce(transform.forward * 4, ForceMode.Impulse);
    }


    void SpecialAttack()
    {

        if(isSpecialAttacking && specialAttackInputDelayTimer == specialAttackInputDelay)
        {
            isMoving = false;
            Debug.Log(specialAttackInputDelayTimer);


            state = DefaultState.SpecialAttak;

            /*
            //�������� 0.5�ʵ��� ����
            isMoveBlocked = true;
            moveBlockTimer = 0.5f;
            */

            GameObject a = Instantiate(specialAttackEffect, effectPos.position, Quaternion.identity);
            a.transform.transform.localScale *= 2;
            //Debug.Log("����� ����!");
            gageAmount = 0;
            specialGageBarValue = 0;
            rb.AddForce(-transform.forward * 10, ForceMode.Impulse);
            specialAttackInputDelayTimer = specialAttackInputDelay;
        }


        if (specialAttackInputDelayTimer == 0)
        {
        }
        else
        {
            // ����Ⱦ��� �ĵ�����
            specialAttackInputDelayTimer -= Time.deltaTime;
            if (specialAttackInputDelayTimer <= 0)
            {
                specialAttackInputDelayTimer = 0;
                isSpecialAttacking = false;
            }
        }
    }

    void Blocking()
    {
        if (isBlocking)
        {
            transform.rotation = parry_target_rotation;

            isMoving = false;
            Debug.Log("�и���");
            state = DefaultState.Parry;
            //�и� �������κ��� �ð� ����
            parryingTimer += Time.deltaTime;

            if (isBlockDamaged)
            {
                Debug.Log("Blocking");
                blockDamagedTimer += Time.deltaTime;
                if (blockDamagedTimer > blockDamagedTime && !Input.GetMouseButton(1))
                {
                    isParryingSuccess = false;
                    isBlocking = false;
                    parryingTimer = 0;
                    blockDamagedTimer = 0;
                    state = DefaultState.Idle;
                    isBlockDamaged = false;
                }
            }else if (Input.GetMouseButtonUp(1))
            {
                //���콺 ��Ŭ�� ��ư�� ���� ��� �и� ���� ����
                isParryingSuccess = false;
                isBlocking = false;
                parryingTimer = 0;
                state = DefaultState.Idle;
            }
            if (parryingTimer <= parryingSuccessTime)
            {
                Debug.Log("ParrySuccessTime");
                //�и� ���� �ð� �̳��� ������ ���� ��� ���� ����
                //�����ð� 1�� ����
                isParryingSuccess = true;
            }
            else
            {
                isParryingSuccess = false;
            }
        }
        else
        {
            isParryingSuccess = false;
        }
    }
    #endregion


    public void GageRecovery(float value)
    {

        gagetTimer = gageMaintainTime;
        isHit = true;
        if (gageAmount < 0)
            gageAmount = 0;

        gageAmount += value;
        if(gageAmount > 100)
            gageAmount= 100;
        specialGageBarValue = gageAmount / 100f;
    }


    #region collider_related_logic
    override public void HandleOnCollisionEnter(Collision collision)
    {
        if (state == DefaultState.Air)//���߿��� ���𰡿� ����� ��� Idle�� ��ȯ
        {
            isCollided = true;
            isAir = false;
            state = DefaultState.Idle;
        }

        /*
        if (collision.gameObject.layer == LayerMask.NameToLayer("Block"))
        {
            gg.StopGrapple();
            Debug.Log("Collision with water detected.");
        }
        */
    }

    override public void HandleDamageTrigger(Vector3 monsterPos, Stat attacker)
    {
        if (playerStat == null) {
            playerStat = this.gameObject.GetComponent<Stat>();
        }
        if (isDashing)//대쉬중일 경우 데미지를 입지 않는다.
            return;
        isMoving = false;
        //Debug.Log($"handledamage : {attacker.Attack}");
        if (isParryingSuccess)
        {
            //Debug.Log("패링 성공");
            HandleParryingCollision(monsterPos); 
            //�ݰݻ��� ON
        } else if (!isParryingSuccess && isBlocking) {
            //Debug.Log("막기 성공");
            HandleBlockCollision(monsterPos);
            playerStat.OnDamaged(attacker);
            //isBlockDamaged = true;
            //blockDamagedTimer = 0;//블락킹 지속시간 갱신
            //������ �ݰ�
            //���׹̳� ����
        } else {
            //Debug.Log("정통으로 맞음");
            HandleHurtCollision(monsterPos);
            playerStat.OnDamaged(attacker);
        }
    }

    //public override void HandleOnTriggerEnter(Collider other)
    //{ 
    //    Debug.Log($"collision : {other.gameObject.layer}");
    //}

    public override void HandleOnCollisionStay(Collision collision)
    {
        isCollided = true;
        currentCollider = collision.gameObject;
        isAir = false;
    }
    
    public override void HandleOnCollisionExit(Collision collision)
    {
        //isAir = true;
        isCollided = false;
        lastCollisionTime = Time.time;
    }
    

    #endregion

    public void HurtAnimationEnd()
    {
        Debug.Log("HurtAnimation End");
        isHurt = false;
        state = DefaultState.Idle;
    }


    public override void StartState()
    {
        //�� ���¸� ���ѱ�� ����
        gg.state = HookState.Exceed;
        state = DefaultState.Idle;
        strategyState = StrategyState.DefaultState;
    }

    public override void ExitState()
    {
        isAir = false;
        Debug.Log("ExitStateExecute");
        state = DefaultState.Null;
        strategyState = StrategyState.DefaultState;
    }

    private void HandleBlockCollision(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;
        this.transform.rotation = Quaternion.LookRotation(dir);
        parry_target_rotation = transform.rotation;
        isBlocking = true;
        blockDamagedTimer = 0;
        Instantiate(blockEffect, blockEffectPos.position, Quaternion.identity); // parrying
        //Debug.Log("Parrying failed execute");
        isBlockDamaged = true;
        rb.velocity = Vector3.zero;
        //Debug.Log($"velocity : {rb.velocity} power : {-transform.forward * 8}");
        //rb.AddRelativeForce(-transform.forward * _powerKnockBack, ForceMode.Impulse);
        rb.velocity += -transform.forward * _powerKnockBack;
        return;

    }

    private void HandleHurtCollision(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;
        this.transform.rotation = Quaternion.LookRotation(dir);
        isHurt = true;
        state = DefaultState.Hurt;
        GetComponent<Animator>().Play("Hurt", -1, 0f);
        Instantiate(hurtEffect, blockEffectPos.position, Quaternion.identity); // parrying
        //Debug.Log("Hurt execute");
        rb.velocity = Vector3.zero;
        //Debug.Log($"velocity : {rb.velocity} power : {-transform.forward * 8}");
        rb.velocity += -transform.forward * _powerKnockBack;
        return;

    }

    private void HandleParryingCollision(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;
        this.transform.rotation = Quaternion.LookRotation(dir);
        parry_target_rotation = transform.rotation;
        Instantiate(parryEffect, blockEffectPos.position, Quaternion.identity); // parrying 
        //Debug.Log("Parrying failed execute");
        rb.velocity = Vector3.zero;

        //Debug.Log($"velocity : {rb.velocity} power : {-transform.forward * 8}");
        rb.velocity += -transform.forward * _powerKnockBack;

        GageRecovery(50f);

        return;
        
    }
}
