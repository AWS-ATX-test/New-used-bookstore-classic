param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("build", "deploy", "destroy", "diff")]
    [string]$Action,
    
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Stop"

switch ($Action) {
    "build" {
        Write-Host "Building application..." -ForegroundColor Yellow
        dotnet build app/Bookstore.Web/Bookstore.Web.csproj
    }
    
    "deploy" {
        Write-Host "Deploying to $Environment..." -ForegroundColor Yellow
        cdk deploy --all --require-approval never
    }
    
    "destroy" {
        Write-Host "Destroying $Environment environment..." -ForegroundColor Red
        cdk destroy --all --force
    }
    
    "diff" {
        Write-Host "Showing deployment diff..." -ForegroundColor Cyan
        cdk diff
    }
}

Write-Host "Operation completed successfully!" -ForegroundColor Green
