// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// The default implementation of <see cref="IRetryPolicy"/>, implementing
/// an exponential back-off with jitter effect. It can be configured by setting
/// the properties.
/// </summary>
[PublicAPI]
public class RetryPolicy : IRetryPolicy
{
    private static readonly Random _random = new();

    private TimeSpan _baseDelay = TimeSpan.FromMilliseconds( 25 );
    private double _multiplier = 1.2;
    private TimeSpan _maxDelay = TimeSpan.FromSeconds( 2 );
    private double _jitterFactor = 0.2;
    private int _maxAttempts = 5;
    private int _noDelayAttempts = 1;

    /// <summary>
    /// Gets or sets the base delay between attempt.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public TimeSpan BaseDelay
    {
        get => this._baseDelay;
        set
        {
            if ( value < TimeSpan.Zero )
            {
                throw new ArgumentOutOfRangeException( nameof(value) );
            }

            this._baseDelay = value;
        }
    }

    /// <summary>
    /// Gets or sets the multiplier (under the exponent).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double Multiplier
    {
        get => this._multiplier;
        set
        {
            if ( value < 1.0 )
            {
                throw new ArgumentOutOfRangeException( nameof(value), "Multiplier must be >= 1.0." );
            }

            this._multiplier = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum delay.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public TimeSpan MaxDelay
    {
        get => this._maxDelay;
        set
        {
            if ( value < TimeSpan.Zero )
            {
                throw new ArgumentOutOfRangeException( nameof(value) );
            }

            this._maxDelay = value;
        }
    }

    /// <summary>
    /// Gets or sets the jitter factor, i.e. a randomness value between 0 and 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double JitterFactor
    {
        get => this._jitterFactor;
        set
        {
            if ( value is < 0 or > 1 )
            {
                throw new ArgumentOutOfRangeException( nameof(value), "Jitter must be in [0,1]." );
            }

            this._jitterFactor = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of attempts.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int MaxAttempts
    {
        get => this._maxAttempts;
        set
        {
            if ( value < 0 )
            {
                throw new ArgumentOutOfRangeException( nameof(value), "MaxAttempts must be greater than or equal to 0." );
            }

            this._maxAttempts = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of initial attempts (including the first attempt) that should not be delayed.
    /// Must be &gt;= 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int NoDelayAttempts
    {
        get => this._noDelayAttempts;
        set
        {
            if ( value < 1 )
            {
                throw new ArgumentOutOfRangeException( nameof(value), "NoDelayAttempts must be >= 1." );
            }

            this._noDelayAttempts = value;
        }
    }

    /// <summary>
    /// Gets or sets an array of exception types that can be retried. A <c>null</c> entry means that non-exception failures
    /// (such as transaction retries) will be retried. By default, all exception types and non-exceptions are retried.
    /// </summary>
    public Type?[] RetryableExceptionTypes { get; set; } = [null, typeof(Exception)];

    /// <inheritdoc />
    public virtual ValueTask<bool> TryAsync(
        OperationKind kind,
        int attempt,
        Exception? exception,
        ref object? state,
        CancellationToken cancellationToken )
        => this.ShouldRetryAsyncImpl( kind, attempt, exception, cancellationToken );

    private bool IsRetryable( Exception? exception )
    {
        if ( exception is AggregateException aggregateException )
        {
            return aggregateException.InnerExceptions.All( this.IsRetryable );
        }
        else
        {
            foreach ( var t in this.RetryableExceptionTypes )
            {
                if ( t == null )
                {
                    // A null entry means non-exception failures (null exception) should be retried
                    if ( exception == null )
                    {
                        return true;
                    }
                }
                else if ( t.IsInstanceOfType( exception ) )
                {
                    return true;
                }
            }

            return false;
        }
    }

    private async ValueTask<bool> ShouldRetryAsyncImpl( OperationKind kind, int attempt, Exception? exception, CancellationToken cancellationToken )
    {
        if ( attempt == 0 )
        {
            return true;
        }

        if ( attempt >= this._maxAttempts )
        {
            throw new CachingException( $"The {kind} operation has been attempted too many times.", exception! );
        }

        if ( !this.IsRetryable( exception ) )
        {
            throw new CachingException( $"The {kind} operation failed with an exception that cannot be retried.", exception! );
        }

        if ( attempt >= this._noDelayAttempts )
        {
            // Map the logical attempt index to the delay attempt index so that the first delayed retry uses multiplier^0.
            var delayAttempt = attempt - (this._noDelayAttempts - 1);
            var delay = this.GetDelay( delayAttempt );
            await Task.Delay( delay, cancellationToken );
        }

        return true;
    }

    private TimeSpan GetDelay( int attempt )
    {
        // attempt is one-based for delay calculation where 1 corresponds to baseDelay.
        var exp = attempt - 1;
        var factor = exp <= 0 ? 1.0 : Math.Pow( this.Multiplier, exp );
        var millis = this.BaseDelay.TotalMilliseconds * factor;
        var cappedMillis = Math.Min( millis, this.MaxDelay.TotalMilliseconds );

        if ( this.JitterFactor > 0 )
        {
            lock ( _random )
            {
                var jitterRange = cappedMillis * this.JitterFactor;
                var delta = ((_random.NextDouble() * 2) - 1) * jitterRange;
                cappedMillis = Math.Max( 0, cappedMillis + delta );
            }
        }

        return TimeSpan.FromMilliseconds( cappedMillis );
    }
}