﻿using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using QuizMaster.API.Account.Proto;
using QuizMaster.API.Authentication.Models;
using Grpc.Core;
using Grpc.Net.Client;
using QuizMaster.API.Authentication.Proto;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using QuizMaster.API.Authentication.Helper;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace QuizMaster.API.Gateway.Controllers
{
    [ApiController]
    [Route("gateway/api/auth")]
    public class AuthenticationGatewayController : Controller
    {
        private readonly GrpcChannel _channel;
        private readonly AuthService.AuthServiceClient _channelClient;

        public AuthenticationGatewayController()
        {
            _channel = GrpcChannel.ForAddress("https://localhost:7062");
            _channelClient = new AuthService.AuthServiceClient(_channel);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest requestModel)
        {
            var request = new AuthenticationRequest()
            {
                Username = requestModel.Username,
                Email = requestModel.Email,
                Password = requestModel.Password,

            };

            // generate the token by calling the authentication service
            var reply = await _channelClient.GetAuthenticationAsync(request);

            // if no token is generated, it is an invalid credentials
            if (reply.Token == "")
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Unauthorized(new { Message = "Invalid Credentials" });
            };

            // create the cookie
            var cookieFragment = CookieHelper.BuildCookie(reply.Token);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, cookieFragment.ClaimsPrincipal, cookieFragment.AuthenticationProperties);

            return Ok(new { Message = "Logged in successfully", reply.Token });
        }

        [Authorize]
        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            // simply call the SightOutAsync method in the HttpContext object to sign out user.
            // this clear's the existing external cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { Message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("info")]
        public async Task<IActionResult> GetCookieInfo()
        {
            // grab the claims identity
            var tokenClaim = User.Claims.ToList().FirstOrDefault(e => e.Type == "token");

            if(tokenClaim == null) { return NotFound(new { Message = "No information found based on session" }); }

            string token = tokenClaim.Value;

            var request = new ValidationRequest()
            {
                Token = token
            };

            // get the AuthStore based on token
            var authStore = await _channelClient.ValidateAuthenticationAsync(request);

            if (authStore.AuthStore == "") return NotFound(new { Message = "No information found based on the token provided" });

            var info = JsonConvert.DeserializeObject<AuthStore>(authStore.AuthStore);

            return Ok(new { Message = "Info", info });
        }

        [Authorize]
        [HttpPost]
        [Route("set_admin/{id}")]
        public IActionResult SetAdmin(int id)
        {
            return Ok();
        }
    }
}
