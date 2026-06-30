using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CategoryService> _logger;
        public CategoryService(AppDbContext db, ILogger<CategoryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryReadDto>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all categories");
            var categories = await _db.Categories.AsNoTracking().ToListAsync();
            _logger.LogInformation("Found {Count} categories", categories.Count);
            return categories.Select(c => c.ToReadDto());
        }

        public async Task<CategoryReadDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Fetching category {CategoryId}", id);
            var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (category is null)
                _logger.LogWarning("Category {CategoryId} not found", id);

            return category?.ToReadDto();
        }

        public async Task<CategoryReadDto> CreateAsync(CategoryCreateDto dto)
        {
            _logger.LogInformation("Creating category {CategoryName}", dto.Name);
            var category = dto.ToEntity();

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Category {CategoryId} created successfully with name {CategoryName}", category.Id, category.Name);
            return category.ToReadDto();
        }

        public async Task<bool> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            _logger.LogInformation("Updating category {CategoryId}", id);
            var existing = await _db.Categories.FindAsync(id);

            if (existing is null)
            {
                _logger.LogWarning("Category {CategoryId} not found for update", id);
                return false;
            }
            existing.ApplyUpdates(dto);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Category {CategoryId} updated successfully", id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting category {CategoryId}", id);
            var existing = await _db.Categories.FindAsync(id);

            if (existing is null)
            {
                _logger.LogWarning("Category {CategoryId} not found for deletion", id);
                return false;
            }

            _db.Categories.Remove(existing);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Category {CategoryId} deleted successfully", id);
            return true;
        }
    }
}
