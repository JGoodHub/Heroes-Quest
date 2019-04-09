# psake
# Copyright (c) 2012 James Kovacs
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

#Requires -Version 2.0

#-- Public Module Functions --#

# .ExternalHelp  psake.psm1-help.xml
function Invoke-Task
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)] [string]$taskName
    )

    Assert $taskName ($msgs.error_invalid_task_name)

    $taskKey = $taskName.ToLower()

    if ($currentContext.aliases.Contains($taskKey)) {
        $taskName = $currentContext.aliases.$taskKey.Name
        $taskKey = $taskName.ToLower()
    }

    $currentContext = $psake.context.Peek()

    Assert ($currentContext.tasks.Contains($taskKey)) ($msgs.error_task_name_does_not_exist -f $taskName)

    if ($currentContext.executedTasks.Contains($taskKey))  { return }

    Assert (!$currentContext.callStack.Contains($taskKey)) ($msgs.error_circular_reference -f $taskName)

    $currentContext.callStack.Push($taskKey)

    $task = $currentContext.tasks.$taskKey

    $precondition_is_valid = & $task.Precondition

    if (!$precondition_is_valid) {
        WriteColoredOutput ($msgs.precondition_was_false -f $taskName) -foregroundcolor Cyan
    } else {
        if ($taskKey -ne 'default') {

            if ($task.PreAction -or $task.PostAction) {
                Assert ($task.Action -ne $null) ($msgs.error_missing_action_parameter -f $taskName)
            }

            if ($task.Action) {
                try {
                    foreach($childTask in $task.DependsOn) {
                        Invoke-Task $childTask
                    }

                    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
                    $currentContext.currentTaskName = $taskName

                    & $currentContext.taskSetupScriptBlock

                    if ($task.PreAction) {
                        & $task.PreAction
                    }

                    if ($currentContext.config.taskNameFormat -is [ScriptBlock]) {
                        & $currentContext.config.taskNameFormat $taskName
                    } else {
                        WriteColoredOutput ($currentContext.config.taskNameFormat -f $taskName) -foregroundcolor Cyan
                    }

                    foreach ($variable in $task.requiredVariables) {
                        Assert ((test-path "variable:$variable") -and ((get-variable $variable).Value -ne $null)) ($msgs.required_variable_not_set -f $variable, $taskName)
                    }

                    & $task.Action

                    if ($task.PostAction) {
                        & $task.PostAction
                    }

                    & $currentContext.taskTearDownScriptBlock
                    $task.Duration = $stopwatch.Elapsed
                } catch {
                    if ($task.ContinueOnError) {
                        "-"*70
                        WriteColoredOutput ($msgs.continue_on_error -f $taskName,$_) -foregroundcolor Yellow
                        "-"*70
                        $task.Duration = $stopwatch.Elapsed
                    }  else {
                        throw $_
                    }
                }
            } else {
                # no action was specified but we still execute all the dependencies
                foreach($childTask in $task.DependsOn) {
                    Invoke-Task $childTask
                }
            }
        } else {
            foreach($childTask in $task.DependsOn) {
                Invoke-Task $childTask
            }
        }

        Assert (& $task.Postcondition) ($msgs.postcondition_failed -f $taskName)
    }

    $poppedTaskKey = $currentContext.callStack.Pop()
    Assert ($poppedTaskKey -eq $taskKey) ($msgs.error_corrupt_callstack -f $taskKey,$poppedTaskKey)

    $currentContext.executedTasks.Push($taskKey)
}

# .ExternalHelp  psake.psm1-help.xml
function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd),
        [Parameter(Position=2,Mandatory=0)][int]$maxRetries = 0,
        [Parameter(Position=3,Mandatory=0)][string]$retryTriggerErrorPattern = $null
    )

    $tryCount = 1

    do {
        try {
            $global:lastexitcode = 0
            & $cmd
            if ($lastexitcode -ne 0) {
                throw ("Exec: " + $errorMessage)
            }
            break
        }
        catch [Exception]
        {
            if ($tryCount -gt $maxRetries) {
                throw $_
            }

            if ($retryTriggerErrorPattern -ne $null) {
                $isMatch = [regex]::IsMatch($_.Exception.Message, $retryTriggerErrorPattern)

                if ($isMatch -eq $false) {
                    throw $_
                }
            }

            Write-Host "Try $tryCount failed, retrying again in 1 second..."

            $tryCount++

            [System.Threading.Thread]::Sleep([System.TimeSpan]::FromSeconds(1))
        }
    }
    while ($true)
}

# .ExternalHelp  psake.psm1-help.xml
function Assert
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)]$conditionToCheck,
        [Parameter(Position=1,Mandatory=1)]$failureMessage
    )
    if (!$conditionToCheck) {
        throw ("Assert: " + $failureMessage)
    }
}

# .ExternalHelp  psake.psm1-help.xml
function Task
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][string]$name = $null,
        [Parameter(Position=1,Mandatory=0)][scriptblock]$action = $null,
        [Parameter(Position=2,Mandatory=0)][scriptblock]$preaction = $null,
        [Parameter(Position=3,Mandatory=0)][scriptblock]$postaction = $null,
        [Parameter(Position=4,Mandatory=0)][scriptblock]$precondition = {$true},
        [Parameter(Position=5,Mandatory=0)][scriptblock]$postcondition = {$true},
        [Parameter(Position=6,Mandatory=0)][switch]$continueOnError = $false,
        [Parameter(Position=7,Mandatory=0)][string[]]$depends = @(),
        [Parameter(Position=8,Mandatory=0)][string[]]$requiredVariables = @(),
        [Parameter(Position=9,Mandatory=0)][string]$description = $null,
        [Parameter(Position=10,Mandatory=0)][string]$alias = $null,
        [Parameter(Position=11,Mandatory=0)][string]$maxRetries = 0,
        [Parameter(Position=12,Mandatory=0)][string]$retryTriggerErrorPattern = $null
    )
    if ($name -eq 'default') {
        Assert (!$action) ($msgs.error_default_task_cannot_have_action)
    }

    $newTask = @{
        Name = $name
        DependsOn = $depends
        PreAction = $preaction
        Action = $action
        PostAction = $postaction
        Precondition = $precondition
        Postcondition = $postcondition
        ContinueOnError = $continueOnError
        Description = $description
        Duration = [System.TimeSpan]::Zero
        RequiredVariables = $requiredVariables
        Alias = $alias
        MaxRetries = $maxRetries
        RetryTriggerErrorPattern = $retryTriggerErrorPattern
    }

    $taskKey = $name.ToLower()

    $currentContext = $psake.context.Peek()

    Assert (!$currentContext.tasks.ContainsKey($taskKey)) ($msgs.error_duplicate_task_name -f $name)

    $currentContext.tasks.$taskKey = $newTask

    if($alias)
    {
        $aliasKey = $alias.ToLower()

        Assert (!$currentContext.aliases.ContainsKey($aliasKey)) ($msgs.error_duplicate_alias_name -f $alias)

        $currentContext.aliases.$aliasKey = $newTask
    }
}

