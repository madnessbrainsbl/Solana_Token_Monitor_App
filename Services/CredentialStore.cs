using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TokenMonitorApp.Services
{
    public static class CredentialStore
    {
        private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TokenMonitorApp");
        private static readonly string KeyFile = Path.Combine(AppDir, "auth.key");
        private static readonly string MoralisFile = Path.Combine(AppDir, "moralis.key");

        public static void SaveKey(string key)
        {
            Directory.CreateDirectory(AppDir);
            var data = Encoding.UTF8.GetBytes(key);
            var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(KeyFile, protectedData);
        }

        public static string? GetKey()
        {
            if (!File.Exists(KeyFile)) return null;
            try
            {
                var bytes = File.ReadAllBytes(KeyFile);
                var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(unprotected);
            }
            catch
            {
                return null;
            }
        }

        public static void Clear()
        {
            if (File.Exists(KeyFile))
            {
                File.Delete(KeyFile);
            }
        }

        // Moralis API key (encrypted per current user)
        public static void SaveMoralisKey(string key)
        {
            Directory.CreateDirectory(AppDir);
            var data = Encoding.UTF8.GetBytes(key);
            var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(MoralisFile, protectedData);
        }

        public static string? GetMoralisKey()
        {
            if (!File.Exists(MoralisFile)) return null;
            try
            {
                var bytes = File.ReadAllBytes(MoralisFile);
                var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(unprotected);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearMoralisKey()
        {
            if (File.Exists(MoralisFile))
            {
                File.Delete(MoralisFile);
            }
        }
    }
}

