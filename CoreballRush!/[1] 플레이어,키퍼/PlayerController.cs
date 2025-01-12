using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityStandardAssets.Utility;
using Photon.Pun.UtilityScripts;

public enum PlayerState
{
    Shooting,
    Throwing,
    Bomb,
    Skill,
    Stun
};

public enum PlayerCharacter
{
    //캐릭터선택
    Alpha,
    Tiagu,
    Kei,
    Daisy
}

public class PlayerController : MonoBehaviourPunCallbacks
{
    public static PlayerController instance;

    private Vector3 moveVector;
    private Vector3 attackVector;
    private Vector3 rotVector;
    private Vector3 dirBall = Vector3.zero;
    private Vector3 bombLineCenter = Vector3.zero;
    private Vector3 bombLineArc = Vector3.zero;
    private Vector3 DaisyShotDir = Vector3.zero;


    //플레이어 변수
    private bool isDie = false;
    public float respawnTime = 3.0f;
    private bool isReloaded = true;
    private bool canUseSkill = false;
    private bool UsingSkill = false;
    private bool FillingSkill = false;
    private int coreBalltype = -1;
    private bool tiagusFireDamaged = false;
    public bool isDaisyGhost = false;
    private bool hpBuff = false;
    public bool canUseBomb = true;
    public string team = null;
    public int playerActorNum;
    public int playerCount = 0;
    public int myRespawnIdx;

    //캐릭터 변수
    private const float initMoveSpeed = 250f;
    private float moveSpeed = 100.0f;
    private float reloadSpeed = 0.3f;
    private float skillLoadSpeed = 10f;
    public float currentHP = 100.0f;
    private float initHP = 100.0f;
    private float atkDamge = 10f;
    private float atkRange = 3f;
    private float bombTime = 3f;

    public GameManager gm;
    public GameObject bullet;
    public GameObject p2coreball;
    public GameObject p3coreball;
    public GameObject bombcoreball;
    public GameObject bomb;
    public GameObject bombScale;
    public GameObject tiaguFire;
    public GameObject weaponPos;
    public GameObject weaponCenter;
    public GameObject coreCenter;
    public GameObject EffectHeal;
    public Button btn_Bomb;
    public Button btn_Skill;
    public Image skilldeActiveImage;
    public Image skillGageImage;
    public Image bombdeActiveImage;
    public Text bombTimeText;
    public EventTrigger bombTrigger;
    public Text nickName;
    public Image hpBar;
    public Image underCharacterColor;
    public LineRenderer bombLine;
    public GameObject coreLine;
    public GameObject holding2pointCoreBall;
    public GameObject holding3pointCoreBall;
    public GameObject holdingbombCoreBall;
    float bombCoreballTime=0f;

    public GameObject effectFire;
    public GameObject effectSpawn;
    public GameObject effectDie;
    public GameObject effectBeShot;
    public GameObject effectSkillUsing;
    

    private movJoystick mjs;
    private atkJoystick ajs;
    private PhotonTransformView ptv;
    public CharacterController controller;
    public Animator anim;

    PlayerState playerState = PlayerState.Shooting;
    public PlayerCharacter playerCharacter;
    public Transform[] points;
    public Text hpText;
    public GameObject hitDamageText;
    public GameObject healHpText;
    public Transform hitTextPos;

