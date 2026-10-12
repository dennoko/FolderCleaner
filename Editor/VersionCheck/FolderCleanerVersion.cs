using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace dennokoworks.FolderCleaner
{
    [InitializeOnLoad]
    internal static class FolderCleanerVersion
    {
        // version.json の GUID
        private const string VersionJsonGuid = "a2e584f7b6d14902a7b8563d12c801fc";
        private const string FallbackVersion = "1.0.0";
        private static string _currentCache = null;

        internal static string Current
        {
            get
            {
                if (string.IsNullOrEmpty(_currentCache))
                {
                    _currentCache = LoadLocalVersion();
                }
                return string.IsNullOrEmpty(_currentCache) ? FallbackVersion : _currentCache;
            }
        }

        // チェック先
        internal const string RepoOwner       = "dennoko";
        internal const string RepoName        = "FolderCleaner";
        // リポジトリの既定ブランチは master ("main" は存在しない)
        internal const string RepoBranch      = "master";
        internal const string VersionFilePath = "version.json";

        // SessionState キー
        internal const string VerCheckDoneKey    = "FolderCleaner_VerCheck_Done";
        internal const string VerCheckErrorKey   = "FolderCleaner_VerCheck_Error";
        internal const string VerCheckLatestKey  = "FolderCleaner_VerCheck_Latest";
        internal const string VerCheckUrlKey     = "FolderCleaner_VerCheck_Url";
        internal const string VerCheckMessageKey = "FolderCleaner_VerCheck_Message";

        // EditorPrefs キー
        internal const string VerCheckLastAttemptKey   = "FolderCleaner_VerCheck_LastAttemptUtc";
        internal const string VerCheckCachedLatestKey  = "FolderCleaner_VerCheck_CachedLatest";
        internal const string VerCheckCachedUrlKey     = "FolderCleaner_VerCheck_CachedUrl";
        internal const string VerCheckCachedMessageKey = "FolderCleaner_VerCheck_CachedMessage";

        private const double CheckIntervalHours = 6.0;

        static FolderCleanerVersion()
        {
            EditorApplication.delayCall += StartCheckBackgroundTask;
        }

        private static bool _checking;

        internal static void StartCheckBackgroundTask()
        {
            bool done  = SessionState.GetBool(VerCheckDoneKey, false);
            bool error = SessionState.GetBool(VerCheckErrorKey, false);
            if (done && !error) return;
            if (_checking) return;

            if (IsInCheckInterval())
            {
                ApplyCachedResult();
                return;
            }

            _checking = true;
            EditorPrefs.SetString(VerCheckLastAttemptKey, DateTime.UtcNow.ToString("o"));

            DennokoVersionChecker.CheckAsync(
                RepoOwner, RepoName, RepoBranch, VersionFilePath, Current, OnVersionChecked);
        }

        private static bool IsInCheckInterval()
        {
            var last = EditorPrefs.GetString(VerCheckLastAttemptKey, string.Empty);
            if (string.IsNullOrEmpty(last)) return false;
            if (!DateTime.TryParse(last, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var lastUtc)) return false;

            var elapsed = DateTime.UtcNow - lastUtc.ToUniversalTime();
            if (elapsed < TimeSpan.Zero) return false;

            return elapsed.TotalHours < CheckIntervalHours;
        }

        private static void ApplyCachedResult()
        {
            var latest = EditorPrefs.GetString(VerCheckCachedLatestKey, string.Empty);
            SessionState.SetBool(VerCheckDoneKey, true);
            SessionState.SetBool(VerCheckErrorKey, string.IsNullOrEmpty(latest));
            SessionState.SetString(VerCheckLatestKey, latest);
            SessionState.SetString(VerCheckUrlKey, EditorPrefs.GetString(VerCheckCachedUrlKey, string.Empty));
            SessionState.SetString(VerCheckMessageKey, EditorPrefs.GetString(VerCheckCachedMessageKey, string.Empty));

            RefreshOpenWindows();
        }

        internal static void ForceRecheck()
        {
            if (_checking) return;
            _currentCache = null;
            SessionState.SetBool(VerCheckDoneKey, false);
            SessionState.SetBool(VerCheckErrorKey, false);
            EditorPrefs.DeleteKey(VerCheckLastAttemptKey);
            StartCheckBackgroundTask();
        }

        private static void OnVersionChecked(DennokoVersionChecker.Result result)
        {
            _checking = false;
            bool failed = result.State == DennokoVersionChecker.State.Error;

            SessionState.SetBool(VerCheckDoneKey, true);
            SessionState.SetBool(VerCheckErrorKey, failed);
            SessionState.SetString(VerCheckLatestKey, result.LatestVersion ?? string.Empty);
            SessionState.SetString(VerCheckUrlKey, result.Url ?? string.Empty);
            SessionState.SetString(VerCheckMessageKey, result.Message ?? string.Empty);

            if (!failed)
            {
                EditorPrefs.SetString(VerCheckCachedLatestKey, result.LatestVersion ?? string.Empty);
                EditorPrefs.SetString(VerCheckCachedUrlKey, result.Url ?? string.Empty);
                EditorPrefs.SetString(VerCheckCachedMessageKey, result.Message ?? string.Empty);
            }

            RefreshOpenWindows();
        }

        private static void RefreshOpenWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<FolderCleanerWindow>();
            if (windows == null) return;
            foreach (var w in windows)
            {
                if (w != null) w.LoadVersionResultFromSessionState();
            }
        }

        [Serializable]
        private class VersionInfo
        {
            public string version;
        }

        private static string LoadLocalVersion()
        {
            var v = TryReadVersion(AssetDatabase.GUIDToAssetPath(VersionJsonGuid));
            if (v != null) return v;

            return TryReadVersion(ResolveVersionJsonByScriptPath());
        }

        private static string TryReadVersion(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var info = JsonUtility.FromJson<VersionInfo>(File.ReadAllText(path));
                if (info != null && !string.IsNullOrEmpty(info.version)) return info.version;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FolderCleanerVersion] Failed to read version.json ({path}): {e.Message}");
            }
            return null;
        }

        private static string ResolveVersionJsonByScriptPath([CallerFilePath] string scriptPath = null)
        {
            if (string.IsNullOrEmpty(scriptPath)) return null;
            var dir = Path.GetDirectoryName(scriptPath);
            for (int i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
            {
                var candidate = Path.Combine(dir, "version.json");
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
