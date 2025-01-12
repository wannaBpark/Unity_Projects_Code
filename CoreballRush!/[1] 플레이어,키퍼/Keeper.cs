using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public enum State
{
    idle, running, attacking, recon
}

public class Keeper : MonoBehaviourPunCallbacks
{
    private bool isDie = false;
    Animator anim;

    private Transform targetPos;
    private NavMeshAgent nv;

    const int reconPointNum = 6;
    private GameObject[] reconPoint = new GameObject[reconPointNum];
    private int reconNum = 0;
    public bool isArrive;

    private float moveSpeed;
    public State k_state;
    public LayerMask targetLayerMask = 9;

    private float lastAtkTime;
    private float delayAtkTime;
    private float damage;
    private float initHP;
    private float currentHP;
    private float tiaguFiredamage = 10f;

    public Image hpBar;
    public GameObject hitDamageText;
    public Transform hitTextPos;

    bool isAttaked;
    private bool tiagusFireDamaged = false;
    void Start()
    {
        anim = GetComponent<Animator>();
        nv = GetComponent<NavMeshAgent>();

        for (int i = 0; i < reconPointNum; i++)
        {
            reconPoint[i] = GameObject.Find("Map").transform.Find("keeperRecPos").GetChild(i).gameObject;
        }

        targetPos = null;
        moveSpeed = 100f;
        k_state = State.recon;
        nv.speed = 100f;
        delayAtkTime = 0.5f;
        damage = 0; //collisionStay에서 명시
        initHP = 1500;
        currentHP = 1500;
        isAttaked = false;
        isArrive = true;
    }

    void Update()
    {
        if (isArrive)
        {
            checkHPState();
            if (currentHP > 0 && !isDie)
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
                if (!isAttaked)
                {
                    SearchTarget();
                }
                if (k_state == State.running || k_state == State.attacking)
                {
                    TargetMove();
                    RotateToTarget(targetPos);
                }
                else if (k_state == State.recon)
                {
                    ReconMove();
                    RotateToTarget(reconPoint[reconNum].transform);
                }
            }
        }
    }

    void SearchTarget()
    {
        Collider[] t_cols = Physics.OverlapSphere(transform.position, 300f, targetLayerMask);
        if (t_cols.Length > 0)
        {
            targetPos = t_cols[0].transform;
            k_state = State.running;
            anim.SetInteger("state", 1);
        }
    }

    void RotateToTarget(Transform t_Pos)
    {
        if (k_state == State.attacking)
            transform.LookAt(t_Pos);
        else
        {
            transform.rotation = Quaternion.LookRotation(nv.velocity.normalized);
        }
    }
    void ReconMove()
    {
        nv.SetDestination(reconPoint[reconNum].transform.position);
        //transform.position = Vector3.MoveTowards(transform.position, reconPoint[reconNum].transform.position, moveSpeed*Time.deltaTime);

        float dist = Vector3.Distance(transform.position, reconPoint[reconNum].transform.position);
        anim.SetInteger("state", 1);
        if (dist < 50)
        {
            nv.velocity = Vector3.zero;

            reconNum = Random.Range(0, 6);
        }
    }

    void TargetMove()
    {
        float dist = Vector3.Distance(transform.position, targetPos.position);
        if (dist > 500f)
        {
            k_state = State.recon;
            anim.SetInteger("state", 1);
            if (!isAttaked)
                isAttaked = false;
        }
        else if (dist > 100f)
        {

            nv.SetDestination(targetPos.position);
            k_state = State.running;
            anim.SetInteger("state", 1);
            // transform.position = Vector3.MoveTowards(transform.position, targetPos.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            nv.velocity = Vector3.zero;
            k_state = State.attacking;
            anim.SetInteger("state", 2);
        }
    }
    private void checkHPState()
    {
        hpBar.fillAmount = currentHP / initHP;
        if (currentHP <= 0f && !isDie)
        {
            isDie = true;
            anim.SetBool("Die", true);
            nv.isStopped = true;
            //nv.SetDestination(transform.position);
            SoundManager.instance.Play("AIDead");
            GameObject.Find("GameMgr").GetComponent<GameManager>().isKeeper = false;
            isArrive = false;
            Destroy(this.gameObject,3f);
        }
    }

    public void onDamage(float damage, int playerNumber)
    {

        currentHP -= damage;
        if (PhotonNetwork.IsMasterClient)
            makeHitDamageText(damage);

        Collider[] t_cols = Physics.OverlapSphere(transform.position, 3500f, targetLayerMask);
        for (int i = 0; i < t_cols.Length; i++)
        {
            if (t_cols[i].GetComponent<PlayerController>().playerActorNum == playerNumber)
            {
                targetPos = t_cols[i].transform;
                isAttaked = true;
                k_state = State.running;
                anim.SetInteger("state", 1);
            }

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "BULLET")
        {
            bullet bt = other.GetComponent<bullet>();
            onDamage(bt.damage, bt.actorNumber);

           
        }
    }

    //공격 판정
    private void OnTriggerStay(Collider other)
    {
        if (isDie) return;
        if (k_state == State.attacking && other.transform.tag == "PLAYER")
        {
            GameObject player = other.gameObject;
            if (Time.time >= lastAtkTime + delayAtkTime && !player.GetComponent<PlayerController>().get_isDie())
            {
                if (player.GetComponent<PlayerController>().currentHP > 0)
                {
                    player.GetComponent<PlayerController>().onDamage(damage);
                    damage = 20f;
                    SoundManager.instance.Play("AttackedByAI");
                    SoundManager.instance.Play("RoaringCrowd");
                }
                lastAtkTime = Time.time;
            }
        }
        if (other.tag == "TiagusFire")
        {
            if (tiagusFireDamaged == false)
            {
                StartCoroutine(TiagusBurning());
                currentHP -= tiaguFiredamage; 
                if (PhotonNetwork.IsMasterClient)
                    makeHitDamageText(tiaguFiredamage);
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (isDie) return;
        if (k_state == State.attacking && other.transform.tag == "PLAYER")
        {
            k_state = State.running;
            anim.SetInteger("state", 1);
        }
    }
    
    [PunRPC]
    void makeHitDamageText(float damage)
    {
        if(PhotonNetwork.IsMasterClient)
            photonView.RPC("makeHitDamageText", RpcTarget.Others, damage);
        GameObject hdt = Instantiate(hitDamageText);
        hdt.transform.position = hitTextPos.position;
        hdt.GetComponent<HitDamageEffect>().damage = damage;
    }

    IEnumerator TiagusBurning()
    {
        tiagusFireDamaged = true;
        yield return new WaitForSeconds(0.25f);
        tiagusFireDamaged = false;

    }
}
