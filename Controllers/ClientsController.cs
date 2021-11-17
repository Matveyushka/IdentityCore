using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

public class ClientModel
{
    public int? Id { get; set; }

    [Required]
    [NoSpaces]
    [Display(Name = "Client ID")]
    [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string ClientId { get; set; }

    [Required]
    [Display(Name = "Client name")]
    [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string ClientName { get; set; }

    [Display(Name = "Description")]
    [StringLength(120, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
    public string Description { get; set; }

    [Display(Name = "Scopes")]
    public List<string> Scopes { get; set; }

    [Display(Name = "Redirect URIs")]
    public List<string> RedirectUris { get; set; }

    public bool Enabled { get; set; }
}

[Authorize(IdentityServer4.IdentityServerConstants.LocalApi.PolicyName)]
[Route("IdentityAdmin/[controller]")]
public class ClientsController : Controller
{
    public IServiceProvider _service { get; set; }

    public ClientsController(IServiceProvider service)
    {
        _service = service;
    }

    private List<string> GetInvalidScopes(ConfigurationDbContext _context, List<string> scopeNames)
    {
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

    private void addClient(ClientModel data, ConfigurationDbContext context)
    {
        if (data.ClientId.Contains(" "))
        {
            ModelState.AddModelError("No spaces", "The client ID must not contain spaces");
        }
        if (context.Clients.Any(client => (client.ClientId == data.ClientId)))
        {
            ModelState.AddModelError("Duplicate id", "The client with this id exists already");
        }
        if (context.Clients.Any(client => (client.ClientName == data.ClientName)))
        {
            ModelState.AddModelError("Duplicate name", "The client with this name exists already");
        }
        if (ModelState.IsValid)
        {
            var newClient = new IdentityServer4.EntityFramework.Entities.Client()
            {
                ClientId = data.ClientId,
                ClientName = data.ClientName,
                Description = data.Description,
                Enabled = data.Enabled,
                AllowOfflineAccess = true,
                RequireClientSecret = false,
                IncludeJwtId = true
            };

            context.Clients.Add(newClient);

            int id = newClient.Id;

            newClient.AllowedGrantTypes = new List<ClientGrantType>() {
                    new ClientGrantType() {
                        GrantType = "authorization_code",
                        ClientId = id
                    }
                };

            var redirectUris = data.RedirectUris.Distinct()
                .Select(uri => new ClientRedirectUri()
                {
                    RedirectUri = uri,
                    ClientId = id
                }).ToList();

            var scopes = data.Scopes.Distinct()
                .Select(scopeName => new ClientScope()
                {
                    Scope = scopeName,
                    ClientId = id
                }).ToList();

            if (scopes != null)
            {
                newClient.AllowedScopes = scopes;
            }
            if (redirectUris != null)
            {
                newClient.AllowedCorsOrigins = new List<ClientCorsOrigin>() { new ClientCorsOrigin() {
                    Origin = redirectUris[0].RedirectUri,
                    ClientId = id
                } };
                newClient.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri>() {
                    new ClientPostLogoutRedirectUri() {
                        PostLogoutRedirectUri = redirectUris[0].RedirectUri,
                        ClientId = id
                    }
                };
                newClient.RedirectUris = redirectUris;
            }

            context.SaveChanges();
        }
    }

    private void updateClient(ClientModel data, ConfigurationDbContext context)
    {
        if (data.ClientId.Contains(" "))
        {
            ModelState.AddModelError("No spaces", "The client ID must not contains spaces");
        }
        if (context.Clients.Any(client => ((client.ClientId == data.ClientId) && (client.Id != data.Id))))
        {
            ModelState.AddModelError("Duplicate id", "The client with this id exists already");
        }
        if (context.Clients.Any(client => ((client.ClientName == data.ClientName) && (client.Id != data.Id))))
        {
            ModelState.AddModelError("Duplicate name", "The client with this name exists already");
        }
        if (ModelState.IsValid)
        {
            var clientToUpdate = context.Clients
                .Include(client => client.AllowedScopes)
                .Include(client => client.RedirectUris)
                .Include(client => client.AllowedGrantTypes)
                .Include(client => client.AllowedCorsOrigins)
                .Include(client => client.PostLogoutRedirectUris)
                .SingleOrDefault(client => client.Id == data.Id);


            if (clientToUpdate != null)
            {
                clientToUpdate.ClientId = data.ClientId;
                clientToUpdate.ClientName = data.ClientName;
                clientToUpdate.Description = data.Description;
                clientToUpdate.Enabled = data.Enabled;
                clientToUpdate.AllowOfflineAccess = true;
                clientToUpdate.RequireClientSecret = false;
                clientToUpdate.IncludeJwtId = true;
                clientToUpdate.AllowedGrantTypes = new List<ClientGrantType>() {
                    new ClientGrantType() {
                        GrantType = "authorization_code",
                        ClientId = data.Id ?? 0
                    }
                };

                var redirectUris = data.RedirectUris.Distinct()
                    .Select(uri => new ClientRedirectUri()
                    {
                        RedirectUri = uri,
                        ClientId = data.Id ?? 0
                    }).ToList();

                clientToUpdate.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri>() {
                    new ClientPostLogoutRedirectUri() {
                        PostLogoutRedirectUri = redirectUris[0].RedirectUri,
                        ClientId = data.Id ?? 0
                    }
                };
                clientToUpdate.AllowedCorsOrigins = new List<ClientCorsOrigin>() { new ClientCorsOrigin() {
                    Origin = redirectUris[0].RedirectUri,
                    ClientId = data.Id ?? 0
                } };

                var scopes = data.Scopes.Distinct()
                    .Select(scopeName => new ClientScope()
                    {
                        Scope = scopeName,
                        ClientId = data.Id ?? 0
                    }).ToList();

                if (scopes != null)
                {
                    clientToUpdate.AllowedScopes = scopes;
                }
                if (redirectUris != null)
                {
                    clientToUpdate.RedirectUris = redirectUris;
                }
            }

            context.SaveChanges();
        }
    }

    [HttpGet]
    public JsonResult Get(int from, int amount, string filter)
    {
        var clients = new List<ClientModel>();
        var resultAmount = 0;
        filter = filter ?? "";

        using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

            var enabledFilter = FilterDispatcher.GetEnabledFilterValue(filter);
            var disabledFilter = FilterDispatcher.GetDisabledFilterValue(filter);

            var filterIsEmpty = filter.Length == 0;

            var filteredClients = context.Clients
                .Include(c => c.AllowedScopes)
                .Include(c => c.RedirectUris)
                .Where(c =>
                    filterIsEmpty ||
                    c.ClientId.Contains(filter) ||
                    c.ClientName.Contains(filter) ||
                    c.Description.Contains(filter) ||
                    c.AllowedScopes.Any(scope => scope.Scope.Contains(filter)) ||
                    c.RedirectUris.Any(uri => uri.RedirectUri.Contains(filter)) ||
                    (c.Enabled && enabledFilter) ||
                    (!c.Enabled && disabledFilter));

            resultAmount = filteredClients.Count();

            clients = filteredClients
                .OrderBy(client => client.ClientName)
                .Skip(from)
                .Take(amount).ToList()
                .Select(c => new ClientModel()
                {
                    Id = c.Id,
                    ClientId = c.ClientId,
                    ClientName = c.ClientName,
                    Description = c.Description,
                    Scopes = c.AllowedScopes.Select(s => s.Scope).ToList(),
                    RedirectUris = c.RedirectUris.Select(s => s.RedirectUri).ToList(),
                    Enabled = c.Enabled
                }).ToList();
        }

        return new JsonResult(new
        {
            Amount = resultAmount,
            Payload = clients
        });
    }

    [HttpPost]
    public IActionResult Post([FromBody] ClientModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                var scopeNames = data.Scopes;

                var invalidScopes = new List<string>();

                if (scopeNames != null)
                {
                    invalidScopes = GetInvalidScopes(context, scopeNames);
                }

                if (invalidScopes.Count > 0)
                {
                    ModelState.AddModelError("Invalid scope", invalidScopes.Count == 1 ?
                        $"The scope {invalidScopes[0]} is invalid" :
                        $"Scopes {string.Join(", ", invalidScopes)} are invalid");
                }
                else
                {
                    addClient(data, context);
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
    public IActionResult Put([FromBody] ClientModel data)
    {
        if (ModelState.IsValid)
        {
            using (var serviceScope = _service.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                var scopeNames = data.Scopes;

                var invalidScopes = new List<string>();

                if (scopeNames != null)
                {
                    invalidScopes = GetInvalidScopes(context, scopeNames);
                }

                if (invalidScopes.Count > 0)
                {
                    ModelState.AddModelError("Invalid scope", invalidScopes.Count == 1 ?
                        $"The scope {invalidScopes[0]} is invalid" :
                        $"Scopes {string.Join(", ", invalidScopes)} are invalid");
                }
                else
                {
                    updateClient(data, context);
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
            var clientToDelete = context.Clients.FirstOrDefault(client => client.Id == id);
            if (clientToDelete != default(Client))
            {
                context.Clients.Remove(clientToDelete);
                context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }
    }
}