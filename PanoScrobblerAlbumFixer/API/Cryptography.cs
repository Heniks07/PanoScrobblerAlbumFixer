using System.Security.Cryptography;
using System.Text;

namespace PanoScrobblerAlbumFixer.API;

public static class Cryptography
{
    public static string GetMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var t in hashBytes)
        {
            sb.Append(t.ToString("x2"));
        }

        return sb.ToString();
    }
}