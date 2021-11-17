using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class ResourceModel
{
    public int? Id { get; set; }

    [NoSpaces]
    [Required]
    [Display(Name = "Name")]
    [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string Name { get; set; }

    [Display(Name = "Display Name")]
    [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string DisplayName { get; set; }

    [Display(Name = "Description")]
    [StringLength(120, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string Description { get; set; }

    public bool Enabled { get; set; }

    [Display(Name = "Scopes")]
    public List<string> Scopes { get; set; }
}

[Authorize(IdentityServer4.IdentityServerConstants.LocalApi.PolicyName)]
[Route("IdentityAdmin/[controller]")]
public class ResourcesController : Controller
{
    public IServiceProvider _service { get; set; }

    public ResourcesController(IServiceProvider service)
    {
        _service = service;
    }

    public List<string> GetInvalidScopes(ConfigurationDbContext _context, List<string> scopeNames)
    {
        if (scopeNames.Count == 0) return new List<string>();

        var existingScopes = _context.ApiScopes
            .Where(scope => scopeNames.Contains(scope.Name))
            .Select(scope => scope.Name)
            .ToList();

        var existingIdentityResources = _context.IdentityResources
            .Where(scope => scopeNames.Contains(scope.Name))
            .Select(scope => scope.Name)
            .ToList();

        var invalidScopes = scopeNames
            .Where(scopeName => (!existingScopes.Contains(scopeName) && !existingIdentityResources.Contains(scopeName)))
            .ToList();

        return invalidScopes;
    }

    [HttpGet]
    public IActionResult Get(int from, int amount, string filter)
    {
        filter = filter ?? "";
        var resources = new List<ResourceModel>();
        var resourcesAmount = 0;

        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

            bool filterIsEmpty = filter.Length == 0;

            var enabledFilter = FilterDispatcher.GetEnabledFilterValue(filter);
            var disabledFilter = FilterDispatcher.GetDisabledFilterValue(filter);

            var result = context.ApiResources
                .Include(r => r.Scopes)
                .Where(r =>
                    filterIsEmpty ||
                    r.Name.Contains(filter) ||
                    r.DisplayName.Contains(filter) ||
                    r.Description.Contains(filter) ||
                    (r.Enabled && enabledFilter) ||
                    (!r.Enabled && disabledFilter) ||
                    r.Scopes.Any(scope => scope.Scope.Contains(filter))
                )
                .Select(r => new ResourceModel()
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    Enabled = r.Enabled,
                    Scopes = r.Scopes.Select(s => s.Scope).ToList()
                });

            resourcesAmount = result.Count();

            resources = result
                .OrderBy(resource => resource.Name)
                .Skip(from)
                .Take(amount)
                .ToList();
        }

        return new JsonResult(new
        {
            Amount = resourcesAmount,
            Payload = resources
        });
    }

    [HttpPost]
    public IActionResult Post([FromBody] ResourceModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (context.ApiResources.Any(res => (res.Name == data.Name)))
                {
                    ModelState.AddModelError("Duplicate", "The resource with this name exists already");
                }
                else
                {
                    var scopeNames = data.Scopes.Distinct().ToList();

                    var invalidScopes = new List<string>();

                    if (scopeNames != null)
                    {
                        invalidScopes = GetInvalidScopes(context, scopeNames);
                    }

                    if (invalidScopes.Count > 0)
                    {
                        ModelState.AddModelError("Invalid scope", invalidScopes.Count == 1 ?
                            $"The scope {invalidScopes[0]} is invalid" :
                            $"Scopes {string.Join(" ", invalidScopes)} are invalid");
                    }
                    else
                    {
                        var newResource = new ApiResource()
                        {
                            Name = data.Name,
                            DisplayName = data.DisplayName,
                            Description = data.Description,
                            Enabled = data.Enabled
                        };

                        context.ApiResources.Add(newResource);

                        int id = newResource.Id;

                        if (scopeNames != null)
                        {
                            var scopes = scopeNames.Select(scopeName => new ApiResourceScope()
                            {
                                Scope = scopeName,
                                ApiResourceId = id
                            }).ToList();

                            newResource.Scopes = scopes;
                        }

                        context.SaveChanges();
                    }
                }
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
    public IActionResult Put([FromBody] ResourceModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (context.ApiResources.Any(res => (res.Name == data.Name) && (res.Id != data.Id)))
                {
                    ModelState.AddModelError("Duplicate", "The resource with this name exists already");
                }
                else
                {
                    var scopeNames = data.Scopes.Distinct().ToList();

                    var invalidScopes = new List<string>();

                    if (scopeNames != null)
                    {
                        invalidScopes = GetInvalidScopes(context, scopeNames);
                    }

                    if (invalidScopes.Count > 0)
                    {
                        ModelState.AddModelError("Invalid scope", invalidScopes.Count == 1 ?
                            $"The scope {invalidScopes[0]} is invalid" :
                            $"Scopes {string.Join(" ", invalidScopes)} are invalid");
                    }
                    else
                    {
                        var toUpdate = context.ApiResources
                            .Include(resource => resource.Scopes)
                            .SingleOrDefault(r => r.Id == data.Id);
                        if (toUpdate != null)
                        {
                            toUpdate.Name = data.Name;
                            toUpdate.DisplayName = data.DisplayName;
                            toUpdate.Description = data.Description;
                            toUpdate.Enabled = data.Enabled;

                            if (scopeNames != null)
                            {
                                var scopes = scopeNames.Select(scopeName => new ApiResourceScope()
                                {
                                    Scope = scopeName,
                                    ApiResourceId = data.Id ?? 0
                                }).ToList();

                                toUpdate.Scopes = scopes;
                            }
                        }
                        context.SaveChanges();
                    }
                }
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
    public IActionResult Delete(int id)
    {
        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            var resourceToDelete = context.ApiResources.FirstOrDefault(resource => resource.Id == id);
            if (resourceToDelete != default(ApiResource))
            {
                context.ApiResources.Remove(resourceToDelete);
                context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }
    }
}