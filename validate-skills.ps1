#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates NScreenPlay Skill files for correctness and compatibility with skills.sh.
.DESCRIPTION
    Checks all skills under skills/ directory for:
    - File existence
    - Proper YAML frontmatter (name and description)
    - Name/directory consistency
    - Markdown validity
.EXAMPLE
    .\validate-skills.ps1
.NOTES
    Exit code: 0 if all valid, 1 if any errors found.
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'
$errors = @()
$warnings = @()
$validSkills = @()
$skillsRoot = Join-Path $PSScriptRoot 'skills'

function Write-Log {
    param([string]$message, [string]$severity = 'INFO')
    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] [$severity] $message"
}

function Extract-FrontmatterValue {
    param([string]$filePath, [string]$key)
    try {
        $lines = @(Get-Content $filePath -Encoding UTF8)
        if ($lines.Count -eq 0 -or $lines[0] -ne '---') {
            return $null
        }
        
        for ($i = 1; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -eq '---') {
                break
            }
            if ($lines[$i].StartsWith("$key`:")) {
                $value = $lines[$i].Substring("$key`:".Length).Trim()
                return $value -replace '^"|"$', ''
            }
        }
    }
    catch { }
    return $null
}

function Validate-FrontmatterSyntax {
    param([string]$filePath)
    try {
        $content = Get-Content $filePath -Raw
        if (-not ($content -match '^---')) {
            return @{ Valid = $false; Error = 'Frontmatter missing: file does not start with ---' }
        }
        if (-not ($content -match '^---\r?\n([\s\S]*?)\r?\n---\r?\n')) {
            return @{ Valid = $false; Error = 'Frontmatter block not properly closed' }
        }
        return @{ Valid = $true; Error = $null }
    }
    catch {
        return @{ Valid = $false; Error = $_.Exception.Message }
    }
}

# Validate all skills
if (-not (Test-Path $skillsRoot -PathType Container)) {
    Write-Log "CRITICAL: Skills directory not found at: $skillsRoot" -Severity ERROR
    exit 1
}

$expectedSkills = @('screenplay', 'playwright', 'reqnroll', 'test-authoring', 'test-review', 'failure-analysis', 'healing')
$foundSkills = @()

Write-Log "Validating NScreenPlay Skills..." -Severity INFO
Write-Log "Skills root: $skillsRoot" -Severity INFO
Write-Log ""

foreach ($skillName in $expectedSkills) {
    $skillDir = Join-Path $skillsRoot $skillName
    $skillFile = Join-Path $skillDir 'SKILL.md'
    
    Write-Log "Checking skill: $skillName"
    
    # Check if directory exists
    if (-not (Test-Path $skillDir -PathType Container)) {
        $errors += "CRITICAL: Skill directory missing: $skillDir"
        Write-Log "  ❌ Directory missing: $skillDir" -Severity ERROR
        continue
    }
    
    # Check if SKILL.md exists
    if (-not (Test-Path $skillFile -PathType Leaf)) {
        $errors += "CRITICAL: SKILL.md missing in $skillDir"
        Write-Log "  ❌ SKILL.md not found: $skillFile" -Severity ERROR
        continue
    }
    
    # Validate frontmatter syntax
    $syntax = Validate-FrontmatterSyntax -filePath $skillFile
    if (-not $syntax.Valid) {
        $errors += "CRITICAL: Invalid frontmatter syntax in $skillFile - $($syntax.Error)"
        Write-Log "  ❌ Frontmatter syntax error: $($syntax.Error)" -Severity ERROR
        continue
    }
    
    # Extract frontmatter values
    $name = Extract-FrontmatterValue -filePath $skillFile -key 'name'
    $description = Extract-FrontmatterValue -filePath $skillFile -key 'description'
    
    # Validate name exists
    if ([string]::IsNullOrWhiteSpace($name)) {
        $errors += "CRITICAL: 'name' field missing or empty in $skillFile"
        Write-Log "  ❌ 'name' field missing or empty" -Severity ERROR
        continue
    }
    
    # Validate description exists
    if ([string]::IsNullOrWhiteSpace($description)) {
        $errors += "CRITICAL: 'description' field missing or empty in $skillFile"
        Write-Log "  ❌ 'description' field missing or empty" -Severity ERROR
        continue
    }
    
    # Validate name matches directory
    if ($name -ne $skillName) {
        $errors += "CRITICAL: Name mismatch in $skillFile. Expected '$skillName', got '$name'"
        Write-Log "  ❌ Name mismatch: expected '$skillName', got '$name'" -Severity ERROR
        continue
    }
    
    # Validate description length
    if ($description.Length -lt 20) {
        $warnings += "WARNING: Description too short in $skillFile ($($description.Length) chars). Aim for 50+ chars."
        Write-Log "  ⚠️  Description is short: '$description'" -Severity WARN
    }
    
    Write-Log "  ✅ Valid skill" -Severity INFO
    Write-Log "     name: $name" -Severity INFO
    Write-Log "     description: $($description.Substring(0, [Math]::Min(60, $description.Length)))..." -Severity INFO
    
    $validSkills += $skillName
    $foundSkills += $skillName
}

Write-Log ""
Write-Log "=== SUMMARY ===" -Severity INFO

# Summary table
$summary = @()
foreach ($skillName in $expectedSkills) {
    if ($skillName -in $validSkills) {
        $summary += "  ✅ $skillName"
    } else {
        $summary += "  ❌ $skillName"
    }
}

$summary | ForEach-Object { Write-Log $_ -Severity INFO }

Write-Log ""
Write-Log "Total expected: $($expectedSkills.Count)" -Severity INFO
Write-Log "Total valid: $($validSkills.Count)" -Severity INFO
Write-Log "Total errors: $($errors.Count)" -Severity INFO
Write-Log "Total warnings: $($warnings.Count)" -Severity INFO

if ($errors.Count -gt 0) {
    Write-Log ""
    Write-Log "=== ERRORS ===" -Severity ERROR
    $errors | ForEach-Object { Write-Log "  • $_" -Severity ERROR }
}

if ($warnings.Count -gt 0) {
    Write-Log ""
    Write-Log "=== WARNINGS ===" -Severity WARN
    $warnings | ForEach-Object { Write-Log "  • $_" -Severity WARN }
}

Write-Log ""

if ($errors.Count -eq 0 -and $validSkills.Count -eq $expectedSkills.Count) {
    Write-Log "✅ All skills validated successfully!" -Severity INFO
    exit 0
} else {
    Write-Log "❌ Skill validation failed" -Severity ERROR
    exit 1
}
