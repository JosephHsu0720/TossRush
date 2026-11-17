# Export a new viewpoint set for itch.io without replacing previous versions.
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $PSScriptRoot
$themeRoot = Join-Path $projectRoot 'Docs/Release/Media/Theme/V7'
$jobs = @(
    @{Source='Banner-Master.png'; Output='Banner-960x300.png'; Width=960; Height=300; Jpeg=$false},
    @{Source='Background-Master.png'; Output='Background-1920x1080.jpg'; Width=1920; Height=1080; Jpeg=$true},
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





# Enforce a strict 500,000-byte background upload limit.
$backgroundSource = [System.Drawing.Image]::FromFile((Join-Path $themeRoot 'Background-Master.png'))
try {
    $backgroundBitmap = [System.Drawing.Bitmap]::new(1920,1080)
    $backgroundGraphics = [System.Drawing.Graphics]::FromImage($backgroundBitmap)
    try {
        $backgroundGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $backgroundGraphics.DrawImage($backgroundSource,0,0,1920,1080)
        foreach ($quality in @(92,90,87,85,82,80,75,70,65,60,50,40,30,20)) {
            $stream = [System.IO.MemoryStream]::new()
            $encoderParams = [System.Drawing.Imaging.EncoderParameters]::new(1)
            try {
                $encoderParams.Param[0] = [System.Drawing.Imaging.EncoderParameter]::new([System.Drawing.Imaging.Encoder]::Quality,[long]$quality)
                $jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -EQ 'image/jpeg'
                $backgroundBitmap.Save($stream,$jpegCodec,$encoderParams)
                if ($stream.Length -lt 500000) {
                    [System.IO.File]::WriteAllBytes((Join-Path $themeRoot 'Background-1920x1080.jpg'),$stream.ToArray())
                    Write-Output "Background: $($stream.Length) bytes; JPEG quality $quality"
                    break
                }
            } finally { $stream.Dispose(); $encoderParams.Dispose() }
        }
        if ((Get-Item -LiteralPath (Join-Path $themeRoot 'Background-1920x1080.jpg')).Length -ge 500000) { throw 'Background exceeds upload limit.' }
    } finally { $backgroundGraphics.Dispose(); $backgroundBitmap.Dispose() }
} finally { $backgroundSource.Dispose() }

