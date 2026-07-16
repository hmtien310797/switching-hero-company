using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor.ExcelConfigTool.Services
{
    public class DownloadRequest
    {
        public int SourceIndex { get; set; }
        public string Url { get; set; }
    }

    public class DownloadResult
    {
        public int SourceIndex { get; set; }
        public string Url { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class GoogleSheetDownloader
    {
        private const int MAX_ATTEMPTS = 3;
        private const int MAX_REDIRECTS = 10;
        private const int REQUEST_TIMEOUT_SECONDS = 30;

        public static async Task<List<DownloadResult>> DownloadAllAsync(
            IReadOnlyList<DownloadRequest> requests,
            string outputFolder,
            bool forceOverwrite,
            CancellationToken cancellationToken = default
        )
        {
            var results = new List<DownloadResult>();

            if (requests == null ||
                requests.Count == 0)
            {
                return results;
            }

            Directory.CreateDirectory(outputFolder);

            var claimedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var request in requests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var url = request.Url?.Trim();

                if (string.IsNullOrWhiteSpace(url))
                {
                    results.Add(Failed(request, "URL is empty."));
                    continue;
                }

                var result = await DownloadOneAsync(
                    request,
                    url,
                    outputFolder,
                    forceOverwrite,
                    claimedPaths,
                    cancellationToken
                );

                results.Add(result);
            }

            return results;
        }

        private static async Task<DownloadResult> DownloadOneAsync(
            DownloadRequest request,
            string url,
            string outputFolder,
            bool forceOverwrite,
            ISet<string> claimedPaths,
            CancellationToken cancellationToken
        )
        {
            Exception lastException = null;

            for (var attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    Debug.Log(
                        $"[GoogleSheetDownloader] Downloading entry #{request.SourceIndex + 1} " +
                        $"(attempt {attempt}/{MAX_ATTEMPTS}): {url}"
                    );

                    using var response = UnityWebRequest.Get(url);
                    response.redirectLimit = MAX_REDIRECTS;
                    response.timeout = REQUEST_TIMEOUT_SECONDS;
                    response.SetRequestHeader("Accept", "text/csv,text/plain,*/*");

                    await SendAsync(response, cancellationToken);

                    if (response.result == UnityWebRequest.Result.ConnectionError ||
                        response.result == UnityWebRequest.Result.DataProcessingError)
                    {
                        throw new IOException(
                            string.IsNullOrWhiteSpace(response.error)
                                ? "UnityWebRequest transport error."
                                : response.error
                        );
                    }

                    if (response.result == UnityWebRequest.Result.ProtocolError)
                    {
                        var statusError = BuildHttpError(response);

                        if (IsTransientStatus(response.responseCode) &&
                            attempt < MAX_ATTEMPTS)
                        {
                            Debug.LogWarning(
                                $"[GoogleSheetDownloader] Entry #{request.SourceIndex + 1}: " +
                                $"{statusError}. Retrying..."
                            );

                            await DelayBeforeRetry(attempt, cancellationToken);
                            continue;
                        }

                        Debug.LogError(
                            $"[GoogleSheetDownloader] Failed entry #{request.SourceIndex + 1}: " +
                            $"{statusError}\nURL: {url}"
                        );

                        return Failed(request, statusError);
                    }

                    if (!string.Equals(response.url, url, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log(
                            $"[GoogleSheetDownloader] Entry #{request.SourceIndex + 1} " +
                            $"followed redirect to: {response.url}"
                        );
                    }

                    var fileName = ExtractFileNameFromResponse(response, request.SourceIndex);
                    var filePath = Path.Combine(outputFolder, $"{fileName}.csv").Replace("\\", "/");

                    cancellationToken.ThrowIfCancellationRequested();
                    var bytes = response.downloadHandler.data;
                    cancellationToken.ThrowIfCancellationRequested();
                    var content = DecodeUtf8(bytes);

                    if (content.TrimStart().StartsWith("<"))
                    {
                        var htmlError =
                            "Response is HTML, not CSV. The sheet may require access or is not published.";

                        Debug.LogError(
                            $"[GoogleSheetDownloader] Failed entry #{request.SourceIndex + 1}: " +
                            $"{htmlError}\nURL: {url}"
                        );

                        return Failed(request, htmlError);
                    }

                    if (!claimedPaths.Add(filePath))
                    {
                        var duplicateError =
                            $"Another URL in this sync already maps to '{fileName}.csv'. " +
                            "The later file was not written.";

                        Debug.LogError(
                            $"[GoogleSheetDownloader] Failed entry #{request.SourceIndex + 1}: " +
                            $"{duplicateError}\nURL: {url}"
                        );

                        return Failed(request, duplicateError);
                    }

                    if (!forceOverwrite &&
                        File.Exists(filePath))
                    {
                        Debug.Log($"[GoogleSheetDownloader] Skip existing: {filePath}");
                    }
                    else
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await File.WriteAllTextAsync(filePath, content);
                        Debug.Log($"[GoogleSheetDownloader] Saved: {filePath}");
                    }

                    return new DownloadResult
                    {
                        SourceIndex = request.SourceIndex,
                        Url = url,
                        FilePath = filePath,
                        FileName = fileName,
                        IsSuccess = true,
                    };
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e) when (IsTransientException(e) && attempt < MAX_ATTEMPTS)
                {
                    lastException = e;

                    Debug.LogWarning(
                        $"[GoogleSheetDownloader] Entry #{request.SourceIndex + 1} " +
                        $"failed on attempt {attempt}: {GetInnermostMessage(e)}. Retrying..."
                    );

                    await DelayBeforeRetry(attempt, cancellationToken);
                }
                catch (Exception e)
                {
                    lastException = e;
                    break;
                }
            }

            var error = lastException == null
                ? "Download failed."
                : GetInnermostMessage(lastException);

            Debug.LogError(
                $"[GoogleSheetDownloader] Failed entry #{request.SourceIndex + 1}: {error}\n" +
                $"URL: {url}\n{lastException}"
            );

            return Failed(request, error);
        }

        private static DownloadResult Failed(DownloadRequest request, string error)
        {
            return new DownloadResult
            {
                SourceIndex = request.SourceIndex,
                Url = request.Url,
                IsSuccess = false,
                ErrorMessage = error,
            };
        }

        private static bool IsTransientStatus(long statusCode)
        {
            // Google occasionally returns a temporary 400 for its signed
            // googleusercontent redirect URL, so retry it with a fresh URL.
            return statusCode == 400 ||
                   statusCode == 408 ||
                   statusCode == 429 ||
                   statusCode >= 500;
        }

        private static async Task SendAsync(
            UnityWebRequest request,
            CancellationToken cancellationToken
        )
        {
            var operation = request.SendWebRequest();

            try
            {
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Yield();
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                if (!operation.isDone)
                {
                    request.Abort();
                }

                throw;
            }
        }

        private static bool IsTransientException(Exception exception)
        {
            return exception is IOException ||
                   exception is TaskCanceledException ||
                   exception is TimeoutException;
        }

        private static string BuildHttpError(UnityWebRequest response)
        {
            var error = $"HTTP {response.responseCode}";

            if (!string.IsNullOrWhiteSpace(response.error))
            {
                error += $" ({response.error})";
            }

            var responseText = response.downloadHandler?.text?.Trim();

            if (!string.IsNullOrWhiteSpace(responseText))
            {
                const int maxLength = 300;

                var snippet = responseText.Length <= maxLength
                    ? responseText
                    : responseText[..maxLength] + "...";

                error += $" - Response: {snippet.Replace('\r', ' ').Replace('\n', ' ')}";
            }

            return error;
        }

        private static string GetInnermostMessage(Exception exception)
        {
            var current = exception;

            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        private static Task DelayBeforeRetry(
            int attempt,
            CancellationToken cancellationToken
        )
        {
            return Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
        }

        private static string DecodeUtf8(byte[] bytes)
        {
            return bytes.Length >= 3 &&
                   bytes[0] == 0xEF &&
                   bytes[1] == 0xBB &&
                   bytes[2] == 0xBF
                ? System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
                : System.Text.Encoding.UTF8.GetString(bytes);
        }

        private static string ExtractFileNameFromResponse(
            UnityWebRequest response,
            int sourceIndex
        )
        {
            var disposition = response.GetResponseHeader("Content-Disposition");

            if (!string.IsNullOrWhiteSpace(disposition))
            {
                var rawFileName = GetDispositionValue(disposition, "filename*") ??
                                  GetDispositionValue(disposition, "filename");

                if (!string.IsNullOrWhiteSpace(rawFileName))
                {
                    var name = DecodeDispositionFileName(rawFileName);

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

            return $"config_{sourceIndex}";
        }

        private static string GetDispositionValue(string disposition, string key)
        {
            foreach (var segment in disposition.Split(';'))
            {
                var pair = segment.Trim();
                var separatorIndex = pair.IndexOf('=');

                if (separatorIndex <= 0 ||
                    !pair.Substring(0, separatorIndex)
                        .Trim()
                        .Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return pair.Substring(separatorIndex + 1).Trim();
            }

            return null;
        }

        private static string DecodeDispositionFileName(string rawFileName)
        {
            var value = rawFileName.Trim('"', '\'', ' ');
            var encodingSeparator = value.IndexOf("''", StringComparison.Ordinal);

            if (encodingSeparator >= 0)
            {
                value = value.Substring(encodingSeparator + 2);
            }

            try
            {
                return Uri.UnescapeDataString(value);
            }
            catch (UriFormatException)
            {
                return value;
            }
        }

        private static string SanitizeFileName(string name)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(name, (current, c) => current.Replace(c, '_'));
        }
    }
}