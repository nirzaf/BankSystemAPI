namespace BankSystem.Web.Infrastructure.Middleware
{
    using System;

    public class SecurityHeadersBuilder
    {
        private readonly SecurityHeadersPolicy policy = new SecurityHeadersPolicy();

        public SecurityHeadersBuilder AddDefaultSecurePolicy()
        {
            AddFrameOptionsSameOrigin();
            AddFeature();
            AddReferrer();
            AddXssProtection();
            AddContentTypeOptionsNoSniff();

            return this;
        }

        public void AddFrameOptionsSameOrigin()
            => AddCustomHeader(MiddlewareConstants.FrameOptions.Header, MiddlewareConstants.FrameOptions.SameOrigin);

        public void AddFeature()
            => AddCustomHeader(MiddlewareConstants.FeaturePolicy.Header, MiddlewareConstants.FeaturePolicy.Ignore);

        public void AddReferrer()
            => AddCustomHeader(MiddlewareConstants.ReferrerPolicy.Header, MiddlewareConstants.ReferrerPolicy.NoReferrer);

        public void AddXssProtection()
            => AddCustomHeader(MiddlewareConstants.XssProtection.Header, MiddlewareConstants.XssProtection.Block);

        public void AddContentTypeOptionsNoSniff()
            => AddCustomHeader(MiddlewareConstants.ContentTypeOptions.Header, MiddlewareConstants.ContentTypeOptions.NoSniff);

        public SecurityHeadersBuilder AddCustomHeader(string header, string value)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentNullException(nameof(header));
            }

            policy.HeadersToSet[header] = value;

            return this;
        }

        public SecurityHeadersBuilder RemoveHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentNullException(nameof(header));
            }

            policy.HeadersToRemove.Add(header);

            return this;
        }

        public SecurityHeadersPolicy Policy() => policy;
    }
}