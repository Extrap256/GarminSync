using Client.Infrastructure.Security.Interfaces;

namespace Client.Infrastructure.API
{
    public class AuthHeaderHandler (ITokenStorage tokenStorage) : DelegatingHandler
    {
        private readonly ITokenStorage _tokenStorage = tokenStorage;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = _tokenStorage.GetToken();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, ct);
        }
    }
}
