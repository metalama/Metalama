// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Metalama.AspectWorkbench.ViewModels
{
    internal sealed class SyntaxColorizer : FormattedCodeWriter
    {
        private static readonly Dictionary<string, Color> _classificationToColor = new()
        {
            { ClassificationTypeNames.Comment, Colors.Green },
            { ClassificationTypeNames.ExcludedCode, Colors.Black },
            { ClassificationTypeNames.Identifier, Colors.Black },
            { ClassificationTypeNames.Keyword, Colors.Blue },
            { ClassificationTypeNames.ControlKeyword, Colors.Blue },
            { ClassificationTypeNames.NumericLiteral, Colors.Black },
            { ClassificationTypeNames.Operator, Colors.Black },
            { ClassificationTypeNames.OperatorOverloaded, Colors.Black },
            { ClassificationTypeNames.PreprocessorKeyword, Colors.Black },
            { ClassificationTypeNames.StringLiteral, Colors.Brown },
            { ClassificationTypeNames.WhiteSpace, Colors.Black },
            { ClassificationTypeNames.Text, Colors.Black },
            { ClassificationTypeNames.StaticSymbol, Colors.Black },
            { ClassificationTypeNames.PreprocessorText, Colors.Gray },
            { ClassificationTypeNames.Punctuation, Colors.Black },
            { ClassificationTypeNames.VerbatimStringLiteral, Colors.Blue },
            { ClassificationTypeNames.StringEscapeCharacter, Colors.LightSeaGreen },
            { ClassificationTypeNames.ClassName, Colors.Teal },
            { ClassificationTypeNames.DelegateName, Colors.Teal },
            { ClassificationTypeNames.EnumName, Colors.Teal },
            { ClassificationTypeNames.InterfaceName, Colors.Teal },
            { ClassificationTypeNames.ModuleName, Colors.Black },
            { ClassificationTypeNames.StructName, Colors.Teal },
            { ClassificationTypeNames.TypeParameterName, Colors.Black },
            { ClassificationTypeNames.FieldName, Colors.Teal },
            { ClassificationTypeNames.EnumMemberName, Colors.Black },
            { ClassificationTypeNames.ConstantName, Colors.Black },
            { ClassificationTypeNames.LocalName, Colors.Black },
            { ClassificationTypeNames.ParameterName, Colors.Brown },
            { ClassificationTypeNames.MethodName, Colors.Teal },
            { ClassificationTypeNames.ExtensionMethodName, Colors.Teal },
            { ClassificationTypeNames.PropertyName, Colors.Black },
            { ClassificationTypeNames.EventName, Colors.Black },
            { ClassificationTypeNames.NamespaceName, Colors.Black },
            { ClassificationTypeNames.LabelName, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentAttributeName, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentAttributeQuotes, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentAttributeValue, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentCDataSection, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentComment, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentDelimiter, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentEntityReference, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentName, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentProcessingInstruction, Colors.Black },
            { ClassificationTypeNames.XmlDocCommentText, Colors.Black },
            { ClassificationTypeNames.XmlLiteralAttributeName, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralAttributeQuotes, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralAttributeValue, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralCDataSection, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralComment, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralDelimiter, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralEmbeddedExpression, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralEntityReference, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralName, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralProcessingInstruction, Colors.Gray },
            { ClassificationTypeNames.XmlLiteralText, Colors.Gray },
            { ClassificationTypeNames.RegexComment, Colors.Indigo },
            { ClassificationTypeNames.RegexCharacterClass, Colors.Indigo },
            { ClassificationTypeNames.RegexAnchor, Colors.Indigo },
            { ClassificationTypeNames.RegexQuantifier, Colors.Indigo },
            { ClassificationTypeNames.RegexGrouping, Colors.Indigo },
            { ClassificationTypeNames.RegexAlternation, Colors.Indigo },
            { ClassificationTypeNames.RegexText, Colors.Indigo },
            { ClassificationTypeNames.RegexSelfEscapedCharacter, Colors.Indigo },
            { ClassificationTypeNames.RegexOtherEscape, Colors.Indigo }
        };

        public async Task<FlowDocument> WriteSyntaxColoringAsync(
            Document document,
            bool areNodesAnnotated = false,
            IEnumerable<Diagnostic>? diagnostics = null )
        {
            static Color WithAlpha( Color brush, double alpha )
            {
                return Color.FromArgb( (byte) (255 * alpha), brush.R, brush.G, brush.B );
            }

            var classified = await this.GetClassifiedTextSpansAsync( document, areNodesAnnotated, diagnostics );
            var sourceText = await document.GetTextAsync();

            var paragraph = new Paragraph();

            foreach ( var span in classified )
            {
                if ( sourceText.Length < span.Span.End )
                {
                    // This must be due to a bug upstream. Ignore it.
                    continue;
                }

                Color foreground = Colors.Black, background;
                var fontWeight = FontWeights.Normal;
                var fontStyle = FontStyles.Normal;
                TextDecorationCollection? textDecoration = null;

                var category = span.Classification;

                // Choose foreground.
                switch ( category )
                {
                    case TextSpanClassification.CompileTimeVariable:
                        fontStyle = FontStyles.Italic;

                        break;

                    case TextSpanClassification.TemplateKeyword:
                        foreground = Colors.Fuchsia;
                        fontWeight = FontWeights.Heavy;

                        break;

                    case TextSpanClassification.Dynamic:
                        textDecoration = TextDecorations.Underline;

                        break;

                    default:
                        if ( span.Tags.TryGetValue( CSharpClassTagName, out var csharpClasses ) )
                        {
                            foreach ( var csClass in csharpClasses.Split( ';' ) )
                            {
                                if ( _classificationToColor.TryGetValue( csClass, out var color ) )
                                {
                                    foreground = color;

                                    break;
                                }
                            }
                        }

                        fontWeight = FontWeights.Normal;

                        break;
                }

                // Choose background.
                switch ( category )
                {
                    case TextSpanClassification.TemplateKeyword:
                    case TextSpanClassification.CompileTimeVariable:
                    case TextSpanClassification.CompileTime:
                    case TextSpanClassification.Dynamic:
                        background = WithAlpha( Colors.SlateGray, 0.2 );

                        break;

                    default:
                        background = Colors.White;

                        break;
                }

                var run = new Run( sourceText.GetSubText( span.Span ).ToString() )
                {
                    Foreground = new SolidColorBrush( foreground ),
                    Background = new SolidColorBrush( background ),
                    FontWeight = fontWeight,
                    FontStyle = fontStyle
                };

                if ( textDecoration != null )
                {
                    run.TextDecorations.Add( textDecoration );
                }

                // Process diagnostic.
                if ( span.Tags.TryGetValue( DiagnosticTagName, out var serializedDiagnostic ) )
                {
                    if ( !string.IsNullOrWhiteSpace( run.ToolTip as string ) )
                    {
                        run.ToolTip += Environment.NewLine;
                    }
                    else
                    {
                        run.ToolTip = "";
                    }

                    var diagnosticAnnotation = DiagnosticAnnotation.FromJson( serializedDiagnostic );
                    run.ToolTip += diagnosticAnnotation.ToString();

                    run.TextDecorations.Add(
                        new TextDecoration(
                            TextDecorationLocation.Underline,
                            new Pen( diagnosticAnnotation.Severity == DiagnosticSeverity.Error ? Brushes.Red : Brushes.Orange, 1 ),
                            3,
                            TextDecorationUnit.Pixel,
                            TextDecorationUnit.Pixel ) );
                }

                ToolTipService.SetShowOnDisabled( run, true );
                paragraph.Inlines.Add( run );
            }

            return new FlowDocument( paragraph ) { PageWidth = 2000 };

            // This is how to enable wrapping on the document:
            // richTextBox.Document.SetBinding( FlowDocument.PageWidthProperty,
            //    new Binding( "ActualWidth" ) { Source = richTextBox } );
        }

        public SyntaxColorizer( in ProjectServiceProvider serviceProvider ) : base( serviceProvider ) { }
    }
}