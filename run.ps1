param(
    [parameter(Mandatory=$true)]
    [string]$AccessToken,

    [string]$Configuration = "Debug"
)

function Run-Project {
    Write-Output "Building project in $Configuration mode..."
    dotnet run --no-build --configuration $Configuration --project "src/Chatbot/Chatbot.fsproj" --AccessToken=$AccessToken
}

Run-Project
