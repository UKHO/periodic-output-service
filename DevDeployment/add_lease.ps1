$noOfDays = 365 * 2

$contentType = "application/json";

$headers = @{ Authorization = 'Bearer $(System.AccessToken)' };

$rawRequest = @{ daysValid = $noOfDays; definitionId = $(System.DefinitionId); ownerId = 'User:$(Build.RequestedForId)'; protectPipeline = $false; runId = $(Build.BuildId) };

$request = ConvertTo-Json @($rawRequest);

$uri = "$(System.CollectionUri)$(System.TeamProject)/__apis/build/retention/leases?api-version=7.0";

try{
  Invoke-RestMethod -uri $uri -method POST -Headers $headers -ContentType $contentType -Body $request;
}
catch{
   Write-Host $_
   Write-Host "##vso[task.complete result=SucceededWithIssues;]"
}
finally{
  Write-Host "Pipeline will be retained for  $noOfDays days"
}
