param(
    [string]$Configuration = "Debug"
)

function Build-Project {
    Write-Output "Building project in $Configuration mode..."
    dotnet build --configuration $Configuration
}

Push-Location "src/ChatBot"

Build-Project

Pop-Location