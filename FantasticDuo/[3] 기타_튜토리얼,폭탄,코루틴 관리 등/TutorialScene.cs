using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialScene : MonoBehaviour
{
    public Sprite[] sprites;

    private Button btn1, btn2, btn3, btn4, btn5;
    private Button btnSkip;
    private Image img;
    void Start()
    {
        img = GameObject.Find("ImgChangable").GetComponent<Image>();

        btn1 = GameObject.Find("Button_Control").GetComponent<Button>();
        btn2 = GameObject.Find("Button_Notes").GetComponent<Button>();
        btn3 = GameObject.Find("Button_MusicType").GetComponent<Button>();
        btn4 = GameObject.Find("Button_Combo").GetComponent<Button>();
        btn5 = GameObject.Find("Button_Groggy").GetComponent<Button>();

        btnSkip = GameObject.Find("Button_Skip").GetComponent<Button>();

        btn1.onClick.AddListener(ImgLoadControl);
        btn2.onClick.AddListener(ImgLoadNotes);
        btn3.onClick.AddListener(ImgLoadMusictype);
        btn4.onClick.AddListener(ImgLoadCombo);
        btn5.onClick.AddListener(ImgLoadGroggy);

        btnSkip.onClick.AddListener(LoadNextScene);
    }

    void LoadNextScene()
    {
        Managers.Scene.LoadScene(Define.Scene.MainScene);
    }

    void ImgLoadControl()
    {
        img.sprite = sprites[0];
    }
    void ImgLoadNotes()
    {
        img.sprite = sprites[1];
    }
    void ImgLoadMusictype()
    {
        img.sprite = sprites[2];
    }
    void ImgLoadCombo()
    {
        img.sprite = sprites[3];

    }
    void ImgLoadGroggy()
    {
        img.sprite = sprites[4];
    }
}
