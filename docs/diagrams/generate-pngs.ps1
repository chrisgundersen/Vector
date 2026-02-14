# Generate PNG images from diagram files
# Prerequisites:
# - PlantUML JAR file (download from https://plantuml.com/download)
# - Mermaid CLI: npm install -g @mermaid-js/mermaid-cli
# - Java runtime (for PlantUML)

param(
    [string]$PlantUmlJar = "plantuml.jar",
    [switch]$MermaidOnly,
    [switch]$PlantUmlOnly
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Generating PNG images from diagrams..." -ForegroundColor Cyan

# Generate from Mermaid files
if (-not $PlantUmlOnly) {
    Write-Host "`nGenerating Mermaid diagrams..." -ForegroundColor Yellow

    $mermaidFiles = Get-ChildItem -Path $ScriptDir -Filter "*.mmd"

    if (Get-Command mmdc -ErrorAction SilentlyContinue) {
        foreach ($file in $mermaidFiles) {
            $outputFile = $file.FullName -replace '\.mmd$', '.png'
            Write-Host "  Processing: $($file.Name)"
            mmdc -i $file.FullName -o $outputFile -b white
        }
        Write-Host "  Mermaid diagrams generated successfully!" -ForegroundColor Green
    } else {
        Write-Host "  WARNING: mermaid-cli (mmdc) not found. Install with: npm install -g @mermaid-js/mermaid-cli" -ForegroundColor Yellow
        Write-Host "  Alternatively, use https://mermaid.live/ to generate PNGs manually" -ForegroundColor Yellow
    }
}

# Generate from PlantUML files
if (-not $MermaidOnly) {
    Write-Host "`nGenerating PlantUML diagrams..." -ForegroundColor Yellow

    $pumlFiles = Get-ChildItem -Path $ScriptDir -Filter "*.puml"

    if (Test-Path $PlantUmlJar) {
        foreach ($file in $pumlFiles) {
            Write-Host "  Processing: $($file.Name)"
            java -jar $PlantUmlJar $file.FullName
        }
        Write-Host "  PlantUML diagrams generated successfully!" -ForegroundColor Green
    } else {
        Write-Host "  WARNING: PlantUML JAR not found at: $PlantUmlJar" -ForegroundColor Yellow
        Write-Host "  Download from: https://plantuml.com/download" -ForegroundColor Yellow
        Write-Host "  Or use VS Code PlantUML extension to generate PNGs" -ForegroundColor Yellow
    }
}

Write-Host "`nDone!" -ForegroundColor Cyan
Write-Host "Generated PNGs are in: $ScriptDir" -ForegroundColor Gray
