$ErrorActionPreference = "Stop"

# Downloads an OpenAPI document and moves its api base path from the individual paths to the server url,
# so the generated clients expose e.g. client.Product instead of client.PartnerApi.V2.Product.
function Get-OpenApiDocument([string]$url, [string]$file, [string]$apiBasePath, [string]$serverUrl) {
    Write-Host "Downloading $url"
    $json = Invoke-WebRequest $url -UseBasicParsing | Select-Object -ExpandProperty Content

    $json = $json -replace [regex]::Escape($apiBasePath), ""
    $json = $json -replace '("servers"\s*:\s*\[\s*\{\s*"url"\s*:\s*")[^"]*"', "`$1$serverUrl$apiBasePath`""
    Set-Content -Path $file -Value $json -Encoding UTF8

    # Rewrite OpenAPI 3.1 constructs Kiota handles poorly, keeping the generated SDK surface stable
    node ./normalize_swagger.mjs $file
    if ($LASTEXITCODE -ne 0) { throw "Normalizing $file failed" }
}

# The partner API consists of two services behind the same gateway: core (orders, products, ...) and assets.
$coreFile = "swagger.json"
$assetsFile = "swagger.assets.json"
Get-OpenApiDocument "https://api.missioncontrol.io/openapi/PartnerV2_0.json" $coreFile "/partnerApi/v2" "https://api.missioncontrol.io"
Get-OpenApiDocument "https://api.missioncontrol.io/assets/openapi/PartnerV1_0.json" $assetsFile "/partnerApi/v1" "https://api.missioncontrol.io/assets"

# dotnet (core first: its --clean-output also removes the nested Assets folder)
Write-Host "Updating dotnet"
$csharpOutput = "./dotnet/src/MissionControl.Client/Generated"
kiota generate `
    --openapi $coreFile `
    --language csharp `
    --output $csharpOutput `
    --class-name Client `
    --clean-output `
    --namespace-name MissionControl.Client.Generated

kiota generate `
    --openapi $assetsFile `
    --language csharp `
    --output "$csharpOutput/Assets" `
    --class-name AssetsClient `
    --clean-output `
    --namespace-name MissionControl.Client.Generated.Assets

# node
Write-Host "Updating node"
kiota generate `
    --openapi $coreFile `
    --language typescript `
    --output ./node/src/client/generated `
    --class-name client `
    --namespace-name missioncontrol.client

kiota generate `
    --openapi $assetsFile `
    --language typescript `
    --output ./node/src/client/generatedAssets `
    --class-name assetsClient `
    --clean-output `
    --namespace-name missioncontrol.client.assets
