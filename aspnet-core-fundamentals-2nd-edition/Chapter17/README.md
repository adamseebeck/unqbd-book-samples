# Chapter 17: Performance Optimization and Benchmarking

The complete code for Chapter 17, matching the listings in the book: Api/ (the ingestion API), Benchmarks/ (BenchmarkDotNet) and Checks/. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

From this folder:

- API: `dotnet run --project Api --urls http://127.0.0.1:42149`
- Benchmarks: `dotnet run -c Release --project Benchmarks -- --filter *CsvParsingBenchmarks* --job short`
- Checks: `dotnet run --project Checks -c Release`

The request file is Api/requests.http.

## Sending requests

`requests.http` holds every request this chapter checks, in order, each with the status you should see. Visual Studio 2026 runs it directly (open the file and select Send Request above a request); VS Code needs the REST Client extension.

In Windows PowerShell, type `curl.exe`, not `curl`: in Windows PowerShell 5.1, `curl` is a different command. The commands below are the same requests. Run them from any folder; JSON bodies go to body.json first because Windows PowerShell removes double quotes from arguments it passes to curl.exe.

```powershell
# One record with three fields (the last one empty). Expect 200 {"records":1,"fields":3}
'a,b,' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: text/plain" --data-binary "@body.json" http://127.0.0.1:42149/api/ingestion/batch

# Two records. Expect 200 {"records":2,"fields":5}
@'
id,name
1,Trail Bottle,24.99
'@ | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: text/plain" --data-binary "@body.json" http://127.0.0.1:42149/api/ingestion/batch

# Quoted fields are outside this lab's narrow format. Expect 400
'"a,b",c' | Set-Content body.json
curl.exe -i -w "\n" -H "Content-Type: text/plain" --data-binary "@body.json" http://127.0.0.1:42149/api/ingestion/batch

# The sample response, compressed because the client accepts gzip (Content-Encoding: gzip). Expect 200 {"records":1000,"fields":3000}
curl.exe -i -w "\n" --compressed http://127.0.0.1:42149/api/ingestion/sample

```
