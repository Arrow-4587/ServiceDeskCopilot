$targetMetrics = @(
    "HateAndUnfairness",
    "CodeVulnerability",
    "Fluency",
    "Violence",
    "SelfHarm",
    "IndirectAttack",
    "UngroundedAttributes",
    "Sexual",
    "Coherence",
    "Groundedness",
    "ToolCallSuccessEvaluator"
)

$files = @(
    "c:\ServiceDeskCopilot\ServiceDeskCopilot\data\evaluation\planner_agent_evaluation.json",
    "c:\ServiceDeskCopilot\ServiceDeskCopilot\data\evaluation\support_specialist_agent_evaluation.json",
    "c:\ServiceDeskCopilot\ServiceDeskCopilot\data\evaluation\reviewer_agent_evaluation.json",
    "c:\ServiceDeskCopilot\ServiceDeskCopilot\data\evaluation\azure_ai_foundry_multi_agent_evaluation.json"
)

foreach ($filePath in $files) {
    if (Test-Path $filePath) {
        $raw = Get-Content $filePath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($item in $raw) {
            if ($item.metadata) {
                $item.metadata.eval_metrics = $targetMetrics
            }
        }
        $json = $raw | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($filePath, $json, [System.Text.Encoding]::UTF8)
        Write-Host "Updated eval_metrics in: $filePath"
    }
}
