using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IdentityCore.Data;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public class UserModel
{
#nullable enable
    public string? Id { get; set; }

#nullable disable

    [Required]
    [NoSpaces]
    [Display(Name = "Name")]
    [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [Display(Name = "Agent type")]
    public int AgentType { get; set; }

    [Display(Name = "Confirmed")]
    public bool Confirmed { get; set; }

    [Display(Name = "Scopes")]
    public string Scopes { get; set; }

    [Display(Name = "IsIdentityAdmin")]
    public bool IsIdentityAdmin { get; set; }
}

[Authorize(IdentityServer4.IdentityServerConstants.LocalApi.PolicyName)]
[Route("IdentityAdmin/[controller]")]
public class UsersController : Controller
{
    public IServiceProvider _service { get; set; }

    public UsersController(IServiceProvider service)
    {
        _service = service;
    }

    [HttpGet]
    public JsonResult Get(int from, int amount, string filter)
    {
        var users = new List<UserModel>();
        var usersAmount = 0;

        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (filter == null)
            {
                filter = "";
            }

            var confirmedFilter = FilterDispatcher.GetBooleanFilterValue(filter,
                new List<string> { "confirmed" }, new List<string> { "+", "true" });

            var isAdminFilter = FilterDispatcher.GetBooleanFilterValue(filter,
                new List<string> { "admin" }, new List<string> { "+", "true" });

            var adminRole = context.Roles.SingleOrDefault(role => role.Name == "IdentityAdmin");

            var admins = context.UserRoles.Where(userRole => userRole.RoleId == adminRole.Id);

            var filterIsNull = filter == null;
            var filterNumber = -1;
            if (int.TryParse(filter, out var _))
            {
                filterNumber = int.Parse(filter);
            }

            var filteredUsers = context.Users
                .Select(user => new UserModel()
                {
                    Id = user.Id,
                    Name = user.UserName,
                    AgentType = user.AgentType,
                    Confirmed = user.EmailConfirmed,
                    IsIdentityAdmin = admins.Any(admin => admin.UserId == user.Id)
                })
                .Where(user =>
                    filterIsNull ||
                    user.Name.Contains(filter) ||
                    user.AgentType == filterNumber ||
                    (user.Confirmed && confirmedFilter) ||
                    (user.IsIdentityAdmin && isAdminFilter)
                );

            usersAmount = filteredUsers.Count();

            users = filteredUsers.OrderBy(user => user.Name).Skip(from).Take(amount).ToList();
        }

        return new JsonResult(new
        {
            Amount = usersAmount,
            Payload = users
        });
    }

    [HttpPost]
    public IActionResult Post([FromBody] UserModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (context.Users.Any(user => ((user.UserName == data.Name) && (user.Id != data.Id))))
                {
                    ModelState.AddModelError("Duplicate", "The user with this name exists already");
                }
                else
                {
                    var newUser = new ApplicationUser()
                    {
                        UserName = data.Name,
                        AgentType = data.AgentType,
                        EmailConfirmed = data.Confirmed
                    };

                    context.Users.Add(newUser);

                    var adminRole = context.Roles.SingleOrDefault(role => role.Name == "IdentityAdmin");

                    var connection = context.UserRoles.SingleOrDefault(userRole =>
                        userRole.UserId == newUser.Id && userRole.RoleId == adminRole.Id);

                    if (data.IsIdentityAdmin)
                    {
                        if (connection == null)
                        {
                            context.UserRoles.Add(new IdentityUserRole<string>()
                            {
                                UserId = newUser.Id,
                                RoleId = adminRole.Id
                            });
                        }
                    }
                    else
                    {
                        if (connection != null)
                        {
                            context.UserRoles.Remove(connection);
                        }
                    }
                }
                context.SaveChanges();
            }
        }
        if (ModelState.ErrorCount > 0)
        {
            return BadRequest(ModelStateHandler.GetErrorList(ModelState));
        }
        else
        {
            return Ok();
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] UserModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (context.Users.Any(user => ((user.UserName == data.Name) && (user.Id != data.Id))))
                {
                    ModelState.AddModelError("Duplicate", "The user with this name exists already");
                }
                else
                {
                    var toUpdate = context.Users
                        .SingleOrDefault(r => r.Id == data.Id);

                    if (toUpdate != null)
                    {
                        toUpdate.EmailConfirmed = data.Confirmed;
                        toUpdate.AgentType = data.AgentType;

                        var adminRole = context.Roles.SingleOrDefault(role => role.Name == "IdentityAdmin");

                        var connection = context.UserRoles.SingleOrDefault(userRole =>
                            userRole.UserId == data.Id && userRole.RoleId == adminRole.Id);

                        if (data.IsIdentityAdmin)
                        {
                            if (connection == null)
                            {
                                context.UserRoles.Add(new IdentityUserRole<string>()
                                {
                                    UserId = data.Id,
                                    RoleId = adminRole.Id
                                });
                            }
                        }
                        else
                        {
                            if (connection != null)
                            {
                                context.UserRoles.Remove(connection);
                            }
                        }
                    }
                }
                context.SaveChanges();
            }
        }
        if (ModelState.ErrorCount > 0)
        {
            return BadRequest(ModelStateHandler.GetErrorList(ModelState));
        }
        else
        {
            return Ok();
        }
    }

    [HttpDelete]
    public IActionResult Delete(string id)
    {
        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userToDelete = context.Users.FirstOrDefault(user => user.Id == id);
            if (userToDelete != default(ApplicationUser))
            {
                context.Users.Remove(userToDelete);
                context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }
    }
}