    public GameObject[] players;
    private Color myTeamColor = new Color(43 / 255f, 215 / 255f, 244 / 255f);
    private Color enemyColor = new Color(212 / 255f, 3 / 255f, 3 / 255f);
    private Color myOwnColor = new Color(141 / 255f, 243 / 255f, 57 / 255f);
    private void Awake()
    {
        PlayerController.instance = this;
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        //if (photonView.Owner.Equals(PhotonNetwork.LocalPlayer) && PhotonNetwork.PlayerList.Length == 4)
        //{
        //    //photonView.RPC("setMyTeam", RpcTarget.All, (playerCount));
        //    players = GameObject.FindGameObjectsWithTag("PLAYER");
        //    foreach (GameObject other in players)
        //    {
        //        bool IsEnemy = (other.GetPhotonView().Owner.GetTeam() == photonView.Owner.GetTeam()) ? false : true;
        //        string other_team = other.GetComponent<PlayerController>().getMyTeam();
        //        if (/*!this.team.Equals(other_team)*/IsEnemy)
        //        { // 적팀일 때
        //            other.GetComponent<PlayerController>().setMyColor(enemyColor);
        //        }
        //        else
        //            other.GetComponent<PlayerController>().setMyColor(myTeamColor);
        //    }
        //    hpBar.color = new Color(141 / 255f, 243 / 255f, 57 / 255f);
        //    underCharacterColor.color = new Color(141 / 255f, 243 / 255f, 57 / 255f);
        //}
    }
    void Start()
    {
        if (photonView.IsMine)
        {
            points = GameObject.Find("SpawnPoint").GetComponentsInChildren<Transform>();
            GameObject.Find("Main Camera").GetComponent<CameraMove>().target = transform;
            controller = GetComponent<CharacterController>();
            anim = GetComponent<Animator>();
            //조이스틱 컴포넌트를 가져오기 위해 Find로 Canvas를 찾고 그 안에 오브젝트 스크립트를 가져옴
            mjs = GameObject.Find("Canvas").transform.GetChild(0).GetComponent<movJoystick>();
            ajs = GameObject.Find("Canvas").transform.GetChild(1).GetComponent<atkJoystick>();
            myRespawnIdx = DataController.instance.gameData.respawnidx;
        }
        //조작
        moveVector = new Vector3(0, 0, 0);
        attackVector = new Vector3(0, 0, 0);
        rotVector = new Vector3(0, 0, 0);

        btn_Skill = GameObject.Find("Canvas").transform.GetChild(3).transform.GetChild(2).GetComponent<Button>();
        skillGageImage = GameObject.Find("Canvas").transform.GetChild(3).transform.GetChild(1).GetComponent<Image>();
        skilldeActiveImage = GameObject.Find("Canvas").transform.GetChild(3).transform.GetChild(3).GetComponent<Image>();


        gm = GameObject.Find("GameMgr").GetComponent<GameManager>();

        btn_Bomb = GameObject.Find("Canvas").transform.GetChild(4).transform.GetChild(0).GetComponent<Button>();
        btn_Bomb.gameObject.AddComponent<EventTrigger>();
        bombTimeText = GameObject.Find("Canvas").transform.GetChild(4).transform.GetChild(2).GetComponent<Text>();
        bombdeActiveImage = GameObject.Find("Canvas").transform.GetChild(4).transform.GetChild(1).GetComponent<Image>();
        bombTrigger = btn_Bomb.GetComponent<EventTrigger>();
        EventTrigger.Entry bombEntry = new EventTrigger.Entry();
        bombEntry.eventID = EventTriggerType.PointerDown;
        bombEntry.callback.AddListener((eventData) => { OnBtnBombClicked((PointerEventData)eventData); });
        bombTrigger.triggers.Add(bombEntry);

        btn_Skill.gameObject.AddComponent<EventTrigger>();
        bombTrigger = btn_Skill.GetComponent<EventTrigger>();
        EventTrigger.Entry skillEntry = new EventTrigger.Entry();
        skillEntry.eventID = EventTriggerType.PointerDown;
        skillEntry.callback.AddListener((eventData) => { OnBtnSkillClicked((PointerEventData)eventData); });
        bombTrigger.triggers.Add(skillEntry);

        //Inspector에서 PhotonTransformView_Position이 체크가 안됨. 이유를 몰라서 아래처럼 구현
        ptv = GetComponent<PhotonTransformView>();
        ptv.m_SynchronizePosition = true;

        bombScale.SetActive(false);
        bombLine.gameObject.SetActive(false);

        //아군, 적군에 따라 hpBar 색깔 반영



        //Main Camera의 Camera Move 스크립트 타켓지정
        //team = (photonView.Owner.ActorNumber % 2 == 0) ? "RED": "BLUE" ;

        //캐럭터 기본 값 설정
        InitCharacterInfo(true);

        //this.team = "BLUE";
        //PhotonNetwork.LocalPlayer.SetTeam(PunTeams.Team.blue);
        
        this.team = (photonView.Owner.GetTeam() == PunTeams.Team.blue) ? "BLUE" : "RED";
        playerCount = PhotonNetwork.PlayerList.Length;


        nickName.text = photonView.Owner.NickName;
        //if (photonView.Owner.Equals(PhotonNetwork.LocalPlayer))
        //{
        //    //photonView.RPC("setMyTeam", RpcTarget.All, (playerCount));
        //    foreach (GameObject other in players)
        //    {
        //        bool IsEnemy = (other.GetPhotonView().Owner.GetTeam() == photonView.Owner.GetTeam()) ? false : true;
        //        Debug.Log(players.Length);
        //        Debug.Log(other.GetPhotonView().Owner.GetTeam() + " " +photonView.Owner.GetTeam());
        //        string other_team = other.GetComponent<PlayerController>().getMyTeam();
        //        if (/*!this.team.Equals(other_team)*/IsEnemy)
        //        { // 적팀일 때
        //            other.GetComponent<PlayerController>().setMyColor(enemyColor);
        //        }
        //        else
        //            other.GetComponent<PlayerController>().setMyColor(myTeamColor);
        //    }
        //    hpBar.color = new Color(141 / 255f, 243 / 255f, 57 / 255f);
        //    underCharacterColor.color = new Color(141 / 255f, 243 / 255f, 57 / 255f);
        //}

        //else if (!photonView.IsMine)
        //{
        //    if (photonView.Owner.ActorNumber % 2 == PhotonNetwork.LocalPlayer.ActorNumber % 2)
        //    {
        //        hpBar.color = new Color(43 / 255f, 215 / 255f, 244 / 255f);
        //    }
        //    else
        //        hpBar.color = new Color(212 / 255f, 3 / 255f, 3 / 255f);
        //}
        //hpBar.color = (this.team.Equals("RED")) ?
        //         new Color(212/255f,3/255f, 3/255f) : 
        //         new Color(43/255f, 215/255f, 244/255f);

        GameObject eff = Instantiate(effectSpawn, transform);
        Destroy(eff, 4f);
    }

    void Update()
    {
        if (photonView.IsMine && !isDie)
        {
            HandleInput();//조이스틱 입력 설정
            PlayerMove();//캐릭터 이동
            checkHPState();//HP바 조절
            TESTINPUT();
            
            if (!UsingSkill && !FillingSkill && !canUseSkill/* && !(skillGageImage.fillAmount ==1)*/)
            {
                StartCoroutine(SkillFill());
            }
        }
    }

    float AlphaStatus(string type)
    {
        switch (type)
        {
            case "hp":
                return 250f;
            case "atkDamage":
                return 20f;
            case "reloadSpeed":
                return 0.4f;
            case "atkRange":
                return 5.0f;
        }
        return 0;
    }
    float TiaguStatus(string type)
    {
        switch (type)
        {
            case "hp":
                return 300f;
            case "atkDamage":
                return 35f;
            case "reloadSpeed":
                return 0.5f;
            case "atkRange":
                return 3.0f;
        }
        return 0;
    }
    float KeiStatus(string type)
    {
        switch (type)
        {
            case "hp":
                return 200f;
            case "atkDamage":
                return 10f;
            case "reloadSpeed":
                return 0.25f;
            case "atkRange":
                return 5.0f;
        }
        return 0;
    }
    float DaisyStatus(string type)
    {
        switch (type)
        {
            case "hp":
                return 250f;
            case "atkDamage":
                return 50f;
            case "reloadSpeed":
                return 0.6f;
            case "atkRange":
                return 7.0f;
        }
        return 0;
    }

