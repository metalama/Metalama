// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters.TypeExtensions;

namespace Flashtrace.Formatters.Implementations;

internal sealed class FormattableFormatter<[BindToRoleType] TRole, [BindToExtendedType] TValue> : Formatter<TValue>
    where TRole : FormattingRole
    where TValue : IFormattable<TRole>
{
    public FormattableFormatter( IFormatterRepository repository ) : base( repository ) { }

    public override void Format( UnsafeStringBuilder stringBuilder, TValue? value )
    {
        // ReSharper disable once CompareNonConstrainedGenericWithNull
        if ( value == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );
        }
        else
        {
            value.Format( stringBuilder, this.Repository );
        }
    }
}