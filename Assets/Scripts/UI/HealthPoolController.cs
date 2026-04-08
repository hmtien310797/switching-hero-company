using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI
{
    public class HealthPoolController : MonoBehaviour
    {
        public static HealthPoolController Instance;

        [SerializeField] HealthTxtController healthTxtPrefab;

        private List<HealthTxtController> healthTxts = new List<HealthTxtController>();
        private int healthTxtInitNum = 5;

        private void Awake()
        {
            Instance = this;

            //InitHealthTxt();
        }

        private void InitHealthTxt()
        {
            for (int i = 0; i < healthTxtInitNum; i++)
            {
                CreateHealthTxt();
            }
        }

        private HealthTxtController CreateHealthTxt()
        {
            var ht = Instantiate(healthTxtPrefab, transform.position, Quaternion.identity);
            ht.gameObject.SetActive(false);
            healthTxts.Add(ht);

            return ht;
        }

        public HealthTxtController GetHealthTxt(float damage)
        {
            var ht = healthTxts?.FirstOrDefault(x => x.IsFree());
            if (ht == null) ht = CreateHealthTxt();

            return ht;
        }
    }
}
