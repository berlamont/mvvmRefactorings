using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace vmGenerator
{
    class PropertyRewriter : CSharpSyntaxRewriter
    {
        readonly SemanticModel _semanticModel;
        readonly PropertyDeclarationSyntax _property;

        public PropertyRewriter(SemanticModel semanticModel, PropertyDeclarationSyntax property)
        {
            _semanticModel = semanticModel;
            _property = property;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax name) => 
            _semanticModel.GetSymbolInfo(name).Symbol == null ? name : name.WithIdentifier(SyntaxFactory.Identifier(_property.Identifier.ValueText)).WithAdditionalAnnotations(Formatter.Annotation);

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration) => 
            propertyDeclaration == _property ? ConvertToAutoProperty(propertyDeclaration).WithAdditionalAnnotations(Formatter.Annotation) : base.VisitPropertyDeclaration(propertyDeclaration);

        
        //public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax variable)
        //{
        //    // Retrieve the symbol for the variable declarator
        //    var field = variable.Parent.Parent as FieldDeclarationSyntax;
        //    if (field != null && field.Declaration.Variables.Count == 1)
        //    {
        //        if (object.Equals(semanticModel.GetDeclaredSymbol(variable), backingField))
        //        {
        //            return null;
        //        }
        //    }

        //    return variable;
        //}

        PropertyDeclarationSyntax ConvertToAutoProperty(PropertyDeclarationSyntax propertyDeclaration)
        {      
            var newImplementation = $@"
public {_property.Type} {_property.Identifier.Text}
{{
  get => GetPropValue<{_property.Type}>();
  set => SetPropValue(value);
}}";
            var newClassNode = SyntaxFactory.ParseSyntaxTree(newImplementation)
                                            .GetRoot()
                                            .DescendantNodes()
                                            .OfType<PropertyDeclarationSyntax>()
                                            .FirstOrDefault()
                                            .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);
            
            return _property.ReplaceNode(_property, newClassNode);
        }
}
}
