using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.Badword
{
    public class BadwordManager : Singleton<BadwordManager>
    {
        [SerializeField]
        private DynamicHeroesGlobalSpecificationsBadWordDatabase badwordDb;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            InitBadwords();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void InitBadwords()
        {
            var badwords = badwordDb.rows.Select(v => v.vi).ToArray();
            IllegalWordDetection.Init(badwords);
        }

        // Mirrors the code-point ranges checked server-side in validateDisplayName
        // (nakama/src/lib/utils.js) so the client rejects emoji before spending the rename fee.
        public static bool ContainsEmoji(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int cp = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                    ? char.ConvertToUtf32(text, i++)
                    : text[i];

                if ((cp >= 0x1F300 && cp <= 0x1FAFF) || // pictographs/emoticons/symbols/supplemental
                    (cp >= 0x2600 && cp <= 0x27BF) || // misc symbols & dingbats
                    (cp >= 0x2300 && cp <= 0x23FF) || // misc technical (watch, hourglass, etc.)
                    (cp >= 0x2B00 && cp <= 0x2BFF) || // misc symbols and arrows
                    (cp >= 0x1F1E6 && cp <= 0x1F1FF) || // regional indicators (flag letters)
                    cp == 0xFE0F || // variation selector-16 (emoji presentation)
                    cp == 0x200D) // zero-width joiner (combined emoji)
                {
                    return true;
                }
            }

            return false;
        }

        // Mirrors containsControlChar in nakama/src/lib/utils.js — rejects newline/tab/other
        // control chars before they ever reach the server, since a multiline TMP_InputField
        // lets the user type \n via Enter and .Trim() only strips it from the ends, not the middle.
        public static bool ContainsControlChar(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int cp = text[i];

                if ((cp >= 0x00 && cp <= 0x1F) ||
                    cp == 0x7F ||
                    cp == 0x2028 ||
                    cp == 0x2029)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckSpecial(string txt)
        {
            if (ContainsEmoji(txt))
            {
                UIManager.Instance.ShowToast("Tên không được chứa emoji");
                return true;
            }

            if (ContainsControlChar(txt))
            {
                UIManager.Instance.ShowToast("Tên chứa ký tự không hợp lệ");
                return true;
            }

            if (txt.Contains("\\"))
            {
                UIManager.Instance.ShowToast("Tên không được chứa ký tự \\");
                return true;
            }

            var badwordMatches = IllegalWordDetection.DetectIllegalWords(txt);

            if (badwordMatches.Count > 0)
            {
                foreach (var match in badwordMatches)
                {
                    var matchedWord = txt.Substring(match.Key, match.Value);

                    Debug.LogError(
                        $"\"{txt}\" bị chặn do khớp badword \"{matchedWord}\" tại vị trí {match.Key} (dài {match.Value})");
                }

                UIManager.Instance.ShowToast(LocalizationManager.GetText("ui_content_contains_prohibited_words"));
                return true;
            }

            return false;
        }
    }
}