using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        private const string GameDatabaseLabel = "game_database";

        private readonly Dictionary<Type, DatabaseFieldBinding>
            gameDatabaseBindings = new();

        private AsyncOperationHandle<IList<ScriptableObject>>
            gameDatabaseHandle;

        private bool isGameDatabaseLoaded;

        private sealed class DatabaseFieldBinding
        {
            public FieldInfo Field;
            public bool Required;
            public bool Assigned;
        }

        /// <summary>
        /// Chỉ scan reflection một lần lúc khởi tạo.
        /// Không chạy mỗi frame.
        /// </summary>
        private void BuildGameDatabaseBindings()
        {
            gameDatabaseBindings.Clear();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo[] fields = GetType().GetFields(flags);

            foreach (FieldInfo field in fields)
            {
                DatabaseBindingAttribute attribute =
                    field.GetCustomAttribute<DatabaseBindingAttribute>();

                if (attribute == null)
                {
                    continue;
                }

                if (!typeof(ScriptableObject).IsAssignableFrom(field.FieldType))
                {
                    Debug.LogError(
                        $"[DatabaseManager] Field {field.Name} có " +
                        $"[DatabaseBinding] nhưng không kế thừa ScriptableObject."
                    );

                    continue;
                }

                if (gameDatabaseBindings.ContainsKey(field.FieldType))
                {
                    Debug.LogError(
                        $"[DatabaseManager] Có nhiều field binding cùng type: " +
                        $"{field.FieldType.Name}. Attribute binding chỉ hỗ trợ " +
                        $"một database singleton cho mỗi type."
                    );

                    continue;
                }

                gameDatabaseBindings.Add(
                    field.FieldType,
                    new DatabaseFieldBinding
                    {
                        Field = field,
                        Required = attribute.Required,
                        Assigned = false,
                    }
                );
            }
        }

        private async UniTask LoadGameDatabaseAsync()
        {
            if (isGameDatabaseLoaded)
            {
                return;
            }

            BuildGameDatabaseBindings();

            gameDatabaseHandle =
                Addressables.LoadAssetsAsync<ScriptableObject>(
                    GameDatabaseLabel,
                    null
                );

            await gameDatabaseHandle.ToUniTask();

            if (gameDatabaseHandle.Status !=
                AsyncOperationStatus.Succeeded)
            {
                Debug.LogError(
                    $"[DatabaseManager] Load label " +
                    $"{GameDatabaseLabel} failed."
                );

                return;
            }

            foreach (ScriptableObject database
                     in gameDatabaseHandle.Result)
            {
                BindGameDatabase(database);
            }

            ValidateRequiredGameDatabases();

            isGameDatabaseLoaded = true;

            Debug.Log(
                $"[DatabaseManager] Loaded " +
                $"{gameDatabaseHandle.Result.Count} assets from " +
                $"{GameDatabaseLabel}."
            );
        }

        private void BindGameDatabase(ScriptableObject database)
        {
            if (database == null)
            {
                return;
            }

            Type databaseType = database.GetType();

            if (!gameDatabaseBindings.TryGetValue(
                    databaseType,
                    out DatabaseFieldBinding binding))
            {
                Debug.LogWarning(
                    $"[DatabaseManager] Không có [DatabaseBinding] cho " +
                    $"{databaseType.Name}, asset: {database.name}."
                );

                return;
            }

            if (binding.Assigned)
            {
                Debug.LogError(
                    $"[DatabaseManager] Có nhiều asset cùng type " +
                    $"{databaseType.Name} trong label {GameDatabaseLabel}. " +
                    $"Asset dư: {database.name}."
                );

                return;
            }

            binding.Field.SetValue(this, database);
            binding.Assigned = true;

            Debug.Log(
                $"[DatabaseManager] Bound {database.name} " +
                $"→ {binding.Field.Name}."
            );
        }

        private void ValidateRequiredGameDatabases()
        {
            foreach (KeyValuePair<Type, DatabaseFieldBinding> pair
                     in gameDatabaseBindings)
            {
                DatabaseFieldBinding binding = pair.Value;

                if (binding.Required && !binding.Assigned)
                {
                    Debug.LogError(
                        $"[DatabaseManager] Missing required database: " +
                        $"{pair.Key.Name}, field: {binding.Field.Name}."
                    );
                }
            }
        }

        private void ReleaseGameDatabase()
        {
            isGameDatabaseLoaded = false;
            gameDatabaseBindings.Clear();

            if (gameDatabaseHandle.IsValid())
            {
                Addressables.Release(gameDatabaseHandle);
            }
        }
    }
}