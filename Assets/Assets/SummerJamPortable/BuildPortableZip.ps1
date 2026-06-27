# Proje kokunden calistir: powershell -File Assets/SummerJamPortable/BuildPortableZip.ps1
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $root "Assets"))) {
    $root = Split-Path $PSScriptRoot -Parent
    while ($root -and -not (Test-Path (Join-Path $root "Assets"))) { $root = Split-Path $root -Parent }
}
if (-not $root) { throw "Unity proje kokunu bulamadi." }

$export = Join-Path $root "SummerJam_Mechanics_Export"
$zip = Join-Path $root "SummerJam_Mechanics_Portable.zip"
$assetsSrc = Join-Path $root "Assets"
$assetsDst = Join-Path $export "Assets"

if (Test-Path $export) { Remove-Item $export -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Path $assetsDst -Force | Out-Null

function Copy-Tree($from, $to) {
    if (-not (Test-Path $from)) { return }
    New-Item -ItemType Directory -Path $to -Force | Out-Null
    Copy-Item -Path $from -Destination $to -Recurse -Force
}

Copy-Item (Join-Path $assetsSrc "SummerJamPortable\KURULUM.txt") (Join-Path $export "KURULUM.txt") -Force
Copy-Tree (Join-Path $assetsSrc "Scripts\Data") (Join-Path $assetsDst "Scripts\Data")
Copy-Tree (Join-Path $assetsSrc "Scripts\Inventory") (Join-Path $assetsDst "Scripts\Inventory")
New-Item -ItemType Directory -Path (Join-Path $assetsDst "Scripts\Kase") -Force | Out-Null
Copy-Item (Join-Path $assetsSrc "Scripts\Kase\Kase.cs*") (Join-Path $assetsDst "Scripts\Kase\") -Force
Copy-Tree (Join-Path $assetsSrc "SummerJamPortable") (Join-Path $assetsDst "SummerJamPortable")
Copy-Tree (Join-Path $assetsSrc "ingredients_recipes") (Join-Path $assetsDst "ingredients_recipes")
Copy-Tree (Join-Path $assetsSrc "Prefabs") (Join-Path $assetsDst "Prefabs")
Copy-Tree (Join-Path $assetsSrc "Resources") (Join-Path $assetsDst "Resources")
New-Item -ItemType Directory -Path (Join-Path $assetsDst "Scenes") -Force | Out-Null
Copy-Item (Join-Path $assetsSrc "Scenes\GameplayDemo.unity*") (Join-Path $assetsDst "Scenes\") -Force

$metaFiles = @(
    "Scripts.meta", "Scripts\Data.meta", "Scripts\Inventory.meta", "Scripts\Kase.meta",
    "ingredients_recipes.meta", "SummerJamPortable.meta", "Prefabs.meta", "Resources.meta", "Scenes.meta"
)
foreach ($meta in $metaFiles) {
    $srcMeta = Join-Path $assetsSrc $meta
    if (Test-Path $srcMeta) {
        $destDir = Join-Path $assetsDst (Split-Path $meta -Parent)
        if ([string]::IsNullOrEmpty((Split-Path $meta -Parent))) { Copy-Item $srcMeta $assetsDst -Force }
        else { New-Item -ItemType Directory -Path $destDir -Force | Out-Null; Copy-Item $srcMeta $destDir -Force }
    }
}

Compress-Archive -Path (Join-Path $export "*") -DestinationPath $zip -Force
Write-Host "Olusturuldu: $zip"
