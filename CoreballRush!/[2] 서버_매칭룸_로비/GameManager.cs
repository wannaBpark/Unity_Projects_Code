using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun.UtilityScripts;
using Photon.Pun.Demo.Cockpit;
using System.Threading;

public class GameManager : MonoBehaviourPunCallbacks
{
    private const int MAX_HEALPACK_COUNT = 2;

    public string team;
    float gameTime;
    public bool IsAllSpawned = false;
    public int SpawnedPlayerCount = 0;
    public Sprite[] timeImage_Sprites = new Sprite[10];
    public Sprite[] redScoreImage_Sprites = new Sprite[4];
    public Sprite[] blueScoreImage_Sprites = new Sprite[4];
    public Sprite[] redTotalScoreImage_Sprites = new Sprite[10];
    public Sprite[] blueTotalScoreImage_Sprites = new Sprite[10];
    public Image[] time = new Image[3];
    public Image redScoreImage;
    public Image blueScoreImage;
    public Image[] redTotalScoreImage = new Image[2];
    public Image[] blueTotalScoreImage = new Image[2];
    public Image[] CountImage = new Image[5];
    public Image StartImage;
    public Image WinImage;
    public Image LoseImage;
    public GameObject startExpImage;
    public GameObject startCameraPos;

    public GameObject[] Walls;
    public GameObject effectCoreBallCreate;
    public GameObject mychr;

    public Text secondText;
    public Text minuteText;
    public Text redScoreText;
    public Text blueScoreText;
    public Text redTotalScoreText;
    public Text blueTotalScoreText;
    public Text connectingText;

    public GameObject panel;
    public GameObject panel2;
    public GameObject panel3;


    //-는 블루팀 +는 레드팀
    private int scoreBgoal; //블루팀 골대 점수
    private int scoreRgoal; //레드팀 골대 점수
    private int scoreCgoal; //중간 골대 점수

    //점령중 이펙트
    public GameObject leftBlue;
    public GameObject leftRed;
    public GameObject centerBlue;
    public GameObject centerRed;
    public GameObject rightBlue;
    public GameObject rightRed;

    private int redTotalScore;
    private int blueTotalScore;
    private int redScore;
    private int blueScore;

    private int coreBallTypeNumber;
    public Transform CoreBallSpawnPos;

    public GameObject Keeper;
    public float keeperDeadTime = 0;
    public Animator AiGround;
    public Transform KeeperSpawnPos;
    public bool isKeeper = false;
    bool GameReady = false;
    bool Win = false;
    bool isEnding = false;
    public bool isCoreBallinGame = true;

    float healpacktimer;

    public Animator GL;
    public Animator GM;
    public Animator GR;

    const int reconPointNum = 6;
    private GameObject[] reconPoint = new GameObject[reconPointNum];
    private int reconNum = 0;
    float[] minXZPos = new float[2] { 999f, 999f };
    float[] maxXZPos = new float[2] { -999f, -999f };
    Vector3 HealPos;
    private void Awake()
    {
        gameTime = 0f;
        healpacktimer = 0f;
        scoreBgoal = 0;
        scoreRgoal = 0;
        scoreCgoal = 0;
        redTotalScore = 0;
        blueTotalScore = 0;
        redScore = 0;
        blueScore = 0;
        //if(PhotonNetwork.IsMasterClient)
        CoreBallSetFalse();
    }

