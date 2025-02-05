// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace ClassLibrary1
{
    [Inheritable]
    public class MyInheritedAspect : TypeAspect
    {
        private IReadOnlyList<int>? _list;

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach ( var m in builder.Target.Methods )
            {
                builder.Advice.Override( m, nameof( MethodTemplate ) );
            }

            this._list = new List<int>() { 1, 2, 3 };
        }

        [Template]
        private dynamic? MethodTemplate()
        {
            Console.WriteLine( "Aspect: " + string.Join( ", ", this._list ) );
            return meta.Proceed();
        }
    }

    [MyInheritedAspect]
    public interface IInterface
    {

    }
}
