// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Backends.Azure;

/// <summary>
/// Options that determine the mode of operation of an <see cref="AzureCacheSynchronizer"/> instance.
/// </summary>
[PublicAPI]
public sealed record AzureCacheSynchronizerConfiguration : CacheSynchronizerConfiguration
{
    public string TopicName { get; init; }

    public string ConnectionString { get; }

    public ServiceBusClientOptions ClientOptions { get; init; } = new();

    public TimeSpan RetryOnReceiveError { get; init; } = TimeSpan.FromSeconds( 5 );

    /// <summary>
    /// Gets the topic subscription name. Not to be confused with the Azure subscription id. If this property is not supplied,
    /// a new auto-deleted subscription is created every time the component is instantiated. 
    /// </summary>
    public string? SubscriptionName { get; init; }

    public ServiceBusAdministrationClientOptions AdministrationClientOptions { get; init; } = new();

    public TimeSpan AutoDeleteOnIdle { get; init; } = TimeSpan.FromMinutes( 5 );

    public int MaxDeliveryCount { get; init; } = 10;

    public AzureCacheSynchronizerConfiguration( string connectionString, string? topicName = null )
    {
        this.ConnectionString = connectionString;

        if ( topicName == null )
        {
            var parsedConnectionString = ParseAzureConnectionString( connectionString );

            if ( !parsedConnectionString.TryGetValue( "EntityPath", out topicName ) )
            {
                throw new ArgumentNullException(
                    nameof(topicName),
                    "The 'topicName' parameter must be supplied because the connection string does not contain it." );
            }
        }

        this.TopicName = topicName;
    }

    private static Dictionary<string, string> ParseAzureConnectionString( string connectionString )
    {
        var keyValuePairs = connectionString.Split( [';'], StringSplitOptions.RemoveEmptyEntries );
        var result = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

        foreach ( var keyValuePair in keyValuePairs )
        {
            var parts = keyValuePair.Split( ['='], 2 ); // We split only on the first '='

            if ( parts.Length == 2 )
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                result[key] = value;
            }
        }

        return result;
    }
}