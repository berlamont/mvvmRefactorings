using System;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace vmGenerator
{

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeListviewViewModelRefactoring))][Shared]
    class MakeListviewViewModelRefactoring : CodeRefactoringProvider
    {

        public string Title = "Generate Listview ViewModel";

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.
                                     GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var classDecl = node as ClassDeclarationSyntax;
            if (classDecl == null)
                return;
            var action = CodeAction.Create(title: Title, createChangedDocument: c => MakeListviewViewModelAsync(context.Document, classDecl, c), equivalenceKey: Title);
            context.RegisterRefactoring(action);
        }

        async Task<Document> MakeListviewViewModelAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var modelClassName = classDeclaration.Identifier.Text;
            var viewModelClassName = $"{modelClassName}VM";

            var newImplementation = $@"public class {viewModelClassName} : BaseVM
{{

public {viewModelClassName}() 
{{
  PageTitle = ""{viewModelClassName}"";
  LoadCmd.Execute(null);
}}

public {modelClassName} SelectedItem
{{
  get => GetPropValue<{modelClassName.Trim()}>();
  set => SetPropValue(value);
}}

public ObservableCollection<{modelClassName}> {modelClassName}List
{{
get => GetPropValue<ObservableCollection<{modelClassName}>>();
set => SetPropValue(value);
}}

public override void OnAppearing()
{{
throw new NotImplementedException();
}}

RelayCommand _loadCmd;
public RelayCommand LoadCmd => _loadCmd ?? (_loadCmd = new RelayCommand(LoadDataAsync, o => !IsBusy));

public async Task LoadDataAsync(object parameter = null)
{{

if(IsBusy)
return;

IsBusy = true;
//{modelClassName}List = await Task.Run(() => {modelClassName}Source.GetData.ToObservable());
IsBusy = false;

}}
}}
";
            var newClassNode = SyntaxFactory.ParseSyntaxTree(newImplementation)
                                            .GetRoot()
                                            .DescendantNodes()
                                            .OfType<ClassDeclarationSyntax>()
                                            .FirstOrDefault()
                                            .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

            var parentNamespace = (NamespaceDeclarationSyntax)classDeclaration.Parent;
            var newParentNamespace = parentNamespace.AddMembers(newClassNode).NormalizeWhitespace();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            
            var newRoot = (CompilationUnitSyntax)root.ReplaceNode(parentNamespace, newParentNamespace).NormalizeWhitespace();
   

            newRoot = newRoot.AddUsings(GenerateUsings("System", "Collections.ObjectModel"),
                GenerateUsings("System", "ComponentModel"), GenerateUsings("Xamarin", "Forms"));

            //return document.Project.AddDocument(viewModelClassName, newRoot);
            return document.WithSyntaxRoot(newRoot);
        }

        UsingDirectiveSyntax GenerateUsings(string first, string second) => 
            SyntaxFactory.UsingDirective(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(first),
            SyntaxFactory.IdentifierName(second)));
    }
}