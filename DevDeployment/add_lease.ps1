Param(
    [Parameter(mandatory=$true)][int]$daysValid,
	[Parameter(mandatory=$true)][string]$accessToken,
    [Parameter(mandatory=$true)][string]$definitionId,
    [Parameter(mandatory=$true)][string]$requestedForId,
    [Parameter(mandatory=$true)][string]$buildId,
    [Parameter(mandatory=$true)][string]$collectionUri,
    [Parameter(mandatory=$true)][string]$teamProject    
)

try{
    $ownerId = @{'User:'$requestedForId};

    Write-Host $ownerId;

    $contentType = "application/json";
    $headers = @{ Authorization = 'Bearer $accessToken' };
    $rawRequest = @{ daysValid = $daysValid; definitionId = $definitionId; ownerId = $ownerId; protectPipeline = $false; runId = $buildId };
    $request = ConvertTo-Json @($rawRequest);
    $uri = "$collectionUri$teamProject/_apis/build/retention/leases?api-version=7.0";

    Write-Host $request;
    Write-Host $uri;

    Invoke-RestMethod -uri $uri -method POST -Headers $headers -ContentType $contentType -Body $request;
    Write-Host "Pipeline will be retained for $daysValid days";
}
catch{
   Write-Host $_;
   Write-Host "##vso[task.complete result=SucceededWithIssues;]";
}