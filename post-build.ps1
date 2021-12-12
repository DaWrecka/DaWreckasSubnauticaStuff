[CmdletBinding(PositionalBinding=$false)]
param
(
    [Parameter(Mandatory=$true)][string]$ProjectDir,
    [Parameter(Mandatory=$true)][string]$TargetPath,
    [Parameter(Mandatory=$true)][string]$AssemblyName,
	[Parameter(Mandatory=$true)][string]$SolutionDir,
	[Parameter(Mandatory=$true)][string]$GameDir,
	[Parameter(Mandatory=$true)][string]$Config
)
#$dirSeparator = [string][System.IO.Path]::DirectorySeparatorChar
$dirSeparator = "\"
Write-Host ("Using directory separator '" + $dirSeparator + "'")

# Sanity-checking; remove all " symbols from the parameters, because clearly Visual Studio and Powershell can't be trusted not to include them.
# Also trim any trailing directory separators
$ProjectDir = ($ProjectDir -replace '"','').TrimEnd( ( '\','/' ) )
$TargetPath = ($TargetPath -replace '"','').TrimEnd( ( '\','/' ) )
$AssemblyName = ($AssemblyName -replace '"','').TrimEnd( ( '\','/' ) )
$SolutionDir = ($SolutionDir -replace '"','').TrimEnd( ( '\','/' ) )
$Config = $Config -replace '"',''

[string]$s = "Executing with parameters:`nProjectDir`t'" + $ProjectDir + "'`nTargetPath`t'" + $TargetPath + "'`nAssemblyName`t'" + $AssemblyName + "'`nSolutionDir`t'" + $SolutionDir + "'`nConfig`t`t'" + $Config + "'" + "'`nGameDir`t`t'" + $GameDir + "'`nVerbose`t`t" + $Verbose
Write-Host $s

$jsonName = "mod_" + $Config + ".json"
$jsonPath = ($ProjectDir,$jsonName) -join $dirSeparator
$SolutionItem = Get-Item -Path $SolutionDir
$modPath = ($SolutionDir,"QMods",$Config,$AssemblyName) -join $dirSeparator
$gameModPath = ($GameDir,"QMods",$AssemblyName) -join $dirSeparator
$archiveDir = ($SolutionDir,"Binaries",$Config) -join $dirSeparator
if(!(Test-Path $archiveDir))
{
	New-Item -Type Directory -Path $archiveDir
}
$archiveFullPath = ($archiveDir,$dirSeparator,$AssemblyName,".zip") -join ""

if(!(Test-Path $jsonPath))
{
	$jsonName = "mod.json"
	$jsonPath = ($ProjectDir,"mod.json") -join $dirSeparator
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
Copy-Item -Path $jsonPath -Destination ($jsonPath + ".bak") -Force # Back up old Json, just in case
$modJson | ConvertTo-Json | Out-File $jsonPath
if(!(Test-Path $modPath))
{
	Write-Host Creating directory $modPath
	New-Item -Type Directory -Path $modPath
}
Write-Host Creating hard links in $modPath
$jsonTarget = ($modPath,"mod.json") -join $dirSeparator
$dllTarget = ($modPath,$dllItem.Name) -join $dirSeparator
Write-Host 'Using jsonTarget' $jsonTarget
Write-Host 'Using dllTarget' $dllTarget
Write-Host Using TargetPath $TargetPath
New-Item -Type HardLink -Path $jsonTarget -Target $jsonPath -Force
if($dllTarget -ne $TargetPath)
{
	New-Item -Type HardLink -Path $dllTarget -Target $TargetPath -Force
}
New-Item -Type HardLink -Path (($gameModPath,$dllItem.Name) -join $dirSeparator) -Target $TargetPath -Force
New-Item -Type HardLink -Path (($gameModPath,"mod.json") -join $dirSeparator) -Target $jsonPath -Force

# Link Assets, if they exist
$assetDir = (($ProjectDir,"Assets") -join $dirSeparator)
if(Test-Path $assetDir)
{
	Write-Host "Processing assets directory $assetDir"
	Push-Location $ProjectDir # This is mainly for the use of Resolve-Path later
	$assetDest = $modPath #(($modPath,"Assets") -join $dirSeparator)
	$gameAssetDest = $gameModPath
	foreach($assetItem in Get-ChildItem -Path $assetDir -Recurse)
	{
		$assetPath = Resolve-Path $assetItem -Relative
		if($assetItem -is [System.Io.DirectoryInfo])
		{
			Write-Host ("Creating directory ${assetPath}")
			New-Item -Type Directory -Path (($assetDest,$assetPath) -join $dirSeparator)
			New-Item -Type Directory -Path (($gameAssetDest,$assetPath) -join $dirSeparator)
		}
		else
		{
			foreach($s in ($assetDest,$gameAssetDest))
			{
				$container = (($s,(Resolve-Path ($assetItem.Directory) -Relative)) -join $dirSeparator)
				if(!(Test-Path $container))
				{
					New-Item -Type Directory -Path $container
				}

				$linkTarget = (($s,$assetPath) -join $dirSeparator)
				if(!(Test-Path $linkTarget))
				{
					New-Item -Type HardLink -Path $linkTarget -Target $assetItem
				}
			}
		}
	}
	Pop-Location
}

Compress-Archive -Path $modPath -DestinationPath $archiveFullPath -Force
Write-Host Created archive $archiveFullPath
