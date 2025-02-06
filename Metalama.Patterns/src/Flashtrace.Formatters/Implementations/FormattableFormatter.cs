// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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