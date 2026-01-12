	[CmdletBinding(PositionalBinding=$false)]param
	(
		[Parameter(Mandatory=$false)][string]$ProjectDir,
		[Parameter(Mandatory=$false)][string]$TargetPath,
		[Parameter(Mandatory=$false)][string]$AssemblyName,
		[Parameter(Mandatory=$false)][string]$SolutionDir,
		#[Parameter(Mandatory=$true)][string]$ModBaseDir,
		[Parameter(Mandatory=$false)][string]$GameDir,
		[Parameter(Mandatory=$false)][string]$PluginLoaderPath,
		[Parameter(Mandatory=$false)][string]$Config,
		[Parameter(Mandatory=$false)][string]$UseQMM

	)
	#$dirSeparator = [string][System.IO.Path]::DirectorySeparatorChar
	#$dirSeparator = "\"
	#Write-Host ("Using directory separator '" + $dirSeparator + "'")

	[string]$s = "Executing with parameters:`nConfig`t'" + $Config + `
		"'`nProjectDir`t'" + $ProjectDir + `
		"'`nTargetPath`t'" + $TargetPath + `
		"'`nAssemblyName`t'" + $AssemblyName + `
		"'`nSolutionDir`t'" + $SolutionDir + `
		"'`nGameDir`t`t'" + $GameDir + `
		"'`nPluginLoaderPath`t`t'" + $PluginLoaderPath + `
		"'`nVerbose`t`t" + $Verbose
	Write-Host $s

	# Sanity-checking; remove all " symbols from the parameters, because clearly Visual Studio and/or Powershell can't be trusted not to include them.
	# Also trim any trailing directory separators
	$ProjectDir = ($ProjectDir -replace '"','').TrimEnd( ( '\','/' ) )
	$TargetPath = ($TargetPath -replace '"','').TrimEnd( ( '\','/' ) )
	$AssemblyName = ($AssemblyName -replace '"','').TrimEnd( ( '\','/' ) )
	$SolutionDir = ($SolutionDir -replace '"','').TrimEnd( ( '\','/' ) )
	#$ModBaseDir = ($ModBaseDir -replace '"','').TrimEnd( ( '\','/' ) )
	$GameDir = ($GameDir -replace '"','').TrimEnd( ( '\','/' ) )
	$PluginLoaderPath = ($PluginLoaderPath -replace '"','').TrimEnd( ( '\','/' ) )
	$Config = $Config -replace '"',''

	$SolutionItem = Get-Item -Path $SolutionDir
	$buildPath = [System.IO.Path]::Combine($SolutionDir,$PluginLoaderPath,$Config,$AssemblyName)
	$gameModPath = [System.IO.Path]::Combine($GameDir,$PluginLoaderPath,$AssemblyName)
	$archiveDir = [System.IO.Path]::Combine($SolutionDir,"Binaries",$Config)
	if(!(Test-Path $archiveDir))
	{
		New-Item -Type Directory -Path $archiveDir
	}

	if(!(Test-Path $TargetPath))
	{
		Write-Host Could not find path to DLL $TargetPath
		Exit
	}

	if(!(Test-Path $gameModPath))
	{
		Write-Host Creating directory $gameModPath
		New-Item -Type Directory -Path $gameModPath
	}

	$dllItem = Get-Item -Path $TargetPath
	$dllVersion = $dllItem.VersionInfo.FileVersion
	$dllTarget = [System.IO.Path]::Combine($gameModPath,$dllItem.Name)
	Write-Host 'Using dllTarget' $dllTarget
	if($UseQMM.ToUpper() -eq "TRUE")
	{
		$jsonName = "mod_" + $Config + ".json"
		$jsonPath = [System.IO.Path]::Combine($ProjectDir,$jsonName)
		if(!(Test-Path $jsonPath))
		{
			$jsonName = "mod.json"
			$jsonPath = [System.IO.Path]::Combine($ProjectDir,"mod.json")
		}
		[bool]$useJSON = Test-Path $jsonPath
		if($useJSON)
		{
			$modJson = Get-Content $jsonPath | ConvertFrom-Json
			Write-Host Updating mod.json with new version $dllVersion
			$modJson.Version = $dllItem.VersionInfo.FileVersion
			Copy-Item -Path $jsonPath -Destination ($jsonPath + ".bak") -Force # Back up old Json, just in case
			$modJson | ConvertTo-Json | Out-File $jsonPath
			Write-Host Creating hard links in $gameModPath
			$jsonTarget = [System.IO.Path]::Combine($gameModPath,"mod.json")
			Write-Host 'Using jsonTarget' $jsonTarget
			Write-Host Using TargetPath $TargetPath
			New-Item -Type HardLink -Path $jsonTarget -Target $jsonPath -Force
		}
		else
		{
			Write-Host Could not find path to existing Json $jsonPath
		}
	}

	if($dllTarget -ne $TargetPath)
	{
		New-Item -Type HardLink -Path $dllTarget -Target $TargetPath -Force
	}
	New-Item -Type HardLink -Path ([System.IO.Path]::Combine($gameModPath,$dllItem.Name)) -Target $TargetPath -Force
	if($useJSON)
	{
		New-Item -Type HardLink -Path ([System.IO.Path]::Combine($gameModPath,"mod.json")) -Target $jsonPath -Force
	}

	# Link Assets, if they exist
	$dirString = ([System.IO.Path]::Combine($ProjectDir,"Assets") + "," + [System.IO.Path]::Combine($ProjectDir,"Assets"+$Config))
	write-host ("DirString: '" + $dirString + "'")
	foreach($assetDir in ($dirString -split ","))
	{
		if(Test-Path $assetDir)
		{
			Write-Host "Processing assets directory $assetDir`nUsing gameModPath '$gameModPath' and buildPath '$buildPath'"
			Push-Location $assetDir # This is mainly for the use of Resolve-Path later
			$assetDest = [System.IO.Path]::Combine($buildPath,"Assets") # Used for storing the built mod for building the archive later
			$gameAssetDest = [System.IO.Path]::Combine($gameModPath,"Assets") # The actual asset directory used by the game
			if(!(Test-Path -PathType Container $gameAssetDest))
			{
				Write-Host ("Creating directory ${gameAssetDest}")
				New-Item -Type Directory -Path $gameAssetDest -Force
			}
			if(!(Test-Path -PathType Container $assetDest))
			{
				Write-Host ("Creating directory ${$assetDest}")
				New-Item -Type Directory -Path $assetDest -Force
			}
			foreach($assetItem in Get-ChildItem -Path "$assetDir\." -Recurse)
			{
				#Write-Host ("Processing asset item " + $assetItem.FullName)
				$assetPath = Resolve-Path $assetItem -Relative
				if($assetItem -is [System.Io.DirectoryInfo])
				{
					$newDir = [System.IO.Path]::Combine($assetDest, $assetPath)
					if(!(Test-Path $newDir))
					{
						Write-Host ("Creating directory ${newDir}")
						New-Item -Type Directory -Path $newDir
					}
					$newDir = [System.IO.Path]::Combine($gameAssetDest,$assetPath)
					if(!(Test-Path $newDir))
					{
						Write-Host ("Creating directory ${newDir}")
						New-Item -Type Directory -Path $newDir
					}
				}
				else
				{
					foreach($s in ($assetDest,$gameAssetDest))
					{
						<#$container = [System.IO.Path]::Combine($s,(Resolve-Path ($assetItem.Directory) -Relative))
						if(!(Test-Path $container))
						{
							Write-Host ("Creating container ${container}")
							New-Item -Type Directory -Path $container
						}#>


						$linkTarget = ([System.IO.Path]::Combine($s,$assetPath))
						if(!(Test-Path $linkTarget))
						{
							try
							{
								New-Item -Type HardLink -Path $linkTarget -Target $assetItem
							}
							catch
							{
								Write-Error ("Exception in New-Hardlink:`n" + $_ + "`ns = " + $s + "`nassetPath = " + $assetPath + "`nPath = " + $linkTarget + "`nTarget = " + $assetItem)
							}
						}
					}
				}
			}
			Pop-Location
		}
	}

	$archiveFullPath = [System.IO.Path]::Combine($archiveDir,(($AssemblyName,$dllVersion,".zip") -join ""))
	Compress-Archive -Path $gameModPath -DestinationPath $archiveFullPath -Force
	Write-Host Created archive $archiveFullPath
