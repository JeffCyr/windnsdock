if (Test-Path .\build\publish) {
    Remove-Item .\build\publish -Recurse -Force | Out-Null
}

dotnet publish -c Release -o .\build\publish
docker build -t windnsdock .\build