param(
    [string]$Configuration = "Debug"
)

function Run-Project {
    Write-Output "Building project in $Configuration mode..."
    dotnet run --no-build --configuration $Configuration --project "src/Chatbot/Chatbot.fsproj"
}

Run-Project
