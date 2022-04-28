using DoNotRename.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DoNotRename
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotRenameAnalyzer : DiagnosticAnalyzer
    {
        public const string NOT_MATCHING_CLASS_NAME_RULE_ID = "DoNotRenameAnalyzer_NotMatchingClassName";
        public const string NOT_MATCHING_CLASS_NAME_WITH_REASON_RULE_ID = "DoNotRenameAnalyzer_NotMatchingClassNameWithReason";
        public const string NOT_STRING_LITERAL_ARGUMENT_VALUE_RULE_ID = "DoNotRenameAnalyzer_NotStringLiteralArgumentValue";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly LocalizableString NotMatchingClassNameMessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat_NotMatchingClassName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NotMatchingClassNameWithReasonMessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat_NotMatchingClassNameWithReason), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString NotStringLiteralArgumentValueMessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat_NotStringLiteralArgumentValue), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor NotMatchingClassNameRule = new DiagnosticDescriptor(NOT_MATCHING_CLASS_NAME_RULE_ID, Title, NotMatchingClassNameMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor NotMatchingClassNameWithReasonRule = new DiagnosticDescriptor(NOT_MATCHING_CLASS_NAME_WITH_REASON_RULE_ID, Title, NotMatchingClassNameWithReasonMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor NotStringLiteralArgumentRule = new DiagnosticDescriptor(NOT_STRING_LITERAL_ARGUMENT_VALUE_RULE_ID, Title, NotStringLiteralArgumentValueMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get => new DiagnosticDescriptor[] 
            {
                NotMatchingClassNameRule,
                NotMatchingClassNameWithReasonRule,
                NotStringLiteralArgumentRule,
            }.ToImmutableArray();
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            bool IsDoNotRenameClassAttribute(AttributeData attributeData)
            {
                var nameMathcing = attributeData.AttributeClass.Name == nameof(DoNotRenameClassAttribute);
                var namespaceMatching = attributeData.AttributeClass.ContainingNamespace.Name == typeof(DoNotRenameClassAttribute).Namespace;
                var assemblyMatching = attributeData.AttributeClass.ContainingAssembly.Name == typeof(DoNotRenameClassAttribute).Assembly.GetName().Name;

                return nameMathcing && namespaceMatching && assemblyMatching;
            };

            if (context.Symbol.IsDefinition)
            {
                var classDeclarationSyntax = context.Symbol.DeclaringSyntaxReferences
                    .First() // TODO: support partial classes?
                    .GetSyntax()
                    .FindNode(new Microsoft.CodeAnalysis.Text.TextSpan(context.Symbol.Locations[0].SourceSpan.Start, 0)) as ClassDeclarationSyntax;

                if (classDeclarationSyntax == null)
                {
                    return;
                }

                bool hasDoNotRenameClassAttribute = false;
                int attributeIndex = 0;
                var attributes = context.Symbol.GetAttributes();
                foreach (var attributeList in classDeclarationSyntax.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (IsDoNotRenameClassAttribute(attributes[attributeIndex++]))
                        {
                            hasDoNotRenameClassAttribute = true;
                            var firstArgumentSyntax = (attribute.ArgumentList.ChildNodes().First() as AttributeArgumentSyntax);
                            var kind = firstArgumentSyntax.Expression.Kind();
                            if (kind != SyntaxKind.StringLiteralExpression)
                            {
                                // Register error not string litteral
                                var diagnostic = Diagnostic.Create(NotStringLiteralArgumentRule, firstArgumentSyntax.GetLocation());
                                // TODO: Support partial classes? (Locations[0])
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }

                if (hasDoNotRenameClassAttribute)
                {
                    var className = context.Symbol.Name;

                    var doNotRenameAttribute = attributes
                        .Where(IsDoNotRenameClassAttribute)
                        .Single(); // TODO: how should analyzers handle exceptions etc?

                    if (doNotRenameAttribute != null)
                    {
                        var constructorNameArgumentValue = doNotRenameAttribute.ConstructorArguments[0].Value as string;
                        if (constructorNameArgumentValue != className)
                        {
                            // ClassName does not match DoNotRenameClassAttribute constructor argument 'className'
                            string reason = null;

                            if (doNotRenameAttribute.ConstructorArguments.Count() > 1)
                            {
                                reason = doNotRenameAttribute.ConstructorArguments[1].Value as string;
                            }

                            if (string.IsNullOrEmpty(reason))
                            {
                                var diagnostic = Diagnostic.Create(NotMatchingClassNameRule, context.Symbol.Locations[0], className);
                                // TODO: support partial classes? (Locations[0])
                                context.ReportDiagnostic(diagnostic);
                            } 
                            else
                            {
                                var diagnostic = Diagnostic.Create(NotMatchingClassNameWithReasonRule, context.Symbol.Locations[0], className, reason);
                                // TODO: support partial classes? (Locations[0])
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }
        }
    }
}