    [PunRPC]
    private void InitCharacterInfo(bool isFirst)
    {
        moveSpeed = initMoveSpeed;
        isDie = false;
        switch (playerCharacter)
        {
            case PlayerCharacter.Alpha:
                initHP = AlphaStatus("hp");
                if (isFirst)
                    currentHP = AlphaStatus("hp");
                atkDamge = AlphaStatus("atkDamage");
                reloadSpeed = AlphaStatus("reloadSpeed");
                atkRange = AlphaStatus("atkRange");
                break;
            case PlayerCharacter.Tiagu:
                initHP = TiaguStatus("hp");
                if (isFirst)
                    currentHP = TiaguStatus("hp");
                atkDamge = TiaguStatus("atkDamage");
                reloadSpeed = TiaguStatus("reloadSpeed");
                atkRange = TiaguStatus("atkRange");
                break;
            case PlayerCharacter.Kei:
                initHP = KeiStatus("hp");
                if (isFirst)
                    currentHP = KeiStatus("hp");
                atkDamge = KeiStatus("atkDamage");
                reloadSpeed = KeiStatus("reloadSpeed");
                atkRange = KeiStatus("atkRange");
                break;
            case PlayerCharacter.Daisy:
                initHP = DaisyStatus("hp");
                if (isFirst)
                    currentHP = DaisyStatus("hp");
                atkDamge = DaisyStatus("atkDamage");
                reloadSpeed = DaisyStatus("reloadSpeed");
                atkRange = DaisyStatus("atkRange");
                break;
        }
    }

    void TESTINPUT()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("character : " + playerCharacter);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            playerCharacter = PlayerCharacter.Alpha;
            InitCharacterInfo(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            playerCharacter = PlayerCharacter.Tiagu;
            InitCharacterInfo(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            playerCharacter = PlayerCharacter.Kei;
            InitCharacterInfo(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            playerCharacter = PlayerCharacter.Daisy;
            InitCharacterInfo(false);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (playerState != PlayerState.Bomb)
                playerState = PlayerState.Bomb;
            else
                playerState = PlayerState.Shooting;

        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (playerCharacter == PlayerCharacter.Daisy)
            {
                SkillDaisy();
            }
            else if (playerState != PlayerState.Skill)
                playerState = PlayerState.Skill;
            else
            {
                InitCharacterInfo(false);
                playerState = PlayerState.Shooting;
            }
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(Burning());
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(Stuned());
        }
    }

    public void PlayerMove()
    {
        if (!controller.isGrounded)
        {
            moveVector += Physics.gravity;
        }
        controller.Move(moveVector * moveSpeed * Time.deltaTime); //addforce 대신 플레이어컨트롤러 move 함수로.
    }

    public void HandleInput()
    {
        if (playerState == PlayerState.Stun)
            return;

        moveVector = MovJoyInput();
        attackVector = AtkJoyInput();

        if (isDaisyGhost)
        {
            if (attackVector != Vector3.zero)
            {
                DaisyShotDir = attackVector;
            }
            else if (attackVector == Vector3.zero && DaisyShotDir != Vector3.zero)
            {
                DaisyShot();
            }
        }
        else if (playerState == PlayerState.Shooting)
        {
            if (attackVector != Vector3.zero && isReloaded == true)
            {
                StartCoroutine(Shooting());
                Vector3 dirBullet = new Vector3(transform.eulerAngles.x, (Mathf.Atan2(attackVector.x, attackVector.y)) * Mathf.Rad2Deg, transform.eulerAngles.z);
                photonView.RPC("Fire", RpcTarget.Others, photonView.OwnerActorNr, dirBullet);
                Fire(photonView.OwnerActorNr, dirBullet);
                //GameObject obj = Instantiate(bullet, weaponPos.position, Quaternion.Euler(dirBullet));
                //obj.GetComponent<bullet>().actorNumber = photonView.OwnerActorNr;
                //obj.GetComponent<bullet>().damage = atkDamge;
                //obj.GetComponent<bullet>().setMyTeam(team);
                //Destroy(obj, atkRange);

            }
        }
        else if (playerState == PlayerState.Throwing) //코어볼 가진 상태
        {
            if (attackVector != Vector3.zero)
            {
                dirBall = ThrowingDir();
                coreLine.gameObject.SetActive(true);
                coreCenter.transform.rotation = Quaternion.Euler(dirBall);
            }
            else if (attackVector == Vector3.zero && dirBall != Vector3.zero)
            {
                coreLine.gameObject.SetActive(false);
                photonView.RPC("Throwing", RpcTarget.Others, photonView.OwnerActorNr, dirBall, coreBalltype);
                Throwing(photonView.OwnerActorNr, dirBall, coreBalltype);
            }
        }
        else if (playerState == PlayerState.Bomb && canUseBomb)
        {
            if (attackVector != Vector3.zero)
            {
                //범위 표시
                bombScale.SetActive(true);
                bombLine.gameObject.SetActive(true);
                Vector3 targetVector = new Vector3(transform.position.x + attackVector.x * 500, 0, transform.position.z + attackVector.y * 500);
                bombScale.transform.position = targetVector;

                //궤적 표시
                bombLineCenter = (weaponPos.transform.position + targetVector) * 0.5f;
                bombLineCenter -= new Vector3(0, 1f, 0);


                Vector3 RelCenter = weaponPos.transform.position - bombLineCenter;
                Vector3 aimRelCenter = targetVector - bombLineCenter;
                for (float i = 0.0f, interval = -0.0417f; interval < 1.0f;)
                {
                    bombLineArc = Vector3.Slerp(RelCenter, aimRelCenter, interval += 0.0417f);
                    bombLine.SetPosition((int)i++, bombLineArc + bombLineCenter);
                }

            }
            else if (attackVector == Vector3.zero && bombScale.active)
            {
                canUseBomb = false;
                StartCoroutine(BombFill(btn_Bomb, bombTime, 1));
                playerState = PlayerState.Shooting;
                float x = bombScale.transform.position.x;
                float y = bombScale.transform.position.y;
                float z = bombScale.transform.position.z;

                shootingBomb(x, y,z);
                photonView.RPC("shootingBomb", RpcTarget.Others, x,y,z);

                bombScale.transform.position = new Vector3(transform.position.x, transform.position.y - 0.3f, transform.position.z);
                bombScale.SetActive(false);
                bombLine.gameObject.SetActive(false);
            }

        }
        else if (playerState == PlayerState.Skill)
        {
            if (attackVector != Vector3.zero)
            {
                switch (playerCharacter)
                {
                    
                    case PlayerCharacter.Alpha:
                        SkillAlpha();
                        break;
                    case PlayerCharacter.Tiagu:
                        photonView.RPC("SkillTiagu", RpcTarget.All);
                        break;
                    case PlayerCharacter.Kei:
                        SkillKei();
                        break;
                    case PlayerCharacter.Daisy:
                        SkillDaisy();
                        break;
                }
            }
            else if (attackVector == Vector3.zero)
            {
                switch (playerCharacter)
                {
                    case PlayerCharacter.Tiagu:
                        tiaguFire.SetActive(false);
                        break;
                }
            }
        }
        if (attackVector != Vector3.zero && !anim.GetBool("running")) // 움직이지 않고 공격할 때는 공격 방향 = rotVector
        {
            rotVector = new Vector3(attackVector.x, 0, attackVector.y);
        }
        else //이외의 경우 이동방향 = rotVector
        {
            if (moveVector != Vector3.zero)
                rotVector = moveVector;
        }

        if (attackVector != Vector3.zero && moveVector != Vector3.zero)
        {
            float Vel_MoveAtk_angle = GetAngle(new Vector3(moveVector.x, moveVector.z, 0), new Vector3(attackVector.x, attackVector.y, 0));

            anim.SetFloat("Vel_MovAtk_angle", Vel_MoveAtk_angle);
        }

        //이동 애니메이션
        anim.SetFloat("VelX", moveVector.x);
        anim.SetFloat("VelY", moveVector.z);

        //공격 애니메이션
        anim.SetFloat("Vel_Atk_X", attackVector.x);
        anim.SetFloat("Vel_Atk_Y", attackVector.y);
        //회전 반영
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, (Mathf.Atan2(rotVector.x, rotVector.z)) * Mathf.Rad2Deg, transform.eulerAngles.z);
    }

