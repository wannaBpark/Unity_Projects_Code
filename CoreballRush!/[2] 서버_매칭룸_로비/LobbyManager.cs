using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public int[] chosenCharacter = new int[4]{ -1, -1, -1, -1 };
    public string[] selected = { "null", "null", "null", "null" };
    private string[] CharName = { "Alpha", "Tiagu", "Kei", "Daisy" };
    private string[] NickNames = { "", "", "", "" };
    public GameObject LoadingScreen;
    public GameObject ChrCamera;
    public GameObject[] prefab_Char;
    public GameObject[] currentChar = new GameObject[4];
    public Button[] TeamBtns;
    public Text[] NicknameTxts;
    public Text myNickname;
    public int mySelection = -1;

    private string gameVersion = "1.0";
    private string myChrName;
    public string userId = "Emty";
    public byte maxPlayer = 4;

    public Text T;
    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        PhotonNetwork.GameVersion = this.gameVersion;
        //PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("Nickname");
        //Debug.Log(PlayerPrefs.GetString("Nickname") + "불러오기 성공");
        //myChrName = CharName[PlayerPrefs.GetInt("Character")];

        PhotonNetwork.LocalPlayer.NickName = DataController.instance.gameData.Nickname;
        //Debug.Log(PlayerPrefs.GetString("Nickname") + "불러오기 성공");
        myChrName = CharName[DataController.instance.gameData.Character + 1];

        PhotonNetwork.ConnectUsingSettings();

        for (int i = 0; i < 4; i++)
        {
            int _i = i;
            //TeamBtns[i] = GameObject.FindGameObjectWithTag("LobbyCanvas").transform.GetChild(5+i).GetComponent<Button>();
            TeamBtns[i].onClick.AddListener(() => onSelectBtnClick(_i));
            NicknameTxts[i] = TeamBtns[i].transform.GetChild(0).GetComponent<Text>();
        }
    }

    public void DisconnectAndGoMain()
    {
        PhotonNetwork.Disconnect();
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("Connect To Master");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene(1);
    }


    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Failed Join Room");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = this.maxPlayer });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room !");
        //mySelection = PhotonNetwork.PlayerList.Length-1;
        //UpdatePlayerImages();

    }

    public override void OnPlayerEnteredRoom(Player newPlayer) // 새로운 플레이어 들어왔을 때
    {
        // 방장이 현재 방상태를 다시 보내준다.
        if (PhotonNetwork.IsMasterClient) {
            photonView.RPC("ApplyNewLobbyState", newPlayer, (object)selected, (object)NickNames);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) // 나간 플레이어 데이터 삭제
    {
        //if (!PhotonNetwork.IsMasterClient) return; // 방장만 업데이트할 수 있도록

        string LeftedName = otherPlayer.NickName;
        int idx = 0;
        for(int i=0; i< NickNames.Length; i++)
        {
            if (NickNames[i].Equals(LeftedName)) idx = i;
        }
        selected[idx] = "null";
        NickNames[idx] = "";
        photonView.RPC("ApplyNewLobbyState", RpcTarget.All, (object)selected, (object)NickNames);
        // 배열을 동기화 시키려면 (object) 붙여야함
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.W))
        //{
        //    selected[0] = "null";
        //    photonView.RPC("ApplyNewLobbyState", RpcTarget.All, (object)selected, (object)NickNames);
        //}
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    selected[1] = "false";
        //    photonView.RPC("ApplyNewLobbyState", RpcTarget.All, (object)selected, (object)NickNames);
        //}
        //if (Input.GetKeyDown(KeyCode.S))
        //{
        //    selected[2] = "false";
        //    photonView.RPC("ApplyNewLobbyState", RpcTarget.All, (object)selected, (object)NickNames);

        //}
        //ApplyNewLobbyState(s);

    }

    [PunRPC]
    public void ApplyNewLobbyState(string[] ns, string[] nicknames) // 플레이어 선택, 닉네임 상태 업데이트
    {
        this.selected = ns;
        this.NickNames = nicknames;
        Debug.Log(ns[1]);
        T.text = "";
        foreach(Player player in PhotonNetwork.PlayerList)
            T.text = T.text + player.NickName;
        UpdatePlayerImages();
        UpdatePlayerNicknames();
    }
    
    public void UpdatePlayerImages() // 플레이어 프리팹 그리기
    {
        DeleteAllImages(); // 현 상태 초기화하고 다시 그릴 준비

        for (int i=0; i<selected.Length; i++) {
            for(int j=0; j< CharName.Length; j++) {
                if (selected[i].Equals(CharName[j])) {
                    GameObject gm = GameObject.Instantiate(prefab_Char[j],transform);
                    Debug.Log(i + " " + j + " " + selected[i] + " " + CharName[j]);
                    gm.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
                    //gm.gameobject.setActive(true);
                    SetPosAndScale(gm, i);
                    
                    //gm.transform.tag = i.ToString();
                }
            }
        }
    }

    public void SetPosAndScale(GameObject gm, int idx) // 플레이어 프리팹 포지션, 스케일 조정
    {
        float[] pos = { -960, -385,385, 960 };
        gm.transform.localPosition = new Vector3(pos[idx], -165f, 0f);
        gm.transform.localScale = new Vector3(52f, 52f, 52f);
        gm.transform.localRotation = new Quaternion(0f, 180f, 0f, 1);
        gm.transform.tag = "PLAYER";
        gm.SetActive(true);
    }

    public void DeleteAllImages() // 플레이어 프리팹 다 지우고 새로 그리기
    {
        GameObject[] CurPlayers = GameObject.FindGameObjectsWithTag("PLAYER");

        foreach(GameObject player in CurPlayers) {
            Destroy(player);
        }
    }

    public void UpdatePlayerNicknames() // 닉네임 업데이트 로컬 함수
    {
        for(int i=0; i<NicknameTxts.Length; i++)
        {
            NicknameTxts[i].text = NickNames[i];
        }
    }

    public void onSelectBtnClick(int idx)
    {
        Debug.Log("선택된 버튼의 인덱스"+idx);
        if (checkSelectable(idx)) {
            if(mySelection != -1) { // 현재 선택한 게 이미 있다면 초기화
                selected[mySelection] = "null";
                NickNames[mySelection] = "";
            }
            SetMyPunTeam(idx);
            //PhotonNetwork.LocalPlayer.NickName= myNickname.text; //현재 inputfield에 있는 닉네임
            selected[idx] = myChrName; // 플레이어가 고른 캐릭터
            NickNames[idx] = PhotonNetwork.LocalPlayer.NickName; // 플레이어의 닉네임
            photonView.RPC("ApplyNewLobbyState", RpcTarget.All, (object)selected, (object)NickNames);
            mySelection = idx;
            DataController.instance.gameData.respawnidx = (mySelection%2);
        }
    }

    public bool checkSelectable(int idx) { // 빈 공간인지 체크
        if (selected[idx].Equals("null")) return true;
        return false;
    }

    public void SetMyPunTeam(int idx) {
        if (idx >= 2) PhotonNetwork.LocalPlayer.SetTeam(PunTeams.Team.red);
        else PhotonNetwork.LocalPlayer.SetTeam(PunTeams.Team.blue);
    }

    public void GoGame() // 메인씬 이동
    {
        if (!PhotonNetwork.IsMasterClient) {
            return;
        }
        //SoundManager.instance.Play("Btn_Click");
        //SceneManager.LoadScene("MainScene");
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(3);
            photonView.RPC("LoadingProcess", RpcTarget.All);
            
        }
        
        //SceneManager.LoadScene("MainScene");
    }
    [PunRPC]
    public void LoadingProcess()
    {
        StartCoroutine(LoadLevelAsync());
    }
    IEnumerator LoadLevelAsync()
    {
        while (PhotonNetwork.LevelLoadingProgress < 1)
        {
            LoadingScreen.SetActive(true);
            ChrCamera.SetActive(false);
            //loadAmountText.text = "Loading: %" + (int)(PhotonNetwork.LevelLoadingProgress * 100);
            ////loadAmount = async.progress;
            //progressBar.fillAmount = PhotonNetwork.LevelLoadingProgress;
            yield return new WaitForEndOfFrame();
        }
    }

}
