using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string path = Application.persistentDataPath + "/save.dat";
    private static readonly string key = "Mec-4lLSpr.:DatSV2025"; // Keep it secret!

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        string encrypted = Encrypt(json, key);
        File.WriteAllText(path, encrypted);
        Debug.Log("Game Saved to " + path);
    }

    public static SaveData Load()
    {
        if (!File.Exists(path)) return null;

        string encrypted = File.ReadAllText(path);
        string json = Decrypt(encrypted, key);
        return JsonUtility.FromJson<SaveData>(json);
    }

    private static string Encrypt(string data, string key)
    {
        byte[] keyBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

        byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
        aes.IV.CopyTo(result, 0);
        encryptedBytes.CopyTo(result, aes.IV.Length);

        return System.Convert.ToBase64String(result);
    }

    private static string Decrypt(string encryptedData, string key)
    {
        byte[] fullData = System.Convert.FromBase64String(encryptedData);
        byte[] keyBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        byte[] iv = new byte[16];
        byte[] encryptedBytes = new byte[fullData.Length - 16];

        System.Array.Copy(fullData, 0, iv, 0, 16);
        System.Array.Copy(fullData, 16, encryptedBytes, 0, encryptedBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static void DeleteSave()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[SAVE SYSTEM] Save file deleted.");
        }
        else
        {
            Debug.Log("[SAVE SYSTEM] No save file found to delete.");
        }
    }

    static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, "save.dat");
    }
}
