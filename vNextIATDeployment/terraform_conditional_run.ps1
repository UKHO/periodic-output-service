param (
    [Parameter(Mandatory = $true)] [string] $deploymentResourceGroupName,
    [Parameter(Mandatory = $true)] [string] $deploymentStorageAccountName,
    [Parameter(Mandatory = $true)] [string] $workSpace,
    [Parameter(Mandatory = $true)] [boolean] $continueEvenIfResourcesAreGettingDestroyed,
    [Parameter(Mandatory = $true)] [string] $terraformJsonOutputFile,
    [Parameter(Mandatory = $true)] [string] $elasticApmServerUrl,
    [Parameter(Mandatory = $true)] [string] $elasticApmApiKey
)

cd $env:AGENT_BUILDDIRECTORY/vnextiatterraformartifact/src

terraform --version

Write-output "Executing terraform scripts for deployment in $workSpace enviroment"
terraform init -backend-config="resource_group_name=$deploymentResourceGroupName" -backend-config="storage_account_name=$deploymentStorageAccountName" -backend-config="key=posterraform.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform initialization"; throw "Error" }

Write-output "Selecting workspace"

$ErrorActionPreference = 'SilentlyContinue'
terraform workspace new $WorkSpace 2>&1 > $null
$ErrorActionPreference = 'Continue'

terraform workspace select $workSpace
if ( !$? ) { echo "Error while selecting workspace"; throw "Error" }

Write-output "Validating terraform"
terraform validate
if ( !$? ) { echo "Something went wrong during terraform validation" ; throw "Error" }

Write-output "Execute Terraform plan"
terraform plan -out "posterraform.deployment.tfplan" -var elastic_apm_server_url=$elasticApmServerUrl -var elastic_apm_api_key=$elasticApmApiKey | tee terraform_output.txt
if ( !$? ) { echo "Something went wrong during terraform plan" ; throw "Error" }

$totalDestroyLines=(Get-Content -Path terraform_output.txt | Select-String -Pattern "destroy" -CaseSensitive |  where {$_ -ne ""}).length
if($totalDestroyLines -ge 2) 
{
    write-Host("Terraform is destroying some resources, please verify...................")
    if ( !$ContinueEvenIfResourcesAreGettingDestroyed) 
    {
        write-Host("exiting...................")
        Write-Output $_
        exit 1
    }
    write-host("Continue executing terraform apply - as continueEvenIfResourcesAreGettingDestroyed param is set to true in pipeline")
}

Write-output "Executing terraform apply"
terraform apply  "posterraform.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform apply" ; throw "Error" }

Write-output "Terraform output as json"
$terraformOutput = terraform output -json | ConvertFrom-Json

write-output "Set JSON output into pipeline variables"
Write-Host "##vso[task.setvariable variable=Website_Url]$($terraformOutput.Website_Url.value)"
Write-Host "##vso[task.setvariable variable=WEB_APP_NAME]$($terraformOutput.web_app_name.value)"
Write-Host "##vso[task.setvariable variable=mockWebAppName]$($terraformOutput.mock_webappname.value)"
Write-Host "##vso[task.setvariable variable=mock_web_app_url]$($terraformOutput.fm_mock_web_app_url.value)"
Write-Host "##vso[task.setvariable variable=mockWebAppResourceGroup]$($terraformOutput.mock_webapp_rg.value)"
Write-Host "##vso[task.setvariable variable=RGName]$($terraformOutput.webapp_rg.value)"
Write-Host "##vso[task.setvariable variable=WEBAPP;isOutput=true]$($terraformOutput.web_app_name.value)"
Write-Host "##vso[task.setvariable variable=mockWebApp;isOutput=true]$($terraformOutput.mock_webappname.value)"
Write-Host "##vso[task.setvariable variable=mockWebAppResourceGroupName;isOutput=true]$($terraformOutput.mock_webapp_rg.value)"
Write-Host "##vso[task.setvariable variable=ResourceGroup;isOutput=true]$($terraformOutput.webapp_rg.value)"
Write-Host "##vso[task.setvariable variable=webJobUsername;isOutput=true]$($terraformOutput.webjob_username.value)"
Write-Host "##vso[task.setvariable variable=webJobPassword;issecret=true;isOutput=true]$($terraformOutput.webjob_password.value)"
Write-Host "##vso[task.setvariable variable=AzureStorageConfiguration.ConnectionString;issecret=true]$($terraformOutput.pos_storage_connection_string.value)"
Write-Host "##vso[task.setvariable variable=BessStorageConfiguration.ConnectionString;issecret=true]$($terraformOutput.bess_storage_connection_string.value)"
Write-Host "##vso[task.setvariable variable=EventHubLoggingConfiguration.ConnectionString;issecret=true]$($terraformOutput.log_primary_connection_string.value)"
Write-Host "##vso[task.setvariable variable=EventHubLoggingConfiguration.EntityPath;issecret=true]$($terraformOutput.entity_path.value)"
Write-Host "##vso[task.setvariable variable=ApplicationInsights.ConnectionString;issecret=true]$($terraformOutput.connection_string.value)"
Write-Host "##vso[task.setvariable variable=AzureWebJobsStorage;issecret=true]$($terraformOutput.bess_storage_connection_string.value)"

$terraformOutput | ConvertTo-Json -Depth 5 > $terraformJsonOutputFile
