param([string]$DocumentPath)
$ErrorActionPreference = 'Stop'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
$doc = $null
try {
    $doc = $word.Documents.Open($DocumentPath, $false, $false)
    $range = $doc.Content
    $find = $range.Find
    if ($find.Execute('[[AUTO_TOC]]')) {
        $range.Text = ''
        $null = $doc.TablesOfContents.Add($range, $true, 1, 3)
    }
    $doc.Repaginate()
    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    $null = $doc.Fields.Update()
    $doc.Repaginate()
    $doc.Save()
    $pdfPath = Join-Path (Split-Path $DocumentPath) 'qa/EggPress3D_Report_Updated.pdf'
    $doc.ExportAsFixedFormat($pdfPath, 17)
    Write-Output "Pages: $($doc.ComputeStatistics(2))"
    Write-Output $pdfPath
} finally {
    if ($null -ne $doc) { $doc.Close(0) }
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
