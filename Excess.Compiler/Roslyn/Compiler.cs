﻿using Excess.Compiler.Core;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Excess.Compiler.Roslyn
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using CSharp = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    public class RoslynCompiler : CompilerBase<SyntaxToken, SyntaxNode, SemanticModel>
    {
        public RoslynCompiler() : base(new RoslynLexicalAnalysis(), new RoslynSyntaxAnalysis())
        {
        }

        protected override IDocument<SyntaxToken, SyntaxNode, SemanticModel> createDocument()
        {
            return new RoslynDocument(new Scope());
        }

        //public override ICompilerPass initialPass(string text)
        //{
        //    return new LexicalPass(text);
        //}

        //out of interface methods, used for testing
        public ExpressionSyntax CompileExpression(string expr)
        {
            var   document = new RoslynDocument(new Scope());
            var   handler  = _lexical as IDocumentHandler<SyntaxToken, SyntaxNode, SemanticModel>;

            handler.apply(document);
            document.applyChanges(CompilerStage.Lexical);

            return CSharp.ParseExpression(document.LexicalText);
        }

        public SyntaxNode ApplyLexicalPass(string text, out string newText)
        {
            var document = new RoslynDocument(new Scope());
            var handler = _lexical as IDocumentHandler<SyntaxToken, SyntaxNode, SemanticModel>;

            handler.apply(document);
            document.applyChanges(CompilerStage.Lexical);

            newText = document.LexicalText;
            return document.Root;
        }

        public string ApplyLexicalPass(string text)
        {
            string result;
            ApplyLexicalPass(text, out result);
            return result;
        }

        public SyntaxTree ApplySyntacticalPass(string text, out string result)
        {
            var document = new RoslynDocument(new Scope());
            var handler = _lexical as IDocumentHandler<SyntaxToken, SyntaxNode, SemanticModel>;

            handler.apply(document);
            document.applyChanges(CompilerStage.Syntactical);

            result = document.LexicalText;
            return document.Root.SyntaxTree;
        }

        public SyntaxTree ApplySyntacticalPass(string text)
        {
            string useless;
            return ApplySyntacticalPass(text, out useless);
        }


        //declarations
        public static TypeSyntax @void    = CSharp.PredefinedType(CSharp.Token(SyntaxKind.VoidKeyword));
        public static TypeSyntax @object  = CSharp.PredefinedType(CSharp.Token(SyntaxKind.ObjectKeyword));
        public static TypeSyntax @double  = CSharp.PredefinedType(CSharp.Token(SyntaxKind.DoubleKeyword));
        public static TypeSyntax @int     = CSharp.PredefinedType(CSharp.Token(SyntaxKind.IntKeyword));
        public static TypeSyntax @string  = CSharp.PredefinedType(CSharp.Token(SyntaxKind.StringKeyword));
        public static TypeSyntax @boolean = CSharp.PredefinedType(CSharp.Token(SyntaxKind.BoolKeyword));

        //modifiers
        public static SyntaxTokenList @public  = CSharp.TokenList(CSharp.Token(SyntaxKind.PublicKeyword));
        public static SyntaxTokenList @private = CSharp.TokenList(CSharp.Token(SyntaxKind.PrivateKeyword));

        //node marking
        static private int _seed = 0;
        public static string uniqueId()
        {
            return (++_seed).ToString();
        }

        public static string SyntaxIdAnnotation = "xs-syntax-id";
        public static string GetSyntaxId(SyntaxNode node)
        {
            var annotation = node.GetAnnotations(SyntaxIdAnnotation).FirstOrDefault();
            if (annotation != null)
                return annotation.Data;

            return null;
        }

        public static SyntaxNode SetSyntaxId(SyntaxNode node, out string id)
        {
            id = uniqueId();
            return node
                .WithoutAnnotations(SyntaxIdAnnotation)
                .WithAdditionalAnnotations(new SyntaxAnnotation(SyntaxIdAnnotation, id));
        }


        public static string NodeIdAnnotation = "xs-node";
        public static SyntaxNode MarkNode(SyntaxNode node, string id)
        {
            return node
                .WithoutAnnotations(NodeIdAnnotation)
                .WithAdditionalAnnotations(new SyntaxAnnotation(NodeIdAnnotation, id));
        }

        public static string NodeMark(SyntaxNode node)
        {
            var annotation = node.GetAnnotations(NodeIdAnnotation).FirstOrDefault();
            if (annotation != null)
                return annotation.Data;

            return null;
        }


        public static string LexicalIdAnnotation = "xs-lexical-id";
        public static string GetLexicalId(SyntaxToken token)
        {
            var annotation = token.GetAnnotations(LexicalIdAnnotation).FirstOrDefault();
            if (annotation != null)
                return annotation.Data;

            return null;
        }

        public static string SyntacticalExtensionIdAnnotation = "xs-syntax-extension";
        public static string GetSyntacticalExtensionId(SyntaxNode node)
        {
            var annotation = node.GetAnnotations(SyntacticalExtensionIdAnnotation).FirstOrDefault();
            if (annotation != null)
                return annotation.Data;

            return null;
        }

        public static SyntaxNode SetSyntacticalExtensionId(SyntaxNode node, out string id)
        {
            id = uniqueId();
            return node
                .WithoutAnnotations(SyntacticalExtensionIdAnnotation)
                .WithAdditionalAnnotations(new SyntaxAnnotation(SyntacticalExtensionIdAnnotation, id));
        }

        public static SyntaxToken MarkToken(SyntaxToken token, string mark, object value)
        {
            var result = value == null ? new SyntaxAnnotation(mark) :
                                         new SyntaxAnnotation(mark, value.ToString());

            return token
                .WithoutAnnotations(mark)
                .WithAdditionalAnnotations(result);
        }

        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            var tokens = CSharp.ParseTokens(text);
            foreach (var token in tokens)
            {
                if (token.CSharpKind() != SyntaxKind.EndOfFileToken)
                    yield return token;
            }
        }

        public static string TokensToString(IEnumerable<SyntaxToken> tokens)
        {
            StringBuilder result = new StringBuilder();
            foreach (var token in tokens)
                result.Append(token.ToFullString());

            return result.ToString();
        }

        public static BlockSyntax ParseCode(IEnumerable<SyntaxToken> tokens)
        {
            string code = TokensToString(tokens); //td: mapping
            return (BlockSyntax)CSharp.ParseStatement("{" + code + "}");
        }

        public static ParameterListSyntax ParseParameterList(IEnumerable<SyntaxToken> parameters)
        {
            string parameterString = TokensToString(parameters); //td: mapping
            return CSharp.ParseParameterList(parameterString);
        }
        public static bool isLexicalIdentifier(SyntaxToken token)
        {
            return isLexicalIdentifier(token.CSharpKind());
        }

        public static bool isLexicalIdentifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.BoolKeyword:
                case SyntaxKind.ByteKeyword:
                case SyntaxKind.SByteKeyword:
                case SyntaxKind.ShortKeyword:
                case SyntaxKind.UShortKeyword:
                case SyntaxKind.IntKeyword:
                case SyntaxKind.UIntKeyword:
                case SyntaxKind.LongKeyword:
                case SyntaxKind.ULongKeyword:
                case SyntaxKind.DoubleKeyword:
                case SyntaxKind.FloatKeyword:
                case SyntaxKind.DecimalKeyword:
                case SyntaxKind.StringKeyword:
                case SyntaxKind.CharKeyword:
                case SyntaxKind.VoidKeyword:
                case SyntaxKind.ObjectKeyword:
                case SyntaxKind.NullKeyword:
                    return true;
            }

            return false;
        }

        //td: refactor
        //public static SyntaxNode ReplaceAnnotated(SyntaxNode node, string annotation, IEnumerable<KeyValuePair<string, Func<ISyntacticalMatchResult<SyntaxNode>, SyntaxNode>>> handlers)
        //{
        //    var nodes = node.GetAnnotatedNodes(annotation);
        //    return node.ReplaceNodes(nodes, (oldNode, newNode) =>
        //    {
        //        var data = oldNode.GetAnnotations(annotation).First().Data;
        //        var dataHandlers = handlers
        //            .Where(h => h.Key == data);

        //        var resultNode = newNode;
        //        foreach (var handler in dataHandlers)
        //        {
        //            RoslynSyntacticalMatchResult result = new RoslynSyntacticalMatchResult(new Scope(), null, resultNode);
        //            resultNode = handler.Value(result);
        //        }

        //        return resultNode.WithoutAnnotations(annotation);
        //    });
        //}
    }
}