[CmdletBinding(PositionalBinding=$false)]
param
(
    [Parameter(Mandatory=$true)][string]$ProjectDir,
    [Parameter(Mandatory=$true)][string]$TargetPath,
    [Parameter(Mandatory=$true)][string]$ProjectName
)

[string]$s = "Executing with parameters ProjectDir '" + $ProjectDir + "', TargetPath '" + $TargetPath + "', ProjectName '" + $ProjectName + "'"
Write-Host $s

$jsonPath = $ProjectDir + "mod.json"
if(!Test-Path $jsonPath)
{
	Write-Host Could not find path to existing Json $jsonPath
	Exit
}

if(!Test-Path $TargetPath)
{
	Write-Host Could not find path to DLL $TargetPath
	Exit
}

$modJson = Get-Content $jsonPath | ConvertFrom-Json
$dllItem = Get-Item -Path $TargetPath
$dllVersion = $dllItem.VersionInfo.FileVersion
Write-Host Updating mod.json with new version $dllVersion
$modJson.Version = $dllItem.VersionInfo.FileVersion
Rename-Item -Path $jsonPath -NewName "mod.json.bak" # Back up old Json, just in case
$modJson | ConvertTo-Json | Out-File $jsonPath
