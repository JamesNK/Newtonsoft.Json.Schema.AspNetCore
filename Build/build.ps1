properties { 
  $zipFileName = "JsonSchemaAspNetCore10r1.zip"
  $majorVersion = "1.0"
  $majorWithReleaseVersion = "1.0.1"
  $nugetPrerelease = "beta1"
  $version = GetVersion $majorWithReleaseVersion
  $packageId = "Newtonsoft.Json.Schema.AspNetCore"
  $signAssemblies = $false
  $signKeyPath = "C:\Development\Releases\newtonsoft.snk"
  $buildDocumentation = $false
  $buildNuGet = $true
  $treatWarningsAsErrors = $false
  $workingName = if ($workingName) {$workingName} else {"Working"}
  $netCliChannel = "2.0"
  $netCliVersion = "2.0.0"
  $nugetUrl = "http://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
  
  $baseDir  = resolve-path ..
  $buildDir = "$baseDir\Build"
  $sourceDir = "$baseDir\Src"
  $toolsDir = "$baseDir\Tools"
  $workingDir = "$baseDir\$workingName"
  $workingSourceDir = "$workingDir\Src"

  $nugetPath = "$buildDir\Temp\nuget.exe"
  $vswhereVersion = "2.1.4"
  $vswherePath = "$buildDir\Temp\vswhere.$vswhereVersion"

  $builds = @(
    @{Framework = "netstandard2.0"; TestsFunction = "NetCliTests"; TestFramework = "netcoreapp2.0"; Enabled=$true}
  )
}

framework '4.6x86'

task default -depends Test,Package

# Ensure a clean working directory
task Clean {
  Write-Host "Setting location to $baseDir"
  Set-Location $baseDir
  
  if (Test-Path -path $workingDir)
  {
    Write-Host "Deleting existing working directory $workingDir"
    
    Execute-Command -command { del $workingDir -Recurse -Force }
  }
  
  Write-Host "Creating working directory $workingDir"
  New-Item -Path $workingDir -ItemType Directory
}

# Build each solution, optionally signed
task Build -depends Clean {
  $script:enabledBuilds = $builds | ? {$_.Enabled}
  Write-Host -ForegroundColor Green "Found $($script:enabledBuilds.Length) enabled builds"

  mkdir "$buildDir\Temp" -Force
  EnsureNuGetExists
  EnsureNuGetPacakge "vswhere" $vswherePath $vswhereVersion

  $script:msBuildPath = GetMsBuildPath
  Write-Host "MSBuild path $script:msBuildPath"

  Write-Host "Copying source to working source directory $workingSourceDir"
  robocopy $sourceDir $workingSourceDir /MIR /NFL /NDL /NP /XD bin obj TestResults AppPackages .vs artifacts /XF *.suo *.user *.lock.json | Out-Default
  Copy-Item -Path $baseDir\LICENSE.md -Destination $workingDir\

  $xml = [xml](Get-Content "$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore\Newtonsoft.Json.Schema.AspNetCore.csproj")
  Edit-XmlNodes -doc $xml -xpath "/Project/PropertyGroup/PackageId" -value $packageId
  Edit-XmlNodes -doc $xml -xpath "/Project/PropertyGroup/VersionPrefix" -value $majorWithReleaseVersion
  Edit-XmlNodes -doc $xml -xpath "/Project/PropertyGroup/VersionSuffix" -value $nugetPrerelease
  Edit-XmlNodes -doc $xml -xpath "/Project/PropertyGroup/AssemblyVersion" -value ($majorVersion + '.0.0')
  Edit-XmlNodes -doc $xml -xpath "/Project/PropertyGroup/FileVersion" -value $version
  $xml.save("$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore\Newtonsoft.Json.Schema.AspNetCore.csproj")

  NetCliBuild
}

