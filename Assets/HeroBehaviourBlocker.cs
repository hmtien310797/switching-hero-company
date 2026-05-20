using System.Collections.Generic;
using UnityEngine;

public class HeroBehaviourBlocker : MonoBehaviour
{
    [Header("Disable these when player is moving")]
    [SerializeField] private List<Behaviour> behavioursToDisable = new();

    private bool isBlocked;

    public void SetBlocked(bool blocked)
    {
        if (isBlocked == blocked)
            return;

        isBlocked = blocked;

        for (int i = 0; i < behavioursToDisable.Count; i++)
        {
            Behaviour behaviour = behavioursToDisable[i];

            if (behaviour == null)
                continue;

            behaviour.enabled = !blocked;
        }
    }
}