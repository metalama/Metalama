// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Programmatic_CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod_Parameters";
                    introduced.AddParameter( "x", typeof(int) );
                    introduced.AddParameter( "y", typeof(int) );
                } );

            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod_ReturnType";
                    introduced.ReturnType = TypeFactory.GetType( typeof(int) );
                } );

            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod_Accessibility";
                    introduced.Accessibility = Accessibility.Private;
                } );

            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod_IsStatic";
                    introduced.IsStatic = true;
                } );

            builder.IntroduceMethod(
                nameof(Template),
                buildMethod: introduced =>
                {
                    introduced.Name = "IntroducedMethod_IsVirtual";
                    introduced.IsVirtual = true;
                } );

            // TODO: Other members.
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }
}