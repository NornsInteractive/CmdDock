using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public interface ICategoryService
{
    Task<List<CategoryItem>> GetAllCategoryItemsAsync(bool forceReload = false);
    Task<List<string>> GetAllCategoriesAsync(bool forceReload = false);
    Task<CategoryItem?> GetCategoryByNameAsync(string name);
    Task AddCategoryAsync(string category, string? iconGlyph = null);
    Task RenameCategoryAsync(string oldName, string newName);
    Task UpdateCategoryAsync(string oldName, string newName, string? newIconGlyph = null);
    Task UpdateCategoryIconAsync(string name, string iconGlyph);
    Task DeleteCategoryAsync(string category, string fallbackCategory = "常用");
    Task ReorderCategoriesAsync(List<string> orderedCategories);
    Task SaveCategoriesAsync(List<CategoryItem> categories);
}
