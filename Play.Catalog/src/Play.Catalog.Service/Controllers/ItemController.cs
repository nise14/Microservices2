using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Catalog.Service.Extensions;
using Play.Common;

namespace Play.Catalog.Service.Controllers;

[Route("[controller]")]
[ApiController]
public class ItemController : ControllerBase
{
    private readonly IRepository<Item> _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ItemController(IRepository<Item> repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
    {
        var items = (await _repository.GetAllAsync())
                    .Select(item => item.AsDto());

        return Ok(items);
    }

    [HttpGet("{id}", Name = "GetByIdAsync")]
    public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        return item.AsDto();
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
    {
        var item = new Item
        {
            Name = createItemDto.Name,
            Description = createItemDto.Description,
            Price = createItemDto.Price,
            CreatedDate = DateTimeOffset.UtcNow
        };

        await _repository.CreateAsync(item);

        await _publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

        return CreatedAtRoute(nameof(GetByIdAsync), new { item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
    {
        var item = await _repository.GetAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        item.Name = updateItemDto.Name;
        item.Description = updateItemDto.Description;
        item.Price = updateItemDto.Price;

        await _repository.UpdateAsync(item);

        await _publishEndpoint.Publish(new CatalogItemUpdated(item.Id, item.Name, item.Description));

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var item = await _repository.GetAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        await _repository.RemoveAsync(item.Id);

        await _publishEndpoint.Publish(new CatalogItemDeleted(id));

        return NoContent();
    }
}