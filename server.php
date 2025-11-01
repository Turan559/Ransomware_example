<?php
// Check if a file was uploaded via HTTP POST
if (isset($_FILES['file'])) {

    // 🔹 Define the directory where uploaded files will be saved
    // You can change 'uploads' to any folder name
    $uploadDir = __DIR__ . '/uploads/';

    // 🔹 Create the upload directory if it doesn't exist
    if (!file_exists($uploadDir)) {
        // 0777 permissions = full read/write/execute for everyone
        // true = recursive creation if needed
        mkdir($uploadDir, 0777, true);
    }

    // 🔹 Get the original name of the uploaded file
    $fileName = basename($_FILES['file']['name']);

    // 🔹 Full path where the file will be saved
    $uploadPath = $uploadDir . $fileName;

    // 🔹 Move the uploaded file from temporary location to the target directory
    if (move_uploaded_file($_FILES['file']['tmp_name'], $uploadPath)) {
        // File uploaded successfully
        echo "File successfully uploaded: " . $fileName;
    } else {
        // Something went wrong during the upload
        echo "Error occurred during file upload.";
    }

} else {
    // No file was sent in the POST request
    echo "No file was uploaded.";
}
?>

