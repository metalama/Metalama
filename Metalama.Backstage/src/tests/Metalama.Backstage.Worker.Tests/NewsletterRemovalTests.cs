// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Pages;
using Metalama.Backstage.Telemetry.User;
using Metalama.Backstage.UserInterface;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Backstage.Worker.Tests;

// Regression tests for #1669. The newsletter/e-mail-course subscription feature, and all collection of the
// user's e-mail address, must be removed from the product. Because these tests assert the *absence* of API
// surface, they reference the removed members by name (reflection) so that the test code keeps compiling across
// the removal: they fail while the feature is still present and pass once it is gone.
public sealed class NewsletterRemovalTests
{
    // Member-name fragments that would betray a leftover newsletter / e-mail-subscription capability.
    private static readonly string[] _forbiddenFragments = { "Newsletter", "Subscribe", "Recaptcha", "Captcha" };

    private static string[] GetMemberNames( Type type )
        => type.GetMembers( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly )
            .Select( m => m.Name )
            .ToArray();

    [Fact]
    public void ConsentsPageOffersNoNewsletterOrEmailOption()
    {
        var offending = GetMemberNames( typeof( ConsentsPageModel ) )
            .Where( name => _forbiddenFragments.Any( f => name.Contains( f, StringComparison.OrdinalIgnoreCase ) )
                            || name.Contains( "Email", StringComparison.OrdinalIgnoreCase ) )
            .ToArray();

        Assert.Empty( offending );
    }

    [Fact]
    public void WebLinksExposeNoNewsletterLinks()
    {
        var offending = GetMemberNames( typeof( WebLinks ) )
            .Where( name => _forbiddenFragments.Any( f => name.Contains( f, StringComparison.OrdinalIgnoreCase ) ) )
            .ToArray();

        Assert.Empty( offending );
    }

    [Fact]
    public void UserInfoDoesNotStoreEmailAddress()
    {
        var offending = GetMemberNames( typeof( UserInfo ) )
            .Where( name => name.Contains( "Email", StringComparison.OrdinalIgnoreCase ) )
            .ToArray();

        Assert.Empty( offending );
    }

    [Fact]
    public void UserInfoServiceDoesNotCollectEmailAddress()
    {
        var offending = GetMemberNames( typeof( IUserInfoService ) )
            .Where( name => name.Contains( "Email", StringComparison.OrdinalIgnoreCase ) )
            .ToArray();

        Assert.Empty( offending );
    }

    [Fact]
    public void RecaptchaTypesAreRemoved()
    {
        // The reCAPTCHA service existed solely to protect the newsletter sign-up form. Once the newsletter is
        // gone there is no remaining caller, so the types (and their backend key-fetch round-trip) must be removed.
        var workerAssembly = typeof( ConsentsPageModel ).Assembly;

        Assert.Null( workerAssembly.GetType( "Metalama.Backstage.Services.RecaptchaService" ) );
        Assert.Null( workerAssembly.GetType( "Metalama.Backstage.Services.RecaptchaSiteKeyValidator" ) );
    }
}
