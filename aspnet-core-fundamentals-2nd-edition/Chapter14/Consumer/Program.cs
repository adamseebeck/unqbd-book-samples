using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

var endpoint = args.Single();
using var http1 = new HttpClient();
using var http2 = new HttpClient();
var adapter1 = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http1)
    { BaseUrl = endpoint };
var adapter2 = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http2)
    { BaseUrl = endpoint };
var v1 = await new Catalog.V1.CatalogClient(adapter1).Api.V1.Products.GetAsync();
var v2 = await new Catalog.V2.CatalogClient(adapter2).Api.V2.Products.GetAsync();
if (v1 is not { Count: 1 } || v1[0].Name != "Notebook" ||
    v2 is not { Count: 1 } || v2[0].UnitPrice != 12.50)
    throw new InvalidOperationException("Generated consumer received an unexpected contract.");
Console.WriteLine("PASS generated v1 and v2 clients against the local endpoints");
