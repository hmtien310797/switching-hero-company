using System;
using System.Collections.Generic;

public static class GameEventManager
{
    private static Dictionary<string, Action> eventTable = new();
    private static Dictionary<string, Action<object>> eventTableWithParam = new();

    private static Dictionary<Delegate, Delegate> wrapperMap = new();

    #region Event Không Có Param

    public static void Subscribe(string eventName, Action listener)
    {
        if (eventTable.ContainsKey(eventName))
            eventTable[eventName] += listener;
        else
            eventTable[eventName] = listener;
    }

    public static void Unsubscribe(string eventName, Action listener)
    {
        if (!eventTable.ContainsKey(eventName)) return;

        eventTable[eventName] -= listener;

        if (eventTable[eventName] == null)
            eventTable.Remove(eventName);
    }

    public static void Trigger(string eventName)
    {
        if (eventTable.TryGetValue(eventName, out var action))
            action?.Invoke();
    }

    #endregion

    #region Event Có Param

    public static void Subscribe<T>(string eventName, Action<T> listener)
    {
        Action<object> wrapper = value => listener((T)value);

        if (!wrapperMap.ContainsKey(listener))
            wrapperMap[listener] = wrapper;

        if (eventTableWithParam.ContainsKey(eventName))
            eventTableWithParam[eventName] += (Action<object>)wrapperMap[listener];
        else
            eventTableWithParam[eventName] = (Action<object>)wrapperMap[listener];
    }

    public static void Unsubscribe<T>(string eventName, Action<T> listener)
    {
        if (!wrapperMap.ContainsKey(listener)) return;
        if (!eventTableWithParam.ContainsKey(eventName)) return;

        eventTableWithParam[eventName] -= (Action<object>)wrapperMap[listener];

        if (eventTableWithParam[eventName] == null)
            eventTableWithParam.Remove(eventName);

        wrapperMap.Remove(listener);
    }

    public static void Trigger<T>(string eventName, T value)
    {
        if (eventTableWithParam.TryGetValue(eventName, out var action))
            action?.Invoke(value);
    }

    #endregion
}