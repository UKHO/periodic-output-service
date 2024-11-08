param (
    [Parameter(Mandatory = $true)] [string] $mockresourcegroup,
    [Parameter(Mandatory = $true)] [string] $mockwebappname,
    [Parameter(Mandatory = $true)] [string] $subscriptionid,
    [Parameter(Mandatory = $true)] [string] $vnetresourcegroup,
    [Parameter(Mandatory = $true)] [string] $vnetname,
    [Parameter(Mandatory = $true)] [string] $subnetname
)

Write-Output "Set Subnet in Mock Network Configuration ..."
az webapp config access-restriction add -g $mockresourcegroup -n $mockwebappname --action Allow --subnet "/subscriptions/$subscriptionid/resourceGroups/$vnetresourcegroup/providers/Microsoft.Network/virtualNetworks/$vnetname/subnets/$subnetname" --priority 65000 --scm-site false -i




