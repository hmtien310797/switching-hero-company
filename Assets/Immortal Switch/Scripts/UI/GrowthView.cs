using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class GrowthView : AnimatedUIView
    {
        public Button testButton;

        private void Start()
        {
            testButton.onClick.AddListener(() => UIManager.Instance.OpenPopupAsync<TestStackableUIView>().Forget());
        }
    }
}