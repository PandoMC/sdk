$swaggerUrl = "https://api.test.missioncontrol.io/swagger/PartnerV2_0/swagger.json"
$swaggerFile = "swagger.json"

# Download file as raw text
$json = Invoke-WebRequest $swaggerUrl -UseBasicParsing | Select-Object -ExpandProperty Content

Write-Host "Fixing server url and paths"

# Step 1: Remove all /partnerApi/v2
$json = $json -replace "/partnerApi/v2", ""

# Step 2: Add /partnerApi/v2 after domain in server urls
# Matches: "url": "https://something"
$json = $json -replace '("url"\s*:\s*"(https?:\/\/[^"]+))"', '$1/partnerApi/v2"'

# Save result
Set-Content -Path $swaggerFile -Value $json -Encoding UTF8

Write-Host "Updating dotnet"

kiota generate `
    --openapi $swaggerFile `
    --additional-data false `
    --language csharp `
    --output ./dotnet/src/MissionControl.Client/Generated `
    --class-name Client `
    --namespace-name MissionControl.Client.Generated


# Write-Host "Updating typescript"

# kiota generate `
#     --openapi $swaggerFile `
#     --additional-data false `
#     --language typescript `
#     --output ./typescript/src/client/generated `
#     --class-name client `
#     --namespace-name missioncontrol.client
