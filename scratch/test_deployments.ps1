$apiKey = "<YOUR_AZURE_OPENAI_API_KEY>"
$endpoint = "https://enterpriceservicedesk.openai.azure.com"
$deployments = @("gpt-5-mini", "gpt-5", "gpt5mini", "gpt-4o", "gpt-4o-mini", "o1-mini", "o3-mini", "chat", "gpt", "copilot")

foreach ($d in $deployments) {
    $url = "$endpoint/openai/deployments/$d/chat/completions?api-version=2024-02-01"
    $body = @{
        messages = @(
            @{ role = "user"; content = "Hello" }
        )
    } | ConvertTo-Json

    try {
        $res = Invoke-RestMethod -Uri $url -Method Post -Headers @{ "api-key" = $apiKey } -ContentType "application/json" -Body $body
        Write-Host "SUCCESS: '$d' responded with:" $res.choices[0].message.content
    } catch {
        Write-Host "FAILED: '$d' ->" $_.Exception.Message
    }
}
