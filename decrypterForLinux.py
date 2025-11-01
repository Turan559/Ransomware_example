#!/usr/bin/env python3
"""
decrypt_linux.py

This script decrypts files that were encrypted using:
- AES-CBC with PKCS7 padding
- AES key wrapped with RSA public key
- RSA private key stored in .NET XML format

Usage:
    python3 decrypt_linux.py

Requirements:
    - PyCryptodome: pip install pycryptodome
    - Encrypted file
    - AES wrapped key file: aes_key.enc
    - AES IV file: aes_iv.bin
    - RSA private key in .NET XML format: privatekey.xml

All necessary files (encrypted file, aes_key.enc, aes_iv.bin, privatekey.xml)
must be in the **same folder** for the script to work properly.
"""

import os
import sys
import base64
import traceback
from xml.etree import ElementTree

try:
    from Crypto.PublicKey import RSA
    from Crypto.Cipher import PKCS1_OAEP, AES
except ImportError:
    print("PyCryptodome is not installed. Run: pip install pycryptodome")
    sys.exit(1)


def b64_to_int(s: str) -> int:
    """Convert a Base64 string to an integer"""
    return int.from_bytes(base64.b64decode(s), byteorder='big')


def load_rsa_private_from_xml(xml_path: str) -> RSA.RsaKey:
    """
    Load RSA private key from .NET-style XML file.
    Expected XML elements: Modulus, Exponent, D, P, Q, DP, DQ, InverseQ
    Returns a PyCryptodome RSA key object
    """
    xml = ElementTree.parse(xml_path).getroot()
    nodes = {child.tag: child.text for child in xml}

    required = ["Modulus", "Exponent", "D", "P", "Q", "DP", "DQ", "InverseQ"]
    if not all(k in nodes for k in required):
        raise ValueError("privatekey.xml is missing required elements for decryption.")

    # Convert Base64 XML elements to integers
    n = b64_to_int(nodes["Modulus"])
    e = b64_to_int(nodes["Exponent"])
    d = b64_to_int(nodes["D"])
    p = b64_to_int(nodes["P"])
    q = b64_to_int(nodes["Q"])

    # Construct RSA key using CRT parameters
    key = RSA.construct((n, e, d, p, q))
    return key


def pkcs7_unpad(data: bytes) -> bytes:
    """
    Remove PKCS7 padding
    """
    if len(data) == 0:
        return data
    pad_len = data[-1]
    if pad_len < 1 or pad_len > AES.block_size:
        raise ValueError("Invalid padding length.")
    if data[-pad_len:] != bytes([pad_len]) * pad_len:
        raise ValueError("Incorrect PKCS7 padding.")
    return data[:-pad_len]


def decrypt_file(encrypted_file_path: str) -> str:
    """
    Decrypt the file using AES-CBC with key unwrapped by RSA
    Returns path to the decrypted file
    """
    base_dir = os.path.dirname(os.path.realpath(encrypted_file_path))
    private_xml = os.path.join(base_dir, "privatekey.xml")
    wrapped_key_file = os.path.join(base_dir, "aes_key.enc")
    iv_file = os.path.join(base_dir, "aes_iv.bin")

    # Check if all necessary files exist
    for path in (private_xml, wrapped_key_file, iv_file):
        if not os.path.isfile(path):
            raise FileNotFoundError(f"Required file not found: {path}")

    # 1) Load RSA private key
    rsa_key = load_rsa_private_from_xml(private_xml)
    rsa_cipher = PKCS1_OAEP.new(rsa_key)

    # 2) Decrypt wrapped AES key
    wrapped_key = open(wrapped_key_file, "rb").read()
    aes_key = rsa_cipher.decrypt(wrapped_key)

    # 3) Read AES IV
    iv = open(iv_file, "rb").read()
    if len(iv) != 16:
        raise ValueError("IV must be 16 bytes (aes_iv.bin).")

    # 4) Read ciphertext
    cipher_data = open(encrypted_file_path, "rb").read()

    # 5) AES-CBC decryption + PKCS7 unpadding
    cipher = AES.new(aes_key, AES.MODE_CBC, iv)
    plain_padded = cipher.decrypt(cipher_data)
    plain = pkcs7_unpad(plain_padded)

    # 6) Write output decrypted file
    out_path = os.path.join(base_dir, os.path.basename(encrypted_file_path) + ".decrypted")
    with open(out_path, "wb") as f:
        f.write(plain)

    return out_path


def main():
    """
    Main interactive loop
    """
    try:
        inp = input("Enter the path of the encrypted file: ").strip()
        if not inp:
            print("No file entered.")
            return

        if os.path.isabs(inp):
            target = inp
        else:
            cwd = os.getcwd()
            candidate = os.path.join(cwd, inp)
            if os.path.isfile(candidate):
                target = candidate
            else:
                alt = input(f"File '{candidate}' not found. Do you want to provide full path? (y/n): ").strip().lower()
                if alt == "y":
                    target = input("Full path: ").strip()
                else:
                    print("Cancelled.")
                    return

        if not os.path.isfile(target):
            print("File not found:", target)
            return

        print("File for decryption:", target)
        print("Make sure the following files are in the same folder:")
        print(" - privatekey.xml")
        print(" - aes_key.enc")
        print(" - aes_iv.bin")
        res = decrypt_file(target)
        print("Successfully decrypted. Output file:", res)

    except Exception as e:
        print("Error:", str(e))
        traceback.print_exc()


if __name__ == "__main__":
    main()
