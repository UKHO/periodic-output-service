param (
    [Parameter(Mandatory = $true)] [string] $mockWebAppName,
    [Parameter(Mandatory = $true)] [string] $mockApipackagePath,
    [Parameter(Mandatory = $true)] [string] $webAppResourceGroup
)

echo "mockApipackagePath : $mockApipackagePath"
echo "mockWebAppName : $mockWebAppName"
echo "ResourceGroup : $webAppResourceGroup"

function DeployWebApp($webAppName, $package, $webAppRGroup){
    
    echo "Function DeployWebApp called with params $webAppName, $package, $webAppRGroup ..."

    az webapp deployment source config-zip -g $webAppRGroup -n $webAppName --src $package

    if ( !$? ) { echo "Error while deplying webapp $webAppName" ; throw $_ }
}


echo "Deploying mock api ..."
DeployWebApp $mockWebAppName $mockApipackagePath $webAppResourceGroup

if ( !$? ) { echo "Error while deploying mock api webapp" ; throw $_ }

echo "Deploying mock api done ..."

