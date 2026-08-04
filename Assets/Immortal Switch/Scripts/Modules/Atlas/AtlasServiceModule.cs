using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Modules.Atlas
{
    /// <summary>
    /// Registry tạo một atlas service dùng chung cho mỗi atlas key.
    /// </summary>
    public static class AtlasServiceModule
    {
        private static readonly Dictionary<string, AtlasServiceBase> Services =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Đăng ký atlas key hoặc lấy lại service đã được đăng ký trước đó.
        /// </summary>
        public static AtlasServiceBase Register(string atlasKey)
        {
            if (string.IsNullOrWhiteSpace(atlasKey))
            {
                throw new ArgumentException("Atlas key cannot be null or empty.", nameof(atlasKey));
            }

            if (Services.TryGetValue(atlasKey, out var service))
            {
                return service;
            }

            service = new RegisteredAtlasService(atlasKey);
            Services.Add(atlasKey, service);
            return service;
        }

        public static T Register<T>(
            string atlasKey,
            Func<string, T> factory
        ) where T : AtlasServiceBase
        {
            if (string.IsNullOrWhiteSpace(atlasKey))
            {
                throw new ArgumentException(
                    "Atlas key cannot be null or empty.",
                    nameof(atlasKey)
                );
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (Services.TryGetValue(atlasKey, out var existingService))
            {
                if (existingService is T typedService)
                {
                    return typedService;
                }

                throw new InvalidOperationException(
                    $"Atlas '{atlasKey}' đã được register bằng " +
                    $"{existingService.GetType().Name}, không thể đổi sang {typeof(T).Name}."
                );
            }

            var service = factory(atlasKey);
            Services.Add(atlasKey, service);
            return service;
        }

        private sealed class RegisteredAtlasService : AtlasServiceBase
        {
            public RegisteredAtlasService(string atlasKey) : base(atlasKey)
            {
            }
        }
    }
}