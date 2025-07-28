using System;
using System.Security.Cryptography;
using System.Text;

public static class Utils
{
    public static string ComputeSHA512Hash(string input)
    {
        using (var sha512 = SHA512.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha512.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}