# .ExternalHelp  psake.psm1-help.xml
function Properties {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$properties
    )
    $psake.context.Peek().properties += $properties
}

# .ExternalHelp  psake.psm1-help.xml
function Include {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][string]$fileNamePathToInclude
    )
    Assert (test-path $fileNamePathToInclude -pathType Leaf) ($msgs.error_invalid_include_path -f $fileNamePathToInclude)
    $psake.context.Peek().includes.Enqueue((Resolve-Path $fileNamePathToInclude));
}

# .ExternalHelp  psake.psm1-help.xml
function FormatTaskName {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)]$format
    )
    $psake.context.Peek().config.taskNameFormat = $format
}

# .ExternalHelp  psake.psm1-help.xml
function TaskSetup {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$setup
    )
    $psake.context.Peek().taskSetupScriptBlock = $setup
}

# .ExternalHelp  psake.psm1-help.xml
function TaskTearDown {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$teardown
    )
    $psake.context.Peek().taskTearDownScriptBlock = $teardown
}

# .ExternalHelp  psake.psm1-help.xml
function Framework {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][string]$framework
    )
    $psake.context.Peek().config.framework = $framework
    ConfigureBuildEnvironment
}

# .ExternalHelp  psake.psm1-help.xml
function Invoke-psake {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = 0)][string] $buildFile,
        [Parameter(Position = 1, Mandatory = 0)][string[]] $taskList = @(),
        [Parameter(Position = 2, Mandatory = 0)][string] $framework,
        [Parameter(Position = 3, Mandatory = 0)][switch] $docs = $false,
        [Parameter(Position = 4, Mandatory = 0)][hashtable] $parameters = @{},
        [Parameter(Position = 5, Mandatory = 0)][hashtable] $properties = @{},
        [Parameter(Position = 6, Mandatory = 0)][alias("init")][scriptblock] $initialization = {},
        [Parameter(Position = 7, Mandatory = 0)][switch] $nologo = $false,
        [Parameter(Position = 8, Mandatory = 0)][switch] $detailedDocs = $false
    )
    try {
        if (-not $nologo) {
            "psake version {0}`nCopyright (c) 2010-2015 James Kovacs, Damian Hickey & Contributors`n" -f $psake.version
        }

        if (!$buildFile) {
          $buildFile = $psake.config_default.buildFileName
        }
        elseif (!(test-path $buildFile -pathType Leaf) -and (test-path $psake.config_default.buildFileName -pathType Leaf)) {
            # If the $config.buildFileName file exists and the given "buildfile" isn 't found assume that the given
            # $buildFile is actually the target Tasks to execute in the $config.buildFileName script.
            $taskList = $buildFile.Split(', ')
            $buildFile = $psake.config_default.buildFileName
        }

        # Execute the build file to set up the tasks and defaults
        Assert (test-path $buildFile -pathType Leaf) ($msgs.error_build_file_not_found -f $buildFile)

        $psake.build_script_file = get-item $buildFile
        $psake.build_script_dir = $psake.build_script_file.DirectoryName
        $psake.build_success = $false

        $psake.context.push(@{
            "taskSetupScriptBlock" = {};
            "taskTearDownScriptBlock" = {};
            "executedTasks" = new-object System.Collections.Stack;
            "callStack" = new-object System.Collections.Stack;
            "originalEnvPath" = $env:path;
            "originalDirectory" = get-location;
            "originalErrorActionPreference" = $global:ErrorActionPreference;
            "tasks" = @{};
            "aliases" = @{};
            "properties" = @();
            "includes" = new-object System.Collections.Queue;
            "config" = CreateConfigurationForNewContext $buildFile $framework
        })

        LoadConfiguration $psake.build_script_dir

        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        set-location $psake.build_script_dir

        LoadModules

        $frameworkOldValue = $framework
        . $psake.build_script_file.FullName

        $currentContext = $psake.context.Peek()

        if ($framework -ne $frameworkOldValue) {
            writecoloredoutput $msgs.warning_deprecated_framework_variable -foregroundcolor Yellow
            $currentContext.config.framework = $framework
        }

        ConfigureBuildEnvironment

        while ($currentContext.includes.Count -gt 0) {
            $includeFilename = $currentContext.includes.Dequeue()
            . $includeFilename
        }

        if ($docs -or $detailedDocs) {
            WriteDocumentation($detailedDocs)
            CleanupEnvironment
            return
        }

        foreach ($key in $parameters.keys) {
            if (test-path "variable:\$key") {
                set-item -path "variable:\$key" -value $parameters.$key -WhatIf:$false -Confirm:$false | out-null
            } else {
                new-item -path "variable:\$key" -value $parameters.$key -WhatIf:$false -Confirm:$false | out-null
            }
        }

        # The initial dot (.) indicates that variables initialized/modified in the propertyBlock are available in the parent scope.
        foreach ($propertyBlock in $currentContext.properties) {
            . $propertyBlock
        }

        foreach ($key in $properties.keys) {
            if (test-path "variable:\$key") {
                set-item -path "variable:\$key" -value $properties.$key -WhatIf:$false -Confirm:$false | out-null
            }
        }

        # Simple dot sourcing will not work. We have to force the script block into our
        # module's scope in order to initialize variables properly.
        . $MyInvocation.MyCommand.Module $initialization

        # Execute the list of tasks or the default task
        if ($taskList) {
            foreach ($task in $taskList) {
                invoke-task $task
            }
        } elseif ($currentContext.tasks.default) {
            invoke-task default
        } else {
            throw $msgs.error_no_default_task
        }

        WriteColoredOutput ("`n" + $msgs.build_success + "`n") -foregroundcolor Green

        WriteTaskTimeSummary $stopwatch.Elapsed

        $psake.build_success = $true
    } catch {
        $currentConfig = GetCurrentConfigurationOrDefault
        if ($currentConfig.verboseError) {
            $error_message = "{0}: An Error Occurred. See Error Details Below: `n" -f (Get-Date)
            $error_message += ("-" * 70) + "`n"
            $error_message += "Error: {0}`n" -f (ResolveError $_ -Short)
            $error_message += ("-" * 70) + "`n"
            $error_message += ResolveError $_
            $error_message += ("-" * 70) + "`n"
            $error_message += "Script Variables" + "`n"
            $error_message += ("-" * 70) + "`n"
            $error_message += get-variable -scope script | format-table | out-string
        } else {
            # ($_ | Out-String) gets error messages with source information included.
            $error_message = "Error: {0}: `n{1}" -f (Get-Date), (ResolveError $_ -Short)
        }

        $psake.build_success = $false

        # if we are running in a nested scope (i.e. running a psake script from a psake script) then we need to re-throw the exception
        # so that the parent script will fail otherwise the parent script will report a successful build
        $inNestedScope = ($psake.context.count -gt 1)
        if ( $inNestedScope ) {
            throw $_
        } else {
            if (!$psake.run_by_psake_build_tester) {
                WriteColoredOutput $error_message -foregroundcolor Red
            }
        }
    } finally {
        CleanupEnvironment
    }
}

