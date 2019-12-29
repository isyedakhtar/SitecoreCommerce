Param(
    [switch]$Initialize,
    [switch]$Bootstrap,
    [string]$engineHostName = "commerceauthoring.bsanz.local",
    [string]$identityServerHost = "bsanz.identityserver",
    [string]$adminUser = "admin",
    [string]$adminPassword = "b",
    [string[]] $engines = @("Authoring", "Minions", "Ops", "Shops")
)

Function Get-IdServerToken {
    $UrlIdentityServerGetToken = ("https://{0}/connect/token" -f $identityServerHost)

    $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
    $headers.Add("Content-Type", 'application/x-www-form-urlencoded')
    $headers.Add("Accept", 'application/json')

    $body = @{
        password   = "$adminPassword"
        grant_type = 'password'
        username   = ("sitecore\{0}" -f $adminUser)
        client_id  = 'postman-api'
        scope      = 'openid EngineAPI postman_api'
    }
    Write-Host "Getting Identity Token From Sitecore.IdentityServer" -ForegroundColor Green
    $response = Invoke-RestMethod $UrlIdentityServerGetToken -Method Post -Body $body -Headers $headers

    $sitecoreIdToken = "Bearer {0}" -f $response.access_token

    $global:sitecoreIdToken = $sitecoreIdToken
	Write-Host $global:sitecoreIdToken
}

Function CleanEnvironment {
    Write-Host "Cleaning Environments" -ForegroundColor Green
    $initializeParam = "/commerceops/CleanEnvironment()"
    $initializeUrl = ("https://{0}{2}" -f $engineHostName, $initializeParam)

    $Environments = @("HabitatAuthoring", "HabitatMinions")

    $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
    
    $headers.Add("Authorization", $global:sitecoreIdToken);

    foreach ($env in $Environments) {
        Write-Host "Cleaning $($env) ..." -ForegroundColor Yellow
        $body = @{
            environment = $env
        }

        $result = Invoke-RestMethod $initializeUrl -TimeoutSec 1200 -Method Post -Headers $headers -Body ($body | ConvertTo-Json) -ContentType "application/json"
        if ($result.ResponseCode -eq "Ok") {
            Write-Host "Cleaning for $($env) completed successfully" -ForegroundColor Green
        }
        else {
            Write-Host "Cleaning for $($env) failed" -ForegroundColor Red
            Exit -1
        }
    }
}

Function BootStrapCommerceServices {

    Write-Host "BootStrapping Commerce Services: $($urlCommerceShopsServicesBootstrap)" -ForegroundColor Green

    $UrlCommerceShopsServicesBootstrap = ("https://{0}/commerceops/Bootstrap()" -f $engineHostName)

    $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
    $headers.Add("Authorization", $global:sitecoreIdToken)
    Invoke-RestMethod $UrlCommerceShopsServicesBootstrap -TimeoutSec 1200 -Method PUT -Headers $headers 
    Write-Host "Commerce Services BootStrapping completed" -ForegroundColor Green
}

Function InitializeCommerceServices {
    Write-Host "Initializing Environments" -ForegroundColor Green
    $initializeParam = "/commerceops/InitializeEnvironment()"
    $UrlInitializeEnvironment = ("https://{0}{2}" -f $engineHostName, $initializeParam)
    $UrlCheckCommandStatus = ("https://{0}:{1}{2}" -f $engineHostName, $``, "/commerceops/CheckCommandStatus(taskId=taskIdValue)")

	Write-Host $UrlInitializeEnvironment
	
    $Environments = @("HabitatAuthoring", "HabitatMinions")

    $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
    $headers.Add("Authorization", $global:sitecoreIdToken);

    foreach ($env in $Environments) {
	
        Write-Host "Initializing $($env) ..." -ForegroundColor Yellow

        $initializeUrl = $UrlInitializeEnvironment

        $payload = @{
            "environment" = $env;
        }

        $result = Invoke-RestMethod $initializeUrl -TimeoutSec 1200 -Method POST -Body ($payload|ConvertTo-Json) -Headers $headers -ContentType "application/json"
        $checkUrl = $UrlCheckCommandStatus -replace "taskIdValue", $result.TaskId

        $sw = [system.diagnostics.stopwatch]::StartNew()
        $tp = New-TimeSpan -Minute 10
        do {
            Start-Sleep -s 30
            Write-Host "Checking if $($checkUrl) has completed ..." -ForegroundColor White
            $result = Invoke-RestMethod $checkUrl -TimeoutSec 1200 -Method Get -Headers $headers -ContentType "application/json"

            if ($result.ResponseCode -ne "Ok") {
                $(throw Write-Host "Initialize environment $($env) failed, please check Engine service logs for more info." -Foregroundcolor Red)
            }
            else {
                write-Host $result.ResponseCode
                Write-Host $result.Status
            }
        } while ($result.Status -ne "RanToCompletion" -and $sw.Elapsed -le $tp)

        Write-Host "Initialization for $($env) completed ..." -ForegroundColor Green
    }

    Write-Host "Initialization completed ..." -ForegroundColor Green
}

Get-IdServerToken
BootStrapCommerceServices