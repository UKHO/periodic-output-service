param (
    [Parameter(Mandatory = $true)] [string] $essapibaseurl,
    [Parameter(Mandatory = $true)] [string] $resourcegroup,
    [Parameter(Mandatory = $true)] [string] $mockresourcegroup,
    [Parameter(Mandatory = $true)] [string] $webappname,
    [Parameter(Mandatory = $true)] [string] $mockwebappname,
    [Parameter(Mandatory = $true)] [string] $fssapibaseurl,
    [Parameter(Mandatory = $true)] [string] $fleetmanagerbaseurl,
    [Parameter(Mandatory = $true)] [string] $fleetmanagerfilepath,
    [Parameter(Mandatory = $true)] [string] $ftrunning,
    [Parameter(Mandatory = $true)] [string] $fssapipollingcutoffime,
    [Parameter(Mandatory = $true)] [string] $fssapipollingdelaytime
)

Write-Output "Set Stub Configuration in appsetting..."
az webapp config appsettings set -g $mockresourcegroup -n $mockwebappname --settings FleetManagerB2BApiConfiguration:GetCatalogueResponseFilePath=$fleetmanagerfilepath
az webapp restart --name $mockwebappname --resource-group $mockresourcegroup

Write-Output "Set Webjob Configuration in appsetting..."
az webapp config appsettings set -g $resourcegroup -n $webappname --settings ESSApiConfiguration:BaseUrl=$essapibaseurl FSSApiConfiguration:BaseUrl=$fssapibaseurl FleetManagerB2BApiConfiguration:BaseUrl=$fleetmanagerbaseurl IsFTRunning=$ftrunning FSSApiConfiguration:BatchStatusPollingCutoffTime=$fssapipollingcutoffime FSSApiConfiguration:BatchStatusPollingDelayTime=$fssapipollingdelaytime
az webapp restart --name $webappname --resource-group $resourcegroup
