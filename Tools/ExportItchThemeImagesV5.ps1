# Export a new viewpoint set for itch.io without replacing previous versions.
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $PSScriptRoot
$themeRoot = Join-Path $projectRoot 'Docs/Release/Media/Theme/V5'
$jobs = @(
    @{Source='Banner-Master.png'; Output='Banner-960x300.png'; Width=960; Height=300; Jpeg=$false},
    @{Source='Background-Master.png'; Output='Background-1920x1080.png'; Width=1920; Height=1080; Jpeg=$false},
    @{Source='EmbedBG-Master.png'; Output='EmbedBG-960x540.jpg'; Width=960; Height=540; Jpeg=$true}
)
foreach ($job in $jobs) {
    $source = [System.Drawing.Image]::FromFile((Join-Path $themeRoot $job.Source))
    $bitmap = [System.Drawing.Bitmap]::new($job.Width, $job.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $attributes = [System.Drawing.Imaging.ImageAttributes]::new()
    try {
        $targetRatio = [double]$job.Width / $job.Height
        $sourceRatio = [double]$source.Width / $source.Height
        if ($sourceRatio -gt $targetRatio) {
            $cropHeight = [double]$source.Height
            $cropWidth = $cropHeight * $targetRatio
        } else {
            $cropWidth = [double]$source.Width
            $cropHeight = $cropWidth / $targetRatio
        }
        $cropX = ($source.Width - $cropWidth) / 2
        $cropY = ($source.Height - $cropHeight) / 2
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0,0,$job.Width,$job.Height), [single]$cropX, [single]$cropY, [single]$cropWidth, [single]$cropHeight, [System.Drawing.GraphicsUnit]::Pixel, $attributes)
        $output = Join-Path $themeRoot $job.Output
        if ($job.Jpeg) {
            $codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -EQ 'image/jpeg'
            $parameters = [System.Drawing.Imaging.EncoderParameters]::new(1)
            try {
                $parameters.Param[0] = [System.Drawing.Imaging.EncoderParameter]::new([System.Drawing.Imaging.Encoder]::Quality, [long]92)
                $bitmap.Save($output, $codec, $parameters)
            } finally { $parameters.Dispose() }
        } else {
            $bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        [pscustomobject]@{ File=$job.Output; Width=$bitmap.Width; Height=$bitmap.Height; Bytes=(Get-Item -LiteralPath $output).Length }
    } finally {
        $attributes.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
        $source.Dispose()
    }
}