    float GetAngle(Vector3 movVec, Vector3 atkVec)
    {
        float angle = Mathf.Acos(Vector3.Dot(movVec, atkVec) / Vector3.Magnitude(movVec) / Vector3.Magnitude(atkVec)) * Mathf.Rad2Deg;
        float dir = Mathf.Asin((movVec.x * atkVec.y - movVec.y * atkVec.x) / Vector3.Magnitude(movVec) / Vector3.Magnitude(atkVec));
        if (dir > 0)
            angle *= -1f;

        if (angle <= 90 && angle >= -90)
            anim.SetBool("MovAtk_front", true);
        else
            anim.SetBool("MovAtk_front", false);

        return angle;
    }

    public Vector3 MovJoyInput()
    {
        Vector3 direction = Vector3.zero;
        direction.x = mjs.GetHorizontalValue();
        direction.z = mjs.GetVerticalValue();

        //테스트용
        if (Input.GetKey(KeyCode.UpArrow))
        {
            direction.z = 1;
            anim.SetBool("running", true);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            anim.SetBool("running", true);
            direction.z = -1;

        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            anim.SetBool("running", true);
            direction.x = -1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            anim.SetBool("running", true);
            direction.x = 1;
        }
        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
            anim.SetBool("running", false);


        if (direction.magnitude > 1)
            direction.Normalize();
        return direction;
    }

