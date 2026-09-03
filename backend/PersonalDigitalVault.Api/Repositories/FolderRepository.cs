using Microsoft.EntityFrameworkCore; using PersonalDigitalVault.Api.Data; using PersonalDigitalVault.Api.Entities; using PersonalDigitalVault.Api.Interfaces.Repositories;
namespace PersonalDigitalVault.Api.Repositories;
public class FolderRepository(AppDbContext db) : IFolderRepository
{
    public Task<List<Folder>> GetByUserAsync(int userId)=>db.Folders.AsNoTracking().Where(x=>x.UserId==userId).OrderBy(x=>x.Name).ToListAsync();
    public Task<Folder?> GetOwnedAsync(int id,int userId)=>db.Folders.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);
    public async Task AddAsync(Folder folder){db.Folders.Add(folder);await db.SaveChangesAsync();}
    public async Task UpdateAsync(Folder folder){db.Folders.Update(folder);await db.SaveChangesAsync();} 
    public async Task DeleteAsync(Folder folder){db.Folders.Remove(folder);await db.SaveChangesAsync();}
}
