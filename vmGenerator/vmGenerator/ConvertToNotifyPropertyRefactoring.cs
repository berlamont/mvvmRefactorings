using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace vmGenerator
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "ConvertToNotifyPropertyRefactoring")]
    [Shared]
    class ConvertToNotifyPropertyRefactoring : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);
            var alttoken = root.FindToken(textSpan.End);
            
            if (token.Parent == null || alttoken.Parent == null)
                return;

            var propertyDeclaration = token.Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            var altpropertyDeclaration = alttoken.Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

            var newprop = propertyDeclaration ?? altpropertyDeclaration;
            if (newprop == null)
                return;
            
            var accessors = newprop.AccessorList.Accessors;
            var getter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.GetAccessorDeclaration);
            var setter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.SetAccessorDeclaration);

            if ((getter != null) && (setter != null) && (getter.Body != null) && (setter.Body != null))
                return;
                
                
            var doesnotintersect = !newprop.Identifier.Span.IntersectsWith(textSpan.Start);
            
            // Refactor only properties with both a getter and a setter.
            if (doesnotintersect)
                return;

            context.RegisterRefactoring(new ConvertToAutoPropertyCodeAction("Convert to notify prop",
                c => ConvertToAutoPropertyAsync(document, propertyDeclaration, c)));
        }


        async Task<Document> ConvertToAutoPropertyAsync(Document document, PropertyDeclarationSyntax property,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Rewrite property
            var propertyRewriter = new PropertyRewriter(semanticModel, property);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = propertyRewriter.Visit(root);

            return document.WithSyntaxRoot(newRoot);
        }

        class ConvertToAutoPropertyCodeAction : CodeAction
        {
            Func<CancellationToken, Task<Document>> generateDocument;

            public ConvertToAutoPropertyCodeAction(string title,
                Func<CancellationToken, Task<Document>> generateDocument)
            {
                Title = title;
                this.generateDocument = generateDocument;
            }

            public override string Title { get; }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken) =>
                generateDocument(cancellationToken);
        }
    }
}