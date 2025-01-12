using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager
{
    public PianoController PianoPlayer { get; set; }
    public ViolinController ViolinPlayer { get; set; }
    public BossController Boss { get; set; }
    public SkinnedMeshRenderer[] myPlayerSkinnedMeshRenderers;
    List<GameObject> _objects = new List<GameObject>();

    public GameObject Add(Define.ObjectType objectType, Vector3 pos)
    {
        GameObject go = null;
        if (objectType == Define.ObjectType.PianoPlayer)
        {
            go = Managers.Resource.Instantiate("Creature/PianoPlayer");
            PianoPlayer = go.GetComponent<PianoController>();
            PianoPlayer.DestPos = pos;
        }
        else if (objectType == Define.ObjectType.ViolinlPlayer)
        {
            go = Managers.Resource.Instantiate("Creature/ViolinPlayer");
            ViolinPlayer = go.GetComponent<ViolinController>();
            ViolinPlayer.DestPos = pos;
        }
        else if (objectType == Define.ObjectType.Boss)
        {
            go = Managers.Resource.Instantiate("Creature/Boss");
            Boss = go.GetComponent<BossController>();
            Boss.DestPos = pos;
        }
        _objects.Add(go);
        return go;
    }

    public void Add(GameObject go)
    {
        PianoController piano = go.GetComponent<PianoController>();
        if (piano != null)
            PianoPlayer = piano;


        ViolinController violin = go.GetComponent<ViolinController>();
        if (violin != null)
            ViolinPlayer = violin;

        _objects.Add(go);
    }

    public void Remove(GameObject go)
    {
        _objects.Remove(go);
    }

    public GameObject FindByTag(string tag)
    {
        foreach (GameObject go in _objects)
        {
            if (go.tag == tag)
                return go;
        }
        return null;
    }

    public void Clear()
    {
        _objects.Clear();
        PianoPlayer = null;
        ViolinPlayer = null;
    }
}
