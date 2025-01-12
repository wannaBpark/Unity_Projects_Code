using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public struct EffectTime
{
    public float activeTime;
    public float refreshRate;
    public float destroyDelay;
    public EffectTime(float activeTime, float refreshRate, float destroyDelay)
    {
        this.activeTime = activeTime;
        this.refreshRate = refreshRate;
        this.destroyDelay = destroyDelay;  
    }
}
#region EffectClass
public class Effect
{
    public EffectType eType;
    public string path;
    public Vector3 pos;
    public Vector3 offset;
    public EffectTime eTime;
    public Effect(EffectType eType, string path, Vector3 pos, Vector3 offset, EffectTime eTime) 
    {
        this.eType = eType;
        this.path = path;
        this.pos = pos;
        this.offset = offset;
        this.eTime = eTime;
    }
}
#endregion

#region DashEffectClass
public class DashEffect : Effect
{
    public DashEffect(EffectType eType, string path, Vector3 pos, Vector3 offset, EffectTime eTime) : base(eType, path, pos, offset, eTime)
    {

    }
}
#endregion

public class EffectManager
{
    #region Settings
    static EffectTime eDashTime = new EffectTime( 2f, 0.2f, 3f);
    static EffectTime eBStunnedTime = new EffectTime(2f, 0.2f, 2f);
    static EffectTime eBDiveTime = new EffectTime(2f, 0.2f, 2f);
    static EffectTime eBScreamTime = new EffectTime(2f, 0.2f, 2f);
    public DashEffect DashEffect = new DashEffect(EffectType.Dash, "Effect/Dash", 
        new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f,1.0f, 1.0f), eDashTime);

    private static Effect ShieldEffect;
    private static Effect bStunnedEffect = new Effect(EffectType.bStunned, "Effect/bStunned",
        Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f), eBStunnedTime);
    private static Effect bDiveEffect = new Effect(EffectType.bDive, "Effect/bDive",
        Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f), eBDiveTime);
    private static Effect bScreamEffect = new Effect(EffectType.bDive, "Effect/bScream",
        Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f), eBScreamTime);

    private float shaderVarRate = 0.1f;
    private float shaderVarRefreshRate = 0.05f;
    #endregion    

    static string getPath(string _effectName)
    {
        return "Effect/" + _effectName;
    }

    public GameObject PlayEffectByName(string name, Transform parent)
    {
        Effect e = FindEffectByName(name);
        string path = getPath(name);
        GameObject go = null;

        switch (e.eType)
        {
            case EffectType.Dash:
                //ActivateDash(parent);
                break;
            case EffectType.Shield:
            case EffectType.Attack:
            case EffectType.bStunned:
            case EffectType.bDive:
            case EffectType.bScream:
                go = Managers.Resource.Instantiate(path, parent);
                go.transform.parent = null;
                break;
        }
        return go;
    }

    public void PlayDashEffect(Transform tr)
    {
        CoroutineHelper.StartCoroutine(ActivateDash(tr));
    }

    private static Effect FindEffectByName(string name)
    {
        Effect effect = null;
        switch (name)
        {
            case "Dash":
                //effect = DashEffect;
                break;
            case "Shield":
                effect = ShieldEffect;
                break;
            case "bStunned":
                effect = bStunnedEffect;
                break;
            case "bDive":
                effect = bDiveEffect;
                break;
            case "bScream":
                effect = bScreamEffect;
                break;
        }
        return effect;
    }

    IEnumerator ActivateDash(Transform tr)
    {
        SkinnedMeshRenderer[] skinnedMeshRenderers = Managers.Object.myPlayerSkinnedMeshRenderers;
        float activeTime = DashEffect.eTime.activeTime;

        if (skinnedMeshRenderers != null) Debug.Log(skinnedMeshRenderers.Length+"\n");

        while (activeTime > 0f)
        {
            activeTime -= DashEffect.eTime.refreshRate;

            for (int i = 0; i < skinnedMeshRenderers.Length; ++i)
            {
                GameObject go = new GameObject();
                go.transform.SetPositionAndRotation(tr.position, tr.rotation);

                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                MeshFilter mf = go.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);

                mf.mesh = mesh;
                //mr.material = Managers.Object.MyPlayer.dashMat;

                CoroutineHelper.StartCoroutine(AnimateMaterialFloat(mr.material, 0, shaderVarRate, shaderVarRefreshRate));

                Object.Destroy(go, DashEffect.eTime.destroyDelay);
            }
            yield return new WaitForSeconds(DashEffect.eTime.refreshRate);
        }
        //Managers.Object.MyPlayer.isDashActive = false;
    }


    IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat("_Alpha");

        while(valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat("_Alpha", valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
