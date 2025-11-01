#!/bin/bash

# --------------------------------------
# Auto DOCX Fixer Script
# --------------------------------------
# This script is designed to "fix" a DOCX file that may have issues.
# The script extracts the DOCX (ZIP format), then repackages it.
# Users can run it if they need to repair DOCX files that were corrupted
# during encryption/decryption or manipulation.

# 🔹 Prompt the user to enter the path of the DOCX file
read -p "Enter the path of the extracted file:  " input_file

# 🔹 Check if the file exists
if [ ! -f "$input_file" ]; then
    echo "File not found: $input_file"
    exit 1
fi

# 🔹 Extract directory and filename components
file_dir=$(dirname "$input_file")             # Directory of the input file
file_name=$(basename "$input_file")          # Full filename with extension
base_name="${file_name%.*}"                  # Filename without extension

# 🔹 Create a temporary directory to extract contents
temp_dir=$(mktemp -d)
echo "Temporary directory created: $temp_dir"

# 🔹 Extract the DOCX (ZIP) into the temporary directory
unzip -q "$input_file" -d "$temp_dir"
if [ $? -ne 0 ]; then
    echo "Failed to open file. Ensure it is a valid DOCX (ZIP) file."
    exit 1
fi

# 🔹 Prepare the output filename for the fixed DOCX
output_file="$file_dir/${base_name}_fixed.docx"

# 🔹 Repackage the extracted files into a new DOCX
cd "$temp_dir"
zip -X -r "$output_file" . > /dev/null
if [ $? -eq 0 ]; then
    echo "Successfully created: $output_file"
else
    echo "Failed to create archive."
fi

# 🔹 Clean up the temporary directory
cd /
rm -rf "$temp_dir"
echo "Temporary directory deleted."
