Param(
    [Parameter(mandatory=$true)][number]$daysValid,
	[Parameter(mandatory=$true)][string]$accessToken,
    [Parameter(mandatory=$true)][string]$definitionId,
    [Parameter(mandatory=$true)][string]$requestedForId,
    [Parameter(mandatory=$true)][string]$buildId,
    [Parameter(mandatory=$true)][string]$collectionUri,
    [Parameter(mandatory=$true)][string]$teamProject    
)

try{

    #$daysValid = 365 * 2

    $contentType = "application/json";

    #$headers = @{ Authorization = 'Bearer $(System.AccessToken)' };
    $headers = @{ Authorization = 'Bearer $accessToken' };

    #$rawRequest = @{ daysValid = $noOfDays; definitionId = $(System.DefinitionId); ownerId = 'User:$(Build.RequestedForId)'; protectPipeline = $false; runId = $(Build.BuildId) };
    $rawRequest = @{ daysValid = $daysValid; definitionId = $definitionId; ownerId = 'User:$requestedForId'; protectPipeline = $false; runId = $buildId };

    $request = ConvertTo-Json @($rawRequest);

    $uri = "$collectionUri$teamProject/_apis/build/retention/leases?api-version=7.0";
    Write-Host $uri
    Invoke-RestMethod -uri $uri -method POST -Headers $headers -ContentType $contentType -Body $request;
}
catch{
   Write-Host $_
   Write-Host "##vso[task.complete result=SucceededWithIssues;]"
}
finally{
  Write-Host "Pipeline will be retained for  $noOfDays days"
}
