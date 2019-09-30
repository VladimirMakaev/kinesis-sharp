[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("Build", "Local", "Test1")]
    [string]
    $TaskName,
    [switch]
    $Up = $false
)

switch ($TaskName) {
    'Build' { 
        Write-Host "Execute ${TaskName}"
    }
    'Local' {
        docker-compose -f "./docker/localstack/docker-compose.yml" build
    }
    'Test1' {
        if ($Up) {
            docker-compose -f "./docker/integration-1/docker-compose.yml" up --build
        }
        else {
            docker-compose -f "./docker/integration-1/docker-compose.yml" build
        }
    }
    Default {
        Write-Error "Error"
    }
}