$files = @(
    "planner_agent_evaluation",
    "support_specialist_agent_evaluation",
    "reviewer_agent_evaluation",
    "azure_ai_foundry_multi_agent_evaluation"
)

$baseDir = "c:\ServiceDeskCopilot\ServiceDeskCopilot\data\evaluation"

foreach ($file in $files) {
    $source = Join-Path $baseDir "$file.json"
    $target = Join-Path $baseDir "$file.jsonl"
    
    if (Test-Path $source) {
        $jsonRaw = Get-Content $source -Raw -Encoding UTF8
        $items = ConvertFrom-Json $jsonRaw
        
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        $streamWriter = [System.IO.StreamWriter]::new($target, $false, $utf8NoBom)
        try {
            foreach ($item in $items) {
                $singleLine = $item | ConvertTo-Json -Compress -Depth 10
                $streamWriter.WriteLine($singleLine)
            }
        }
        finally {
            $streamWriter.Close()
        }
        Write-Host "Generated: $target ($($items.Count) records)"
    }
}
