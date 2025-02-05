// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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