// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Advising
{
    internal static class TemplateMemberFactory
    {
        public static TemplateMember<T> Create<T>(
            T implementation,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IAdviceAttribute adviceAttribute,
            IObjectReader tags,
            TemplateKind selectedKind,
            TemplateKind interpretedKind )
            where T : class, IMemberOrNamedType
            => new(
                (ISymbolRef<T>) implementation.ToRef().As<T>(),
                templateClassMember,
                templateProvider,
                adviceAttribute,
                tags,
                selectedKind,
                interpretedKind );

        public static TemplateMember<T> Create<T>(
            T implementation,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IAdviceAttribute adviceAttribute,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new( (ISymbolRef<T>) implementation.ToRef().As<T>(), templateClassMember, templateProvider, adviceAttribute, tags, selectedKind );

        public static TemplateMember<T> Create<T>(
            ISymbol symbol,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IAdviceAttribute adviceAttribute,
            RefFactory refFactory,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new( refFactory.FromSymbol<T>( symbol ), templateClassMember, templateProvider, adviceAttribute, tags, selectedKind );

        public static TemplateMember<T> Create<T>(
            T implementation,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new(
                (ISymbolRef<T>) implementation.ToRef().As<T>(),
                templateClassMember,
                templateProvider,
                (ITemplateAttribute) templateClassMember.Attribute.AssertNotNull(),
                tags,
                selectedKind );

        public static TemplateMember<T> Create<T>(
            ISymbol symbol,
            TemplateClassMember templateClassMember,
            TemplateProvider templateProvider,
            RefFactory refFactory,
            IObjectReader tags,
            TemplateKind selectedKind = TemplateKind.Default )
            where T : class, IMemberOrNamedType
            => new(
                refFactory.FromSymbol<T>( symbol ),
                templateClassMember,
                templateProvider,
                (ITemplateAttribute) templateClassMember.Attribute.AssertNotNull(),
                tags,
                selectedKind );
    }
}