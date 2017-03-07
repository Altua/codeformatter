using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [SyntaxRule(OrderFieldsRule.Name, OrderFieldsRule.Description, SyntaxRuleOrder.OrderFieldsRule)]
    internal sealed class OrderFieldsRule : CSharpOnlyFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = "OrderFieldsRule";
        internal const string Description = "Ensure fields are ordered correctly";

        public SyntaxNode Process(SyntaxNode syntaxRoot, string languageName)
        {
            var rewriter = new CSharpOrderFieldRewriter();
            
            return rewriter.Visit(syntaxRoot);
        }

        internal sealed class CSharpOrderFieldRewriter : CSharpSyntaxRewriter
        {

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {

                var children = node.ChildNodes().ToList();
                var ordered = children.Select(x => new ComparableNode(x)).ToList();
                ordered.Sort();
                

                var t = children.Zip(ordered, (k, v) => new { k, v}).ToDictionary(x => x.k, x => x.v);
                
                
                var result = node.ReplaceNodes(children, (_, n) =>
                {
                    return UnwrapAndClean(t[n]);
                });

                return result;
            }

            private SyntaxNode UnwrapAndClean(ComparableNode comparableNode)
            {
                var node = comparableNode.Node;

                if (!node.HasLeadingTrivia)
                    return node;

                var trivia = node.GetLeadingTrivia();

                var trim = trivia.Where(x => x.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
                
                foreach(var t in trim)
                {
                    trivia = trivia.Remove(t);
                }

                if(comparableNode.Section != CodeSections.PrivateField || comparableNode.Section != CodeSections.PrivateReadonlyField)
                    trivia = trivia.Insert(0, SyntaxFactory.EndOfLine("\r\n"));

                return node.WithLeadingTrivia(trivia);
                
            }

            private static class CodeSections
            {
                public static int Unknown = 0;
                public static int PrivateReadonlyField = 5;
                public static int PrivateField = 6;
                public static int Constructor = 10;
                public static int PrivateMethod = 21;
                public static int PublicMethod = 22;
                public static int Class = 30;
            }

            private class ComparableNode:IComparable<ComparableNode>
            {
                private string _text;

                //TODO section should probably be a class so we can support explicit interfaces etc
                private int _section = 0;

                public int Section => _section;

                public SyntaxNode Node { get; }

                public ComparableNode(SyntaxNode node)
                {
                    Node = node;

                    var field = node as FieldDeclarationSyntax;
                    if (field != null)
                    {
                        _text = field.Declaration.ToString();
                        _section = CodeSections.PrivateField;

                        if (field.Modifiers.Any(x => x.IsKind(SyntaxKind.ReadOnlyKeyword)))
                            _section = CodeSections.PrivateReadonlyField;
                    }

                    var ctor = node as ConstructorDeclarationSyntax;
                    if(ctor != null)
                    {
                        _section = CodeSections.Constructor;
                    }

                    var method = node as MethodDeclarationSyntax;
                    if (method != null)
                    {
                        _section = CodeSections.PrivateMethod;
                        
                        if (method.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.InternalKeyword)))
                            _section = CodeSections.PublicMethod;
                    }

                    var @class = node as ClassDeclarationSyntax;
                    if (@class != null)
                    {
                        _text = @class.ToString();
                        _section = CodeSections.Class;
                    }

                    if (_text == null)
                        _text = node.ToString();
                }

                public int CompareTo(ComparableNode other)
                {
                    var result = _section.CompareTo(other._section);

                    if (result == 0)
                        result = _text.CompareTo(other._text);

                    return result;
                }
            }

            
        }
    }
}
