using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using IdentityCore.Data;

public class BecomeAdminModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    public IServiceProvider _service { get; set; }
    public BecomeAdminModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IServiceProvider service)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _service = service;
    }

    private bool adminExists(ApplicationDbContext context) {
        var adminExists = false;

        var adminRole = context.Roles.SingleOrDefault(role => role.Name == "IdentityAdmin");
        
        if (adminRole != null) 
        {
            adminExists = context.UserRoles
                .Any(userRole => userRole.RoleId == adminRole.Id);
        }

        return adminExists;
    }

    public async Task<IActionResult> OnGet()
    {
        var adminAlreadyExists = false;

        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            adminAlreadyExists = adminExists(context);

            if (!adminAlreadyExists)
            {
                await _userManager.AddToRoleAsync(
                    await _userManager.GetUserAsync(User), 
                    "IdentityAdmin");
            }
        }

        if (adminAlreadyExists) 
        {
            return StatusCode(403);
        } 
        else 
        {
            return StatusCode(200);
        }
    }
}