    private void Start()
    {
        for (int i = 0; i < reconPointNum; i++) {
            reconPoint[i] = GameObject.Find("Map").transform.Find("keeperRecPos").GetChild(i).gameObject;
            float rx, rz;
            rx = reconPoint[i].transform.position.x;
            rz = reconPoint[i].transform.position.z;
            if (rx < minXZPos[0]) minXZPos[0] = rx;
            if (rz < minXZPos[1]) minXZPos[1] = rz;
            if (rx > maxXZPos[0]) maxXZPos[0] = rx;
            if (rz > maxXZPos[1]) maxXZPos[1] = rx;
        }

        SpawnPlayer();
        //if (PhotonNetwork.IsMasterClient)
        //    photonView.RPC("StartGame", RpcTarget.All);
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (GameObject wall in Walls)
            {
                wall.SetActive(true);
            }
        }
    }
    private void Update()
    {
        //checkStartGame();
        changeUI();
        if (PhotonNetwork.IsMasterClient)
        {
            if (keeperDeadTime>=30f && isKeeper == false)
            {
                keeperDeadTime = 0f;
                SpawnKeeper();
            }
            if (healpacktimer >= 10f) {
                SpawnHealPack();
                healpacktimer = 0f;
            }
        }
        if (gameTime <= 6f && !isEnding && gameTime != 0f)
        {
            photonView.RPC("EndThisGame", RpcTarget.All);
        }
        if (Input.GetKeyDown(KeyCode.C) && PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                PhotonNetwork.PlayerList[i].SetCustomProperties(new Hashtable { { "score", blueTotalScore } });
            }

        }
        //Hashtable CP = PhotonNetwork.LocalPlayer.CustomProperties;
        //blueTotalScore = (int)CP["score"];
        //Debug.Log("blueTotalScore"+ blueTotalScore);
    }

    void SpawnHealPack()
    {
        GameObject[] healpacks = GameObject.FindGameObjectsWithTag("HealPack");
        if (healpacks.Length >= MAX_HEALPACK_COUNT)
            return;
        SetHealPackPosition();
        PhotonNetwork.Instantiate("HealPack", HealPos, Quaternion.Euler(-50.78f, 0f, 47.314f));
    }

    void SpawnKeeper()
    {
        isKeeper = true;
        photonView.RPC("AiGroundAnimation", RpcTarget.All);
        PhotonNetwork.Instantiate("Keeper", KeeperSpawnPos.position, Quaternion.identity);
    }
    [PunRPC]
    public void AiGroundAnimation()
    {
        AiGround.SetBool("open", false);
        AiGround.SetBool("open", true);

    }


    void changeUI()
    {

        //시간
        if (((int)(gameTime % 60)) / 10 == 0)
        {
            secondText.text = "0" + ((int)(gameTime % 60)).ToString();
        }
        else
            secondText.text = ((int)(gameTime % 60)).ToString();
        minuteText.text = ((int)(gameTime / 60)).ToString();

        //time[0].sprite = timeImage_Sprites[(int)(gameTime / 100)];
        //time[1].sprite = timeImage_Sprites[(int)(gameTime % 100 / 10)];
        //time[2].sprite = timeImage_Sprites[(int)(gameTime % 10)];

        //점령 점수
        redScoreText.text = redScore.ToString();
        blueScoreText.text = blueScore.ToString();

        //redScoreImage.sprite = redScoreImage_Sprites[redScore];
        //blueScoreImage.sprite = blueScoreImage_Sprites[blueScore];

        //총 점수
        redTotalScoreText.text = redTotalScore.ToString();
        blueTotalScoreText.text = blueTotalScore.ToString();

        //redTotalScoreImage[0].sprite = redTotalScoreImage_Sprites[redTotalScore / 10];
        //redTotalScoreImage[1].sprite = redTotalScoreImage_Sprites[redTotalScore % 10];
        //blueTotalScoreImage[1].sprite = blueTotalScoreImage_Sprites[blueTotalScore / 10];
        //blueTotalScoreImage[0].sprite = blueTotalScoreImage_Sprites[blueTotalScore % 10];
    }

    public void Goal(string team, string goalPointName, int coreBallType)
    {
        SoundManager.instance.Play("Get_Score");
        int point = 3;

        if (coreBallType == 0)
            point = 2;

        if (goalPointName == "GoalPointB") {
            if (team == "RED")
            {

                scoreBgoal += point;
                redTotalScore += point;
            }
            if (team == "BLUE")
            {
                scoreBgoal -= point;
                blueTotalScore += point;
            }
        }
        else if (goalPointName == "GoalPointC")
        {
            if (team == "RED")
            {
                scoreCgoal += point;
                redTotalScore += point;
            }
            if (team == "BLUE")
            {
                scoreCgoal -= point;
                blueTotalScore += point;
            }
        }
        else if (goalPointName == "GoalPointR")
        {
            if (team == "RED")
            {
                scoreRgoal += point;
                redTotalScore += point;
            }
            if (team == "BLUE")
            {
                scoreRgoal -= point;
                blueTotalScore += point;
            }
        }
        photonView.RPC("ApplyNewScore", RpcTarget.AllViaServer, scoreBgoal,
        scoreRgoal,
        scoreCgoal,
        redTotalScore,
        blueTotalScore,
        redScore,
        blueScore);

        if(PhotonNetwork.IsMasterClient)
            CheckPoint();
    }
    private void CheckPoint()
    {
        int tmpB = 0, tmpR = 0;
        if (scoreBgoal > 0)
        {
            if (scoreBgoal <= 3)//점령시
            {
                SoundManager.instance.Play("GetGoal");
                photonView.RPC("SetGoalGround", RpcTarget.All, "right", true);
            }
            tmpR++;
            GL.SetInteger("goalPoint", -1);
        }
        else if (scoreBgoal < 0)
        {
            if (scoreBgoal >= -3)//점령시
            {
                SoundManager.instance.Play("GetGoal");
                photonView.RPC("SetGoalGround", RpcTarget.All, "right", false);
            }
            tmpB++;
            GL.SetInteger("goalPoint", 1);
        }
        else
            GL.SetInteger("goalPoint", 0);

        if (scoreRgoal > 0)
        {
            if (scoreRgoal <= 3)//점령시
            {
                SoundManager.instance.Play("GetGoal");
                photonView.RPC("SetGoalGround", RpcTarget.All, "left", true);
            }
            tmpR++;
            GR.SetInteger("goalPoint", -1);
        }
        else if (scoreRgoal < 0)
        {
            if (scoreRgoal >= -3)//점령시
            {
                SoundManager.instance.Play("GetGoal");
                photonView.RPC("SetGoalGround", RpcTarget.All, "left", false);
            }
            tmpB++;
            GR.SetInteger("goalPoint", 1);
        }
        else
            GR.SetInteger("goalPoint", 0);

        if (scoreCgoal > 0)
        {
            if (scoreCgoal <= 3)//점령시
            {
                SoundManager.instance.Play("GetGoal");
                photonView.RPC("SetGoalGround", RpcTarget.All, "center", true);
            }
            tmpR++;
            GM.SetInteger("goalPoint", -1);
        }
        else if (scoreCgoal < 0)
        {
            if (scoreCgoal >= -3)//점령시
            {
                SoundManager.instance.Play("GetGoal");
                photonView.RPC("SetGoalGround", RpcTarget.All, "center", false);
            }
            tmpB++;
            GM.SetInteger("goalPoint", 1);
        }
        else
            GM.SetInteger("goalPoint", 0);
        redScore = tmpR;
        blueScore = tmpB;


        photonView.RPC("ApplyNewScore", RpcTarget.AllViaServer, scoreBgoal,
            scoreRgoal,
            scoreCgoal,
            redTotalScore,
            blueTotalScore,
            redScore,
            blueScore);

    }
    [PunRPC]
    public void SetGoalGround(string pos, bool isRed)
    {
        if(pos == "center")
        {
            if (isRed)
            {
                centerBlue.SetActive(false);
                centerRed.SetActive(true);
            }
            else
            {
                centerBlue.SetActive(true);
                centerRed.SetActive(false);
            }
        }
        else if(pos == "right")
        {
            if (isRed)
            {
                rightBlue.SetActive(false);
                rightRed.SetActive(true);
            }
            else
            {
                rightBlue.SetActive(true);
                rightRed.SetActive(false);
            }
        }
        else
        {
            if (isRed)
            {
                leftBlue.SetActive(false);
                leftRed.SetActive(true);
            }
            else
            {
                leftBlue.SetActive(true);
                leftRed.SetActive(false);
            }
        }

    }
    public void CoreBallSetFalse()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("CoreBallSetFalseRPC", RpcTarget.All);
            photonView.RPC("RespawnCoreBall", RpcTarget.All);
        }
    }
    [PunRPC]
    public void CoreBallSetFalseRPC()
    {
        isCoreBallinGame = false;
    }

    [PunRPC]
    public void RespawnCoreBall()
    {
        //photonView.RPC("CoreBall", RpcTarget.Others, photonView.OwnerActorNr);
        if (!isCoreBallinGame)
        {
            StartCoroutine(CoreBallSpawnTime());
            isCoreBallinGame = true;
        }
    }
    IEnumerator CoreBallSpawnTime()
    {
        yield return new WaitForSeconds(3f);
        GameObject eff = Instantiate(effectCoreBallCreate, CoreBallSpawnPos.position,
            CoreBallSpawnPos.rotation);
        Destroy(eff, 3f);
        yield return new WaitForSeconds(2f);

        SoundManager.instance.Play("CoreBall_Spawn");

        coreBallTypeNumber = Random.Range(0, 10);

        if (PhotonNetwork.IsMasterClient)
            InstantiateNewCoreBall(coreBallTypeNumber);
        //photonView.RPC("InstantiateNewCoreBall", RpcTarget.All, coreBallTypeNumber);
        yield return null;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount);
        connectingText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/4명 접속중..";
        StartGame();
        //StartCoroutine(StartGameTimer());

    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("playerenteredroom");
        connectingText.text = PhotonNetwork.CurrentRoom.PlayerCount + "/4명 접속중..";


        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                photonView.RPC("StartGame", RpcTarget.Others);
                StartGame();
                //connectingText.text = "게임 준비 완료!";
                //photonView.RPC("StartGame", RpcTarget.Others);
                //StartGame();
                //Debug.Log("checkstartgame");
                //ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
                //roomProperties.Add("gameTime", 150);
                //PhotonNetwork.LocalPlayer.SetCustomProperties(roomProperties);
            }
        }
    }

    public void checkStartGame()
    {
        Debug.Log("checkstartgame");

    }

    [PunRPC]
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {

            //GameObject wall = PhotonNetwork.Instantiate("MoveWallVertical", new Vector3(-521.4f, -24.46f, -17.09f),
            //    Quaternion.Euler(-90, 0, -90));
            //GameObject wall2 = PhotonNetwork.Instantiate("MoveWallVertical", new Vector3(535f, -24.46f, 0f),
            //    Quaternion.Euler(-90, 180, -90));
            //GameObject wall3 = PhotonNetwork.Instantiate("MoveWall", new Vector3(0f, -24.46f, 530f),
            //    Quaternion.Euler(-90, 90, -90));
            //foreach (GameObject wall in Walls)
            //{
            //    wall.SetActive(true);
            //}
        }
        StartCoroutine(StartTimerCoroutine());
    }

    [PunRPC]
    IEnumerator StartTimerCoroutine()
    {

        connectingText.text = "게임 준비 완료!";
        yield return new WaitForSeconds(2f);
        connectingText.gameObject.SetActive(false);
        panel.SetActive(false);
        startExpImage.SetActive(false);
        startCameraPos.GetComponent<startCameraMove>().isMoving = false;
        for (int i = 2; i >= 0; i--)
        {
            CountImage[i].gameObject.SetActive(true);
            CountImage[i].GetComponent<GameStartImage>().isNum = true;
            SoundManager.instance.Play("CountDown");
            yield return new WaitForSeconds(1f);
        }

        StartImage.gameObject.SetActive(true);
        StartImage.GetComponent<GameStartImage>().isNum = false;
        panel2.SetActive(false);
        yield return new WaitForSeconds(2f);


        gameTime = 150f;
        while (gameTime >= 0)
        {
            healpacktimer += Time.deltaTime;
            if(!isKeeper)
                keeperDeadTime += Time.deltaTime;
            gameTime -= Time.deltaTime;

            yield return null;
        }
        yield break;
    }

    [PunRPC]
    public void EndThisGame()
    {
        StartCoroutine(EndCoroutine());
        //5초 카운트
        //    win or lost
        //    애니메이션 5초 보여주고
        //    씬전환
    }

    IEnumerator EndCoroutine()
    {
        isEnding = true;
        for (int i = 4; i >= 0; i--)
        {
            CountImage[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            CountImage[i].gameObject.SetActive(false);
        }

        if ((blueScore > redScore && PhotonNetwork.LocalPlayer.GetTeam() == PunTeams.Team.blue) ||
            (blueScore < redScore && PhotonNetwork.LocalPlayer.GetTeam() == PunTeams.Team.red))
        {
            Win = true;
            DataController.instance.gameData.Win = true;
        }
       
        else
        {
            Win = false;
            DataController.instance.gameData.Win = false;
        }
        CameraMove.instance.offsetX = 0f;
        CameraMove.instance.offsetY = 170f;
        CameraMove.instance.offsetZ = -83f;
        panel3.SetActive(true);
        if (Win)
        {
            WinImage.gameObject.SetActive(true);
        }
        else if (!Win)
        {
            LoseImage.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(2f);
        
        switch (Win)
        {
            case true:
                mychr.GetComponent<Animator>().SetBool("Win", true); break;
            case false:
                mychr.GetComponent<Animator>().SetBool("Lose", true); break;
        }
        yield return new WaitForSeconds(5f);
        
        PhotonNetwork.Disconnect();
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(4);
        SceneManager.LoadScene(4);
        //PhotonNetwork.LoadLevel("EndPage");'
        yield return null;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene(4);
    }

    [PunRPC]
    public void ApplyNewScore(int scoreBgoal, int scoreRgoal, int scoreCgoal, int redTotalScore,
            int blueTotalScore, int redScore, int blueScore)
    {
        this.scoreBgoal = scoreBgoal;
        this.scoreRgoal = scoreRgoal;
        this.scoreCgoal = scoreCgoal;
        this.redTotalScore = redTotalScore;
        this.blueTotalScore = blueTotalScore;
        this.redScore = redScore;
        this.blueScore = blueScore;
    }
    //public virtual void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    //{
    //    Debug.Log("onphotoncustomroompropertieschanged");

    //}

    //void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
    //{
    //    Debug.Log("OnPhotonPlayerPropertiesChanged");
    //    StartCoroutine(StartGameTimer());

    //}

    void SpawnPlayer()
    {
        //아직 스폰 위치 랜덤. 이후 맵에 맞추어 수정
        Transform[] points = GameObject.Find("SpawnPoint").GetComponentsInChildren<Transform>();
        //int idx = Random.Range(1, points.Length);

        //int idx = PhotonNetwork.PlayerList.Length % 2 == 1 ? 1 : 3;
        //int playerCount = PhotonNetwork.PlayerList.Length;
        //int playerCount = 2;
        PunTeams.Team Myteam = PhotonNetwork.LocalPlayer.GetTeam();
        int playerCount = (Myteam.Equals(PunTeams.Team.blue)) ? 1 : 2;
        if (DataController.instance.gameData.Character != -1)
            playerCount = DataController.instance.gameData.Character + 1;
        int idx = (Myteam.Equals(PunTeams.Team.blue)) ? 1 : 3;
        idx += DataController.instance.gameData.respawnidx;
        string playerCharacter = "";
        switch (playerCount)
        {
            case 1:
                playerCharacter = "Alpha"; break;
            case 2:
                playerCharacter = "Tiago"; break;
            case 3:
                playerCharacter = "Kei"; break;
            case 4:
                playerCharacter = "Daisy"; break;

        }
        mychr= PhotonNetwork.Instantiate(playerCharacter, points[idx].position, Quaternion.identity);
        photonView.RPC("SendSpawnedComplete", RpcTarget.All);
    }

    [PunRPC]
    public void SendSpawnedComplete() // 모든 플레이어가 생성됨을 알리는 함수
    {
        ++SpawnedPlayerCount;
        Debug.Log(SpawnedPlayerCount);
        if (SpawnedPlayerCount == PhotonNetwork.PlayerList.Length) // 현재 접속한 인원만큼 생성이 완료되면
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("StartGame", RpcTarget.All);
            IsAllSpawned = true;
            GameObject[] players = GameObject.FindGameObjectsWithTag("PLAYER");
            foreach (GameObject player in players)
            {
                player.GetComponent<PlayerController>().SetHpFootColor();
                // 씬 내 모든 플레이어컨트롤러의 함수 전달, 실제론 로컬플레이어만 전달받음 
            }
        }
    }

    [PunRPC]
    public void InstantiateNewCoreBall(int coreBallTypeNumber)
    {
        int coreBallType = 0;
        GameObject obj;
        if (coreBallTypeNumber >= 5 && coreBallTypeNumber < 7) coreBallType = 1;
        else if (coreBallTypeNumber >= 7) coreBallType = 2;
        switch (coreBallType)
        {
            case 0:
                obj = PhotonNetwork.Instantiate("ball_W", CoreBallSpawnPos.position,
            CoreBallSpawnPos.rotation);
                obj.GetComponent<coreBall>().isPoss = false;
                obj.GetComponent<coreBall>().coreBallType = coreBallType;
                break;
            case 1:
                obj = PhotonNetwork.Instantiate("ball_Y", CoreBallSpawnPos.position,
            CoreBallSpawnPos.rotation);
                obj.GetComponent<coreBall>().isPoss = false;
                obj.GetComponent<coreBall>().coreBallType = coreBallType;
                break;
            case 2:
                obj = PhotonNetwork.Instantiate("BombBall", CoreBallSpawnPos.position,
            CoreBallSpawnPos.rotation);
                obj.GetComponent<coreBall>().isPoss = false;
                obj.GetComponent<coreBall>().coreBallType = coreBallType;
                break;
        }

    }
    public void SetHealPackPosition()
    {
        while (true)
        {
            HealPos.x = Random.Range(minXZPos[0], maxXZPos[0]);
            HealPos.z = Random.Range(minXZPos[1], maxXZPos[1]);
            HealPos.y = 0;

            Collider[] colliders = Physics.OverlapSphere(HealPos, 8f);
            Debug.Log(colliders.Length);
            if (colliders.Length == 0) break;
        }

    }
}
