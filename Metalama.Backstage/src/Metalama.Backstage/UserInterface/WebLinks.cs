// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

#pragma warning disable CA1822

// ReSharper disable MemberCanBeMadeStatic.Global
public sealed class WebLinks : IBackstageService
{
    // We don't add campaign tracking query string parameters so we do not override the attribution to the original campaign.
    public string Welcome => GetLink( "metalama-oss-welcome", false ); // TODO - Implement

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

    public string DisableTelemetryInstructions => GetLink( "metalama-disable-telemetry" ); // TODO - Implement

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