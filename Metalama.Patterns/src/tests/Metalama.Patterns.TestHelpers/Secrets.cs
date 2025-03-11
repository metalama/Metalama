// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Collections.Concurrent;

namespace Metalama.Patterns.TestHelpers;

public static class Secrets
{
    private static readonly SecretClient _client = new( new Uri( "https://testserviceskeyvault.vault.azure.net/" ), new DefaultAzureCredential() );
    private static readonly ConcurrentDictionary<string, string> _secrets = new();

    public static string Get( string name ) => _secrets.GetOrAdd( name, n => _client.GetSecret( n ).Value.Value );
}