# Optional build documentation, add files to final zip
task Package -depends Build {
  foreach ($build in $script:enabledBuilds)
  {
    $finalDir = $build.Framework

    $sourcePath = "$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore\bin\Release\$finalDir"

    if (!(Test-Path -path $sourcePath))
    {
      throw "Could not find $sourcePath"
    }

    robocopy $sourcePath $workingDir\Package\Bin\$finalDir Newtonsoft.Json.Schema.AspNetCore.dll Newtonsoft.Json.Schema.AspNetCore.pdb Newtonsoft.Json.Schema.AspNetCore.xml /NFL /NDL /NJS /NC /NS /NP /XO /XF *.CodeAnalysisLog.xml | Out-Default
  }
  
  if ($buildNuGet)
  {
    Write-Host -ForegroundColor Green "Creating NuGet package"

    $targetFrameworks = ($script:enabledBuilds | Select-Object @{Name="Framework";Expression={$_.Framework}} | select -expand Framework) -join ";"

    exec { & $script:msBuildPath "/t:pack" "/p:IncludeSource=true" "/p:Configuration=Release" "/p:TargetFrameworks=`"$targetFrameworks`"" "$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore\Newtonsoft.Json.Schema.AspNetCore.csproj" }

    mkdir $workingDir\NuGet
    move -Path $workingSourceDir\Newtonsoft.Json.Schema.AspNetCore\bin\Release\*.nupkg -Destination $workingDir\NuGet
  }
  
  Copy-Item -Path $baseDir\LICENSE.md -Destination $workingDir\Package\

  robocopy $workingSourceDir $workingDir\Package\Source\Src /MIR /NFL /NDL /NJS /NC /NS /NP /XD bin obj TestResults AppPackages .vs artifacts /XF *.suo *.user *.lock.json | Out-Default
  robocopy $buildDir $workingDir\Package\Source\Build /MIR /NFL /NDL /NJS /NC /NS /NP /XD Temp | Out-Default
  robocopy $toolsDir $workingDir\Package\Source\Tools /MIR /NFL /NDL /NJS /NC /NS /NP | Out-Default
  
  Compress-Archive -Path $workingDir\Package\* -DestinationPath $workingDir\$zipFileName
}

task Test -depends Build {
  foreach ($build in $script:enabledBuilds)
  {
    Write-Host "Calling $($build.TestsFunction)"
    & $build.TestsFunction $build
  }
}

function NetCliBuild()
{
  $projectPath = "$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore.sln"
  $libraryFrameworks = ($script:enabledBuilds | Select-Object @{Name="Framework";Expression={$_.Framework}} | select -expand Framework) -join ";"
  $testFrameworks = ($script:enabledBuilds | Select-Object @{Name="Resolved";Expression={if ($_.TestFramework -ne $null) { $_.TestFramework } else { $_.Framework }}} | select -expand Resolved) -join ";"

  $additionalConstants = switch($signAssemblies) { $true { "SIGNED" } default { "" } }

  Write-Host -ForegroundColor Green "Restoring packages for $libraryFrameworks in $projectPath"
  Write-Host

  exec { & $script:msBuildPath "/t:restore" "/p:Configuration=Release" "/p:LibraryFrameworks=`"$libraryFrameworks`"" "/p:TestFrameworks=`"$testFrameworks`"" $projectPath | Out-Default } "Error restoring $projectPath"

  Write-Host -ForegroundColor Green "Building $libraryFrameworks in $projectPath"
  Write-Host

  exec { & $script:msBuildPath "/t:build" $projectPath "/p:Configuration=Release" "/p:LibraryFrameworks=`"$libraryFrameworks`"" "/p:TestFrameworks=`"$testFrameworks`"" "/p:AssemblyOriginatorKeyFile=$signKeyPath" "/p:SignAssembly=$signAssemblies" "/p:TreatWarningsAsErrors=$treatWarningsAsErrors" "/p:AdditionalConstants=$additionalConstants" }
}

function EnsureNuGetExists()
{
  if (!(Test-Path $nugetPath)) {
    Write-Host "Couldn't find nuget.exe. Downloading from $nugetUrl to $nugetPath"
    (New-Object System.Net.WebClient).DownloadFile($nugetUrl, $nugetPath)
  }
}

function EnsureNuGetPacakge($packageName, $packagePath, $packageVersion)
{
  if (!(Test-Path $packagePath))
  {
    Write-Host "Couldn't find $packagePath. Downloading with NuGet"
    exec { & $nugetPath install $packageName -OutputDirectory $buildDir\Temp -Version $packageVersion | Out-Default } "Error restoring $packagePath"
  }
}

function GetMsBuildPath()
{
  $path = & $vswherePath\tools\vswhere.exe -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
  if (!($path)) {
    throw "Could not find Visual Studio install path"
  }
  return join-path $path 'MSBuild\15.0\Bin\MSBuild.exe'
}

function NetCliTests($build)
{
  $projectPath = "$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore.Tests\Newtonsoft.Json.Schema.AspNetCore.Tests.csproj"
  $location = "$workingSourceDir\Newtonsoft.Json.Schema.AspNetCore.Tests"
  $testDir = if ($build.TestFramework -ne $null) { $build.TestFramework } else { $build.Framework }

  exec { .\Tools\Dotnet\dotnet-install.ps1 -Channel $netCliChannel -Version $netCliVersion | Out-Default }

  try
  {
    Set-Location $location

    exec { dotnet --version | Out-Default }

    Write-Host -ForegroundColor Green "Running tests for $testDir"
    Write-Host

    exec { dotnet test $projectPath -f $testDir -c Release -l trx -r $workingDir --no-restore --no-build | Out-Default }
  }
  finally
  {
    Set-Location $baseDir
  }
}

function GetVersion($majorVersion)
{
    $now = [DateTime]::Now
    
    $year = $now.Year - 2000
    $month = $now.Month
    $totalMonthsSince2000 = ($year * 12) + $month
    $day = $now.Day
    $minor = "{0}{1:00}" -f $totalMonthsSince2000, $day
    
    $hour = $now.Hour
    $minute = $now.Minute
    $revision = "{0:00}{1:00}" -f $hour, $minute
    
    return $majorVersion + "." + $minor
}

function Edit-XmlNodes {
    param (
        [xml] $doc,
        [string] $xpath = $(throw "xpath is a required parameter"),
        [string] $value = $(throw "value is a required parameter")
    )
    
    $nodes = $doc.SelectNodes($xpath)
    $count = $nodes.Count

    Write-Host "Found $count nodes with path '$xpath'"
    
    foreach ($node in $nodes) {
        if ($node -ne $null) {
            if ($node.NodeType -eq "Element")
            {
                $node.InnerXml = $value
            }
            else
            {
                $node.Value = $value
            }
        }
    }
}

function Execute-Command($command) {
    $currentRetry = 0
    $success = $false
    do {
        try
        {
            & $command
            $success = $true
        }
        catch [System.Exception]
        {
            if ($currentRetry -gt 5) {
                throw $_.Exception.ToString()
            } else {
                write-host "Retry $currentRetry"
                Start-Sleep -s 1
            }
            $currentRetry = $currentRetry + 1
        }
    } while (!$success)
}