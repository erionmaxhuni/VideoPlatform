using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;
using FluentValidation;
using VideoPlatform.Models;
using Newtonsoft.Json;
using VideoPlatform.Data;
using DbConnection = VideoPlatform.Data.DbConnection;

namespace VideoPlatform.Controllers;
[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly DbConnection _dbConnection;
    private readonly IValidator<CategoryDto> _validator;
    public CategoryController(DbConnection dbConnection, IValidator<CategoryDto> validator)
    {
        _dbConnection = dbConnection;
        _validator = validator;
    }

    [HttpGet]
    public ActionResult<List<Category>> Get()
    {
        var categories = JsonConvert.SerializeObject(_dbConnection.Categories.ToList());
        return Ok(categories);
    }

    [HttpGet("{id}")] // ? 
    public ActionResult<Category> GetCategory(int id)
    {
        var category = _dbConnection.Categories.FirstOrDefault(c => c.Id == id);
        if (category is null)
            return NotFound("Category does not exist");
        return Ok(category);
    }


    [HttpPost]
    [Authorize(Roles = "admin")]
    public ActionResult<Category> PostCategory([FromForm] CategoryDto category)
    {

        var validationResult = _validator.Validate(category);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var existingCategory = _dbConnection.Categories.FirstOrDefault(c => c.Name.ToLower() == category.Name.ToLower());

        if (existingCategory != null)
            return Conflict("Category already Exists");

        var newCategory = new Category()
        {
            Name = category.Name,
            ImageName = $"{Guid.NewGuid()}{Path.GetExtension(category.ImageFile.FileName)}"
        };

        Console.WriteLine(newCategory.ImageName);

        //save image to assets folder
        using (var fileStream = new FileStream(Path.Combine("/Users/Esad/Desktop/BackEnd-2nd/VideoPlatform/VideoPlatform", "assets", "category-images", newCategory.ImageName), FileMode.Create))
        {
            category.ImageFile.CopyTo(fileStream);
        }

        var addedCategory = _dbConnection.Categories.Add(newCategory);
        _dbConnection.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = addedCategory.Entity.Id }, category);
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize(Roles = "admin")]
    public ActionResult<Category> UpdateCategory(int id, [FromBody] CategoryUpdateDto category)
    {

        var existingCategory = _dbConnection.Categories.FirstOrDefault(c => c.Id == id);

        if (existingCategory is null)
            return NotFound("Category does not exist");

        if (category.Name == null || category.Name == "")
            return BadRequest("Category Name cannot be empty");

        existingCategory.Name = category.Name;
        _dbConnection.Categories.Update(existingCategory);
        _dbConnection.SaveChanges();

        return Ok(existingCategory);
    }

    [HttpDelete]
    [Route("{id}")]
    [Authorize(Roles = "admin")]
    public ActionResult<Category> DeleteCategory(int id)
    {

        var existingCategory = _dbConnection.Categories.FirstOrDefault(c => c.Id == id);

        if (existingCategory is null)
            return NotFound("Category does not exist");

        _dbConnection.Categories.Remove(existingCategory); // Check again for cascade delete
        _dbConnection.SaveChanges();

        return NoContent();
    }
}
