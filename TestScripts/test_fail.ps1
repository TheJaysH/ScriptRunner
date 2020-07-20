param (
    [Parameter(Mandatory = $True)]
    [string]$TextValue,
    [Parameter(Mandatory = $True)]
    [string]$DropDownValue_0,
    [Parameter(Mandatory = $True)]
    [string]$DropDownValue_1,
    [Parameter(Mandatory = $True)]
    [string]$NumberValue,
)

Write-Host '$TextValue='$TextValue
Write-Host '$DropDownValue_0='$DropDownValue_0
Write-Host '$DropDownValue_1='$DropDownValue_1
Write-Host '$NumberValue='$NumberValue

Start-Sleep -Seconds 2

Write-Error "Test Error"
