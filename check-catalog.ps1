Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq "AsTask" -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq "IAsyncOperation`1"
}[0]

[Windows.ApplicationModel.AppExtensions.AppExtensionCatalog, Windows.ApplicationModel.AppExtensions, ContentType = WindowsRuntime] | Out-Null
$catalog = [Windows.ApplicationModel.AppExtensions.AppExtensionCatalog]::Open("com.microsoft.windows.widgets")
$asyncOp = $catalog.FindAllAsync()
$extType = [Windows.ApplicationModel.AppExtensions.AppExtension]
$listType = [System.Collections.Generic.IReadOnlyList``1].MakeGenericType($extType)
$asTaskMethod = $asTaskGeneric.MakeGenericMethod($listType)
$task = $asTaskMethod.Invoke($null, @($asyncOp))
$task.Wait()
$results = $task.Result

Write-Host "Total widgets found in AppExtensionCatalog: $($results.Count)"
foreach ($ext in $results) {
    Write-Host "  * Id: $($ext.Id) | DisplayName: $($ext.DisplayName) | Package: $($ext.Package.Id.FullName)"
}