    public Vector3 AtkJoyInput()
    {
        Vector3 direction = Vector3.zero;
        direction.x = ajs.GetHorizontalValue();
        direction.y = ajs.GetVerticalValue();
        // weaponPos.transform.position = new Vector3(transform.position.x+direction.x*5f, 0, transform.position.z+direction.y*5f);

        if (direction.magnitude > 1)
            direction.Normalize();


        if (ajs.setAnimationAtk != null && playerState != PlayerState.Throwing && playerState != PlayerState.Bomb)
            anim.SetBool("attaking", ajs.setAnimationAtk);

        return direction;
    }

    
    private void checkHPState()
    {
        //hpBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0.8f, 0));
        hpBar.fillAmount = currentHP / initHP;
        hpText.text = ((int)(currentHP)).ToString();
        //hp버프
        if(coreBalltype == 2)
        {
            bombCoreballTime += Time.deltaTime;
            if (bombCoreballTime >= 5f)
            {
                bombCoreballTime = 0f;
                holdingbombCoreBall.GetComponent<BombCoreBall_Holding>().ExplosionDamageToPlayer();
                SoundManager.instance.Play("Thr_Explosion");
                photonView.RPC("ResetCoreBallState", RpcTarget.All);
                coreLine.gameObject.SetActive(false);
                playerState = PlayerState.Shooting;
                coreBalltype = -1;
                this.dirBall = Vector3.zero;
                gm.CoreBallSetFalse();
            }
        }

        if (currentHP / initHP <= 0.3)
        {
            hpBuff = true;
            switch (playerCharacter)
            {
                case PlayerCharacter.Alpha:
                    atkDamge = AlphaStatus("atkDamage") * 1.1f;
                    break;
                case PlayerCharacter.Kei:
                    moveSpeed = initMoveSpeed * 1.2f;
                    break;
                case PlayerCharacter.Daisy:
                    reloadSpeed = 0.5f;
                    break;
            }
        }
        else
        {
            hpBuff = false;
            switch (playerCharacter)
            {
                case PlayerCharacter.Alpha:
                    atkDamge = AlphaStatus("atkDamage");
                    break;
                case PlayerCharacter.Kei:
                    moveSpeed = initMoveSpeed;
                    break;
                case PlayerCharacter.Daisy:
                    reloadSpeed = 0.8f;
                    break;
            }
        }
    }
    //
    [PunRPC]
    void shootingBomb(float x, float y, float z) {
        Vector3 targetPos = new Vector3(x, y, z);
        GameObject o_bomb = null;
        //switch (playerCharacter)
        //{
        //    case PlayerCharacter.Alpha:
        //        o_bomb = Instantiate(bomb, weaponPos.transform.position, weaponPos.transform.rotation);
        //        break;
        //    case PlayerCharacter.Tiagu:
        //        o_bomb = PhotonNetwork.Instantiate("boom_Tiago", weaponPos.transform.position, weaponPos.transform.rotation);
        //        break;
        //    case PlayerCharacter.Kei:
        //        o_bomb = PhotonNetwork.Instantiate("boom_kei", weaponPos.transform.position, weaponPos.transform.rotation);
        //        break;
        //    case PlayerCharacter.Daisy:
        //        o_bomb = PhotonNetwork.Instantiate("boom_Daisy", weaponPos.transform.position, weaponPos.transform.rotation);
        //        break;
        //}
        o_bomb = Instantiate(bomb, weaponPos.transform.position, weaponPos.transform.rotation);
        o_bomb.GetComponent<bomb>().setTargetVector(targetPos);
        o_bomb.GetComponent<bomb>().setBombType((int)playerCharacter);
        o_bomb.GetComponent<bomb>().setMyTeam(team);
        //o_bomb.GetComponent<bomb>().actorNumber = photonView.OwnerActorNr;
    }

    IEnumerator Shooting()
    {
        isReloaded = false;
        yield return new WaitForSeconds(reloadSpeed);
        isReloaded = true;
    }
    IEnumerator Burning()
    {
        yield return new WaitForSeconds(1.0f);
        currentHP -= 5f;
        yield return new WaitForSeconds(1.0f);
        currentHP -= 5f;
        yield return new WaitForSeconds(1.0f);
        currentHP -= 5f;
        yield return new WaitForSeconds(1.0f);
        currentHP -= 5f;
    }
    IEnumerator TiagusBurning()
    {
        tiagusFireDamaged = true;
        yield return new WaitForSeconds(0.25f);
        tiagusFireDamaged = false;

    }

    Vector3 ThrowingDir()
    {

        Vector3 dirBall = new Vector3(transform.eulerAngles.x, (Mathf.Atan2(attackVector.x, attackVector.y)) * Mathf.Rad2Deg, transform.eulerAngles.z);
        return dirBall;
    }


    [PunRPC]
    void Throwing(int number, Vector3 dirBall, int _coreBalltype)
    {
        GameObject obj;
        if (coreBalltype == 0)
            obj = Instantiate(p2coreball, weaponPos.transform.position, Quaternion.Euler(dirBall));
        else if (coreBalltype == 1)
            obj = Instantiate(p3coreball, weaponPos.transform.position, Quaternion.Euler(dirBall));
        else
            obj = Instantiate(bombcoreball, weaponPos.transform.position, Quaternion.Euler(dirBall));

        obj.GetComponent<coreBall>().coreBallType = _coreBalltype;
        //obj.GetComponent<coreBall>().actorNumber = number;
        obj.GetComponent<coreBall>().isPoss = true;
        obj.GetComponent<coreBall>().setMyTeam(team);

        photonView.RPC("ResetCoreBallState", RpcTarget.All);
        //nickName.text = "Throw";
        playerState = PlayerState.Shooting;
        coreBalltype = -1;
        this.dirBall = Vector3.zero;
    }

    void SkillAlpha()
    {
        atkDamge = 25f;
        atkRange = 6f;
        reloadSpeed = 0.3f;

        if (isReloaded)
        {
            canUseSkill = false;
            StartCoroutine(Shooting());
            if (!UsingSkill) {
                SoundManager.instance.Play("SpcAtkActive");
                StartCoroutine(SkillUse());
            }
            Vector3[] dirBullets = new Vector3[3];
            Vector3 dirBullet = new Vector3(transform.eulerAngles.x, (Mathf.Atan2(attackVector.x, attackVector.y)) * Mathf.Rad2Deg, transform.eulerAngles.z);
            dirBullets[0] = dirBullet;
            for (int i = 1; i < 3; i++)
            {
                dirBullets[i] = dirBullets[i - 1];
                dirBullets[i].y -= 15;
            }
            for (int i = 0; i < 3; i++)
            {
                photonView.RPC("Fire", RpcTarget.Others, photonView.OwnerActorNr, dirBullets[i]);
                Fire(photonView.OwnerActorNr, dirBullets[i]);
            }

        }
    }

    [PunRPC]
    public void SkillTiagu()
    {
        tiaguFire.SetActive(true);
        if (!UsingSkill)
            StartCoroutine(SkillUse());
    }

