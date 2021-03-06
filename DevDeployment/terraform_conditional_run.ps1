param (
    [Parameter(Mandatory = $true)] [string] $deploymentResourceGroupName,
    [Parameter(Mandatory = $true)] [string] $deploymentStorageAccountName,
    [Parameter(Mandatory = $true)] [string] $workSpace,
    [Parameter(Mandatory = $true)] [boolean] $continueEvenIfResourcesAreGettingDestroyed,
    [Parameter(Mandatory = $true)] [string] $terraformJsonOutputFile
)

cd $env:AGENT_BUILDDIRECTORY/devterraformartifact/src

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
terraform plan -out "posterraform.deployment.tfplan" | tee terraform_output.txt
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
Write-Host "##vso[task.setvariable variable=RG]$($terraformOutput.webapp_rg.value)"

$terraformOutput | ConvertTo-Json -Depth 5 > $terraformJsonOutputFile
