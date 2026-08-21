using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwitchingHero.BuildTool
{
    /// <summary>
    /// Cấu hình cho Tool Build tùy chọn (Tools/Switching Hero/Build Tool). Lưu thành ScriptableObject
    /// nên có thể commit theo project. Quan trọng: mapping group remote/local CHỈ áp dụng cho
    /// build Addressable = Remote; khi build Local thì mọi group đều về local.
    /// </summary>
    [CreateAssetMenu(menuName = "SwitchingHero/Build Tool Settings", fileName = "BuildToolSettings")]
    public class BuildToolSettings : ScriptableObject
    {
        [Serializable]
        public class GroupRemoting
        {
            [Tooltip("Tên group trong AddressableAssetSettings (cùng tên hiển thị trong Addressables Groups).")]
            public string groupName;

            [Tooltip("true = build REMOTE sẽ đẩy group này lên CDN (Remote path, AppendHash, Prevent Update). " +
                     "false = các group còn lại để local (nhúng APK, NoHash). CHỈ áp dụng khi build Remote.")]
            public bool remote;

            public GroupRemoting() { }

            public GroupRemoting(string name, bool isRemote)
            {
                groupName = name;
                remote = isRemote;
            }
        }

        [Tooltip("Thư mục chứa file build APK/AAB (tương đối thư mục gốc project).")]
        public string outputPath = "Builds/Android/";

        [Tooltip("Mapping nhóm group remote/local — CHỈ áp dụng khi build Addressable = Remote.")]
        public List<GroupRemoting> groupRemoting = new List<GroupRemoting>();
    }
}