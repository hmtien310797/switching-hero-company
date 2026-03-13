using System.Collections.Generic;
using UnityEngine;

public class GroupFlashController : MonoBehaviour
{
    public static GroupFlashController Instance { get; private set; }

    [SerializeField] List<Transform> flashTrans;
    private int flashCount = 0;
    private int flashIdx = -1;

    void Awake()
    {
        Instance = this;
        flashCount = flashTrans.Count;
    }

    public Vector3 GetRandPos()
    {
        var idx = Random.Range(0, flashCount);
        if(flashIdx == idx)
        {
            idx = Random.Range(0, flashCount);
        }
        flashIdx = idx;

        return flashTrans[flashIdx].position;
    }

    public Vector3 GetPosByIdx(int idx)
    {
        if (idx < 0 || idx >= flashCount) return flashTrans[flashCount -1].position;

        return flashTrans[idx].position;
    }

    public Vector3 GetNearestPoint()
    {
        return GetPosByIdx(0); 
    }

    public Vector3 GetFarestPoint()
    {
        return GetPosByIdx(flashCount - 1); 
    }
}
