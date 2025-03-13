// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.UserInterface;
using System.Threading.Tasks;

namespace Metalama.Backstage.Services;

internal class RecaptchaService( IHttpClientFactory httpClientFactory, WebLinks webLinks )
{
    private readonly TaskCompletionSource<string?> _recaptchaSiteKeyTcs = new();
    
    public void Initialize()
        => Task.Run(
            async () =>
            {
                try
                {
                    using var httpClient = httpClientFactory.Create();
                    var recaptchaSiteKey = await httpClient.GetStringAsync( webLinks.NewsletterGetCaptchaSiteKeyApi );
                    this._recaptchaSiteKeyTcs.SetResult( recaptchaSiteKey );
                }
                catch
                {
                    this._recaptchaSiteKeyTcs.SetResult( null );
                }
            } );

    public Task<string?> GetRecaptchaSiteKeyAsync() => this._recaptchaSiteKeyTcs.Task;

    public async Task<bool> IsDeviceOnlineAsync() => await this._recaptchaSiteKeyTcs.Task != null;
}