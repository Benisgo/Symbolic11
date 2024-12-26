$paths = "C:\Games", "$env:USERPROFILE"

$items = Get-ChildItem -Path $paths -Force -Recurse -Depth 2 -ErrorAction SilentlyContinue

$filteredItems = $items | Where-Object {
    ($_.LinkType -match "HardLink" -and $_.Extension -match "\.exe$") -or
    ($_.LinkType -match "SymbolicLink") -or
    ($_.Extension -match "\.lnk$")
}

$finalItems = $filteredItems | Where-Object {$_.DirectoryName -notmatch "Windows"}

$finalItems | Format-List Target, LinkType, FullName 

$csvData = $finalItems | ConvertTo-Csv -Delimiter "|" 
$csvData | Out-String