namespace PersonalDigitalVault.Api.DTOs.Admin;
public class UpdateUserStatusDto 
{
    // true  = Enable
    // false = Disable
    public bool IsActive { get; set; }
}