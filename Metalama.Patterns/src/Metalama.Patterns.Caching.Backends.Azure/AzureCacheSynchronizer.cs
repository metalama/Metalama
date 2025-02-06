// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Flashtrace.Messages;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using System.Text;

namespace Metalama.Patterns.Caching.Backends.Azure
{
    /// <summary>
    /// An implementation of <see cref="CacheSynchronizer"/> based on Microsoft Azure Service Bus, using the <c>Azure.Messaging.ServiceBus</c> API
    /// meant for .NET Standard.
    /// </summary>
    [PublicAPI]
    internal sealed class AzureCacheSynchronizer : CacheSynchronizer
    {
        private const string _subject = "Metalama.Patterns.Caching.Backends.Azure.Invalidation";

        private readonly AzureCacheSynchronizerConfiguration _configuration;
        private readonly CancellationTokenSource _receiverCancellation = new();

        private string _subscriptionName = null!;
        private ServiceBusReceiver? _receiver;
        private ServiceBusSender? _sender;
        private int _backgroundTaskExceptions;

        public AzureCacheSynchronizer( CachingBackend underlyingBackend, AzureCacheSynchronizerConfiguration configuration ) : base(
            underlyingBackend,
            configuration )
        {
            this._configuration = configuration;
        }

        public override int BackgroundTaskExceptions => base.BackgroundTaskExceptions + this._backgroundTaskExceptions;

        public event EventHandler<AzureCacheSynchronizerExceptionEventArgs>? ReceiverException;

        protected override void InitializeCore()
        {
            Task.Run( () => this.InitializeCoreAsync() ).Wait();
        }

        protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
        {
            if ( this._configuration.SubscriptionName == null )
            {
                this._subscriptionName = await this.CreateSubscriptionAsync( this._configuration, cancellationToken );
            }
            else
            {
                this._subscriptionName = this._configuration.SubscriptionName;
            }

            var client = new ServiceBusClient( this._configuration.ConnectionString, this._configuration.ClientOptions );

            this._receiver = client.CreateReceiver( this._configuration.TopicName, this._subscriptionName );
            this._sender = client.CreateSender( this._configuration.TopicName );

            // Start a background task to process received messages.
            _ = Task.Run( this.ReceiveMessagesAsync, this._receiverCancellation.Token );
        }

        private async Task ReceiveMessagesAsync()
        {
            while ( true )
            {
                try
                {
                    await foreach ( var message in this._receiver!.ReceiveMessagesAsync( this._receiverCancellation.Token ) )
                    {
                        this._receiverCancellation.Token.ThrowIfCancellationRequested();

                        try
                        {
                            this.OnMessageReceived( message.Body.ToString() );
                        }
                        catch ( OperationCanceledException )
                        {
                            this.LogSource.Debug.Write( FormattedMessageBuilder.Formatted( "Cancellation received. Exiting." ) );

                            return;
                        }
                        catch ( Exception e )
                        {
                            this.LogSource.Error.Write( FormattedMessageBuilder.Formatted( "Exception while processing Azure Service Bus message." ), e );
                            this._backgroundTaskExceptions++;
                        }
                    }
                }
                catch ( OperationCanceledException )
                {
                    this.LogSource.Debug.Write( FormattedMessageBuilder.Formatted( "Cancellation received. Exiting." ) );

                    return;
                }
                catch ( Exception e )
                {
                    this.ReceiverException?.Invoke( this, new AzureCacheSynchronizerExceptionEventArgs( e ) );
                    await Task.Delay( ((AzureCacheSynchronizerConfiguration) this.Configuration).RetryOnReceiveError );
                }
            }

            // ReSharper disable once FunctionNeverReturns
        }

        /// <inheritdoc />
        protected override Task SendMessageAsync( string message, CancellationToken cancellationToken )
        {
            var azureMessage = new ServiceBusMessage( Encoding.UTF8.GetBytes( message ) )
            {
                ContentType = "text/plain", Subject = _subject, TimeToLive = TimeSpan.FromMinutes( 5 )
            };

            return this._sender!.SendMessageAsync( azureMessage, cancellationToken );
        }

        /// <inheritdoc />
        protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
        {
            base.DisposeCore( disposing, cancellationToken );

            this._receiverCancellation.Cancel();

            if ( this._receiver is { IsClosed: false } || this._sender is { IsClosed: false } )
            {
                Task.Run(
                        async () =>
                        {
                            if ( this._receiver is { IsClosed: false } )
                            {
                                await this._receiver.CloseAsync( cancellationToken );
                            }

                            if ( this._sender is { IsClosed: false } )
                            {
                                await this._sender.CloseAsync( cancellationToken );
                            }
                        },
                        cancellationToken )
                    .Wait( cancellationToken );
            }

            if ( disposing )
            {
                GC.SuppressFinalize( this );
            }
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
        {
            await base.DisposeAsyncCore( cancellationToken );

            this._receiverCancellation.Cancel();

            if ( this._receiver != null )
            {
                await this._receiver.CloseAsync( cancellationToken );
            }

            if ( this._sender != null )
            {
                await this._sender.CloseAsync( cancellationToken );
            }

            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AzureCacheSynchronizer"/> class.
        /// </summary>
        ~AzureCacheSynchronizer()
        {
            this.DisposeCore( false, default );
        }

        private async Task<string> CreateSubscriptionAsync( AzureCacheSynchronizerConfiguration configuration, CancellationToken cancellationToken )
        {
            try
            {
                var subscriptionId = Guid.NewGuid().ToString();

                var client = new ServiceBusAdministrationClient( configuration.ConnectionString, configuration.AdministrationClientOptions );

                var subscriptionOptions =
                    new CreateSubscriptionOptions( configuration.TopicName, subscriptionId )
                    {
                        MaxDeliveryCount = configuration.MaxDeliveryCount, AutoDeleteOnIdle = configuration.AutoDeleteOnIdle
                    };

                await client.CreateSubscriptionAsync( subscriptionOptions, cancellationToken );

                return subscriptionId;
            }
            catch ( Exception e )
            {
                this.LogSource.Error.Write( FormattedMessageBuilder.Formatted( "Exception while processing Azure Service Bus subscription." ), e );

                throw;
            }
        }
    }
}