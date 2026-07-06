using Immortal_Switch.Scripts.UI;
using UnityEngine.Events;

public class BottomMainButton : NavigationButtonBase
{
    public void AddListener(UnityAction methodCall)
    {
        Button.onClick.AddListener(methodCall);
    }
}