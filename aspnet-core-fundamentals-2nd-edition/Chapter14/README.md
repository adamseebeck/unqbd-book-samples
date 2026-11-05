# Chapter 14

The complete code for Chapter 14: the API in this folder (AdventureWorks.OpenApi.csproj), which publishes v1 and v2 OpenAPI documents, and Consumer/, the generated-client check. Like every chapter from Chapter 7, this is its own small project: start from this folder rather than adding it to an earlier chapter's project.

## Run it

This folder has no launch profile, and the app maps its OpenAPI documents only in Development, so pass the environment yourself. From this folder:

```shell
dotnet run -- --environment Development
```

The app listens on http://localhost:5000:

- http://localhost:5000/api/v1/products and http://localhost:5000/api/v2/products
- http://localhost:5000/openapi/v1.json and http://localhost:5000/openapi/v2.json

Without `--environment Development` the app runs in Production and the two OpenAPI addresses return 404. Consumer/README.md generates clients from the two documents and calls both versions.
