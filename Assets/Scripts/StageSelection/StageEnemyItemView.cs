using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageEnemyItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image elementIcon;
        [SerializeField] private TMP_Text txtBoss;
        [SerializeField] private GameObject overlay;

        private bool isBoss;

        private void Awake()
        {
            overlay.SetActive(false);
        }

        public void Bind(int enemyId, Sprite enemyIcon = null, bool isBoss = false)
        {
            if (icon != null)
            {
                icon.sprite = enemyIcon;
                icon.gameObject.SetActive(enemyIcon != null);
            }

            if (txtBoss != null && isBoss)
                txtBoss.text = "Trùm";
        }
    }
}