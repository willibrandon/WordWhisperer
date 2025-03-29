$outputFile = Join-Path $PSScriptRoot "cmudict.txt"
$url = "https://raw.githubusercontent.com/cmusphinx/cmudict/master/cmudict.dict"

Write-Host "Downloading CMU Dictionary from $url to $outputFile"
try {
    Invoke-WebRequest -Uri $url -OutFile $outputFile
    Write-Host "Download complete. Dictionary saved to $outputFile"
} catch {
    Write-Host "Error downloading file: $_"
    exit 1
}

# Display some statistics
$totalLines = (Get-Content $outputFile | Measure-Object).Count
$sampleLines = Get-Content $outputFile -TotalCount 5
Write-Host "Total entries: $totalLines"
Write-Host "Sample entries:"
$sampleLines | ForEach-Object { Write-Host "  $_" }

Write-Host "`nCMU Dictionary is ready for use with the ML-based phonetic service."
