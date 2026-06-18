using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _db;
        public CategoryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<CategoryReadDto>> GetAllAsync()
        {
            var categories = await _db.Categories.AsNoTracking().ToListAsync();
            return categories.Select(c => c.ToReadDto());
        }

        public async Task<CategoryReadDto?> GetByIdAsync(int id)
        {
            var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return category?.ToReadDto();
        }

        public async Task<CategoryReadDto> CreateAsync(CategoryCreateDto dto)
        {
            var category = dto.ToEntity();
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return category.ToReadDto();
        }

        public async Task<bool> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var existing = await _db.Categories.FindAsync(id);
            if (existing is null) return false;

            existing.ApplyUpdates(dto);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.Categories.FindAsync(id);
            if (existing is null) return false;

            _db.Categories.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
