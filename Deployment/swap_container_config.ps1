param (
    [Parameter(Mandatory = $true)] [string] $resourcegroup,    
    [Parameter(Mandatory = $true)] [string] $webappname,    
    [Parameter(Mandatory = $true)] [string] $containername
)

Write-Output "Set Webjob Configuration in appsetting..."
az webapp config appsettings set -g $resourcegroup -n $webappname --settings BessStorageConfiguration:ContainerName=$containername 
az webapp restart --name $webappname --resource-group $resourcegroup
