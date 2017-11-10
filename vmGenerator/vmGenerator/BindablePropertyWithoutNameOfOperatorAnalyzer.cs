/*using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace vmGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BindablePropertyWithoutNameOfOperatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BindablePropertyWithoutNameOfOperatorAnalyzer";
        internal const string Category = "Custom Controls";

        internal const string BindablePropertyTypeName = "Xamarin.Forms.BindableProperty";
        internal const string CreateMethodName = "Create";
        internal static readonly LocalizableString Title = "Use nameof";

        internal static readonly LocalizableString MessageFormat =
            "Bindable property '{0}' can use nameof() operator for BindableProperty.Create() call";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationNode = (InvocationExpressionSyntax) context.Node;
            var memberAccessNode = (MemberAccessExpressionSyntax) invocationNode.Expression;
            var methodNameNode = (IdentifierNameSyntax) memberAccessNode.Name;

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(methodNameNode).Symbol;
            if (memberAccessSymbol == null)
                return;

            if (!GetRegisterMethodSymbols(context.SemanticModel.Compilation).Contains(memberAccessSymbol))
                return;
            
            var firstArgumentNode = invocationNode.ArgumentList.Arguments.First();
            var firstArgumentExpressionNode = firstArgumentNode.Expression;
            if (IsNameOf(firstArgumentExpressionNode))
                return;
                
            var depPropName = context.SemanticModel.GetConstantValue(firstArgumentExpressionNode).Value;
            context.ReportDiagnostic(Diagnostic.Create(Rule, firstArgumentNode.GetLocation(), depPropName));
        }

        static ImmutableArray<ISymbol> GetRegisterMethodSymbols(Compilation compilation)
        {
            var dependencyPropertySymbol = compilation.GetTypeByMetadataName(BindablePropertyTypeName);
            return dependencyPropertySymbol.GetMembers(CreateMethodName);
        }

        static bool IsNameOf(ExpressionSyntax firstArgumentExpressionNode)
        {
            return (firstArgumentExpressionNode.Kind() == SyntaxKind.InvocationExpression) &&
                   ((InvocationExpressionSyntax) firstArgumentExpressionNode).IsNameOfExpression();
        }
    }

    static class NameOfExaminator
    {
        public static bool IsNameOfExpression(this InvocationExpressionSyntax invocationNode)
        {
            var identifierNode = invocationNode.Expression as IdentifierNameSyntax;
            if (identifierNode == null)
                return false;
            return identifierNode.IsNameOfIdentifier();
        }

        public static bool IsNameOfIdentifier(this IdentifierNameSyntax identifierNode) =>
            identifierNode.Identifier.IsKindOrHasMatchingText(SyntaxKind.NameOfKeyword);
    }

    static class SyntaxTokenExtensions
    {
        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind) =>
            (token.Kind() == kind) || token.HasMatchingText(kind);

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            return token.ToString() == text;
        }
    }
}*/