param(
    [string]$SecretId = "SQLServerRDSSecret",
    [string]$Database = "BookStoreClassic",
    [string]$WebConfigPath = "..\app\Bookstore.Web\Web.config"
)

$scriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent

try {
    # Get credentials from AWS Secrets Manager
    $secret = (Get-SECSecretValue -SecretId $SecretId).SecretString | ConvertFrom-Json
    
    # Get RDS endpoint
    $endpoint = (Get-RDSDBInstance | Select-Object -First 1).Endpoint.Address -replace ':1433$'
    
    # Build connection string
    $connectionString = "Server=$endpoint;Database=$Database;User Id=$($secret.username);Password=$($secret.password);"
    
    # Update web.config
    $configPath = Join-Path $scriptPath $WebConfigPath
    $xml = [xml](Get-Content $configPath)
    $xml.configuration.connectionStrings.add.connectionString = $connectionString
    $xml.Save($configPath)
    
    Write-Host "Connection string updated successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to update connection string: $($_.Exception.Message)"
    exit 1
}
