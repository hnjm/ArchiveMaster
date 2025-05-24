using System.Security.Cryptography;

namespace ArchiveMaster.Services;

internal class OfflineSyncHelper
{
    public static Aes GetAes(string password)
    {
        Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.SetStringKey(password);
        return aes;
    }
}