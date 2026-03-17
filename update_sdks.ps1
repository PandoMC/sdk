$swaggerUrl = "https://api.test.missioncontrol.io/swagger/PartnerV2_0/swagger.json"
$swaggerFile = "swagger.json"

# Download file as raw text
$json = Invoke-WebRequest $swaggerUrl -UseBasicParsing | Select-Object -ExpandProperty Content

# Apply server url fix and save
Write-Host "Fixing server url and paths"
$json = $json -replace "/partnerApi/v2", ""
$json = $json -replace '("url"\s*:\s*"(https?:\/\/[^"]+))"', '$1/partnerApi/v2"'
Set-Content -Path $swaggerFile -Value $json -Encoding UTF8

# dotnet
Write-Host "Updating dotnet"
$csharpOutput = "./dotnet/src/MissionControl.Client/Generated"
kiota generate `
    --openapi $swaggerFile `
    --additional-data false `
    --language csharp `
    --output $csharpOutput `
    --class-name Client `
    --clean-output `
    --namespace-name MissionControl.Client.Generated

# typescript
Write-Host "Updating typescript"
kiota generate `
    --openapi $swaggerFile `
    --additional-data false `
    --language typescript `
    --output ./typescript/src/client/generated `
    --class-name client `
    --namespace-name missioncontrol.client
