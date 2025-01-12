using UnityEngine;

public abstract class StatusEffect : MonoBehaviour // StatusEffect Inherits "MonoBehaviour"
{
    protected float _duration;
    protected GameObject _target;

    public float Duration { get { return _duration; }  set { _duration = value; }  }

    public StatusEffect(float duration, GameObject target)
    {
        this._duration = duration;
        this._target = target;
    }

    public abstract void ApplyEffect();
    public abstract void RemoveEffect();
}
