// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CodeModel
{
    public sealed partial class CompilationModel
    {
        /// <summary>
        /// Discovers custom attributes in a syntax tree and index them by attribute name.
        /// </summary>
        private sealed class AttributeDiscoveryVisitor : SafeSyntaxWalker
        {
            private readonly CompilationModel _compilation;

            private readonly ImmutableDictionaryOfArray<IRef<INamedType>, AttributeRef>.Builder _builder =
                ImmutableDictionaryOfArray<IRef<INamedType>, AttributeRef>.CreateBuilder( RefEqualityComparer<INamedType>.Default );

            public AttributeDiscoveryVisitor( CompilationModel compilation )
            {
                this._compilation = compilation;
            }

            public override void VisitAttribute( AttributeSyntax node )
            {
                // We always need to resolve the constructor from the semantic model because the attribute name from
                // the syntax may not correspond to the class name because of `using xx = yy` directives.

                var semanticModel = this._compilation.CompilationContext.SemanticModelProvider.GetSemanticModel( node.SyntaxTree );
                var attributeConstructor = semanticModel.GetSymbolInfo( node ).Symbol;

                if ( attributeConstructor == null )
                {
                    return;
                }

                var attributeType = this._compilation.RefFactory.FromSymbol<INamedType>( attributeConstructor.ContainingType );

                // A local method that adds the attribute.
                void IndexAttribute( SyntaxNode parentDeclaration, RefTargetKind kind )
                {
                    void Add( SyntaxNode realDeclaration )
                    {
                        this._builder.Add( attributeType, new SyntaxAttributeRef( attributeType, node, realDeclaration, this._compilation.RefFactory, kind ) );
                    }

                    switch ( parentDeclaration )
                    {
                        case IncompleteMemberSyntax or (StatementSyntax and not LocalFunctionStatementSyntax):
                            // This happens at design time when we have an invalid syntax. Local functions are skipped to produce correct errors later.
                            break;

                        case BaseFieldDeclarationSyntax field:
                            {
                                // In case of fields and field-like events, add the attribute to all defined fields.

                                foreach ( var variable in field.Declaration.Variables )
                                {
                                    Add( variable );
                                }

                                break;
                            }

                        default:
                            Add( parentDeclaration );

                            break;
                    }
                }

                // Get the parent declaration. 
                var attributeList = (AttributeListSyntax) node.Parent.AssertNotNull();

                var declaration = attributeList.Parent.AssertNotNull();

                if ( attributeList.Target != null )
                {
                    var targetKind = attributeList.Target.Identifier.Kind();

                    switch ( targetKind )
                    {
                        case SyntaxKind.ModuleKeyword:
                            IndexAttribute( declaration, RefTargetKind.Module );

                            break;

                        case SyntaxKind.AssemblyKeyword:
                            IndexAttribute( declaration, RefTargetKind.Assembly );

                            break;

                        case SyntaxKind.FieldKeyword:
                            IndexAttribute( declaration, RefTargetKind.Field );

                            break;

                        case SyntaxKind.ReturnKeyword:
                            IndexAttribute( declaration, RefTargetKind.Return );

                            break;

                        case SyntaxKind.ParamKeyword:
                            IndexAttribute( declaration, RefTargetKind.Parameter );

                            break;

                        case SyntaxKind.MethodKeyword:
                            if ( declaration is BasePropertyDeclarationSyntax { AccessorList: { } } property )
                            {
                                foreach ( var accessor in property.AccessorList.Accessors )
                                {
                                    IndexAttribute( accessor, RefTargetKind.Default );
                                }
                            }

                            break;

                        case SyntaxKind.PropertyKeyword:
                            IndexAttribute( declaration, RefTargetKind.Property );

                            break;

                        case SyntaxKind.EventKeyword:
                            IndexAttribute( declaration, RefTargetKind.Event );

                            break;

                        case SyntaxKind.TypeKeyword:
                        case SyntaxKind.TypeVarKeyword:
                            // Using Default because we don't support types and generic parameter references at the moment.
                            IndexAttribute( declaration, RefTargetKind.Default );

                            break;

                        default:
                            throw new AssertionFailedException( $"Unexpected attribute target: '{targetKind}'." );
                    }
                }
                else
                {
                    IndexAttribute( declaration, RefTargetKind.Default );
                }

                base.VisitAttribute( node );
            }

            public ImmutableDictionaryOfArray<IRef<INamedType>, AttributeRef> GetDiscoveredAttributes() => this._builder.ToImmutable();

            public void Visit( SyntaxTree tree )
            {
                this.Visit( tree.GetRoot() );
            }
        }
    }
}