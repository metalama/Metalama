// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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