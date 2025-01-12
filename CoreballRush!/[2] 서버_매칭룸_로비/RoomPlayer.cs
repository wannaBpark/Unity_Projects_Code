using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
public class RoomPlayer : MonoBehaviourPunCallbacks
{
    public Button[] buttons;
    public Button gameStartBtn;
    public Text[] texts;
    public Image[] images;
    public InputField n_inputfield;

    public bool[] isClicked;
    public int myClick;
    public string tag;
    public string nickname = null;
    public string[] defaultText;
    // Start is called before the first frame update

    private void Awake()
    {
        n_inputfield = GameObject.FindGameObjectWithTag("nickname_Input").GetComponent<InputField>();

        gameStartBtn = GameObject.FindGameObjectWithTag("StartButton").GetComponent<Button>();
        gameStartBtn.onClick.AddListener(() => onClickGameStart());
        for (int i = 0; i < 4; i++)
        {
            buttons[i] = GameObject.Find("SendableUI").transform.GetChild(i).GetComponent<Button>();
            buttons[i].onClick.AddListener(() => onClick());

            texts[i] = buttons[i].transform.GetChild(0).GetComponent<Text>();
            images[i] = buttons[i].GetComponent<Image>();

            defaultText[i] = texts[i].text;
        }
    }
    void Start()
    {
        isClicked = new bool[] { false, false, false, false };
        myClick = -1;

        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                photonView.RPC("send", RpcTarget.All, "Hi");
            }
        

        //Debug.Log(EventSystem.current.currentSelectedGameObject.name);
    }

    [PunRPC]
    public void send(string i)
    {
        Debug.Log(i);
        n_inputfield.text = i;

    }

    public void onClick()
    {
        int index = 0;
        if (photonView.IsMine/* || PhotonNetwork.IsMasterClient*/)
        {
            nickname = n_inputfield.text;
            //n_inputfield.text = nickname + "이 선택함";
            string name = EventSystem.current.currentSelectedGameObject.name;
            for (int i = 0; i < buttons.Length; i++)
            {
                Debug.Log("asdf" + name);
                index = (buttons[i].name == name) ? i : index;
            }
            Debug.Log(index + " " + name);
            //photonView.RPC("sendNameColor", RpcTarget.All, index,nickname);

            photonView.RPC("sendNameColor", RpcTarget.AllBuffered, index, nickname);

        }
        //Button btn = sendUI.transform.FindChild(name).GetComponent<Button>();
        //btn.GetComponent<Image>().color = Color.gray;
        //Debug.Log("asdf" + btn.name);

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        //if (PhotonNetwork.IsMasterClient)
        //{
        //    if (PhotonNetwork.CurrentRoom.PlayerCount == 4)
        //    {
        //        PhotonNetwork.LoadLevel("MainScene");
        //    }
        //}
    }
    //[PunRPC]
    //public void ApplyCurrentState()
    //{
    //    if()
    //}
    [PunRPC]
    public void sendNameColor(int index, string nickname)
    {
        Debug.Log(index + " " + nickname + " " + buttons.Length);
        Debug.Log("send함수 호출됨" + buttons[index].name);

        Color currentColor = images[index].color;

        if (!isClicked[index] && myClick == -1)
        {
            currentColor.a += 20;
            images[index].color = currentColor;
            texts[index].text = nickname;

            isClicked[index] = true;
            myClick = index;
            tag = (myClick == 0 || myClick == 1) ? "RED":"BLUE";
        }
        else if (isClicked[index] && myClick == index)
        {
            currentColor.a -= 20;
            images[index].color = currentColor;
            texts[index].text = defaultText[index];

            isClicked[index] = false;
            myClick = -1;
        }
        else if (isClicked[index] && myClick != index)
        {
            n_inputfield.text = "다른 사람이 이미 선택한 버튼입니다.";
        }
    }

    [PunRPC]
    public void onClickGameStart()
    {
        //PhotonNetwork.IsMessageQueueRunning = false;
        //SceneManager.LoadScene("MainScene");

        //if (PhotonNetwork.IsMasterClient)
        photonView.RPC("onloadScene", RpcTarget.AllBufferedViaServer);
        
        
    }

    [PunRPC]
    public void onloadScene()
    {
        PhotonNetwork.IsMessageQueueRunning = false;
        SceneManager.LoadScene("MainScene");
    }
}
