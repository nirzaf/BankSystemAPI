namespace BankSystem.Web.Infrastructure.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate next;
        private readonly SecurityHeadersPolicy policy;

        public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersPolicy policy)
        {
            this.next = next;
            this.policy = policy;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            IHeaderDictionary headers = context.Response.Headers;

            foreach ((string key, string value) in policy.HeadersToSet)
            {
                headers[key] = value;
            }

            foreach (var header in policy.HeadersToRemove)
            {
                headers.Remove(header);
            }

            await next(context);
        }
    }
}