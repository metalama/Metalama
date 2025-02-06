// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit.Abstractions;

namespace Flashtrace.Formatters.UnitTests;

public abstract class FormattersTestsBase
{
#pragma warning disable SA1401
#pragma warning disable IDE1006
    protected readonly ITestOutputHelper? _logger;
#pragma warning restore IDE1006
#pragma warning restore SA1401

    protected FormattersTestsBase( ITestOutputHelper? logger )
    {
        this._logger = logger;
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    protected bool EnableLogging { get; set; }

    /// <summary>
    /// Gets a new instance of <see cref="FormatterRepository"/>.
    /// </summary>
    protected static FormatterRepository CreateRepository( Action<FormatterRepository.Builder>? build = null, bool includeDefaultFormatters = true )
        => FormatterRepository.Create(
            TestRole.Instance,
            builder =>
            {
                if ( !includeDefaultFormatters )
                {
                    builder.Clear();
                }

                build?.Invoke( builder );
            } );

    protected string? FormatDefault<T>( T? value )
    {
        var formatters = CreateRepository( includeDefaultFormatters: true );

        return this.Format( formatters, value );
    }

    /// <summary>
    /// Formats a value using the specified <see cref="IFormatterRepository"/>.
    /// </summary>
    protected string? Format<T>( IFormatterRepository formatterRepository, T? value )
    {
        var stringBuilder = new UnsafeStringBuilder( 1024 );
        formatterRepository.Get<T>().Format( stringBuilder, value );
        var result = stringBuilder.ToString();

        if ( this.EnableLogging )
        {
            this._logger?.WriteLine( "'" + value?.ToString() + "' => '" + result + "'" );
        }

        return result;
    }
}