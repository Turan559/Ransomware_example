RANSOMWARE SIMULATION PROJECT (EDUCATIONAL)

DISCLAIMER:
This project is strictly for educational and defensive purposes (research, learning, lab testing).
Do NOT run these tools on production machines, third‑party systems, or real user data. Always use isolated
virtual machines or air‑gapped lab environments. Unauthorized or malicious use may result in legal consequences.

PROJECT SUMMARY:
A compact educational ransomware simulation demonstrating encryption, key handling, upload, DOCX repair,
and decryption in a controlled environment. Replace any real endpoints with safe placeholders (example.com).
The purpose is to learn cryptography, testing, and defensive analysis — not to harm systems.

PROJECT FILES (as provided):
- ransomware.cs               # C# encryption tool (console)
- server.php                  # Simple PHP upload endpoint (server-side)
- scriptForDocxFile.sh        # Bash DOCX fixer (extract & rezip)
- decrypterForLinux.py        # Python decryption tool (Linux)

HIGH-LEVEL WORKFLOW:
1) ransomware.cs
   - Encrypts files in a target folder using AES-128 (CBC, PKCS7).
   - Generates RSA key pair (XML form) and writes:
     * privatekey.xml
     * publickey.xml
     * aes_key.enc    (AES key wrapped with RSA public key)
     * aes_iv.bin     (16-byte IV)
   - Optionally uploads encrypted files and key files to a server endpoint.
   - Deletes local keys after upload (default behavior in provided code).

2) server.php
   - Receives uploaded files via HTTP POST and stores them in an 'uploads' directory.
   - Minimal example without production hardening.

3) scriptForDocxFile.sh
   - Extracts DOCX (ZIP archive), then repackages it to repair structure if needed.
   - Saves repaired file as <original>_fixed.docx.

4) decrypterForLinux.py
   - Requires the encrypted file and, in the same folder:
     * privatekey.xml (RSA private key in .NET XML format)
     * aes_key.enc
     * aes_iv.bin
   - Unwraps AES key with RSA private key (OAEP), decrypts AES-CBC data, removes PKCS7 padding.
   - Produces <original>.decrypted output file.

USAGE (BASIC):
- Prepare a safe isolated VM and sample files for testing.
- Edit ransomware.cs to set the target folder and keys folder (see CUSTOMIZATION).
- Compile and run the C# encryptor on the VM to produce encrypted files and key artifacts.
- Optionally POST files to the PHP server at: http://example.com/server.php
- If a DOCX becomes corrupted, run scriptForDocxFile.sh to fix it.
- Use decrypterForLinux.py with the correct key files to decrypt a sample ciphertext.

CUSTOMIZATION GUIDE (exact places to edit):

1) ransomware.cs (C# encryptor)
   - Target folder (change "ranstest"):
     string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ranstest");
   - Local keys folder (change "ranstest_keys"):
     string keysDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ranstest_keys");
   - Server upload URL (replace with your safe test URL):
     string uploadUrl = "http://example.com/server.php";
   - RSA key size:
     using (var rsa = new RSACryptoServiceProvider(1024)) // change to 2048 for stronger RSA
   - AES parameters:
     aes.KeySize = 128; // change to 256 if you also change key handling
     aes.Mode = CipherMode.CBC;
     aes.Padding = PaddingMode.PKCS7;
   - Upload behavior:
     To disable uploads, remove or comment out the call to UploadFileAsync(...) or add a boolean toggle.

2) server.php (PHP upload endpoint)
   - Upload directory path:
     $uploadDir = __DIR__ . '/uploads/';
   - Permissions and hardening: change mkdir permission (use 0755), add MIME type checks, limit sizes,
     and use HTTPS and authentication for even lab use.
   - Avoid storing privatekey.xml in a public server for safety during tests.

3) scriptForDocxFile.sh (Bash fixer)
   - Output file naming (change suffix):
     output_file="$file_dir/${base_name}_fixed.docx"
   - For debugging, do not delete temp directory. Comment out rm -rf "$temp_dir".

4) decrypterForLinux.py (Python decryptor)
   - RSA XML format requirement: privatekey.xml coming from .NET RSACryptoServiceProvider.ToXmlString(true)
   - RSA padding: script assumes PKCS1_OAEP (OAEP). Change only if encryption used different padding.
   - AES mode: AES.MODE_CBC is assumed. Change carefully and ensure compatibility.
   - Output filename:
     out_path = os.path.join(base_dir, os.path.basename(encrypted_file_path) + ".decrypted")

SAFETY & RECOMMENDATIONS:
- ALWAYS run in an isolated virtual machine and snapshot before testing.
- Use only harmless sample files (e.g., text files in a samples/ directory).
- Implement a safe 'dry-run' mode in ransomware.cs that:
  * Encrypts copies (e.g., .enc) instead of overwriting originals.
  * Does NOT upload to any server.
  * Does NOT delete keys automatically.
- Do NOT commit private keys to any public repository.
- When demonstrating server uploads, use example.com or a local test server; never expose real IPs in public code.

EXAMPLE SAFE WORKFLOW (STEP-BY-STEP):
1. Create a VM snapshot.
2. Place test files in the target folder (e.g., C:\Users\lab\ranstest).
3. Edit ransomware.cs target path and set upload to disabled (or to http://example.com/server.php).
4. Compile and run the encryptor on the VM; verify encrypted files and keys in the keys folder.
5. If necessary, repair DOCX with scriptForDocxFile.sh.
6. Copy key files into the same folder as a ciphertext file and run decrypterForLinux.py to decrypt.
7. Verify the decrypted file matches the original.
8. Revert the VM snapshot when finished.

README NOTES FOR GITHUB:
- Keep a prominent disclaimer at the top of the repo.
- Provide a 'samples/' directory with harmless example files for quick testing.
- Offer a 'safe_mode' branch or instructions to run encryption in non-destructive mode.
- Provide clear instructions on how to remove or replace any real server URLs with example.com before publishing.

CONTACT & LICENSE:
- LICENSE: Use a permissive license that includes an explicit "for educational use only" clause, or add a CONTRIBUTING.md that explains responsible use.
- CONTACT: Provide author contact for responsible disclosure and research collaboration.

---
END OF FILE
