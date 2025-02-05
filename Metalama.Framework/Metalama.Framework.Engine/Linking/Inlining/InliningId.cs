// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Globalization;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal readonly record struct InliningId( int Value )
{
    public override string ToString() => this.Value.ToString( CultureInfo.InvariantCulture );
}