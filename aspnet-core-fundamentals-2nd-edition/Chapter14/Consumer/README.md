# Generated client check

This folder holds the inputs for checking Chapter 14's OpenAPI documents with a generated client; it is not a pre-generated client.

1. Run the Chapter 14 application and save its `v1` and `v2` OpenAPI documents as `v1.json` and `v2.json`.
2. Generate the clients with Microsoft.OpenApi.Kiota 1.36.1:

   ```shell
   kiota generate -l CSharp -c CatalogClient -n Catalog.V1 -d v1.json -o GeneratedV1
   kiota generate -l CSharp -c CatalogClient -n Catalog.V2 -d v2.json -o GeneratedV2
   ```

3. Build and run the Consumer project against the running application. It references Microsoft.Kiota.Bundle 2.1.2.

The check works with both the OpenAPI 3.2 documents the application produces by default and the 3.1 compatibility format.
