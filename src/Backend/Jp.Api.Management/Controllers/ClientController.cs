﻿using IdentityServer4.Models;
using Jp.Api.Management.ViewModel;
using JPProject.Admin.Application.Interfaces;
using JPProject.Admin.Application.ViewModels;
using JPProject.Admin.Application.ViewModels.ClientsViewModels;
using JPProject.Domain.Core.Bus;
using JPProject.Domain.Core.Interfaces;
using JPProject.Domain.Core.Notifications;
using JPProject.Domain.Core.ViewModels;
using JPProject.Sso.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using ServiceStack;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Jp.Api.Management.Controllers
{
    [Route("clients"), Authorize(Policy = "Default")]
    public class ClientsController : ApiController
    {
        private readonly IClientAppService _clientAppService;
        private readonly ISystemUser _user;
        private readonly IStorage _storage;

        public ClientsController(
            INotificationHandler<DomainNotification> notifications,
            IMediatorHandler mediator,
            IClientAppService clientAppService,
            ISystemUser user,
            IStorage storage) : base(notifications, mediator)
        {
            _clientAppService = clientAppService;
            _user = user;
            _storage = storage;
        }

        [HttpGet("")]
        public async Task<ActionResult<IEnumerable<ClientListViewModel>>> ListClients()
        {
            var clients = await _clientAppService.GetClients();
            return ResponseGet(clients);
        }

        [HttpGet("{client}")]
        public async Task<ActionResult<Client>> GetClient(string client)
        {
            var clients = await _clientAppService.GetClientDefaultDetails(client);
            return ResponseGet(clients);
        }

        [HttpPost("")]
        public async Task<ActionResult<Client>> Post([FromBody] SaveClientWithLogoViewModel client)
        {
            if (!ModelState.IsValid)
            {
                NotifyModelStateErrors();
                return ModelStateErrorResponseError();
            }

            if (client.Logo != null)
            {
                client.Logo.VirtualLocation = "images";
                client.LogoUri = await _storage.Upload(client.Logo);
            }
            await _clientAppService.Save(client);

            var newClient = await _clientAppService.GetClientDetails(client.ClientId);

            return ResponsePost(nameof(GetClient), new { client = client.ClientId }, newClient);
        }

        [HttpPut("{client}")]
        public async Task<ActionResult<ClientViewModel>> Update(string client, [FromBody] UpdateClientWithLogoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                NotifyModelStateErrors();
                return ModelStateErrorResponseError();
            }

            if (model.Logo != null)
            {
                await _storage.Remove(Path.GetFileName(model.LogoUri), "images");
                model.LogoUri = await _storage.Upload(model.Logo);
            }

            await _clientAppService.Update(client, model);
            return ResponsePutPatch();
        }

        [HttpPatch("{client}")]
        public async Task<ActionResult<ClientViewModel>> PartialUpdate(string client, [FromBody] JsonPatchDocument<Client> model)
        {
            if (!ModelState.IsValid)
            {
                NotifyModelStateErrors();
                return ModelStateErrorResponseError();
            }

            var clientDb = await _clientAppService.GetClientDetails(client);
            if (clientDb == null)
            {
                ModelState.AddModelError("client", "Invalid Api Resource");
                return ModelStateErrorResponseError();
            }

            model.ApplyTo(clientDb);
            await _clientAppService.Update(client, clientDb);
            return ResponsePutPatch();
        }

        [HttpDelete("{client}")]
        public async Task<ActionResult> Delete(string client)
        {
            var command = new RemoveClientViewModel(client);
            await _clientAppService.Remove(command);
            return ResponseDelete();
        }


        [HttpPost("{client}/copy")]
        public async Task<ActionResult<Client>> Copy(string client)
        {
            var clientDb = new CopyClientViewModel(client);
            await _clientAppService.Copy(clientDb);
            var newClient = await _clientAppService.GetClientDetails(client);
            return ResponsePost(nameof(GetClient), new { client }, newClient);
        }

        [HttpGet("{client}/secrets")]
        public async Task<ActionResult<IEnumerable<SecretViewModel>>> Secrets(string client)
        {
            var clients = await _clientAppService.GetSecrets(client);
            return ResponseGet(clients);
        }

        [HttpDelete("{client}/secrets/{secretId:int}")]
        public async Task<ActionResult> RemoveSecret(string client, int secretId)
        {
            var model = new RemoveClientSecretViewModel(client, secretId);
            await _clientAppService.RemoveSecret(model);
            return ResponseDelete();
        }

        [HttpPost("{client}/secrets")]
        public async Task<ActionResult<IEnumerable<SecretViewModel>>> SaveSecret(string client, [FromBody] SaveClientSecretViewModel model)
        {
            if (!ModelState.IsValid)
            {
                NotifyModelStateErrors();
                return ModelStateErrorResponseError();
            }
            await _clientAppService.SaveSecret(model);
            var secret = await _clientAppService.GetSecrets(client);
            return ResponsePost(nameof(Secrets), new { client }, secret);
        }

        [HttpGet("{client}/properties")]
        public async Task<ActionResult<IEnumerable<ClientPropertyViewModel>>> Properties(string client)
        {
            var clients = await _clientAppService.GetProperties(client);
            return ResponseGet(clients);
        }

        [HttpDelete("{client}/properties/{propertyId:int}")]
        public async Task<ActionResult> RemoveProperty(string client, int propertyId)
        {
            var model = new RemovePropertyViewModel(propertyId, client);
            await _clientAppService.RemoveProperty(model);
            return ResponseDelete();
        }


        [HttpPost("{client}/properties")]
        public async Task<ActionResult<IEnumerable<ClientPropertyViewModel>>> SaveProperty(string client, [FromBody] SaveClientPropertyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                NotifyModelStateErrors();
                return ModelStateErrorResponseError();
            }
            await _clientAppService.SaveProperty(model);
            var properties = await _clientAppService.GetProperties(client);
            return ResponsePost(nameof(Properties), new { client }, properties);
        }

        [HttpGet("{client}/claims")]
        public async Task<ActionResult<IEnumerable<ClaimViewModel>>> Claims(string client)
        {
            var clients = await _clientAppService.GetClaims(client);
            return ResponseGet(clients);
        }

        [HttpDelete("{client}/claims/{claimId:int}")]
        public async Task<ActionResult> RemoveClaim(string client, int claimId)
        {
            var model = new RemoveClientClaimViewModel(client, claimId);
            await _clientAppService.RemoveClaim(model);
            return ResponseDelete();
        }


        [HttpPost("{client}/claims")]
        public async Task<ActionResult<IEnumerable<ClaimViewModel>>> SaveClaim(string client, [FromBody] SaveClientClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                NotifyModelStateErrors();
                return ModelStateErrorResponseError();
            }
            await _clientAppService.SaveClaim(model);
            var claims = await _clientAppService.GetClaims(client);
            return ResponsePost(nameof(Claims), new { client }, claims);
        }

    }

}
