// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;

namespace Flashtrace.Formatters.Implementations;

internal class FormatterConverter<TTarget> : Formatter<TTarget>
{
    private readonly IFormatter _wrapped;
    private readonly ConcurrentDictionary<IFormatter, FormatterConverter<TTarget>> _cache = new();

    // ReSharper disable once MemberCanBeProtected.Global
    public FormatterConverter( IFormatter wrapped, IFormatterRepository repository )
        : base( repository )
    {
        this._wrapped = wrapped;
    }

    // ReSharper disable once UnusedMember.Global
    public IFormatter<TTarget>? Convert( IFormatter? formatter )
        => formatter == null
            ? null
            : formatter as IFormatter<TTarget> ?? this._cache.GetOrAdd( formatter, f => new FormatterConverter<TTarget>( f, this.Repository ) );

    public override void Format( UnsafeStringBuilder stringBuilder, TTarget? value )
    {
        this._wrapped.Format( stringBuilder, value );
    }

    public override FormatterAttributes Attributes => this._wrapped.Attributes | FormatterAttributes.Converter;
}