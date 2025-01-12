using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameScene : BaseScene
{
    //GameObject _enemyTargetLocator;
    //CameraFollow _cameraFollow;

    public override void Init()
    {
        base.Init();

        SceneType = Define.Scene.TestScene;

        Screen.SetResolution(1920, 1080, false);

        // Find Objects
        //_enemyTargetLocator = GameObject.Find("EnemyTarget_Locator");
        //_cameraFollow = GameObject.Find("CameraTarget").GetComponent<CameraFollow>();

        // Map
        //Managers.Resource.Instantiate("Map/Map00");

        // Player
        GameObject pianoPlayer = Managers.Object.Add(Define.ObjectType.PianoPlayer, new Vector3(-2.0f, 0.0f, 0.0f));
        GameObject violinPlayer = Managers.Object.Add(Define.ObjectType.ViolinlPlayer, new Vector3(2.0f, 0.0f, 0.0f));

        // Enemy
        GameObject boss = Managers.Object.Add(Define.ObjectType.Boss, new Vector3(0.0f, 0.0f, 10.0f));

        // UI
        Managers.UI.ShowSceneUI<UI_Game>();

        // Camera
        CameraController cc = Camera.main.GetOrAddComponent<CameraController>();
        cc.ObjectsToTrack.Add(pianoPlayer.transform);
        cc.ObjectsToTrack.Add(violinPlayer.transform);
        cc.ObjectsToTrack.Add(boss.transform);
        cc.CameraOffset = new Vector3(0.0f, 70.0f, -65.0f);
        Camera.main.fieldOfView = 12.5f;

        // BGM Intro
        Managers.Note.PlayMusic(Define.MusicType.IntroFirst);

        // Set Camera
        //EnemyLockOn enemyLockOn = player.GetComponent<EnemyLockOn>();
        //enemyLockOn.EnemyTarget_Locator = _enemyTargetLocator.transform;
        //enemyLockOn.LockOnCanvas = Managers.UI.MakeWorldSpaceUI<UI_LockOnCanvas>().transform;
        //enemyLockOn.CamFollow = _cameraFollow;
        //_cameraFollow.Target = player.transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public override void Clear()
    {

    }
}
