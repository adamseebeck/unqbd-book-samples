using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductDto[]>> Search([FromQuery] string? q,
        [FromQuery, Range(1, 10000)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 20) =>
        Ok(await products.SearchAsync(q, page, pageSize));

    [HttpGet("{id:int}", Name = "GetProductV1")]
    [ProducesResponseType<ProductDto>(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await products.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
    {
        var product = await products.CreateAsync(request);
        return CreatedAtRoute("GetProductV1", new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request) =>
        await products.UpdateAsync(id, request) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await products.DeleteAsync(id) ? NoContent() : NotFound();
}
