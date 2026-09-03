using Microsoft.EntityFrameworkCore; using PersonalDigitalVault.Api.Data; using PersonalDigitalVault.Api.Entities; using PersonalDigitalVault.Api.Interfaces.Repositories;
namespace PersonalDigitalVault.Api.Repositories;
public class CredentialRepository(AppDbContext db) : ICredentialRepository
{
    public Task<List<Credential>> GetByUserAsync(int userId)=>db.Credentials.AsNoTracking().Where(x=>x.UserId==userId).OrderBy(x=>x.Title).ToListAsync();
    public Task<Credential?> GetOwnedAsync(int id,int userId)=>db.Credentials.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);
    public async Task AddAsync(Credential credential){db.Credentials.Add(credential);await db.SaveChangesAsync();} 
    public async Task UpdateAsync(Credential credential){db.Credentials.Update(credential);await db.SaveChangesAsync();} 
    public async Task DeleteAsync(Credential credential){db.Credentials.Remove(credential);await db.SaveChangesAsync();}
}
