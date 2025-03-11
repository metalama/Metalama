# Get all .cs files in the directory recursively
$files = Get-ChildItem -Path . -Filter *.cs -Recurse

foreach ($file in $files) {
    
    echo $file.FullName
    
    # Read the content of the file
    $content = Get-Content -Path $file.FullName -Raw

    # Perform the multi-line find and replace
    $newContent = $content -replace "(?m)// (SharpCrafters s\.r\.o\. licenses this file to you under either the MIT license)\s*// (or a proprietary license, depending on the repository from which it was obtained.)", "// `$1 `$2"
    $newContent = $newContent -replace "(?m)// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.\s*", "// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.\n// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.\n// Refer to LICENSE.md in the repository root for complete details."
    $newContent = $newContent -replace "an MIT license", "the MIT license"

    # Write the updated content back to the file
    if ($content -ne $newContent) {
        [System.IO.File]::WriteAllText($file.FullName, $newContent)
    }
}