param (
    [Parameter(Mandatory = $true)] [string] $mockresourcegroup,
    [Parameter(Mandatory = $true)] [string] $mockwebappname,
    [Parameter(Mandatory = $true)] [string] $qasubscriptionid,
    [Parameter(Mandatory = $true)] [string] $qavnetresourcegroup,
    [Parameter(Mandatory = $true)] [string] $qavnetname,
    [Parameter(Mandatory = $true)] [string] $qasubnetname
)

Write-Output "Set QA Subnet in Mock Network Configuration ..."
az webapp config access-restriction add -g $mockresourcegroup -n $mockwebappname --action Allow --subnet "/subscriptions/$qasubscriptionid/resourceGroups/$qavnetresourcegroup/providers/Microsoft.Network/virtualNetworks/$qavnetname/subnets/$qasubnetname" --priority 65000 --scm-site false -i




