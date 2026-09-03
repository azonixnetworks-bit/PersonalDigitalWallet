using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalDigitalVault.Api.DTOs.Profile; 
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profile")] 
public class ProfileController(IProfileService service):ControllerBase
{ [HttpGet] 
    public async Task<IActionResult> Get()=>Ok(await service.GetAsync()); 
    [HttpPut] 
    public async Task<IActionResult> Update(UpdateProfileDto dto)=>Ok(await service.UpdateAsync(dto)); 
}
