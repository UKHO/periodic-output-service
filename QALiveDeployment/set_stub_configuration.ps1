param (
    [Parameter(Mandatory = $true)] [string] $mockresourcegroup,  
    [Parameter(Mandatory = $true)] [string] $mockwebappname,     
    [Parameter(Mandatory = $true)] [string] $fleetmanagerfilepath,
    [Parameter(Mandatory = $true)] [string] $ftrunning
)

Write-Output "Set Stub Configuration in appsetting..."
az webapp config appsettings set -g $mockresourcegroup -n $mockwebappname --settings FleetManagerB2BApiConfiguration:GetCatalogueResponseFilePath=$fleetmanagerfilepath IsFTRunning=$ftrunning
az webapp restart --name $mockwebappname --resource-group $mockresourcegroup


