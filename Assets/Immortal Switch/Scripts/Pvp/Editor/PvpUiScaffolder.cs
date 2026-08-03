#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Immortal_Switch.Scripts.UI;
using Immortal_Switch.Scripts.Pvp.Views;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Editor
{
    /// <summary>
    /// Scaffolder tạo toàn bộ PvP UI prefab (Addressable address = class name, để UIManager load).
    /// Chạy menu <c>Tools/PvP/Scaffold PvP UI Prefabs</c> một lần trong editor. Mỗi prefab có cấu trúc
    /// children khớp với [SerializeField] đã khai báo trong từng PvpXxxView (xem header comment view).
    /// Style là scaffold tối giản (VerticalLayoutGroup + solid color) — dev restyle sau.
    /// </summary>
    public static class PvpUiScaffolder
    {
        private const string OutputFolder = "Assets/Immortal Switch/Addressable/UI/Pvp";

        private enum Kind { Label, Button, Input, Container, Item }

        [MenuItem("Tools/PvP/Scaffold PvP UI Prefabs")]
        public static void Scaffold()
        {
            EnsureFolder(OutputFolder);
            TMP_FontAsset font = ResolveFont();

            // 1) Build the shared item prefab first (list/card views reference it).
            GameObject itemPrefabGo = BuildItemPrefab(font);
            PvpOptionItemView itemPrefab = itemPrefabGo.GetComponent<PvpOptionItemView>();

            // 2) Build all view prefabs.
            var specs = BuildSpecs();
            var saved = new List<string> { AssetDatabase.GetAssetPath(itemPrefabGo) };

            foreach (var s in specs)
            {
                var prefab = BuildPrefab(s.Name, s.ViewType, s.Layer, s.Mode, s.Cache, s.NeedsItem, s.Refs, s.Arrays, itemPrefab, font);
                if (prefab != null) saved.Add(AssetDatabase.GetAssetPath(prefab));
            }

            // 3) Best-effort: mark Addressable (address = class/prefab name).
            TryMarkAddressable(saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PvP] Scaffolded {saved.Count} prefabs into {OutputFolder}. Mark them Addressable (address = prefab name) if not auto-marked.");
        }

        // ── Specs ──────────────────────────────────────────────────────────────────

        private struct RefSpec { public string field; public string child; public Kind kind; }
        private struct ArrSpec { public string field; public int count; public Kind kind; public string prefix; }
        private struct Spec
        {
            public string Name;
            public Type ViewType;
            public UILayer Layer;
            public MainLayerMode Mode;
            public bool Cache;
            public bool NeedsItem;
            public RefSpec[] Refs;
            public ArrSpec[] Arrays;
        }

        private static RefSpec R(string field, string child, Kind k) => new RefSpec { field = field, child = child, kind = k };
        private static ArrSpec A(string field, int count, Kind k, string prefix) => new ArrSpec { field = field, count = count, kind = k, prefix = prefix };

        private static List<Spec> BuildSpecs()
        {
            return new List<Spec>
            {
                new Spec { Name = nameof(PvpMainView), ViewType = typeof(PvpMainView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtSeason","txtSeason",Kind.Label), R("txtRank","txtRank",Kind.Label),
                    R("txtTickets","txtTickets",Kind.Label), R("txtTokens","txtTokens",Kind.Label),
                    R("txtFrontHero","txtFrontHero",Kind.Label), R("txtBackHero","txtBackHero",Kind.Label),
                    R("btnFindMatch","btnFindMatch",Kind.Button), R("btnFormation","btnFormation",Kind.Button),
                    R("btnBuffs","btnBuffs",Kind.Button), R("btnRankSeason","btnRankSeason",Kind.Button),
                    R("btnHistory","btnHistory",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpFormationSetupView), ViewType = typeof(PvpFormationSetupView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtTeamPower","txtTeamPower",Kind.Label),
                    R("txtFrontHero","txtFrontHero",Kind.Label), R("txtBackHero","txtBackHero",Kind.Label), R("txtValidation","txtValidation",Kind.Label),
                    R("btnFrontHero","btnFrontHero",Kind.Button), R("btnBackHero","btnBackHero",Kind.Button),
                    R("btnReset","btnReset",Kind.Button), R("btnSave","btnSave",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }, Arrays = new[] { A("frontBuffTexts",3,Kind.Label,"FrontBuff"), A("backBuffTexts",3,Kind.Label,"BackBuff") } },
                new Spec { Name = nameof(PvpHeroSelectionView), ViewType = typeof(PvpHeroSelectionView), Layer = UILayer.Popup, Mode = MainLayerMode.PageExclusive, Cache = true, NeedsItem = true, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtTitle","txtTitle",Kind.Label),
                    R("inpSearch","inpSearch",Kind.Input), R("listContainer","listContainer",Kind.Container),
                    R("tabAll","tabAll",Kind.Button), R("tabOwned","tabOwned",Kind.Button), R("tabPower","tabPower",Kind.Button),
                    R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpBuffLoadoutView), ViewType = typeof(PvpBuffLoadoutView), Layer = UILayer.Popup, Mode = MainLayerMode.PageExclusive, Cache = true, NeedsItem = true, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtTitle","txtTitle",Kind.Label),
                    R("txtTargetSlot","txtTargetSlot",Kind.Label), R("txtConflict","txtConflict",Kind.Label),
                    R("tabAll","tabAll",Kind.Button), R("tabCore","tabCore",Kind.Button), R("tabSupport","tabSupport",Kind.Button), R("tabTrigger","tabTrigger",Kind.Button),
                    R("listContainer","listContainer",Kind.Container),
                    R("btnEquip","btnEquip",Kind.Button), R("btnUnequip","btnUnequip",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpMatchingView), ViewType = typeof(PvpMatchingView), Layer = UILayer.Popup, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtStatus","txtStatus",Kind.Label),
                    R("txtRankRange","txtRankRange",Kind.Label), R("txtCurrentTeam","txtCurrentTeam",Kind.Label),
                    R("btnCancel","btnCancel",Kind.Button),
                }},
                new Spec { Name = nameof(PvpBattlePreviewView), ViewType = typeof(PvpBattlePreviewView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtYourPower","txtYourPower",Kind.Label), R("txtOpponentPower","txtOpponentPower",Kind.Label),
                    R("txtYourFront","txtYourFront",Kind.Label), R("txtYourBack","txtYourBack",Kind.Label),
                    R("txtOpponentFront","txtOpponentFront",Kind.Label), R("txtOpponentBack","txtOpponentBack",Kind.Label),
                    R("txtBattleId","txtBattleId",Kind.Label), R("txtSeed","txtSeed",Kind.Label), R("txtRules","txtRules",Kind.Label), R("txtTicketCost","txtTicketCost",Kind.Label),
                    R("btnStartBattle","btnStartBattle",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpBattleHudView), ViewType = typeof(PvpBattleHudView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtTimer","txtTimer",Kind.Label), R("txtAutoState","txtAutoState",Kind.Label),
                    R("txtYourFrontHp","txtYourFrontHp",Kind.Label), R("txtYourBackHp","txtYourBackHp",Kind.Label),
                    R("txtEnemyFrontHp","txtEnemyFrontHp",Kind.Label), R("txtEnemyBackHp","txtEnemyBackHp",Kind.Label),
                    R("txtActiveBuffs","txtActiveBuffs",Kind.Label),
                    R("btnResolve","btnResolve",Kind.Button), R("btnSurrender","btnSurrender",Kind.Button),
                    R("btnSpeed","btnSpeed",Kind.Button), R("btnAuto","btnAuto",Kind.Button),
                }},
                new Spec { Name = nameof(PvpBattleResultView), ViewType = typeof(PvpBattleResultView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtResult","txtResult",Kind.Label),
                    R("txtRankChange","txtRankChange",Kind.Label), R("txtReward","txtReward",Kind.Label),
                    R("txtHeroA","txtHeroA",Kind.Label), R("txtHeroB","txtHeroB",Kind.Label), R("txtSummary","txtSummary",Kind.Label),
                    R("btnContinue","btnContinue",Kind.Button), R("btnHistory","btnHistory",Kind.Button),
                }},
                new Spec { Name = nameof(PvpBuffGachaView), ViewType = typeof(PvpBuffGachaView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtPool","txtPool",Kind.Label), R("txtCost","txtCost",Kind.Label),
                    R("txtToken","txtToken",Kind.Label), R("txtPity","txtPity",Kind.Label), R("txtPending","txtPending",Kind.Label),
                    R("btnRoll","btnRoll",Kind.Button), R("btnRates","btnRates",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpGachaChoicesView), ViewType = typeof(PvpGachaChoicesView), Layer = UILayer.Popup, Mode = MainLayerMode.PageExclusive, Cache = true, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtRollId","txtRollId",Kind.Label), R("txtPity","txtPity",Kind.Label),
                    R("btnConfirm","btnConfirm",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }, Arrays = new[] { A("cards",3,Kind.Item,"Card") } },
                new Spec { Name = nameof(PvpBuffDetailView), ViewType = typeof(PvpBuffDetailView), Layer = UILayer.Popup, Mode = MainLayerMode.PageExclusive, Cache = true, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtName","txtName",Kind.Label), R("txtLevel","txtLevel",Kind.Label),
                    R("txtCurrent","txtCurrent",Kind.Label), R("txtNext","txtNext",Kind.Label), R("txtShards","txtShards",Kind.Label),
                    R("txtToken","txtToken",Kind.Label), R("txtReq","txtReq",Kind.Label), R("txtEquipState","txtEquipState",Kind.Label),
                    R("btnUpgrade","btnUpgrade",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpRankSeasonView), ViewType = typeof(PvpRankSeasonView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtTier","txtTier",Kind.Label), R("txtProgress","txtProgress",Kind.Label),
                    R("txtSeasonTime","txtSeasonTime",Kind.Label), R("txtRewards","txtRewards",Kind.Label),
                    R("btnClaim","btnClaim",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
                new Spec { Name = nameof(PvpBuffInventoryView), ViewType = typeof(PvpBuffInventoryView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, NeedsItem = true, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtToken","txtToken",Kind.Label),
                    R("btnGacha","btnGacha",Kind.Button), R("btnClose","btnClose",Kind.Button),
                    R("listContainer","listContainer",Kind.Container),
                }},
                new Spec { Name = nameof(PvpHistoryView), ViewType = typeof(PvpHistoryView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, NeedsItem = true, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("btnClose","btnClose",Kind.Button),
                    R("listContainer","listContainer",Kind.Container),
                }},
                new Spec { Name = nameof(PvpDebugView), ViewType = typeof(PvpDebugView), Layer = UILayer.Main, Mode = MainLayerMode.PageExclusive, Refs = new[] {
                    R("txtDevBadge","txtDevBadge",Kind.Label), R("txtOutput","txtOutput",Kind.Label),
                    R("btnAddToken","btnAddToken",Kind.Button), R("btnAddTickets","btnAddTickets",Kind.Button),
                    R("btnUnlockAll","btnUnlockAll",Kind.Button), R("btnRegenOpponents","btnRegenOpponents",Kind.Button),
                    R("btnSim1000","btnSim1000",Kind.Button), R("btnVerify","btnVerify",Kind.Button),
                    R("btnExport","btnExport",Kind.Button), R("btnClear","btnClear",Kind.Button), R("btnClose","btnClose",Kind.Button),
                }},
            };
        }

        // ── Prefab builders ──────────────────────────────────────────────────────────

        private static GameObject BuildItemPrefab(TMP_FontAsset font)
        {
            var go = new GameObject(nameof(PvpOptionItemView), typeof(RectTransform), typeof(Image), typeof(Button), typeof(PvpOptionItemView), typeof(VerticalLayoutGroup));
            var img = go.GetComponent<Image>(); img.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
            var vlg = go.GetComponent<VerticalLayoutGroup>(); vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var lbl = MakeLabel(go.transform, "Label", font); lbl.text = "Item";
            var icon = MakeImage(go.transform, "Icon");
            var sel = MakeImage(go.transform, "SelectedHighlight", new Color(1f, 0.85f, 0f, 0.5f)); sel.SetActive(false);
            var dis = MakeImage(go.transform, "DisabledOverlay", new Color(0, 0, 0, 0.6f)); dis.SetActive(false);

            var item = go.GetComponent<PvpOptionItemView>();
            item.button = btn;
            item.label = lbl;
            item.icon = icon.GetComponent<Image>();
            item.selectedHighlight = sel;
            item.disabledOverlay = dis;

            string path = OutputFolder + "/" + nameof(PvpOptionItemView) + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildPrefab(string name, Type viewType, UILayer layer, MainLayerMode mode,
            bool cache, bool needsItem, RefSpec[] refs, ArrSpec[] arrays, PvpOptionItemView itemPrefab, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 6; vlg.padding = new RectOffset(10, 10, 10, 10);

            Component view = go.AddComponent(viewType);
            var so = new SerializedObject(view);
            SetEnum(so, "Layer", (int)layer);
            SetEnum(so, "MainMode", (int)mode);
            if (cache) { var p = so.FindProperty("CacheOnClose"); if (p != null) p.boolValue = true; }

            if (refs != null)
            {
                foreach (var r in refs)
                {
                    UnityEngine.Object comp = r.kind switch
                    {
                        Kind.Label => MakeLabel(go.transform, r.child, font),
                        Kind.Button => MakeButton(go.transform, r.child, font),
                        Kind.Input => MakeInput(go.transform, r.child, font),
                        Kind.Container => MakeContainer(go.transform, r.child),
                        _ => null
                    };
                    if (!string.IsNullOrEmpty(r.field)) { var p = so.FindProperty(r.field); if (p != null) p.objectReferenceValue = comp; }
                }
            }

            if (arrays != null)
            {
                foreach (var a in arrays)
                {
                    var arr = so.FindProperty(a.field);
                    if (arr == null) continue;
                    arr.arraySize = a.count;
                    for (int i = 0; i < a.count; i++)
                    {
                        string childName = a.prefix + "_" + i;
                        UnityEngine.Object comp = a.kind switch
                        {
                            Kind.Item => MakeItemChild(go.transform, childName, font),
                            Kind.Label => MakeLabel(go.transform, childName, font),
                            _ => null
                        };
                        arr.GetArrayElementAtIndex(i).objectReferenceValue = comp;
                    }
                }
            }

            if (needsItem && itemPrefab != null)
            {
                var p = so.FindProperty("itemPrefab");
                if (p != null) p.objectReferenceValue = itemPrefab;
            }

            so.ApplyModifiedProperties();
            string path = OutputFolder + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        // ── UI element helpers ───────────────────────────────────────────────────────

        private static TextMeshProUGUI MakeLabel(Transform parent, string name, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = Pretty(name);
            t.fontSize = 28;
            t.alignment = TextAlignmentOptions.Left;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(0, 40);
            return t;
        }

        private static GameObject MakeImage(Transform parent, string name, Color color = default)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color == default ? Color.white : color;
            return go;
        }

        private static Button MakeButton(Transform parent, string name, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
            var lbl = MakeLabel(go.transform, "Label", font);
            lbl.text = Pretty(name);
            lbl.alignment = TextAlignmentOptions.Center;
            ((RectTransform)go.transform).sizeDelta = new Vector2(0, 56);
            return btn;
        }

        private static TMP_InputField MakeInput(Transform parent, string name, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var inp = go.GetComponent<TMP_InputField>();
            var ta = MakeLabel(go.transform, "Text", font); ta.text = "";
            var ph = MakeLabel(go.transform, "Placeholder", font); ph.text = "Search...";
            ph.color = new Color(1f, 1f, 1f, 0.4f);
            inp.textComponent = ta;
            inp.placeholder = ph;
            inp.textViewport = (RectTransform)ta.transform;
            ((RectTransform)go.transform).sizeDelta = new Vector2(0, 48);
            return inp;
        }

        private static RectTransform MakeContainer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandWidth = true; vlg.spacing = 4;
            var csf = go.GetComponent<ContentSizeFitter>(); csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return (RectTransform)go.transform;
        }

        private static PvpOptionItemView MakeItemChild(Transform parent, string name, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PvpOptionItemView), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
            var vlg = go.GetComponent<VerticalLayoutGroup>(); vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            var item = go.GetComponent<PvpOptionItemView>();
            item.button = btn;
            item.label = MakeLabel(go.transform, "Label", font);
            item.icon = MakeImage(go.transform, "Icon").GetComponent<Image>();
            var sel = MakeImage(go.transform, "SelectedHighlight", new Color(1f, 0.85f, 0f, 0.5f)); sel.SetActive(false);
            var dis = MakeImage(go.transform, "DisabledOverlay", new Color(0, 0, 0, 0.6f)); dis.SetActive(false);
            item.selectedHighlight = sel;
            item.disabledOverlay = dis;
            return item;
        }

        // ── Utils ───────────────────────────────────────────────────────────────────

        private static void SetEnum(SerializedObject so, string field, int value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.enumValueIndex = value;
        }

        private static string Pretty(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int i = 0;
            if (s.StartsWith("btn") || s.StartsWith("txt") || s.StartsWith("tab") || s.StartsWith("inp")) i = 3;
            var sb = new System.Text.StringBuilder();
            for (int j = i; j < s.Length; j++)
            {
                char c = s[j];
                if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
                sb.Append(c);
            }
            return sb.Length == 0 ? s : sb.ToString();
        }

        private static TMP_FontAsset ResolveFont()
        {
            var f = TMP_Settings.defaultFontAsset;
            if (f != null) return f;
            f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (f != null) return f;
            // Last resort: find any TMP_FontAsset in the project.
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids != null && guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void TryMarkAddressable(List<string> paths)
        {
            // Best-effort: set Addressable address = prefab name so UIManager (key = typeof(T).Name) loads it.
            // If the Addressables editor API isn't available/working, log instructions for manual marking.
            try
            {
                var settingsType = Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettings,Unity.Addressables.Editor");
                if (settingsType == null)
                {
                    Debug.LogWarning("[PvP] Addressables editor API not found — mark prefabs Addressable (address = prefab name) manually.");
                    return;
                }
                // Use reflection to avoid a hard compile dependency on the Addressables editor assembly.
                var defaultSettings = Type.GetType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject,Unity.Addressables.Editor");
                if (defaultSettings == null) return;
                var settingsProp = defaultSettings.GetProperty("Settings");
                object settings = settingsProp?.GetValue(null);
                if (settings == null) return;
                var groupProp = settingsType.GetProperty("DefaultGroup");
                object group = groupProp?.GetValue(settings);
                var createEntry = settingsType.GetMethod("CreateOrMoveEntry", new[] { typeof(string), group?.GetType() ?? typeof(object) });
                if (createEntry == null) return;
                foreach (var path in paths)
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    string address = Path.GetFileNameWithoutExtension(path);
                    var entry = createEntry.Invoke(settings, new object[] { guid, group });
                    entry?.GetType().GetProperty("address")?.SetValue(entry, address);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PvP] Auto-mark Addressable failed ({e.Message}). Mark prefabs Addressable (address = prefab name) manually via Addressables window.");
            }
        }
    }
}
#endif
