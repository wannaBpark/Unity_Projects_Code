using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_PlayerStatBar : UI_Base
{
    private float _HpRatio;
    private float _StaminaRatio;
    private float _MentalRatio;
    enum GameObjects
    {
        HPBar,
        StaminaBar,
        MentalBar,
    }

    //[SerializeField]
    Stat _stat;
   

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        //_stat = transform.parent.GetComponent<CreatureController>().Stat;
        _stat = transform.parent.GetComponent<PlayerControllerV1>().Stat;
        Managers.UI.SetCanvas(this.gameObject, true);
    }
    // Update is called once per frame
    void Update()
    {
        //Transform parent = transform.parent;
        //transform.position = parent.position + Vector3.up * (parent.GetComponent<Collider>().bounds.size.y);
        //transform.rotation = Camera.main.transform.rotation;  // 빌보드

        _HpRatio = _stat.Hp / (float)_stat.MaxHp;
        _StaminaRatio = _stat.Stamina / (float)_stat.MaxStamina;
        _MentalRatio = _stat.Mental / (float)_stat.MaxMental;

        SetSliderRatio();  // 슬라이더 값 매 프레임마다 갱신
    }

    public void SetSliderRatio()
    {
        //Debug.Log("cur Hp slider: " + _stat.Hp);
        GetObject((int)GameObjects.HPBar).GetComponent<Slider>().value = _HpRatio;
        GetObject((int)GameObjects.StaminaBar).GetComponent<Slider>().value = _StaminaRatio;
        GetObject((int)GameObjects.MentalBar).GetComponent<Slider>().value = _MentalRatio;
    }

    public void UpdateStatInfo()
    {

    }

    // Undo : Changed Start() into Awake()
    void Start()
    {
        Init();
    }
}
