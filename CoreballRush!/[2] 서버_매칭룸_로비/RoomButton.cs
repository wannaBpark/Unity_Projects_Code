using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;

public class RoomButton : MonoBehaviourPunCallbacks
{
    public GameObject RoomMgr;
    public Button btn;
    public Image img;
    public Text btnText;
    public InputField n_inputfield;

    public string nickname;
    public string defaultText;
    const float COLOR_GAP = 50;
    public bool isBtnClicked = false;
    public bool isUserSelected;
    public bool isConnected = false;
    public bool test = false;
    void Awake()
    {
        this.GetComponent<RoomButton>().enabled = true;

        //this.GetComponent<Transform>().SetParent(GameObject.Find("Canvas").GetComponent<Transform>(),false);
        n_inputfield = GameObject.FindGameObjectWithTag("nickname_Input").GetComponent<InputField>();

        RoomMgr = GameObject.FindGameObjectWithTag("RoomMgr");
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
        btnText = transform.GetChild(0).GetComponent<Text>();
        defaultText = n_inputfield.text;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
       

    }
    
    //[PunRPC]
    //public void sendColor(bool test)
    //{
    //    if(test)
    //        this.GetComponent<Image>().color = Color.gray;

    //}
    //[PunRPC]
    //public void OnClickRoomButton()
    //{
    //    if (photonView.IsMine) {
    //        photonView.RPC("sendColor",RpcTarget.All,true);
    //    }

    //}
    //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    //{
    //    if (stream.IsWriting) stream.SendNext(btnText.text);
    //    else btnText.text = (string)stream.ReceiveNext();
    //}

    [PunRPC]
    public void BtnClickRPC()
    {
        isUserSelected = RoomMgr.GetComponent<RoomManager>().getisUserSelected();
        if (!isBtnClicked && !isUserSelected)
        {
            nickname = n_inputfield.text;
            Color currentColor = img.color;
            currentColor.a += COLOR_GAP;
            setnickColor1(nickname, currentColor);
        }
        else if (isBtnClicked && isUserSelected)
        {
            Color currentColor = img.color;
            currentColor.a -= COLOR_GAP;
            setnickColor2(defaultText, currentColor);
        }
        
    }
    [PunRPC]
    public void setnickColor1(string nickname, Color currentColor)
    {
            #region ColorChange
            img.color = currentColor;
            #endregion

            #region TextChange

            btnText.text = nickname;
            #endregion

            isBtnClicked = true;
            Debug.Log(isBtnClicked + " " + btnText.text);
        
    }

    [PunRPC]
    public void setnickColor2(string defaultText, Color currentColor)
    {
            #region ColorChange

            img.color = currentColor;
            #endregion

            #region TextChange
            btnText.text = defaultText;
            #endregion

            isBtnClicked = false;
        
    }
    
    //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    //{
    //    //통신을 보내는 
    //    if (stream.IsWriting)
    //    {
    //        stream.SendNext(btnText);
    //        stream.SendNext(img);
    //        stream.SendNext(isBtnClicked);
    //        stream.SendNext(isUserSelected);

    //    }

    //    //클론이 통신을 받는 
    //    else
    //    {
    //        btnText = (Text)stream.ReceiveNext();
    //        img = (Image)stream.ReceiveNext();
    //        isBtnClicked = (bool)stream.ReceiveNext();
    //        isUserSelected = (bool)stream.ReceiveNext();
    //    }
    //}
}





