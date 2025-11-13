# PowerShell-скрипт: получить ответ LM Studio с кириллицей без кракозябр
$body = @{
    model = "cpatonn/Qwen3-30B-A3B-Instruct-2507-AWQ-4bit"
    messages = @(@{ role = "user"; content = "Кто ты?" })
} | ConvertTo-Json -Depth 4

# Отправить запрос и сохранить ответ в файл
Invoke-WebRequest -Uri "http://10.61.19.99:1234/v1/chat/completions" `
    -Method Post `
    -ContentType "application/json; charset=utf-8" `
    -Body $body `
    -OutFile "llm_response.json"

# Прочитать ответ как UTF-8 и вывести в консоль
Get-Content "llm_response.json" -Encoding UTF8
