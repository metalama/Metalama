// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

#pragma warning disable CA1822

public class WebLinks : IBackstageService
{
    // We don't add campaign tracking query string parameters so we do not override the attribution to the original campaign.
    public string AfterSetup => GetLink( "metalama-after-activation", false );

    public string GetTeamTeamTrial => GetLink( "metalama-team-evaluation" );

    public string VisualStudioMarketplace => GetLink( "metalama-download-vsx" );

    public string PrivacyPolicy => GetLink( "metalama-privacy-policy" );

    public string LicenseAgreement => GetLink( "metalama-license-agreement" );

    public string Documentation => GetLink( "metalama-documentation" );

    public string InstallVsx => this.VisualStudioMarketplace;

    public string RenewSubscription => GetLink( "metalama-renew-subscription" );
    
    public string DotNetTool => GetLink( "metalama-dotnet_tool" );

    public string NewsletterGetCaptchaSiteKeyApi => "https://licensing.postsharp.net/GetCaptchaSiteKey.ashx";

    public string NewsletterSubscribeApi => "https://licensing.postsharp.net/MetalamaNewsletter.ashx";

    private static string GetLink( string alias, bool trackCampaign = true, string? queryString = null )
    {
        var url = $"https://www.postsharp.net/links/{alias}";
        var queryStringSeparator = '?';

        if ( trackCampaign )
        {
            url += "?utm_source=app&utm_medium=app&utm_campaign=backstage";
            queryStringSeparator = '&';
        }

        if ( !string.IsNullOrEmpty( queryString ) )
        {
            url += queryStringSeparator + queryString;
        }

        return url;
    }
}