#-- Private Module Functions --#
function WriteColoredOutput {
    param(
        [string] $message,
        [System.ConsoleColor] $foregroundcolor
    )

    $currentConfig = GetCurrentConfigurationOrDefault
    if ($currentConfig.coloredOutput -eq $true) {
        if (($Host.UI -ne $null) -and ($Host.UI.RawUI -ne $null) -and ($Host.UI.RawUI.ForegroundColor -ne $null)) {
            $previousColor = $Host.UI.RawUI.ForegroundColor
            $Host.UI.RawUI.ForegroundColor = $foregroundcolor
        }
    }

    $message

    if ($previousColor -ne $null) {
        $Host.UI.RawUI.ForegroundColor = $previousColor
    }
}

function LoadModules {
    $currentConfig = $psake.context.peek().config
    if ($currentConfig.modules) {

        $scope = $currentConfig.moduleScope

        $global = [string]::Equals($scope, "global", [StringComparison]::CurrentCultureIgnoreCase)

        $currentConfig.modules | foreach {
            resolve-path $_ | foreach {
                "Loading module: $_"
                $module = import-module $_ -passthru -DisableNameChecking -global:$global
                if (!$module) {
                    throw ($msgs.error_loading_module -f $_.Name)
                }
            }
        }
        ""
    }
}

function LoadConfiguration {
    param(
        [string] $configdir = $PSScriptRoot
    )

    $psakeConfigFilePath = (join-path $configdir "psake-config.ps1")

    if (test-path $psakeConfigFilePath -pathType Leaf) {
        try {
            $config = GetCurrentConfigurationOrDefault
            . $psakeConfigFilePath
        } catch {
            throw "Error Loading Configuration from psake-config.ps1: " + $_
        }
    }
}

function GetCurrentConfigurationOrDefault() {
    if ($psake.context.count -gt 0) {
        return $psake.context.peek().config
    } else {
        return $psake.config_default
    }
}

function CreateConfigurationForNewContext {
    param(
        [string] $buildFile,
        [string] $framework
    )

    $previousConfig = GetCurrentConfigurationOrDefault

    $config = new-object psobject -property @{
        buildFileName = $previousConfig.buildFileName;
        framework = $previousConfig.framework;
        taskNameFormat = $previousConfig.taskNameFormat;
        verboseError = $previousConfig.verboseError;
        coloredOutput = $previousConfig.coloredOutput;
        modules = $previousConfig.modules;
        moduleScope =  $previousConfig.moduleScope;
    }

    if ($framework) {
        $config.framework = $framework;
    }

    if ($buildFile) {
        $config.buildFileName = $buildFile;
    }

    return $config
}

