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

public class ScopeModel
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

    [Display(Name = "Enabled")]
    public bool Enabled { get; set; }
}

[Authorize(IdentityServer4.IdentityServerConstants.LocalApi.PolicyName)]
[Route("IdentityAdmin/[controller]")]
public class ScopesController : Controller
{
    public IServiceProvider _service { get; set; }

    public ScopesController(IServiceProvider service)
    {
        _service = service;
    }

    [HttpGet]
    public JsonResult Get(int from, int amount, string filter)
    {
        var scopes = new List<ApiScope>();
        var scopeAmount = 0;

        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

            if (string.IsNullOrEmpty(filter))
            {
                scopes = context.ApiScopes.OrderBy(scope => scope.Name).Skip(from).Take(amount).ToList();
                scopeAmount = context.ApiScopes.Count();
            }
            else
            {
                var enabledFilter = FilterDispatcher.GetEnabledFilterValue(filter);
                var disabledFilter = FilterDispatcher.GetDisabledFilterValue(filter);

                var filterResult = context.ApiScopes.Where(scope =>
                    scope.Name.Contains(filter) ||
                    scope.DisplayName.Contains(filter) ||
                    scope.Description.Contains(filter) ||
                    (scope.Enabled && enabledFilter) ||
                    (!scope.Enabled && disabledFilter)
                );

                scopeAmount = filterResult.Count();

                scopes = filterResult.OrderBy(scope => scope.Name).Skip(from).Take(amount).ToList();
            }
        }

        return new JsonResult(new
        {
            Amount = scopeAmount,
            Payload = scopes
        });
    }

    [HttpPost]
    public IActionResult Post([FromBody] ScopeModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (context.ApiScopes.Any(scope => (scope.Name == data.Name)))
                {
                    ModelState.AddModelError("Duplicate", "The scope with this name exists already");
                }
                else
                {
                    context.ApiScopes.Add(new ApiScope()
                    {
                        Name = data.Name,
                        DisplayName = data.DisplayName,
                        Description = data.Description,
                        Enabled = data.Enabled
                    });

                    context.SaveChanges();
                    return Ok();
                }
            }
        }

        return BadRequest(ModelStateHandler.GetErrorList(ModelState));
    }

    [HttpPut]
    public IActionResult Put([FromBody] ScopeModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (context.ApiScopes.Any(scope => ((scope.Name == data.Name) && (scope.Id != data.Id))))
                {
                    ModelState.AddModelError("Duplicate", "The scope with this name exists already");
                }
                else
                {
                    var toUpdate = context.ApiScopes.SingleOrDefault(scope => scope.Id == data.Id);
                    if (toUpdate != null)
                    {
                        toUpdate.Name = data.Name;
                        toUpdate.DisplayName = data.DisplayName;
                        toUpdate.Description = data.Description;
                        toUpdate.Enabled = data.Enabled;
                    }

                    context.SaveChanges();
                    return Ok();
                }
            }
        }

        return BadRequest(ModelStateHandler.GetErrorList(ModelState));
    }

    [HttpDelete]
    public IActionResult Delete(int id)
    {
        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            var scopeToDelete = context.ApiScopes.FirstOrDefault(scope => scope.Id == id);
            if (scopeToDelete != default(ApiScope))
            {
                context.ApiScopes.Remove(scopeToDelete);
                context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }

    }
}