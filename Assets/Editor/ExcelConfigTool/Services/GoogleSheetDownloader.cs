using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Editor.ExcelConfigTool.Services
{
    public class DownloadResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

    public static class GoogleSheetDownloader
    {
        public static async Task<List<DownloadResult>> DownloadAllAsync(
            List<string> urls,
            string outputFolder,
            bool forceOverwrite)
        {
            var downloaded = new List<DownloadResult>();

            if (urls == null ||
                urls.Count == 0)
            {
                return downloaded;
            }

            Directory.CreateDirectory(outputFolder);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/csv,text/plain,*/*");

            for (int i = 0; i < urls.Count; i++)
            {
                var url = urls[i].Trim();

                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                try
                {
                    Debug.Log($"[GoogleSheetDownloader] Downloading URL #{i}...");
                    using var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var fileName = ExtractFileNameFromResponse(response, i);
                    var filePath = Path.Combine(outputFolder, $"{fileName}.csv").Replace("\\", "/");

                    if (!forceOverwrite &&
                        File.Exists(filePath))
                    {
                        Debug.Log($"[GoogleSheetDownloader] Skip existing: {filePath}");
                        downloaded.Add(new DownloadResult { FilePath = filePath, FileName = fileName });
                        continue;
                    }

                    var bytes = await response.Content.ReadAsByteArrayAsync();

                    var content = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
                        ? System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
                        : System.Text.Encoding.UTF8.GetString(bytes);

                    if (content.TrimStart().StartsWith("<"))
                    {
                        Debug.LogError(
                            $"[GoogleSheetDownloader] Response is HTML, not CSV. URL may not be published correctly: {url}");

                        continue;
                    }

                    await File.WriteAllTextAsync(filePath, content);
                    downloaded.Add(new DownloadResult { FilePath = filePath, FileName = fileName });
                    Debug.Log($"[GoogleSheetDownloader] Saved: {filePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GoogleSheetDownloader] Failed URL #{i}: {e.Message}");
                }
            }

            return downloaded;
        }

        private static string ExtractFileNameFromResponse(HttpResponseMessage response, int index)
        {
            if (response.Content.Headers.ContentDisposition != null)
            {
                var disposition = response.Content.Headers.ContentDisposition;

                if (!string.IsNullOrEmpty(disposition.FileName))
                {
                    var name = disposition.FileName.Trim('"', '\'', ' ');

                    if (name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        name = name[..^4];
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return SanitizeFileName(name);
                    }
                }
            }

            return $"config_{index}";
        }

        private static string SanitizeFileName(string name)
        {
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
        }
    }
}