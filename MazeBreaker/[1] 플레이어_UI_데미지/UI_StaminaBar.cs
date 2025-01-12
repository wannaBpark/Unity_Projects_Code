//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using static Define;

//public class UI_StaminaBar : UI_Base
//{
//    enum GameObjects
//    {
//        StaminaBar,
//    }

//    [SerializeField]
//    Stat _stat;


//    public override void Init()
//    {
//        Bind<GameObject>(typeof(GameObjects));
//        _stat = transform.parent.GetComponent<CreatureController>().Stat;
//        Managers.UI.SetCanvas(this.gameObject, true);
//    }
//    // Update is called once per frame
//    void Update()
//    {
//        Transform parent = transform.parent;
//        //transform.position = parent.position + Vector3.up * (parent.GetComponent<Collider>().bounds.size.y);
//        //transform.rotation = Camera.main.transform.rotation;  // 빌보드

//        float ratio = _stat.Stamina / (float)_stat.MaxStamina;
//        SetHpRatio(ratio);  // 슬라이더 값 매 프레임마다 갱신
//    }

//    public void SetHpRatio(float ratio)
//    {
//        GetObject((int)GameObjects.StaminaBar).GetComponent<Slider>().value = ratio;
//    }

//    // Undo : Changed Start() into Awake()
//    void Start()
//    {
//        Init();
//    }
//}
