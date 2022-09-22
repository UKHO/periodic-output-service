param (
    [Parameter(Mandatory = $true)] [string] $mockresourcegroup,
    [Parameter(Mandatory = $true)] [string] $mockwebappname
)

Write-Output "Set QA Subnet in Mock Network Configuration ..."
az webapp config access-restriction add -g $mockresourcegroup -n $mockwebappname --action Allow --subnet "/subscriptions/3fbb5f2f-bce2-4d01-a92e-91f4483d939e/resourceGroups/m-spokeconfig-rg/providers/Microsoft.Network/virtualNetworks/ESSQA-vnet/subnets/ess-fulfilment-service-l-1" --priority 65000 --scm-site false -i




