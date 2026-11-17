# Export the selected gameplay cover master at itch.io's recommended dimensions.
Add-Type -AssemblyName System.Drawing
$releaseRoot = Split-Path -Parent $PSScriptRoot
$mediaRoot = Join-Path $releaseRoot 'Docs/Release/Media'
$source = [System.Drawing.Image]::FromFile((Join-Path $mediaRoot 'Cover-Gameplay-Master.png'))
$bitmap = [System.Drawing.Bitmap]::new(630, 500)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$attributes = [System.Drawing.Imaging.ImageAttributes]::new()
try {
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
    $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0, 0, 630, 500), 0, 0, $source.Width, $source.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)
    $bitmap.Save((Join-Path $mediaRoot 'Cover-630x500.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $attributes.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    $source.Dispose()
}
Write-Output (Join-Path $mediaRoot 'Cover-630x500.png')
