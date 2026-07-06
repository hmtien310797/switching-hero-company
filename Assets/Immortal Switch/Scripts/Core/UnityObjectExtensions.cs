using UnityEngine;
public static class UnityObjectExtensions
{
    public static bool IsUnityAlive(this object target)
    {
        if (target == null)
            return false;

        if (target is Object unityObject)
            return unityObject != null;

        return true;
    }
}