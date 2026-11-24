using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UmlGenerator;

internal sealed record SourceType(string Project, string Area, string Namespace, string Name,
    IReadOnlyList<BaseTypeDeclarationSyntax> Declarations)
{
    public string QualifiedName => $"{Namespace}.{Name}";
    public bool IsInterface => Declarations[0] is InterfaceDeclarationSyntax;
    public bool IsEnumeration => Declarations[0] is EnumDeclarationSyntax;

    public IEnumerable<string> Members => Declarations.SelectMany(declaration => declaration switch
    {
        EnumDeclarationSyntax enumeration => enumeration.Members.Select(member => $"+{member.Identifier}"),
        TypeDeclarationSyntax type => type.Members.Where(member => member.Modifiers.Any(SyntaxKind.PublicKeyword))
            .SelectMany(MemberNames).Concat(type is RecordDeclarationSyntax
                ? type.ParameterList?.Parameters.Select(parameter => $"+{parameter.Identifier}") ?? []
                : []),
        _ => []
    }).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    public IEnumerable<TypeSyntax> BaseTypes => Declarations.SelectMany(declaration =>
        declaration.BaseList?.Types.Select(type => type.Type) ?? []);

    public IEnumerable<TypeSyntax> Dependencies => Declarations.OfType<TypeDeclarationSyntax>().SelectMany(type =>
        (type.ParameterList?.Parameters.Select(parameter => parameter.Type) ?? [])
        .Concat(type.Members.OfType<ConstructorDeclarationSyntax>().SelectMany(ctor => ctor.ParameterList.Parameters.Select(p => p.Type)))
        .Concat(type.Members.OfType<PropertyDeclarationSyntax>().Where(p => p.Modifiers.Any(SyntaxKind.PublicKeyword)).Select(p => p.Type))
        .Concat(type.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
            .SelectMany(m => m.ParameterList.Parameters.Select(p => p.Type).Prepend(m.ReturnType)))
        .OfType<TypeSyntax>());

    private static IEnumerable<string> MemberNames(MemberDeclarationSyntax member) => member switch
    {
        PropertyDeclarationSyntax property => [$"+{property.Identifier}"],
        MethodDeclarationSyntax method => [$"+{method.Identifier}()"],
        FieldDeclarationSyntax field => field.Declaration.Variables.Select(variable => $"+{variable.Identifier}"),
        EventDeclarationSyntax signal => [$"+{signal.Identifier}"],
        EventFieldDeclarationSyntax signals => signals.Declaration.Variables.Select(variable => $"+{variable.Identifier}"),
        _ => []
    };
}
