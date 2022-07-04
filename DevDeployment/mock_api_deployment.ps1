param (
    [Parameter(Mandatory = $true)] [string] $mockWebAppName,
    [Parameter(Mandatory = $true)] [string] $mockApipackagePath,
    [Parameter(Mandatory = $true)] [string] $mockWebAppResourceGroup
)

echo "mockApipackagePath : $mockApipackagePath"
echo "mockWebAppName : $mockWebAppName"
echo "ResourceGroup : $mockWebAppResourceGroup"

function DeployWebApp($mockWebAppName, $mockApipackagePath, $mockWebAppResourceGroup){
    
    echo "Function DeployWebApp called with params $mockWebAppName, $mockApipackagePath, $mockWebAppResourceGroup ..."

    az webapp deployment source config-zip -g $mockWebAppResourceGroup -n $mockWebAppName --src $mockApipackagePath

    if ( !$? ) { echo "Error while deplying webapp $mockWebAppName" ; throw $_ }
}

echo "Deploying mock api ..."
DeployWebApp $mockWebAppName $mockApipackagePath $mockWebAppResourceGroup

if ( !$? ) { echo "Error while deploying mock api webapp" ; throw $_ }

echo "Deploying mock api done ..."

