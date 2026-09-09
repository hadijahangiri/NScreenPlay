Add-Type -AssemblyName System.IO.Compression.FileSystem
$files = Get-ChildItem -Path "artifacts" -Filter "*.nupkg" -ErrorAction SilentlyContinue
if (-not $files) { Write-Host "No nupkg files found in artifacts/"; exit 0 }
foreach ($f in $files) {
    Write-Host "Package: $($f.Name)"
    $zip = [System.IO.Compression.ZipFile]::OpenRead($f.FullName)
    foreach ($entry in $zip.Entries) {
        if ($entry.Name -like '*.nuspec') {
            Write-Host '---nuspec---'
            $sr = New-Object System.IO.StreamReader($entry.Open())
            Write-Host $sr.ReadToEnd()
            $sr.Close()
        }
    }
    $zip.Dispose()
}