    void SkillKei()
    {
        Debug.Log("skillkei");
        atkDamge = 20f;
        atkRange = 6f;
        reloadSpeed = 0.25f;
        moveSpeed = initMoveSpeed * 1.2f;
        if (isReloaded)
        {
            canUseSkill = false;
            StartCoroutine(Shooting());
            if (!UsingSkill)
                StartCoroutine(SkillUse());
            Vector3 dirBullet = new Vector3(transform.eulerAngles.x, (Mathf.Atan2(attackVector.x, attackVector.y)) * Mathf.Rad2Deg, transform.eulerAngles.z);
            Vector3[] dirBullets = new Vector3[4];
            dirBullets[0] = dirBullet;
            for (int i = 1; i < 4; i++)
            {
                dirBullets[i] = dirBullets[i - 1];
                dirBullets[i].y += 90;
            }
            for (int i = 0; i < 4; i++)
            {
                photonView.RPC("Fire", RpcTarget.Others, photonView.OwnerActorNr, dirBullets[i]);
                Fire(photonView.OwnerActorNr, dirBullets[i]);
            }
        }

    }

    void SkillDaisy()
    {
        atkDamge = 150f;
        atkRange = 10f;
        isDaisyGhost = true;
        StartCoroutine(DaisyTime());
        if (!UsingSkill)
            StartCoroutine(SkillUse());
    }

    void DaisyShot()
    {
        Vector3 dirBullet = new Vector3(transform.eulerAngles.x, (Mathf.Atan2(DaisyShotDir.x, DaisyShotDir.y)) * Mathf.Rad2Deg, transform.eulerAngles.z);
        GameObject obj = Instantiate(bullet, weaponPos.transform.position, Quaternion.Euler(dirBullet));
        //obj.GetComponent<bullet>().actorNumber = photonView.OwnerActorNr;
        obj.GetComponent<bullet>().damage = atkDamge;
        obj.GetComponent<bullet>().setMyTeam(team);
        Destroy(obj, atkRange);
        isDaisyGhost = false;
        DaisyShotDir = Vector3.zero;
        InitCharacterInfo(false);
    }
    IEnumerator DaisyTime()
    {
        yield return new WaitForSeconds(3.0f);
        isDaisyGhost = false;
        InitCharacterInfo(false);
    }

    public void GetStun()
    {
        StartCoroutine(Stuned());
    }


    public void MinePoision()
    {
        moveSpeed = initMoveSpeed * 0.9f;
        StartCoroutine(Poision());
    }

    IEnumerator Poision()
    {
        int poisTime = Random.RandomRange(3, 6);
        for (int i = 0; i < poisTime; i++)
        {
            yield return new WaitForSeconds(1f);
            onDamage(5f);
        }
        moveSpeed = 100f;
    }



    IEnumerator Stuned()
    {
        playerState = PlayerState.Stun;
        moveVector = Vector3.zero;
        anim.SetFloat("VelX", moveVector.x);
        anim.SetFloat("VelY", moveVector.z);
        yield return new WaitForSeconds(2.0f);
        playerState = PlayerState.Shooting;
    }


    IEnumerator RespawnCoroutine(int actorNumber)
    {
        Transform playerTR = null;
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("PLAYER"))
        {
            if (player.GetComponent<PhotonView>().OwnerActorNr == actorNumber)
            {
                playerTR = player.transform;
                break;
            }
        }
        GameObject.Find("Main Camera").GetComponent<CameraMove>().target = playerTR;
        yield return new WaitForSeconds(respawnTime);

