// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.UserInterface;
using System.Threading.Tasks;

namespace Metalama.Backstage.Services;

internal sealed class RecaptchaService( IHttpClientFactory httpClientFactory, WebLinks webLinks )
{
    private readonly TaskCompletionSource<string?> _recaptchaSiteKeyTcs = new();

    public void Initialize()
        => Task.Run(
            async () =>
            {
                try
                {
                    using var httpClient = httpClientFactory.Create();
                    var recaptchaSiteKey = (await httpClient.GetStringAsync( webLinks.NewsletterGetCaptchaSiteKeyApi ))?.Trim();

                    // The site key comes from the backend server, which is outside our trust boundary. It is reflected
                    // into the setup page, so we validate its format before using it and treat a malformed key the same
                    // as a missing one (i.e. the device is considered offline and no reCAPTCHA is rendered). See #1649.
                    this._recaptchaSiteKeyTcs.SetResult( RecaptchaSiteKeyValidator.IsValid( recaptchaSiteKey ) ? recaptchaSiteKey : null );
                }
                catch
                {
                    this._recaptchaSiteKeyTcs.SetResult( null );
                }
            } );

    public Task<string?> GetRecaptchaSiteKeyAsync() => this._recaptchaSiteKeyTcs.Task;
}