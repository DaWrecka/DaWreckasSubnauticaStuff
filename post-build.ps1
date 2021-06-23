[CmdletBinding(PositionalBinding=$false)]
param
(
    [Parameter(Mandatory=$true)][string]$ProjectDir,
    [Parameter(Mandatory=$true)][string]$TargetPath,
    [Parameter(Mandatory=$true)][string]$ProjectName,
	[Parameter(Mandatory=$true)][string]$SolutionDir,
	[Parameter(Mandatory=$true)][string]$Config
)
#$dirSeparator = [string][System.IO.Path]::DirectorySeparatorChar
$dirSeparator = "\"
Write-Host ("Using directory separator '" + $dirSeparator + "'")

# Sanity-checking; remove all " symbols from the parameters, because clearly Visual Studio and Powershell can't be trusted not to include them.
$ProjectDir = $ProjectDir -replace '"',''
$TargetPath = $TargetPath -replace '"',''
$ProjectName = $ProjectName -replace '"',''
$SolutionDir = $SolutionDir -replace '"',''
$Config = $Config -replace '"',''

[string]$s = "Executing with parameters:`nProjectDir '" + $ProjectDir + "'`nTargetPath '" + $TargetPath + "'`nProjectName '" + $ProjectName + "'`nSolutionPath '" + $SolutionDir + "'`nConfig '" + $Config + "'"
Write-Host $s

$jsonName = "mod_" + $Config + ".json"
$jsonPath = $ProjectDir + $jsonName
$SolutionItem = Get-Item -Path $SolutionDir
$modPath = $SolutionDir + $dirSeparator + "QMods" + $dirSeparator + $Config + $dirSeparator + $ProjectName

if(!(Test-Path $jsonPath))
{
	$jsonName = "mod.json"
	$jsonPath = $ProjectDir + $dirSeparator + "\mod.json"
}

if(!(Test-Path $jsonPath))
{
	Write-Host Could not find path to existing Json $jsonPath
	Exit
}

if(!(Test-Path $TargetPath))
{
	Write-Host Could not find path to DLL $TargetPath
	Exit
}

$modJson = Get-Content $jsonPath | ConvertFrom-Json
$dllItem = Get-Item -Path $TargetPath
$dllVersion = $dllItem.VersionInfo.FileVersion
Write-Host Updating mod.json with new version $dllVersion
$modJson.Version = $dllItem.VersionInfo.FileVersion
Copy-Item -Path $jsonPath -Destination ($jsonName + ".bak") -Force # Back up old Json, just in case
$modJson | ConvertTo-Json | Out-File $jsonPath
if(!(Test-Path $modPath))
{
	Write-Host Creating directory $modPath
	New-Item -Type Directory -Path $modPath
}
Write-Host Creating hard links in $modPath
New-Item -Type HardLink -Path (($modPath,"mod.json") -join $dirSeparator) -Target $jsonPath -Force
New-Item -Type HardLink -Path (($modPath,$dllItem.Name) -join $dirSeparator) -Target $TargetPath -Force
