# Two refreshes of one refresh token, sent at the same moment.
# One rotates the token (200); the other loses (401): at the
# check for a consumed token if the first has already saved, or
# at the [ConcurrencyCheck] on ConsumedByHash if both read the
# token before either saved. The app's log tells them apart.
# Run it in Windows PowerShell 5.1 while the app is running:
#   .\RefreshRace.ps1
# If scripts are disabled on your machine, paste these lines
# into the PowerShell window instead.
param(
    [string]$Password = "Practice-Pass-2026!",
    [int]$Rounds = 5)

Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$auth = "https://localhost:7001/api/auth"

function Send([string]$path, [string]$json) {
    $content = New-Object System.Net.Http.StringContent(
        $json, [Text.Encoding]::UTF8, "application/json")
    $client.PostAsync("$auth/$path", $content)
}

$email = "casey@adventure-works.test"
foreach ($round in 1..$Rounds) {
    $login = (Send "login" ('{"email":"' + $email +
        '","password":"' + $Password + '"}')).Result
    $tokens = $login.Content.ReadAsStringAsync().Result |
        ConvertFrom-Json
    $body = '{"token":"' + $tokens.refreshToken + '"}'
    # Both requests start before either answers.
    $pair = @((Send "refresh" $body), (Send "refresh" $body))
    [Threading.Tasks.Task]::WaitAll($pair)
    $codes = $pair | ForEach-Object { [int]$_.Result.StatusCode }
    "Round {0}: {1}" -f $round, ($codes -join " and ")
}
