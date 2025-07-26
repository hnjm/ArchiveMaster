using System.Security.Cryptography;
using System.Text;

namespace ArchiveMaster.Helpers;

public static class AesHelper
{
    public static Aes GetDefaultAes(string password)
    {
        Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.SetStringKey(password);
        return aes;
    }

    public static Aes SetStringKey(this Aes manager, string key)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(nameof(ArchiveMaster)), 100000,
            HashAlgorithmName.SHA256);
        manager.Key = deriveBytes.GetBytes(manager.KeySize / 8);
        return manager;
    }
}