using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;

public class movJoystick : MonoBehaviourPunCallbacks, IDragHandler, IPointerUpHandler, IPointerDownHandler //터치 이벤트
{
    //UI backgroundImage에 들어갈 스크립트
    //backgroundImage 오브젝트의 자식으로 inImage 오브젝트 존재


    //사용할 이미지
    private Image bgImg;
    private Image joystickImg;

    private Vector3 inputVector; //이동 할 벡터 값


    void Start()
    {
        //이미지 할당
        bgImg = GetComponent<Image>();
        joystickImg = transform.GetChild(0).GetComponent<Image>();
    }

    //터치패드를 누르고 있을 때
    public virtual void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        
        //bgImg영역 터치 될때
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle (bgImg.rectTransform, eventData.position, eventData.pressEventCamera, out pos))
        {
            //터치된 자표 pos에 저장
            pos.x = (pos.x / bgImg.rectTransform.sizeDelta.x);
            pos.y = (pos.y / bgImg.rectTransform.sizeDelta.y);

            //pos.x&&pos.y 값을 0~1값으로 변환
            inputVector = new Vector3(pos.x * 2, pos.y * 2, 0);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            //joystickImg를 터치한 좌표로 이동
            joystickImg.rectTransform.anchoredPosition = new Vector3(inputVector.x * (bgImg.rectTransform.sizeDelta.x / 3), inputVector.y * (bgImg.rectTransform.sizeDelta.y / 3));

        }
    }

    //터치가 발생하고 있을때
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
        PlayerController.instance.anim.SetBool("running", true);
    }

    //터치가 중지했을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        //위치초기화
        inputVector = Vector3.zero;
        joystickImg.rectTransform.anchoredPosition = Vector3.zero;

        PlayerController.instance.anim.SetBool("running", false);
    }

    //Player가 x값을 받을 때 사용
    public float GetHorizontalValue()
    {
        return inputVector.x;
    }

    //Player가 y값을 받을 때 사용
    public float GetVerticalValue()
    {
        return inputVector.y;
    }

}
