[CmdletBinding()]
param(
    [string]$WmExePath,
    [string]$TestDataFile,
    [string]$Header,
    [string]$Footer,
    [switch]$ShowOutput
)

$ErrorActionPreference = 'Stop'

function Encode-String([string]$str) { $str -replace '"', '""' }


if (-not $testDataFile) {
    $testDataFile = Join-Path $PSScriptRoot tests.txt
}

if (-not $wmExePath) {
     $wmExeSearch =
        '.exe', '.cmd', '.bat', '.sh', '' |
            % { Join-Path $PSScriptRoot "wm$_" } |
            ? { Test-Path -PathType Leaf $_ } |
            select -First 1
    $wmExePath = if ($wmExeSearch) { $wmExeSearch } else { 'wm' }
}

$generator = {

    if ($header) { echo $header }

    echo "namespace WildWildMatch.Tests
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using NUnit.Framework;

    partial class Tests
    {
        [GeneratedCode(""$(Split-Path -Leaf $PSCommandPath)"", ""1.0"")]
        static IEnumerable<TestCaseData> WmExeTestData()
        {"

    $ln = 1
    type $testDataFile |
        % { New-Object psobject -Property @{ LineNumber = $ln++; Text = $_ } } |
        ? { $_.Text -match '^[01] +[01] +(''.+?''|.+?) +(.+)' } |
        % {
            New-Object psobject -Property @{ Text = $matches[1]; Pattern = $matches[2]; LineNumber = $_.LineNumber }
        } |
        ? { $_.Text.Length -gt 0 -and $_.Pattern.Length -gt 0 } |
        % {
            & $wmExePath $_.Pattern $_.Text 1 | Out-Null
            echo "            yield return new TestCaseData($LASTEXITCODE, @`"$(Encode-String($_.Pattern))`", @`"$(Encode-String($_.Text))`", $($_.LineNumber));"
        }

    echo "
        }
    }
}"

    if ($footer) { echo $footer }
}

if ($showOutput) {
    & $generator
} else {
    $outFile = Join-Path $PSScriptRoot TestData.cs
    [IO.File]::WriteAllLines($outFile, (& $generator))
}
