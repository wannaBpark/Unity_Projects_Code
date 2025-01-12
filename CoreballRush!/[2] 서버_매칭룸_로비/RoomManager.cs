using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class RoomManager : MonoBehaviourPunCallbacks
{
    public string nickname = "asdfa";
    private string gameVersion = "2.0";

    public GameObject Roomplayer;
    public GameObject roomPanel;

    /*[HideInInspector]*/
    public Text connectionInfoText; //네트워크 정보를 표시할 텍스트
    public Text[] n_Texts;
    public InputField n_inputfield;
    public Text testText;
    public Button GameStartBtn; // 룸 접속 버튼
    /*[HideInInspector]*/
    public string selected_T = "asdf";
    public bool isUserSelected = false;
    // 닉네임 불러오기
    // 태그 불러오기
    // 스폰포인트 불러오기
   // public PhotonView photonView;
    // 게임 실행과 동시에 마스터 서버 접속 시도

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        testText = GameStartBtn.GetComponent<Text>();
    }
    private void Start()
    {
        // 접속에 필요한 정보(게임 버전) 설정
        PhotonNetwork.GameVersion = gameVersion;
        // 설정한 정보로 마스터 서버 접속 시도
        PhotonNetwork.ConnectUsingSettings();

        // 룸 접속 버튼 잠시 비활성화
        GameStartBtn.interactable = false;
        connectionInfoText.text = "마스터 서버에 접속 중...";


       // DontDestroyOnLoad(gameObject);
    }

    //마스터 서버 접속 성공 시 자동 실행
    public override void OnConnectedToMaster()
    {
        // 룸 접속 버튼 활성화
        GameStartBtn.interactable = true;
        // 접속 정보 표시
        connectionInfoText.text = "온라인: 마스터 서버와 연결됨";
        PhotonNetwork.JoinRandomRoom();
        
    }
    

    // 마스터 서버 접속 실패 시 자동 실행
    public override void OnDisconnected(DisconnectCause cause)
    {
        // 룸 접속 버튼 비활성화
        GameStartBtn.interactable = false;
        // 접속 정보 표시
        connectionInfoText.text = "오프라인: 마스터 서버와 연결되지 않음\n접속 재시도 중...";

        // 마스터 서버로의 재접속 시도
        PhotonNetwork.ConnectUsingSettings();
    }

    // 룸 접속 시도
    public void Connect()
    {
        // 중복 접속 시도를 막기 위해 접속 버튼 잠시 비활성화
        GameStartBtn.interactable = false;

        // 마스터 서버에 접속 중이라면
        if (PhotonNetwork.IsConnected)
        {
            //룸 접속 실행
            connectionInfoText.text = "룸에 접속...";
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            // 마스터 서버에 접속 중이 아니라면 마스터 서버에 접속 시도
            connectionInfoText.text = "오프라인: 마스터 서버와 연결되지 않음\n접속 재시도 중...";
            // 마스터 서버로의 재접속 시도
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // (빈 방이 없어) 랜덤 룸 참가에 실패한 경우 자동 실행
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // 접속 상태 표시
        connectionInfoText.text = "빈 방이 없음, 새로운 방 생성...";
        // 최대 4명을 수용 가능한 빈 방 생성
        string roomname = Random.Range(0, 100).ToString();
        PhotonNetwork.CreateRoom(roomname, new RoomOptions { MaxPlayers = 50 });
    }
        
    public override void OnJoinedRoom()
    {
        
        roomPanel.SetActive(true);
        // 접속 상태 표시
        connectionInfoText.text = "방 참가 성공" +" " +  PhotonNetwork.CurrentRoom.Name+" " +
            PhotonNetwork.LocalPlayer.ActorNumber;
        Roomplayer.SetActive(true);
        
        //PhotonNetwork.Instantiate("select_red", new Vector3(-300, 0, 0), Quaternion.identity);
        //PhotonNetwork.Instantiate("select_red", new Vector3(-100, 0, 0), Quaternion.identity);
        //PhotonNetwork.Instantiate("select_blue", new Vector3(100, 0, 0), Quaternion.identity);
        //PhotonNetwork.Instantiate("select_blue", new Vector3(300, 0, 0), Quaternion.identity);
        // 모든 룸 참가자가 Main 씬을 로드하게 함
        //PhotonNetwork.LoadLevel("MainScene");


    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        connectionInfoText.text = PhotonNetwork.CurrentRoom.Name+" / "+PhotonNetwork.CurrentRoom.PlayerCount;
    }

    void Update()
    {
        
    }
    
    

    public bool getisUserSelected()
    {
        isUserSelected = false;
        nickname = n_inputfield.text;
        foreach (Text t in n_Texts)
        {
            if (t.text == nickname)
            {
                isUserSelected = true;
            }
        }
        return isUserSelected;
    }
}
