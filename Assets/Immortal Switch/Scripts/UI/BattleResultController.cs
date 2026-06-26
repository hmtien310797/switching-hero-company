using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class BattleResultController : MonoBehaviour
    {
        [SerializeField] Button confirmBtn;
        [SerializeField] Button autoNextBtn;
        [SerializeField] GameObject innerSelected;
        
        private bool isAutoActived = false;

        private void Awake()
        {
            //GameEventManager.Subscribe(GameEvents.OnStageCleared, ()=>SetBattleResultState(true));
        }
        
        void Start()
        {
            confirmBtn?.onClick.AddListener(OnConfirmBtnClick);
            gameObject.SetActive(false);
            autoNextBtn?.onClick.AddListener(AutoNextCallback);
        }
        
        private void OnConfirmBtnClick()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            //GameEventManager.Trigger(GameEvents.OnNextStageButtonClicked);
            SetBattleResultState(false);
        }

        private void SetBattleResultState(bool isEnable)
        {
            gameObject.SetActive(isEnable);

            if(isEnable && isAutoActived)
            {
                Invoke(nameof(OnConfirmBtnClick), 3f);
            }
        }

        private void AutoNextCallback()
        {
            isAutoActived = innerSelected?.activeInHierarchy ?? false;
            if (innerSelected)
            {
                isAutoActived = !isAutoActived;
                innerSelected.SetActive(isAutoActived);
            }
        }
    }
}
