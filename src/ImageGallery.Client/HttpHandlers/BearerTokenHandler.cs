using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ImageGallery.Client.HttpHandlers
{
    public class BearerTokenHandler: DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
             _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var accessToken = await GetAccessTokenAsync();// _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetBearerToken(accessToken);
            }

           

            return await base.SendAsync(request, cancellationToken);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var expiresAt = await _httpContextAccessor.HttpContext.GetTokenAsync("expires_at");

            var expiresAtDatetimeOffset = DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture);
            if ((expiresAtDatetimeOffset.AddSeconds(-60)).ToUniversalTime() > DateTime.UtcNow)
            {
                //no need to refresh. Return access token
                return await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            }

            var idpClient = _httpClientFactory.CreateClient("IDPClient");
            var discoveryResponse = await idpClient.GetDiscoveryDocumentAsync();

            var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            var refreshResponse = await idpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                ClientId = "imagegalleryclient",
                ClientSecret = "secret",
                RefreshToken = refreshToken
            });

            var updatedTokens = new List<AuthenticationToken>();
            updatedTokens.Add(new AuthenticationToken { 
                Name = OpenIdConnectParameterNames.IdToken,
                Value = refreshResponse.IdentityToken
            });
            updatedTokens.Add(new AuthenticationToken
            {
                Name = OpenIdConnectParameterNames.AccessToken,
                Value = refreshResponse.AccessToken
            });
            updatedTokens.Add(new AuthenticationToken
            {
                Name = OpenIdConnectParameterNames.RefreshToken,
                Value = refreshResponse.RefreshToken
            });
            updatedTokens.Add(new AuthenticationToken
            {
                Name = "expires_at",
                Value = (DateTime.UtcNow + TimeSpan.FromSeconds(refreshResponse.ExpiresIn))
                        .ToString("o", CultureInfo.InvariantCulture)
            });

            //get authentication result 
            var currentAuthenticationResult = await _httpContextAccessor
                .HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            currentAuthenticationResult.Properties.StoreTokens(updatedTokens);

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    currentAuthenticationResult.Principal,
                    currentAuthenticationResult.Properties);

            return refreshResponse.AccessToken;
        }
    }
}