function ConfigureBuildEnvironment {
    $framework = $psake.context.peek().config.framework
    if ($framework -cmatch '^((?:\d+\.\d+)(?:\.\d+){0,1})(x86|x64){0,1}$') {
        $versionPart = $matches[1]
        $bitnessPart = $matches[2]
    } else {
        throw ($msgs.error_invalid_framework -f $framework)
    }
    $versions = $null
    $buildToolsVersions = $null
    switch ($versionPart) {
        '1.0' {
            $versions = @('v1.0.3705')
        }
        '1.1' {
            $versions = @('v1.1.4322')
        }
        '2.0' {
            $versions = @('v2.0.50727')
        }
        '3.0' {
            $versions = @('v2.0.50727')
        }
        '3.5' {
            $versions = @('v3.5', 'v2.0.50727')
        }
        '4.0' {
            $versions = @('v4.0.30319')
        }
        {($_ -eq '4.5.1') -or ($_ -eq '4.5.2')} {
            $versions = @('v4.0.30319')
            $buildToolsVersions = @('14.0', '12.0')
        }
        '4.6' {
            $versions = @('v4.0.30319')
            $buildToolsVersions = @('14.0')
        }

        default {
            throw ($msgs.error_unknown_framework -f $versionPart, $framework)
        }
    }

    $bitness = 'Framework'
    if ($versionPart -ne '1.0' -and $versionPart -ne '1.1') {
        switch ($bitnessPart) {
            'x86' {
                $bitness = 'Framework'
                $buildToolsKey = 'MSBuildToolsPath32'
            }
            'x64' {
                $bitness = 'Framework64'
                $buildToolsKey = 'MSBuildToolsPath'
            }
            { [string]::IsNullOrEmpty($_) } {
                $ptrSize = [System.IntPtr]::Size
                switch ($ptrSize) {
                    4 {
                        $bitness = 'Framework'
                        $buildToolsKey = 'MSBuildToolsPath32'
                    }
                    8 {
                        $bitness = 'Framework64'
                        $buildToolsKey = 'MSBuildToolsPath'
                    }
                    default {
                        throw ($msgs.error_unknown_pointersize -f $ptrSize)
                    }
                }
            }
            default {
                throw ($msgs.error_unknown_bitnesspart -f $bitnessPart, $framework)
            }
        }
    }
    $frameworkDirs = @()
    if ($buildToolsVersions -ne $null) {
        foreach($ver in $buildToolsVersions) {
            if (Test-Path "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver") {
                $frameworkDirs += (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver" -Name $buildToolsKey).$buildToolsKey
            }
        }
    }
    $frameworkDirs = $frameworkDirs + @($versions | foreach { "$env:windir\Microsoft.NET\$bitness\$_\" })

    for ($i = 0; $i -lt $frameworkDirs.Count; $i++) {
        $dir = $frameworkDirs[$i]
        if ($dir -Match "\$\(Registry:HKEY_LOCAL_MACHINE(.*?)@(.*)\)") {
            $key = "HKLM:" + $matches[1]
            $name = $matches[2]
            $dir = (Get-ItemProperty -Path $key -Name $name).$name
            $frameworkDirs[$i] = $dir
        }
    }

    $frameworkDirs | foreach { Assert (test-path $_ -pathType Container) ($msgs.error_no_framework_install_dir_found -f $_)}

    $env:path = ($frameworkDirs -join ";") + ";$env:path"
    # if any error occurs in a PS function then "stop" processing immediately
    # this does not effect any external programs that return a non-zero exit code
    $global:ErrorActionPreference = "Stop"
}

function CleanupEnvironment {
    if ($psake.context.Count -gt 0) {
        $currentContext = $psake.context.Peek()
        $env:path = $currentContext.originalEnvPath
        Set-Location $currentContext.originalDirectory
        $global:ErrorActionPreference = $currentContext.originalErrorActionPreference
        [void] $psake.context.Pop()
    }
}

function SelectObjectWithDefault
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        [PSObject]
        $InputObject,
        [string]
        $Name,
        $Value
    )

    process {
        if ($_ -eq $null) { $Value }
        elseif ($_ | Get-Member -Name $Name) {
          $_.$Name
        }
        elseif (($_ -is [Hashtable]) -and ($_.Keys -contains $Name)) {
          $_.$Name
        }
        else { $Value }
    }
}

# borrowed from Jeffrey Snover http://blogs.msdn.com/powershell/archive/2006/12/07/resolve-error.aspx
# modified to better handle SQL errors
function ResolveError
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        $ErrorRecord=$Error[0],
        [Switch]
        $Short
    )

    process {
        if ($_ -eq $null) { $_ = $ErrorRecord }
        $ex = $_.Exception

        if (-not $Short) {
            $error_message = "`nErrorRecord:{0}ErrorRecord.InvocationInfo:{1}Exception:`n{2}"
            $formatted_errorRecord = $_ | format-list * -force | out-string
            $formatted_invocationInfo = $_.InvocationInfo | format-list * -force | out-string
            $formatted_exception = ''

            $i = 0
            while ($ex -ne $null) {
                $i++
                $formatted_exception += ("$i" * 70) + "`n" +
                    ($ex | format-list * -force | out-string) + "`n"
                $ex = $ex | SelectObjectWithDefault -Name 'InnerException' -Value $null
            }

            return $error_message -f $formatted_errorRecord, $formatted_invocationInfo, $formatted_exception
        }

        $lastException = @()
        while ($ex -ne $null) {
            $lastMessage = $ex | SelectObjectWithDefault -Name 'Message' -Value ''
            $lastException += ($lastMessage -replace "`n", '')
            if ($ex -is [Data.SqlClient.SqlException]) {
                $lastException += "(Line [$($ex.LineNumber)] " +
                    "Procedure [$($ex.Procedure)] Class [$($ex.Class)] " +
                    " Number [$($ex.Number)] State [$($ex.State)] )"
            }
            $ex = $ex | SelectObjectWithDefault -Name 'InnerException' -Value $null
        }
        $shortException = $lastException -join ' --> '

        $header = $null
        $current = $_
        $header = (($_.InvocationInfo |
            SelectObjectWithDefault -Name 'PositionMessage' -Value '') -replace "`n", ' '),
            ($_ | SelectObjectWithDefault -Name 'Message' -Value ''),
            ($_ | SelectObjectWithDefault -Name 'Exception' -Value '') |
                ? { -not [String]::IsNullOrEmpty($_) } |
                Select -First 1

        $delimiter = ''
        if ((-not [String]::IsNullOrEmpty($header)) -and
            (-not [String]::IsNullOrEmpty($shortException)))
            { $delimiter = ' [<<==>>] ' }

        return "$($header)$($delimiter)Exception: $($shortException)"
    }
}

function WriteDocumentation($showDetailed) {
    $currentContext = $psake.context.Peek()

    if ($currentContext.tasks.default) {
        $defaultTaskDependencies = $currentContext.tasks.default.DependsOn
    } else {
        $defaultTaskDependencies = @()
    }

    $docs = $currentContext.tasks.Keys | foreach-object {
        if ($_ -eq "default") {
            return
        }

        $task = $currentContext.tasks.$_
        new-object PSObject -property @{
            Name = $task.Name;
            Alias = $task.Alias;
            Description = $task.Description;
            "Depends On" = $task.DependsOn -join ", "
            Default = if ($defaultTaskDependencies -contains $task.Name) { $true }
        }
    }
    if ($showDetailed) {
        $docs | sort 'Name' | format-list -property Name,Alias,Description,"Depends On",Default
    } else {
        $docs | sort 'Name' | format-table -autoSize -wrap -property Name,Alias,"Depends On",Default,Description
    }

}

function WriteTaskTimeSummary($invokePsakeDuration) {
    if ($psake.context.count -gt 0) {
        "-" * 70
        "Build Time Report"
        "-" * 70
        $list = @()
        $currentContext = $psake.context.Peek()
        while ($currentContext.executedTasks.Count -gt 0) {
            $taskKey = $currentContext.executedTasks.Pop()
            $task = $currentContext.tasks.$taskKey
            if ($taskKey -eq "default") {
                continue
            }
            $list += new-object PSObject -property @{
                Name = $task.Name;
                Duration = $task.Duration
            }
        }
        [Array]::Reverse($list)
        $list += new-object PSObject -property @{
            Name = "Total:";
            Duration = $invokePsakeDuration
        }
        # using "out-string | where-object" to filter out the blank line that format-table prepends
        $list | format-table -autoSize -property Name,Duration | out-string -stream | where-object { $_ }
    }
}

