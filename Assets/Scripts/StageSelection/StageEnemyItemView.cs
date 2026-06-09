using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageEnemyItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image elementIcon;
        [SerializeField] private TMP_Text enemyIdText;
        [SerializeField] private GameObject bossMark;

        private bool isBoss;

        public void Bind(int enemyId, Sprite enemyIcon = null, bool isBoss = false)
        {
            if (icon != null)
            {
                icon.sprite = enemyIcon;
                icon.gameObject.SetActive(enemyIcon != null);
            }

            if (enemyIdText != null)
                enemyIdText.text = enemyId.ToString();

            if (bossMark != null)
                bossMark.SetActive(isBoss);
        }
    }
}