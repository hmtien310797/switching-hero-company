using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] UIHeroBattleController uIHeroBattleController;
        [SerializeField] bool isIntutorial = true;
        [SerializeField] RectTransform centerRectTrans;
        [SerializeField] List<Button> targetBtns;
        [SerializeField] RectTransform fingerRectTrans;
        [SerializeField] Transform startPos;
        [SerializeField] Transform lastPos;

        private Vector2 oPos;
        private int currentIdx = 0;
        private int lastIdx = 0;
        private RectTransform curButtonSeleted;

        private Coroutine coMoveAndFlash = null;
        private bool isWaitingForAction = false;
        private bool isWaitingForChangeAction = false;

        public bool IsIntutorial => isIntutorial;
        public float moveSpeed = 500f;

        void Start()
        {
            lastIdx = targetBtns.Count;
            oPos = transform.position;
            StartTutorialAsync().Forget();
        }

        private async UniTaskVoid StartTutorialAsync()
        {
            while(IsIntutorial)
            {
                await UniTask.Delay(2000);
                if (isWaitingForAction || isWaitingForChangeAction) continue;

                switch(currentIdx)
                {
                    case 0:
                        if(uIHeroBattleController.IsSkillAvailable(HeroNameAction.Skill1Btn))
                        {
                            PointToButton(targetBtns[currentIdx]);
                        }
                        break;
                    case 1:
                        if (uIHeroBattleController.IsSkillAvailable(HeroNameAction.SwitchBtn))
                        {
                            PointToButton(targetBtns[currentIdx]);
                        }
                        break;
                    case 2:
                    case 3:
                    case 4:
                        PointToButton(targetBtns[currentIdx]);
                        break;
                }
            }
        }

        public void PointToButton(Button buttonInput)
        {
            if (currentIdx >= lastIdx) return;

            curButtonSeleted = buttonInput.GetComponent<RectTransform>();
            if (curButtonSeleted == null) return;

            isWaitingForAction = true;
            if (coMoveAndFlash != null)
            {
                StopCoroutine(coMoveAndFlash);
                coMoveAndFlash = null;
            }

            coMoveAndFlash = StartCoroutine(MoveAndFlash(curButtonSeleted));
            currentIdx++;
        }

        private IEnumerator MoveAndFlash(RectTransform buttonRect)
        {
            fingerRectTrans.gameObject.SetActive(false);
            // 1. Lấy vị trí đích (Button) trong không gian Canvas
            Vector3 targetPos = buttonRect.position;

            // 2. Di chuyển mượt mà đến nút bấm
            while (Vector3.Distance(centerRectTrans.position, targetPos) > 1f)
            {
                centerRectTrans.position = Vector3.MoveTowards(
                    centerRectTrans.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }

            centerRectTrans.position = targetPos;

            // 3. Hiệu ứng Flash (Nhấp nháy)
            Image indicatorImg = centerRectTrans.GetComponent<Image>();
            float flashDuration = 0.3f;

            fingerRectTrans.localPosition = startPos.localPosition;
            fingerRectTrans.gameObject.SetActive(true);
            while (true) 
            {
                // Mờ dần
                float t = 0;
                while (t < flashDuration)
                {
                    t += Time.deltaTime;
                    Color c = indicatorImg.color;
                    c.a = Mathf.Lerp(1f, 0.2f, t / flashDuration);
                    indicatorImg.color = c;
                    fingerRectTrans.localPosition = Vector3.Lerp(fingerRectTrans.localPosition, startPos.localPosition, t / flashDuration);
                    fingerRectTrans.localScale = Vector3.Lerp(fingerRectTrans.localScale, Vector3.one, t / flashDuration);
                    yield return null;
                }
                // Hiện rõ lại
                t = 0;
                while (t < flashDuration)
                {
                    t += Time.deltaTime;
                    Color c = indicatorImg.color;
                    c.a = Mathf.Lerp(0.2f, 1f, t / flashDuration);
                    indicatorImg.color = c;
                    fingerRectTrans.localPosition = Vector3.Lerp(fingerRectTrans.localPosition, lastPos.localPosition, t / flashDuration);
                    fingerRectTrans.localScale = Vector3.Lerp(fingerRectTrans.localScale, Vector3.one*1.05f, t / flashDuration);
                    yield return null;
                }
            }
        }

        public bool IsAbleActionCallback(Button inputBtn)
        {
            var inputRectTrans = inputBtn.GetComponent<RectTransform>();
            if (!isWaitingForAction || isWaitingForChangeAction || inputRectTrans != curButtonSeleted) return false;

            if(coMoveAndFlash != null)
            {
                StopCoroutine(coMoveAndFlash);
                coMoveAndFlash = null;
            }

            centerRectTrans.position = oPos;

            if (currentIdx >= lastIdx) isIntutorial = false;

            if(currentIdx == 1) StartCoroutine(NextTutorial(6));
            else if (currentIdx == 2) StartCoroutine(NextTutorial(6));
            else if (currentIdx == 3) StartCoroutine(NextTutorial(3));
            else if(currentIdx == 4) StartCoroutine(NextTutorial(3));
            return true;
        }

        private IEnumerator NextTutorial(float dur)
        {
            isWaitingForAction = false;
            isWaitingForChangeAction = true;
            yield return new WaitForSeconds(dur);
            isWaitingForChangeAction = false;
        }
    }
}