DATA msgs {
convertfrom-stringdata @'
    error_invalid_task_name = Task name should not be null or empty string.
    error_task_name_does_not_exist = Task {0} does not exist.
    error_circular_reference = Circular reference found for task {0}.
    error_missing_action_parameter = Action parameter must be specified when using PreAction or PostAction parameters for task {0}.
    error_corrupt_callstack = Call stack was corrupt. Expected {0}, but got {1}.
    error_invalid_framework = Invalid .NET Framework version, {0} specified.
    error_unknown_framework = Unknown .NET Framework version, {0} specified in {1}.
    error_unknown_pointersize = Unknown pointer size ({0}) returned from System.IntPtr.
    error_unknown_bitnesspart = Unknown .NET Framework bitness, {0}, specified in {1}.
    error_no_framework_install_dir_found = No .NET Framework installation directory found at {0}.
    error_bad_command = Error executing command {0}.
    error_default_task_cannot_have_action = 'default' task cannot specify an action.
    error_duplicate_task_name = Task {0} has already been defined.
    error_duplicate_alias_name = Alias {0} has already been defined.
    error_invalid_include_path = Unable to include {0}. File not found.
    error_build_file_not_found = Could not find the build file {0}.
    error_no_default_task = 'default' task required.
    error_loading_module = Error loading module {0}.
    warning_deprecated_framework_variable = Warning: Using global variable $framework to set .NET framework version used is deprecated. Instead use Framework function or configuration file psake-config.ps1.
    required_variable_not_set = Variable {0} must be set to run task {1}.
    postcondition_failed = Postcondition failed for task {0}.
    precondition_was_false = Precondition was false, not executing task {0}.
    continue_on_error = Error in task {0}. {1}
    build_success = Build Succeeded!
'@
}

import-localizeddata -bindingvariable msgs -erroraction silentlycontinue

$script:psake = @{}
$psake.version = "4.4.2" # contains the current version of psake
$psake.context = new-object system.collections.stack # holds onto the current state of all variables
$psake.run_by_psake_build_tester = $false # indicates that build is being run by psake-BuildTester
$psake.config_default = new-object psobject -property @{
    buildFileName = "default.ps1";
    framework = "4.0";
    taskNameFormat = "Executing {0}";
    verboseError = $false;
    coloredOutput = $true;
    modules = $null;
    moduleScope = "";
} # contains default configuration, can be overriden in psake-config.ps1 in directory with psake.psm1 or in directory with current build script

$psake.build_success = $false # indicates that the current build was successful
$psake.build_script_file = $null # contains a System.IO.FileInfo for the current build script
$psake.build_script_dir = "" # contains a string with fully-qualified path to current build script

LoadConfiguration

export-modulemember -function Invoke-psake, Invoke-Task, Task, Properties, Include, FormatTaskName, TaskSetup, TaskTearDown, Framework, Assert, Exec -variable psake

