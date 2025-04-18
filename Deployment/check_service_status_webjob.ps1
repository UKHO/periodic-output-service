Param(
	[Parameter(mandatory=$true)][string]$RGName,
    [Parameter(mandatory=$true)][string]$webAppName,
    [Parameter(mandatory=$true)][string]$waitTimeInMinute,
    [Parameter(mandatory=$false)][string]$SlotName = $null
)

$sleepTimeInSecond = 10
$recheckTimeInSecond = 35
$isServiceActive = 'false'

$stopWatch = New-Object -TypeName System.Diagnostics.Stopwatch
$fullTimeSpan = New-TimeSpan -Minutes $waitTimeInMinute
$recheckTimeSpan = New-TimeSpan -Seconds $recheckTimeInSecond
$stopWatch.Start()

do
{
    Write-Host "Polling AZ ..."
    $StateRequest = $null

    if ([string]::IsNullOrWhitespace($SlotName)) {
        $StateRequest = az webapp show --name $webAppName --resource-group $RGName --query "state"
    }
    else {
        $StateRequest = az webapp show --name $webAppName --slot $SlotName --resource-group $RGName --query "state"
    }

    Write-Host "State of webjob is $StateRequest ..."
    
    If ($StateRequest -eq '"Running"') {
        Write-Host "Webjob service is running..."
        $isServiceActive = 'true'
        If($stopWatch.Elapsed -ge $recheckTimeSpan) {
            Write-Host "Stopping polling ..."
            break
        }

    }
    ElseIf ($StateRequest -eq '"PendingRestart"'){
        Write-Host "Webjob has encountered an error. Webjob state: $StateRequest..."
        throw "Error"
    }
    Else {
        Write-Host "Webjob is not yet running. Webjob state: $StateRequest re-checking after $sleepTimeInSecond sec ..."
    }
    
    Start-Sleep -Seconds $sleepTimeInSecond
}
until ($stopWatch.Elapsed -ge $fullTimeSpan)

if ($isServiceActive -eq 'true' ) {
    Write-Host "Webjob is running, returning from script ..."
}
Else { 
    Write-Error "Error: Webjob was not running within $waitTimeInMinute minutes..."
    throw "Error"
}
