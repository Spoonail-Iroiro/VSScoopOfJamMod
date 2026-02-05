set shell := ["pwsh", "-NoLogo", "-NoProfile", "-Command"]
default:
    just -l
copy-texture:
    Copy-Item "./Resource/material/fill-peanutbutter3.png" "ScoopOfJamMod/assets/scoopofjammod/textures/block/food/pie/fill-peanutbutter.png" 
    just copy-assets
copy-assets:
    pwsh ./robocopy_asset.ps1 "ScoopOfJamMod" "./ScoopOfJamMod" "./ScoopOfJamMod/bin/Debug/Mods/mod"
