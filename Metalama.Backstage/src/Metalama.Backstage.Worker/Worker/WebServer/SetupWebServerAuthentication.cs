// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.UserInterface;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Metalama.Backstage.Worker.WebServer;

/// <summary>
/// Middleware requiring the per-session token of <see cref="SetupWebServerToken"/> on every request of the local
/// setup web server.
/// </summary>
/// <remarks>
/// The server only binds to the loopback interface, but loopback is reachable by every local user account on the
/// machine, so binding alone does not stop another local user from registering a license key, changing the telemetry
/// consent, or reading and uploading an exception report belonging to the user who started the server. Requiring the
/// token closes that gap. It is required on <c>ping</c> too, otherwise a local peer could keep the server alive
/// indefinitely. See https://github.com/metalama/Metalama/issues/1769.
/// </remarks>
internal static class SetupWebServerAuthentication
{
    /// <summary>
    /// The name of the session cookie in which the token is stored after it has been presented once.
    /// </summary>
    /// <remarks>
    /// The token arrives in the query string of the first URL opened in the browser, but the pages link to each other
    /// and post forms, and none of those requests would carry the query string. The cookie is what carries the token
    /// through the rest of the session.
    /// </remarks>
    private const string _cookieName = "metalama-setup-token";

    /// <summary>
    /// Adds the authentication middleware to <paramref name="app"/>. It must be added before any middleware that acts
    /// on the request, so that an unauthenticated request reaches nothing at all.
    /// </summary>
    /// <param name="expectedToken">The token the server accepts, as produced by <see cref="SetupWebServerToken.GenerateToken"/>.</param>
    public static void UseSetupWebServerAuthentication( this WebApplication app, string expectedToken )
    {
        var expectedTokenBytes = Encoding.UTF8.GetBytes( expectedToken );

        app.Use(
            async ( context, next ) =>
            {
                if ( IsTokenValid( context.Request.Cookies[_cookieName], expectedTokenBytes ) )
                {
                    await next( context );

                    return;
                }

                if ( IsTokenValid( context.Request.Query[SetupWebServerToken.QueryParameterName], expectedTokenBytes ) )
                {
                    context.Response.Cookies.Append(
                        _cookieName,
                        expectedToken,
                        new CookieOptions()
                        {
                            HttpOnly = true,

                            // The setup server is plain HTTP on loopback, so the cookie cannot be marked Secure.
                            Secure = false,
                            SameSite = SameSiteMode.Strict,
                            Path = "/",

                            // The cookie is required for the application to work at all, so it is not subject to consent.
                            IsEssential = true
                        } );

                    if ( HttpMethods.IsGet( context.Request.Method ) && AcceptsHtml( context.Request ) )
                    {
                        // Redirect to the same page without the token, so that the token does not linger in the address
                        // bar, in the browsing history, or in the 'Referer' header of outgoing links.
                        context.Response.Redirect( context.Request.PathBase + context.Request.Path + RemoveToken( context.Request.Query ) );

                        return;
                    }

                    await next( context );

                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            } );
    }

    /// <summary>
    /// Determines whether the request is a browser navigation, as opposed to a programmatic call such as the readiness
    /// probe of <c>UserInterfaceService</c>. Only a navigation is worth redirecting, because only a navigation leaves
    /// the URL in an address bar and in the browsing history.
    /// </summary>
    private static bool AcceptsHtml( HttpRequest request )
    {
        foreach ( var accept in request.Headers.Accept )
        {
            if ( accept != null && accept.Contains( "text/html", StringComparison.OrdinalIgnoreCase ) )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rebuilds the query string of <paramref name="query"/> without the token parameter.
    /// </summary>
    private static QueryString RemoveToken( IQueryCollection query )
    {
        var result = QueryString.Empty;

        foreach ( var parameter in query )
        {
            // The lookup of a query parameter is case-insensitive, so a token presented under a different casing
            // authenticates the request and must therefore be stripped from the URL as well.
            if ( string.Equals( parameter.Key, SetupWebServerToken.QueryParameterName, StringComparison.OrdinalIgnoreCase ) )
            {
                continue;
            }

            foreach ( var value in parameter.Value )
            {
                result = result.Add( parameter.Key, value ?? string.Empty );
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether <paramref name="actualToken"/> matches the expected token, comparing in constant time so
    /// that the comparison cannot be turned into an oracle.
    /// </summary>
    private static bool IsTokenValid( string? actualToken, byte[] expectedTokenBytes )
    {
        if ( string.IsNullOrEmpty( actualToken ) )
        {
            return false;
        }

        // FixedTimeEquals returns false for buffers of different lengths. The token length is fixed and public, so
        // leaking it is not a concern.
        return CryptographicOperations.FixedTimeEquals( Encoding.UTF8.GetBytes( actualToken ), expectedTokenBytes );
    }
}
