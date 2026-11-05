# Generated client check

This folder holds the inputs for checking Chapter 14's OpenAPI documents with a generated client; it is not a pre-generated client.

1. Run the Chapter 14 application and save its `v1` and `v2` OpenAPI documents as `v1.json` and `v2.json`. Start it from the Chapter14 folder with `dotnet run -- --environment Development` (see ../README.md), then from this Consumer folder save the two documents (in Windows PowerShell, type `curl.exe`):

   ```shell
   curl -o v1.json http://localhost:5000/openapi/v1.json
   curl -o v2.json http://localhost:5000/openapi/v2.json
   ```

2. Generate the clients with Microsoft.OpenApi.Kiota 1.36.1. Kiota 1.36.1 is built for .NET 8 to 10, so install it with `--allow-roll-forward` to let it run on .NET 11:

   ```shell
   dotnet tool install --global Microsoft.OpenApi.Kiota --version 1.36.1 --allow-roll-forward
   ```

   Then, from this Consumer folder:

   ```shell
   kiota generate -l CSharp -c CatalogClient -n Catalog.V1 -d v1.json -o GeneratedV1
   kiota generate -l CSharp -c CatalogClient -n Catalog.V2 -d v2.json -o GeneratedV2
   ```

3. Build and run the Consumer project against the running application. It references Microsoft.Kiota.Bundle 2.1.2. Until step 2 has generated the clients, the build stops with error CS0246 for `Catalog`. From this folder:

   ```shell
   dotnet run -- http://localhost:5000
   ```

   It prints "PASS generated v1 and v2 clients against the local endpoints".

The check works with both the OpenAPI 3.2 documents the application produces by default and the 3.1 compatibility format.
