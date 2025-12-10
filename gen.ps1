# Output file name
$outputFile = "context_for_ai.txt"

# 1. Folders to completely ignore
$foldersToIgnore = @(
    "bin", "obj", ".git", ".github", ".vs", ".vscode", ".idea", 
    "wwwroot\lib", "node_modules", "Properties", "Migrations", 
    "dist", "TestResults"
)

# 2. File extensions to include
# ADDED: *.html
$extensionsToInclude = @(
    "*.cs", "*.razor", "*.css", "*.js", "*.sql", "*.json", "*.csproj", "*.html"
)

# 3. Specific filenames to ignore (Exact Match)
# ADDED: Class1.cs, TestConnection.cs
$filesToIgnore = @(
    "package-lock.json", 
    "yarn.lock",
    "appsettings.Development.json",
    "compilerconfig.json.defaults",
    "launchSettings.json",
    "Class1.cs",
    "TestConnection.cs"
)

# 4. Wildcard patterns to ignore
$filenamePatternsToIgnore = @(
    "bootstrap*",       # Exclude bootstrap
    "jquery*",          # Exclude jquery
    "popper*",          # Exclude popper
    "*.min.*",          # Exclude minified files
    "*.designer.cs",    # Exclude auto-generated code
    "AssemblyInfo.cs",  # Exclude metadata
    "GlobalUsings.cs",  # Exclude boilerplate
    "*.svg",            # Exclude vectors
    "*.Tests.csproj"    # Exclude Test project files
)

# 5. Path patterns to ignore (New Category)
# Exclude any file path that contains these strings
$pathPatternsToIgnore = @(
    "\StorhaugenEats.API.Tests\" # Exclude the entire Test project folder content
)

Write-Host "Generating context file..." -ForegroundColor Cyan

$header = @"
================================================================================
PROJECT CONTEXT FILE
Generated on: $(Get-Date)
================================================================================
"@
Set-Content -Path $outputFile -Value $header

function Get-SourceFiles {
    param ([string]$Path)
    $items = Get-ChildItem -Path $Path -Force
    
    foreach ($item in $items) {
        if ($item.PSIsContainer) {
            if ($foldersToIgnore -contains $item.Name) { continue }
            Get-SourceFiles -Path $item.FullName
        }
        else {
            $ext = "*" + $item.Extension
            
            # --- FILTERING LOGIC ---

            # 1. Check Exact Ignored Filenames
            if ($filesToIgnore -contains $item.Name) { continue }

            # 2. Check Wildcard Patterns
            $isNoise = $false
            foreach ($pattern in $filenamePatternsToIgnore) {
                if ($item.Name -like $pattern) { $isNoise = $true; break }
            }
            if ($isNoise) { continue }

            # 3. Check Path Patterns (Exclude Tests)
            foreach ($pathPattern in $pathPatternsToIgnore) {
                if ($item.FullName -like "*$pathPattern*") { $isNoise = $true; break }
            }
            if ($isNoise) { continue }

            # 4. Check Allowed Extensions
            if ($extensionsToInclude -contains $ext) {
                
                # SPECIAL RULE: CSS
                # Only allow CSS if in specific folder OR is scoped css
                if ($ext -eq ".css") {
                    $isInWwwRoot = $item.FullName -match "StorhaugenWebsite\\wwwroot\\css"
                    $isScopedCss = $item.Name -match ".razor.css"
                    
                    if (-not ($isInWwwRoot -or $isScopedCss)) { continue }
                }

                # Output the file
                $item
            }
        }
    }
}

$allFiles = Get-SourceFiles -Path .

# Generate Tree
Write-Host "Generating file tree..." -ForegroundColor Green
Add-Content -Path $outputFile -Value "PROJECT STRUCTURE:`n=================="
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace($PWD.Path, "")
    Add-Content -Path $outputFile -Value $relativePath
}
Add-Content -Path $outputFile -Value "`n`n"

# Append Content
Write-Host "Appending file contents..." -ForegroundColor Green
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace($PWD.Path, "")
    $fileHeader = "`n================================================================================`nFILE START: $relativePath`n================================================================================"
    Add-Content -Path $outputFile -Value $fileHeader
    
    try {
        $content = Get-Content -Path $file.FullName -Raw
        Add-Content -Path $outputFile -Value $content
    }
    catch {
        Add-Content -Path $outputFile -Value "[Error reading file]"
    }
    
    Add-Content -Path $outputFile -Value "`n================================================================================`nFILE END: $relativePath`n================================================================================`n"
}

Write-Host "Done! Clean context saved to $outputFile" -ForegroundColor Cyan