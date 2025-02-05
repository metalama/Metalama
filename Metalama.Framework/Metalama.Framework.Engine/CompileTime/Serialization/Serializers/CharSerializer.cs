// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers
{
    internal sealed class CharSerializer : IntrinsicSerializer<char>
    {
        public override object Convert( object value, Type targetType )
        {
            return System.Convert.ToChar( value, CultureInfo.InvariantCulture );
        }
    }
}