# SIG # Begin signature block
# MIIfuQYJKoZIhvcNAQcCoIIfqjCCH6YCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCDcKRc7cZuIvtRz
# 0+gOJXxQx793aKbAb73cwcpnKHh2BaCCDfMwggPFMIICraADAgECAhACrFwmagtA
# m48LefKuRiV3MA0GCSqGSIb3DQEBBQUAMGwxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xKzApBgNV
# BAMTIkRpZ2lDZXJ0IEhpZ2ggQXNzdXJhbmNlIEVWIFJvb3QgQ0EwHhcNMDYxMTEw
# MDAwMDAwWhcNMzExMTEwMDAwMDAwWjBsMQswCQYDVQQGEwJVUzEVMBMGA1UEChMM
# RGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSswKQYDVQQD
# EyJEaWdpQ2VydCBIaWdoIEFzc3VyYW5jZSBFViBSb290IENBMIIBIjANBgkqhkiG
# 9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxszlc+b71LvlLS0ypt/lgT/JzSVJtnEqw9WU
# NGeiChywX2mmQLHEt7KP0JikqUFZOtPclNY823Q4pErMTSWC90qlUxI47vNJbXGR
# fmO2q6Zfw6SE+E9iUb74xezbOJLjBuUIkQzEKEFV+8taiRV+ceg1v01yCT2+OjhQ
# W3cxG42zxyRFmqesbQAUWgS3uhPrUQqYQUEiTmVhh4FBUKZ5XIneGUpX1S7mXRxT
# LH6YzRoGFqRoc9A0BBNcoXHTWnxV215k4TeHMFYE5RG0KYAS8Xk5iKICEXwnZreI
# t3jyygqoOKsKZMK/Zl2VhMGhJR6HXRpQCyASzEG7bgtROLhLywIDAQABo2MwYTAO
# BgNVHQ8BAf8EBAMCAYYwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQUsT7DaQP4
# v0cB1JgmGggC72NkK8MwHwYDVR0jBBgwFoAUsT7DaQP4v0cB1JgmGggC72NkK8Mw
# DQYJKoZIhvcNAQEFBQADggEBABwaBpfc15yfPIhmBghXIdshR/gqZ6q/GDJ2QBBX
# wYrzetkRZY41+p78RbWe2UwxS7iR6EMsjrN4ztvjU3lx1uUhlAHaVYeaJGT2imbM
# 3pw3zag0sWmbI8ieeCIrcEPjVUcxYRnvWMWFL04w9qAxFiPI5+JlFjPLvxoboD34
# yl6LMYtgCIktDAZcUrfE+QqY0RVfnxK+fDZjOL1EpH/kJisKxJdpDemM4sAQV7jI
# dhKRVfJIadi8KgJbD0TUIDHb9LpwJl2QYJ68SxcJL7TLHkNoyQcnwdJc9+ohuWgS
# nDycv578gFybY83sR6olJ2egN/MAgn1U16n46S4To3foH0owggSRMIIDeaADAgEC
# AhAHsEGNpR4UjDMbvN63E4MjMA0GCSqGSIb3DQEBCwUAMGwxCzAJBgNVBAYTAlVT
# MRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5j
# b20xKzApBgNVBAMTIkRpZ2lDZXJ0IEhpZ2ggQXNzdXJhbmNlIEVWIFJvb3QgQ0Ew
# HhcNMTgwNDI3MTI0MTU5WhcNMjgwNDI3MTI0MTU5WjBaMQswCQYDVQQGEwJVUzEY
# MBYGA1UEChMPLk5FVCBGb3VuZGF0aW9uMTEwLwYDVQQDEyguTkVUIEZvdW5kYXRp
# b24gUHJvamVjdHMgQ29kZSBTaWduaW5nIENBMIIBIjANBgkqhkiG9w0BAQEFAAOC
# AQ8AMIIBCgKCAQEAwQqv4aI0CI20XeYqTTZmyoxsSQgcCBGQnXnufbuDLhAB6GoT
# NB7HuEhNSS8ftV+6yq8GztBzYAJ0lALdBjWypMfL451/84AO5ZiZB3V7MB2uxgWo
# cV1ekDduU9bm1Q48jmR4SVkLItC+oQO/FIA2SBudVZUvYKeCJS5Ri9ibV7La4oo7
# BJChFiP8uR+v3OU33dgm5BBhWmth4oTyq22zCfP3NO6gBWEIPFR5S+KcefUTYmn2
# o7IvhvxzJsMCrNH1bxhwOyMl+DQcdWiVPuJBKDOO/hAKIxBG4i6ryQYBaKdhDgaA
# NSCik0UgZasz8Qgl8n0A73+dISPumD8L/4mdywIDAQABo4IBPzCCATswHQYDVR0O
# BBYEFMtck66Im/5Db1ZQUgJtePys4bFaMB8GA1UdIwQYMBaAFLE+w2kD+L9HAdSY
# JhoIAu9jZCvDMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcDAzAS
# BgNVHRMBAf8ECDAGAQH/AgEAMDQGCCsGAQUFBwEBBCgwJjAkBggrBgEFBQcwAYYY
# aHR0cDovL29jc3AuZGlnaWNlcnQuY29tMEsGA1UdHwREMEIwQKA+oDyGOmh0dHA6
# Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEhpZ2hBc3N1cmFuY2VFVlJvb3RD
# QS5jcmwwPQYDVR0gBDYwNDAyBgRVHSAAMCowKAYIKwYBBQUHAgEWHGh0dHBzOi8v
# d3d3LmRpZ2ljZXJ0LmNvbS9DUFMwDQYJKoZIhvcNAQELBQADggEBALNGxKTz6gq6
# clMF01GjC3RmJ/ZAoK1V7rwkqOkY3JDl++v1F4KrFWEzS8MbZsI/p4W31Eketazo
# Nxy23RT0zDsvJrwEC3R+/MRdkB7aTecsYmMeMHgtUrl3xEO3FubnQ0kKEU/HBCTd
# hR14GsQEccQQE6grFVlglrew+FzehWUu3SUQEp9t+iWpX/KfviDWx0H1azilMX15
# lzJUxK7kCzmflrk5jCOCjKqhOdGJoQqstmwP+07qXO18bcCzEC908P+TYkh0z9gV
# rlj7tyW9K9zPVPJZsLRaBp/QjMcH65o9Y1hD1uWtFQYmbEYkT1K9tuXHtQYx1Rpf
# /dC8Nbl4iukwggWRMIIEeaADAgECAhAKcaGwwpb1x5BlRwo8IFN+MA0GCSqGSIb3
# DQEBCwUAMFoxCzAJBgNVBAYTAlVTMRgwFgYDVQQKEw8uTkVUIEZvdW5kYXRpb24x
# MTAvBgNVBAMTKC5ORVQgRm91bmRhdGlvbiBQcm9qZWN0cyBDb2RlIFNpZ25pbmcg
# Q0EwHhcNMTgxMDI1MDAwMDAwWhcNMjExMDI5MTIwMDAwWjCBjDEUMBIGA1UEBRML
# NjAzIDM4OSAwNjgxCzAJBgNVBAYTAlVTMQswCQYDVQQIEwJ3YTEQMA4GA1UEBxMH
# UmVkbW9uZDEjMCEGA1UEChMaSnNvbi5ORVQgKC5ORVQgRm91bmRhdGlvbikxIzAh
# BgNVBAMTGkpzb24uTkVUICguTkVUIEZvdW5kYXRpb24pMIIBIjANBgkqhkiG9w0B
# AQEFAAOCAQ8AMIIBCgKCAQEAuN7uHXlw69xJiDtuvtaEmG2+W4tcwqucLUj7xf+D
# W5mTiqIa4uSUZ1upBEQmpNnjHV6iKvMzDLjMsjNEhgMnkoL+6HBvhy5GOyKpG/T3
# NHxX6bUN3JTExwZUD/CpXHIh31n3KSthNRlXyZod+5hWuC93UJxzNcXtR7iYR39x
# 5oIx4IEdq0xyXt9PclKu/V6R9D7Fjllx9aKlTg/hnPoMKF35P7kxMv5ULagSv3HO
# JkKa70biuqv8625wBE9HNA9S5LXNFMk2ZuUhYTxLk1qYx58me+NU6VUvoDsqzLx+
# GXTAkl6r1kSeDMskOe4fXevoCkPm1BFZHd68MmH6tIuMLQIDAQABo4ICHjCCAhow
# HwYDVR0jBBgwFoAUy1yTroib/kNvVlBSAm14/KzhsVowHQYDVR0OBBYEFF4wRcl3
# CrbuKXpbC4oTKXnW5tsmMDQGA1UdEQQtMCugKQYIKwYBBQUHCAOgHTAbDBlVUy1X
# QVNISU5HVE9OLTYwMyAzODkgMDY4MA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAK
# BggrBgEFBQcDAzCBmQYDVR0fBIGRMIGOMEWgQ6BBhj9odHRwOi8vY3JsMy5kaWdp
# Y2VydC5jb20vTkVURm91bmRhdGlvblByb2plY3RzQ29kZVNpZ25pbmdDQS5jcmww
# RaBDoEGGP2h0dHA6Ly9jcmw0LmRpZ2ljZXJ0LmNvbS9ORVRGb3VuZGF0aW9uUHJv
# amVjdHNDb2RlU2lnbmluZ0NBLmNybDBMBgNVHSAERTBDMDcGCWCGSAGG/WwDATAq
# MCgGCCsGAQUFBwIBFhxodHRwczovL3d3dy5kaWdpY2VydC5jb20vQ1BTMAgGBmeB
# DAEEATCBhAYIKwYBBQUHAQEEeDB2MCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5k
# aWdpY2VydC5jb20wTgYIKwYBBQUHMAKGQmh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0
# LmNvbS9ORVRGb3VuZGF0aW9uUHJvamVjdHNDb2RlU2lnbmluZ0NBLmNydDAMBgNV
# HRMBAf8EAjAAMA0GCSqGSIb3DQEBCwUAA4IBAQCgwZvcU0JuY6FZlCQFuHPKr2Ay
# dS1d+SAqK7gT3RCtdzzPJBt8VWhdaEMoHvAbsEdLFPdY9+iIL0LxdiQHUzvE6c37
# gvPpuCsM6ZzM/T4OmUEcKCadYQAssYJ5F2PdvZv+3Rlddp2RtBN3WmNEfbOCRFRB
# jvrR6Oxep8ZJyJAv4fyJrFVSNj/jS7IkyYLQJChC70Rh6UF7JChyEZaG15/NPUUB
# jIq8u5phqDvIGFyem1BV5oeoC6AA2wqcRwobgaq7v8GIaAcVA902B3aLEEKFH1HL
# z1yOs+AEcP+Sf3LXQPM4jITKLXQFhHVGUqSOwIW/RDWMl4gk1eEzLh2Nw4HyMYIR
# HDCCERgCAQEwbjBaMQswCQYDVQQGEwJVUzEYMBYGA1UEChMPLk5FVCBGb3VuZGF0
# aW9uMTEwLwYDVQQDEyguTkVUIEZvdW5kYXRpb24gUHJvamVjdHMgQ29kZSBTaWdu
# aW5nIENBAhAKcaGwwpb1x5BlRwo8IFN+MA0GCWCGSAFlAwQCAQUAoIG0MBkGCSqG
# SIb3DQEJAzEMBgorBgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3
# AgEVMC8GCSqGSIb3DQEJBDEiBCAZGkaNDZElj6EbTRRaxc87TNeotXotZssdYQoZ
# IM67bzBIBgorBgEEAYI3AgEMMTowOKASgBAASgBzAG8AbgAuAE4ARQBUoSKAIGh0
# dHBzOi8vd3d3Lm5ld3RvbnNvZnQuY29tL2pzb24gMA0GCSqGSIb3DQEBAQUABIIB
# AIQ7zYK0PQ8XuLSrELc/0IhwMJVA/gZHFtsbOiXAx8vf1vXz3I6b59a/het1hRLL
# npGZ9QubzJ+/Qp3jsDbqxJWAoarm6ZmqeJ+sAeUUBIqtQ8T79MiqcJKjM/k7QUd5
# DspZYXPz+8M8VY0D4df2SXtguvbsjQAW5n4oSf9PmMxkAPs5I4lkxAQchRYF08c3
# Z2Gf9U9O2k65a+gDJMvlI5dyrP4MYhvUVkbJfPTZFGXUqyjLQWMHQQbtsMEim0KO
# HvDaQ0BLnTjP/TqyL+LygLzjGawPBXDVxGB0/E6zIdSEGVpBjjers9bjRsEK87q9
# 9QMmN6qvjgKeyIIdln1yExGhgg7IMIIOxAYKKwYBBAGCNwMDATGCDrQwgg6wBgkq
# hkiG9w0BBwKggg6hMIIOnQIBAzEPMA0GCWCGSAFlAwQCAQUAMHcGCyqGSIb3DQEJ
# EAEEoGgEZjBkAgEBBglghkgBhv1sBwEwMTANBglghkgBZQMEAgEFAAQghUDfZ4zQ
# tcVhn/mddDvpjBjNzaIhfv9SGXFHwgXD/8UCEHyVCZsiXeQv59ss9V5RkwsYDzIw
# MTgxMTI3MTgwNzQzWqCCC7swggaCMIIFaqADAgECAhAJwPxGyARCE7VZi68oT05B
# MA0GCSqGSIb3DQEBCwUAMHIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xMTAvBgNVBAMTKERpZ2lD
# ZXJ0IFNIQTIgQXNzdXJlZCBJRCBUaW1lc3RhbXBpbmcgQ0EwHhcNMTcwMTA0MDAw
# MDAwWhcNMjgwMTE4MDAwMDAwWjBMMQswCQYDVQQGEwJVUzERMA8GA1UEChMIRGln
# aUNlcnQxKjAoBgNVBAMTIURpZ2lDZXJ0IFNIQTIgVGltZXN0YW1wIFJlc3BvbmRl
# cjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAJ6VmGo0O3MbqH78x74p
# aYnHaCZGXz2NYnOHgaOhnPC3WyQ3WpLU9FnXdonk3NUn8NVmvArutCsxZ6xYxUqR
# WStFHgkB1mSzWe6NZk37I17MEA0LimfvUq6gCJDCUvf1qLVumyx7nee1Pvt4zTJQ
# GL9AtUyMu1f0oE8RRWxCQrnlr9bf9Kd8CmiWD9JfKVfO+x0y//QRoRMi+xLL79dT
# 0uuXy6KsGx2dWCFRgsLC3uorPywihNBD7Ds7P0fE9lbcRTeYtGt0tVmveFdpyA8J
# Anjd2FPBmdtgxJ3qrq/gfoZKXKlYYahedIoBKGhyTqeGnbUCUodwZkjTju+BJMzc
# 2GUCAwEAAaOCAzgwggM0MA4GA1UdDwEB/wQEAwIHgDAMBgNVHRMBAf8EAjAAMBYG
# A1UdJQEB/wQMMAoGCCsGAQUFBwMIMIIBvwYDVR0gBIIBtjCCAbIwggGhBglghkgB
# hv1sBwEwggGSMCgGCCsGAQUFBwIBFhxodHRwczovL3d3dy5kaWdpY2VydC5jb20v
# Q1BTMIIBZAYIKwYBBQUHAgIwggFWHoIBUgBBAG4AeQAgAHUAcwBlACAAbwBmACAA
# dABoAGkAcwAgAEMAZQByAHQAaQBmAGkAYwBhAHQAZQAgAGMAbwBuAHMAdABpAHQA
# dQB0AGUAcwAgAGEAYwBjAGUAcAB0AGEAbgBjAGUAIABvAGYAIAB0AGgAZQAgAEQA
# aQBnAGkAQwBlAHIAdAAgAEMAUAAvAEMAUABTACAAYQBuAGQAIAB0AGgAZQAgAFIA
# ZQBsAHkAaQBuAGcAIABQAGEAcgB0AHkAIABBAGcAcgBlAGUAbQBlAG4AdAAgAHcA
# aABpAGMAaAAgAGwAaQBtAGkAdAAgAGwAaQBhAGIAaQBsAGkAdAB5ACAAYQBuAGQA
# IABhAHIAZQAgAGkAbgBjAG8AcgBwAG8AcgBhAHQAZQBkACAAaABlAHIAZQBpAG4A
# IABiAHkAIAByAGUAZgBlAHIAZQBuAGMAZQAuMAsGCWCGSAGG/WwDFTAfBgNVHSME
# GDAWgBT0tuEgHf4prtLkYaWyoiWyyBc1bjAdBgNVHQ4EFgQU4acySu4BISh9VNXy
# B5JutAcPPYcwcQYDVR0fBGowaDAyoDCgLoYsaHR0cDovL2NybDMuZGlnaWNlcnQu
# Y29tL3NoYTItYXNzdXJlZC10cy5jcmwwMqAwoC6GLGh0dHA6Ly9jcmw0LmRpZ2lj
# ZXJ0LmNvbS9zaGEyLWFzc3VyZWQtdHMuY3JsMIGFBggrBgEFBQcBAQR5MHcwJAYI
# KwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBPBggrBgEFBQcwAoZD
# aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0U0hBMkFzc3VyZWRJ
# RFRpbWVzdGFtcGluZ0NBLmNydDANBgkqhkiG9w0BAQsFAAOCAQEAHvBBgjKu7fG0
# NRPcUMLVl64iIp0ODq8z00z9fL9vARGnlGUiXMYiociJUmuajHNc2V4/Mt4WYEyL
# Nv0xmQq9wYS3jR3viSYTBVbzR81HW62EsjivaiO1ReMeiDJGgNK3ppki/cF4z/WL
# 2AyMBQnuROaA1W1wzJ9THifdKkje2pNlrW5lo5mnwkAOc8xYT49FKOW8nIjmKM5g
# XS0lXYtzLqUNW1Hamk7/UAWJKNryeLvSWHiNRKesOgCReGmJZATTXZbfKr/5pUws
# k//mit2CrPHSs6KGmsFViVZqRz/61jOVQzWJBXhaOmnaIrgEQ9NvaDU2ehQ+RemY
# ZIYPEwwmSjCCBTEwggQZoAMCAQICEAqhJdbWMht+QeQF2jaXwhUwDQYJKoZIhvcN
# AQELBQAwZTELMAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IEluYzEZMBcG
# A1UECxMQd3d3LmRpZ2ljZXJ0LmNvbTEkMCIGA1UEAxMbRGlnaUNlcnQgQXNzdXJl
# ZCBJRCBSb290IENBMB4XDTE2MDEwNzEyMDAwMFoXDTMxMDEwNzEyMDAwMFowcjEL
# MAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IEluYzEZMBcGA1UECxMQd3d3
# LmRpZ2ljZXJ0LmNvbTExMC8GA1UEAxMoRGlnaUNlcnQgU0hBMiBBc3N1cmVkIElE
# IFRpbWVzdGFtcGluZyBDQTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEB
# AL3QMu5LzY9/3am6gpnFOVQoV7YjSsQOB0UzURB90Pl9TWh+57ag9I2ziOSXv2Mh
# kJi/E7xX08PhfgjWahQAOPcuHjvuzKb2Mln+X2U/4Jvr40ZHBhpVfgsnfsCi9aDg
# 3iI/Dv9+lfvzo7oiPhisEeTwmQNtO4V8CdPuXciaC1TjqAlxa+DPIhAPdc9xck4K
# rd9AOly3UeGheRTGTSQjMF287DxgaqwvB8z98OpH2YhQXv1mblZhJymJhFHmgudG
# UP2UKiyn5HU+upgPhH+fMRTWrdXyZMt7HgXQhBlyF/EXBu89zdZN7wZC/aJTKk+F
# HcQdPK/P2qwQ9d2srOlW/5MCAwEAAaOCAc4wggHKMB0GA1UdDgQWBBT0tuEgHf4p
# rtLkYaWyoiWyyBc1bjAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAS
# BgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggr
# BgEFBQcDCDB5BggrBgEFBQcBAQRtMGswJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3Nw
# LmRpZ2ljZXJ0LmNvbTBDBggrBgEFBQcwAoY3aHR0cDovL2NhY2VydHMuZGlnaWNl
# cnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNydDCBgQYDVR0fBHoweDA6
# oDigNoY0aHR0cDovL2NybDQuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElE
# Um9vdENBLmNybDA6oDigNoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lD
# ZXJ0QXNzdXJlZElEUm9vdENBLmNybDBQBgNVHSAESTBHMDgGCmCGSAGG/WwAAgQw
# KjAoBggrBgEFBQcCARYcaHR0cHM6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzALBglg
# hkgBhv1sBwEwDQYJKoZIhvcNAQELBQADggEBAHGVEulRh1Zpze/d2nyqY3qzeM8G
# N0CE70uEv8rPAwL9xafDDiBCLK938ysfDCFaKrcFNB1qrpn4J6JmvwmqYN92pDqT
# D/iy0dh8GWLoXoIlHsS6HHssIeLWWywUNUMEaLLbdQLgcseY1jxk5R9IEBhfiThh
# TWJGJIdjjJFSLK8pieV4H9YLFKWA1xJHcLN11ZOFk362kmf7U2GJqPVrlsD0WGkN
# fMgBsbkodbeZY4UijGHKeZR+WfyMD+NvtQEmtmyl7odRIeRYYJu6DC0rbaLEfrvE
# JStHAgh8Sa4TtuF8QkIoxhhWz0E0tmZdtnR79VYzIi8iNrJLokqV2PWmjlIxggJN
# MIICSQIBATCBhjByMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5j
# MRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMTEwLwYDVQQDEyhEaWdpQ2VydCBT
# SEEyIEFzc3VyZWQgSUQgVGltZXN0YW1waW5nIENBAhAJwPxGyARCE7VZi68oT05B
# MA0GCWCGSAFlAwQCAQUAoIGYMBoGCSqGSIb3DQEJAzENBgsqhkiG9w0BCRABBDAc
# BgkqhkiG9w0BCQUxDxcNMTgxMTI3MTgwNzQzWjAvBgkqhkiG9w0BCQQxIgQggHeC
# kikbYHMJgmIMNGpjxASMjCoN7cwEcNl60095KugwKwYLKoZIhvcNAQkQAgwxHDAa
# MBgwFgQUQAGRR1yYiR3roQSvRwkbXrbUy8swDQYJKoZIhvcNAQEBBQAEggEAK7Hs
# O6NpvuI3BT95BvVyKeI7gaOhraHs6KXcDJ8hZfUBBvlytPL5ezZGgZCwxaazqF01
# IGIGKWU0XUp8ryE+0wqadnGJFKTSwZ7ua8+pP2d/g3RyeSDsVkRykWCna7PSTB98
# 1i3r7TNfMuDlWdfJ2q/FXs7XalgctRluJhtQYJnb4R9ubsxwqeGSQb7EfKJM+6I4
# gMyLajWhgeVMMQRtfmTnilSkgasDPTUSfEeg8easAJKPh7x2hUoPZteJkt8XrX81
# dZ3t08fbl1yfq/PDJhfE/6HQqu7YeSAKM4VGQSOxv3QuLMN80tVLucT12ph57Di7
# zkL9HZz4/ps+S/Eupg==
# SIG # End signature block
