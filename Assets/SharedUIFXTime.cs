using UnityEngine;

public sealed class SharedUIFXTime : MonoBehaviour
{
    private static readonly int SharedFXTimeId =
        Shader.PropertyToID("_SharedFXTime");

    [SerializeField]
    private float speed = 1f;

    private float currentTime;

    private void Update()
    {
        currentTime += Time.unscaledDeltaTime * speed;
        Shader.SetGlobalFloat(SharedFXTimeId, currentTime);
    }

    public void ResetTimeValue()
    {
        currentTime = 0f;
        Shader.SetGlobalFloat(SharedFXTimeId, currentTime);
    }
}