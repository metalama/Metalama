// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.TestApp
{
    public class IntroduceSomeMethodAspect : Attribute, IAspect<INamedType>
    {
        private readonly string[] _methodNames;

        public IntroduceSomeMethodAspect(params string[] methodNames)
        {
            this._methodNames = methodNames;
        }

        public void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            foreach ( var methodName in this._methodNames )
            {
                aspectBuilder.Advice.IntroduceMethod( aspectBuilder.Target, nameof( SomeIntroducedMethod ), buildMethod: m => m.Name = methodName );
            }
        }

        [Template]
        public static void SomeIntroducedMethod()
        {
            Console.WriteLine( "From IntroduceSomeMethodAspect!" );

            var x = meta.Proceed();
        }

        [Introduce]
        public void SomeOtherIntroducedMethod()
        {
            
        }

        [Introduce]
        public void SomeOtherIntroducedMethod5()
        {

        }

        
    }
}