        GameObject eff = Instantiate(effectSpawn, transform);
        Destroy(eff, 1f);
        //hpBar.fillAmount = currentHP / initHP;
        playerState = PlayerState.Shooting;
        currentHP = initHP; isDie = false;
        ApplyNewHealth(currentHP, isDie);
        photonView.RPC("ResetPosition", RpcTarget.All);
        photonView.RPC("ApplyNewHealth", RpcTarget.Others, currentHP, isDie);
    }

    [PunRPC]
    public void ResetCoreBallState()
    {
        holding2pointCoreBall.SetActive(false);
        holding3pointCoreBall.SetActive(false);
        holdingbombCoreBall.SetActive(false);
    }
    [PunRPC]
    public void SetHoldingCoreBall(int holdingBallType)
    {
        if (holdingBallType == 0)
            holding2pointCoreBall.SetActive(true);
        else if (holdingBallType == 1)
            holding3pointCoreBall.SetActive(true);
        else if (holdingBallType == 2)
        {
            holdingbombCoreBall.SetActive(true);
        }
    }

    [PunRPC]
    public void ResetPosition()
    {
        anim.SetBool("Dead", false);
        int idx = (photonView.Owner.GetTeam() == PunTeams.Team.blue) ? 1 : 3;
        idx += myRespawnIdx;
        Debug.Log("리스폰 위치: " + idx);
        SoundManager.instance.Play("Respawn");
        gameObject.transform.position = points[idx].position;
    }

    string GetNickNameByActorNumber(int actorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerListOthers)
        {
            if (player.ActorNumber == actorNumber)
            {
                return player.NickName;
            }
        }
        return "Ghost";
    }
    [PunRPC]
    public void SetCoreBallTypeRpc(int num)
    {
        coreBalltype = num;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "coreball" && !isDie && other.gameObject.GetComponent<coreBall>().isPoss==false)
        {
            int actorNumber = other.gameObject.GetComponent<coreBall>().actorNumber;
            if (actorNumber == -1)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("SetCoreBallTypeRpc", RpcTarget.All, other.gameObject.GetComponent<coreBall>().coreBallType);
                    photonView.RPC("SetHoldingCoreBall", RpcTarget.All, coreBalltype);
                }
                playerState = PlayerState.Throwing;
                Destroy(other.gameObject);
                if (coreBalltype == 2) { 
                    bombCoreballTime = other.gameObject.GetComponent<coreBall>().bombTime;
                }

                SoundManager.instance.Play("Get_CoreBall");
            }
        }
        if (other.transform.tag == "BULLET" && !isDie)
        {
            int actorNumber = other.gameObject.GetComponent<bullet>().actorNumber;
            string hitter = GetNickNameByActorNumber(actorNumber);
            string bullet_Team = other.gameObject.GetComponent<bullet>().getMyTeam();
            Destroy(other.gameObject);
            

            if (!this.team.Equals(bullet_Team))
                onDamage(20f);
            if (photonView.Owner.Equals(PhotonNetwork.LocalPlayer))
                SoundManager.instance.Play("AttackedByChr");
        }
        if(other.transform.tag == "HealPack" && !isDie)
        {
            onDamage(-20f);
            SoundManager.instance.Play("GetHealPack");
            Destroy(other.gameObject);
        }

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "TiagusFire")
        {
            if (tiagusFireDamaged == false)
            {
                StartCoroutine(TiagusBurning());
                onDamage(10f);
            }
        }
    }

    public string getMyTeam()
    {
        return team;
    }

    [PunRPC]
    void Fire(int number, Vector3 dirBullet)
    {
        switch (playerCharacter)
        {
            case PlayerCharacter.Alpha:
                SoundManager.instance.Play("Alpha_atk"); break;
            case PlayerCharacter.Daisy:
                SoundManager.instance.Play("Daisy_Atk"); break;
            case PlayerCharacter.Kei:
                SoundManager.instance.Play("Kei_Atk"); break;
            case PlayerCharacter.Tiagu:
                SoundManager.instance.Play("Thiago_Atk"); break;
        }

        GameObject obj = Instantiate(bullet, weaponPos.transform.position, Quaternion.Euler(dirBullet));
        weaponCenter.transform.rotation = Quaternion.Euler(dirBullet);

        photonView.RPC("FireEffectProcessOnClients", RpcTarget.All,
            dirBullet.x, dirBullet.y, dirBullet.z);
        //obj.GetComponent<bullet>().actorNumber = number;
        obj.GetComponent<bullet>().damage = atkDamge;
        obj.GetComponent<bullet>().setMyTeam(team);
        Destroy(obj, atkRange);
    }
   
    [PunRPC]
    public void onDamage(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (damage > 0) // 양수인 경우는 총알, 폭탄 등
            {
                if (playerCharacter == PlayerCharacter.Tiagu && hpBuff)
                    damage *= 0.7f;
                makeHitDamageText(damage);
                photonView.RPC("BeShotEffectProcessOnClients", RpcTarget.All);

            }
            else if (damage < 0)
            { //음수인 경우는 체력회복제
                if (currentHP - damage >= initHP)
                    damage = currentHP - initHP;
                makeHealHpText(-damage);
                photonView.RPC("HealEffectProcessOnClients", RpcTarget.All, false);
                photonView.RPC("HealEffectProcessOnClients", RpcTarget.All, true);
            }
            currentHP -= damage;

            photonView.RPC("ApplyNewHealth", RpcTarget.All, currentHP, isDie);
            //모든 클라이언트에서 자신의 체력반영
            photonView.RPC("onDamage", RpcTarget.Others, damage);
            //자신을 제외한 모든 클라이언트에서 데미지 반영

            
        }

        if (currentHP <= 0.0f && !isDie) //넘어올 때 이미 photonView.isMine 처리해줬음
        {
            hpText.text = "0"; // hp가 음수로 변하지 않게 함
            isDie = true;
            //코어볼 처리
            photonView.RPC("ResetCoreBallState", RpcTarget.All);
            gm.CoreBallSetFalse();
            coreLine.gameObject.SetActive(false);
            playerState = PlayerState.Shooting;
            coreBalltype = -1;
            this.dirBall = Vector3.zero;

            anim.SetBool("Dead", true); // animator view가 있으므로 rpc 필요 x
            photonView.RPC("DeadEffectProcessOnClients", RpcTarget.All);
            //Debug.Log("Killed by " + hitter);
            RespawnPlayer(photonView.OwnerActorNr);
        }
    }

    [PunRPC]
    public void ApplyNewHealth(float newHealth, bool dead)
    {
        currentHP = newHealth;
        hpBar.fillAmount = currentHP / initHP;
        isDie = dead;
        hpText.text = ((int)(currentHP)).ToString();
    }

    [PunRPC]
    public void setMyTeam(int playerCount)
    {
        this.team = (playerCount % 2 == 0) ? "RED" : "BLUE";
    }

    [PunRPC]
    public void RespawnPlayer(int deadActorNumber)
    {
        StartCoroutine(RespawnCoroutine(deadActorNumber));
    }

    public void OnBtnBombClicked(PointerEventData data)
    {
        if (canUseBomb)
        {
            SoundManager.instance.Play("UIClick");
            playerState = PlayerState.Bomb;
        }
    }

    public void OnBtnSkillClicked(PointerEventData data)
    {
        if (canUseSkill && !FillingSkill && !UsingSkill)
        {
            SoundManager.instance.Play("UIClick");
            playerState = PlayerState.Skill;
        }
    }

    IEnumerator BombFill(Button btn, float coolTime, int btnType)
    {
        bombdeActiveImage.gameObject.SetActive(true);
        bombTimeText.gameObject.SetActive(true);
        while (coolTime > 0)
        {
            coolTime -= Time.deltaTime;
            bombTimeText.text = ((int)coolTime).ToString();
            if (coolTime <= 0)
            {
                bombdeActiveImage.gameObject.SetActive(false);
                bombTimeText.gameObject.SetActive(false);
                if (btnType == 1)
                    canUseBomb = true;
                else if (btnType == 2)
                    canUseSkill = true;
            }
            yield return null;
        }
        yield break;
    }

    IEnumerator SkillUse()
    {
        //Debug.Log("RUNNING_SKillUse");
        UsingSkill = true;
        photonView.RPC("SkillUsingEffectProcessOnClients", RpcTarget.All, true);
        SoundManager.instance.Play("SpcAtkActive");
        skillGageImage.fillAmount = 1f;
        while (skillGageImage.fillAmount >= 0f)
        {
            skillGageImage.fillAmount -= (1 * Time.smoothDeltaTime / 3f);
            //Debug.Log("RUNNING_SKillUse");

            if (skillGageImage.fillAmount == 0)
            {
                if (playerCharacter == PlayerCharacter.Tiagu)
                    tiaguFire.SetActive(false);
                photonView.RPC("SkillUsingEffectProcessOnClients", RpcTarget.All, false);
                skilldeActiveImage.gameObject.SetActive(true);

                canUseSkill = false;
                //effectSkillUsing.SetActive(false);
                skillGageImage.fillAmount = 0f;
                playerState = PlayerState.Shooting;
                UsingSkill = false;
                break;
            }
            yield return null;
        }
        yield break;
    }
    IEnumerator SkillFill() //여기 yield 수정필요
    {
        FillingSkill = true;
        skillGageImage.fillAmount = 0f;
        while (skillGageImage.fillAmount <= 1f)
        {
            skillGageImage.fillAmount += (1 * Time.smoothDeltaTime / skillLoadSpeed);
            if (playerState == PlayerState.Shooting)
                skillGageImage.fillAmount += (1 * Time.smoothDeltaTime / 15f);
            if (skillGageImage.fillAmount == 1)
            {
                photonView.RPC("SkillUsingEffectProcessOnClients", RpcTarget.All, false);
                skilldeActiveImage.gameObject.SetActive(false);

                canUseSkill = true;
                FillingSkill = false;
                break;
            }
            yield return null;
        }
        yield break;
    }
    [PunRPC]
    void makeHitDamageText(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("makeHitDamageText", RpcTarget.Others, damage);
        GameObject hdt = Instantiate(hitDamageText);
        hdt.transform.position = hitTextPos.position;
        hdt.GetComponent<HitDamageEffect>().damage = damage;
        hdt.GetComponent<HitDamageEffect>().isDamage = true;
    }
    [PunRPC]
    void makeHealHpText(float healHP)
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("makeHealHpText", RpcTarget.Others, healHP);
        GameObject hht = Instantiate(healHpText);
        hht.transform.position = hitTextPos.position;
        hht.GetComponent<HitDamageEffect>().damage = healHP;
        hht.GetComponent<HitDamageEffect>().isDamage = false;
    }

    public bool get_isDie()
    {
        return isDie;
    }

    [PunRPC]
    public void DeadEffectProcessOnClients()
    {
        GameObject eff = Instantiate(effectDie, transform.position, Quaternion.identity);
        Destroy(eff, 4f);
    }

    [PunRPC]
    public void BeShotEffectProcessOnClients()
    {
        float randomX = Random.Range(-30f, 30f);
        float randomY = Random.Range(-30f, 30f);
        float randomZ = Random.Range(-30f, 30f);
        Vector3 effectPos = new Vector3(weaponCenter.transform.position.x + randomX, weaponCenter.transform.position.y + randomY, weaponCenter.transform.position.z + randomX);

        GameObject eff = Instantiate(effectBeShot, effectPos, Quaternion.identity);
        Destroy(eff, 3f);
    }

    [PunRPC]
    public void SkillUsingEffectProcessOnClients(bool IsActive)
    {
        effectSkillUsing.SetActive(IsActive);
    }
    [PunRPC]
    public void HealEffectProcessOnClients(bool IsActive)
    {
        EffectHeal.gameObject.SetActive(IsActive);
    }



    [PunRPC]
    public void FireEffectProcessOnClients(float x, float y, float z)
    {
        Vector3 dirBullet = new Vector3(x, y, z);
        GameObject eff = Instantiate(effectFire, weaponPos.transform.position, Quaternion.Euler(dirBullet));
        Destroy(eff, 0.5f);
    }

    public void setMyColor(Color c)
    {
        hpBar.color = c;
        underCharacterColor.color = c;
    }

    public void SetHpFootColor() // 체력바, 발판, 리스폰 위치 정하는 함수
    {
        players = GameObject.FindGameObjectsWithTag("PLAYER");
        if (photonView.Owner.Equals(PhotonNetwork.LocalPlayer))
        {
            //photonView.RPC("setMyTeam", RpcTarget.All, (playerCount));
            foreach (GameObject other in players)
            {
                PhotonView otherPlayer = other.GetPhotonView();
                bool IsEnemy = (otherPlayer.Owner.GetTeam() == photonView.Owner.GetTeam()) ? false : true;
                Debug.Log(players.Length);
                Debug.Log(otherPlayer.Owner.GetTeam() + " " + photonView.Owner.GetTeam());
                string other_team = other.GetComponent<PlayerController>().getMyTeam();
                
                if (/*!this.team.Equals(other_team)*/IsEnemy)
                { // 적팀일 때
                    other.GetComponent<PlayerController>().setMyColor(enemyColor);
                }
                else
                {
                    other.GetComponent<PlayerController>().setMyColor(myTeamColor);
                    //if (!otherPlayer.Owner.Equals(photonView.Owner)
                    //    && photonView.OwnerActorNr < otherPlayer.OwnerActorNr)
                    //    myRespawnIdx = 0;
                    //else
                    //    myRespawnIdx = 1;
                }
            }
            setMyColor(myOwnColor);
        }
    }

}