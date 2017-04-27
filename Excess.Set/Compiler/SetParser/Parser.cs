﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.SetParser
{
    using CSharp = SyntaxFactory;

    public class Parser
    {
        public static SetSyntax Parse(string text)
        {
            throw new NotImplementedException();
        }

        public static SetSyntax Parse(IEnumerable<SyntaxToken> tokens)
        {
            var tokenArray = tokens.ToArray();
            var index = 0;

            var variables = new List<VariableSyntax>();
            var consumed = 0;
            if (!parseVariables(tokenArray, index, out consumed, variables))
                return null; //td: errors

            index += consumed;
            ConstructorSyntax constructor;
            if (!parseConstructor(tokenArray, index, out consumed, out constructor, variables))
                return null; //td: errors

            return new SetSyntax(variables.ToArray(), constructor);
        }

        private static bool parseVariables(SyntaxToken[] tokens, int index, out int consumed, List<VariableSyntax> result)
        {
            consumed = 0;
            var startIndex = index;
            for (;;)
            {
                if (index >= tokens.Length)
                    return false; //td: error, unexpected en of file

                var iconsumed = 0;
                var token = tokens[index];
                var finished = false;
                if (!parseVariableSeparator(token, out iconsumed, out finished))
                    return false;

                index += iconsumed;
                if (finished)
                    break;

                var vs = null as VariableSyntax;
                if (!parseVariable(tokens, index, out iconsumed, out vs))
                    return false;

                index += iconsumed;
                result.Add(vs);
            }

            consumed = index - startIndex;
            return true;
        }

        private static bool parseVariableSeparator(SyntaxToken token, out int consumed, out bool finished)
        {
            consumed = 0;
            finished = false;
            switch ((SyntaxKind)token.RawKind)
            {
                case SyntaxKind.BarToken:
                    consumed = 1;
                    finished = true;
                    break;
                case SyntaxKind.CommaToken:
                    consumed = 1;
                    break;
                case SyntaxKind.IdentifierToken:
                    break;
                default:
                    return false; //td: error
            }

            return true;
        }

        private static bool parseVariable(SyntaxToken[] tokens, int index, out int consumed, out VariableSyntax variable)
        {
            consumed = 0;
            variable = null;
            var token = tokens[index];
            if (token.RawKind != (int)SyntaxKind.IdentifierToken)
                return false; //td: error, variable name expected

            //variable: name x, or xi for indexing, x as name for alias 
            string varName;
            bool isIndexed;
            string indexName;
            if (!parseVariableName(token, out varName, out isIndexed, out indexName))
                return false;

            consumed++;

            int iconsumed;
            string alias;
            SyntaxToken aliasToken;
            if (!parseAlias(tokens, index + consumed, out alias, out aliasToken, out iconsumed))
                return false;

            consumed += iconsumed;

            //type
            SyntaxToken typeToken;
            string typeName;
            if (!parseType(tokens, index + consumed, out typeToken, out typeName, out iconsumed))
                return false;

            consumed += iconsumed;

            //we golden
            variable = new VariableSyntax(varName, token, typeName, typeToken, alias, aliasToken, isIndexed, indexName);
            return true;
        }

        private static bool parseVariableName(SyntaxToken token, out string varName, out bool isIndexed, out string indexName)
        {
            varName = null;
            isIndexed = false;
            indexName = null;
            varName = token.ToString();
            if (varName.Length > 2)
                return false; //td: error

            if (!varName.All(c => char.IsLower(c)))
                return false; //td: error

            isIndexed = varName.Length == 2; //td: !!! multiple indices
            indexName = isIndexed? varName[1].ToString() : string.Empty;

            return true;
        }

        //false means "there was an error parsing" where as true can be "there is no alias"
        //with consumed == 0
        private static bool parseAlias(SyntaxToken[] tokens, int index, out string alias, out SyntaxToken token, out int consumed)
        {
            consumed = 0;
            alias = string.Empty;
            token = default(SyntaxToken);
            if (!tokens[index].IsKind(SyntaxKind.AsKeyword))
                return true;

            if (index + 1 >= tokens.Length)
                return false; //td: error, eof

            token = tokens[index + 1];
            if (!token.IsKind(SyntaxKind.IdentifierToken))
                return false; //td: error, identifier expected

            consumed = 2;
            alias = token.ToString();
            return true;
        }

        private static bool parseType(SyntaxToken[] tokens, int index, out SyntaxToken token, out string name, out int consumed)
        {
            consumed = 0;
            name = string.Empty;
            token = tokens[index];
            if (!token.IsKind(SyntaxKind.AsKeyword) && !token.IsKind(SyntaxKind.ColonToken))
                return true;

            if (index + 1 >= tokens.Length)
                return false; //td: error, eof

            token = tokens[index + 1];
            if (!token.IsKind(SyntaxKind.IdentifierToken))
                return false; //td: error, identifier expected

            consumed = 2;
            name = token.ToString();
            return true;
        }

        private static bool parseConstructor(SyntaxToken[] tokens, int index, out int consumed, out ConstructorSyntax constructor, List<VariableSyntax> variables)
        {
            consumed = 0;
            constructor = null;

            var toCompile = new StringBuilder();
            var parenthesis = 0;
            var expressions = new List<ExpressionSyntax>();
            for (var i = index; i < tokens.Length; i++)
            {
                int iconsumed;
                if (toCompile.Length == 0)
                {
                    //at the beggining of a constructor item we give the parser
                    //chance to match custom syntax. Because custom === good.
                    ExpressionSyntax cexpr;
                    if (parseCustomConstructor(tokens, index = consumed, out iconsumed, out cexpr))
                    {
                        Debug.Assert(cexpr != null);

                        consumed += iconsumed;
                        i += iconsumed;
                        expressions.Add(cexpr);
                        continue;
                    }
                }

                consumed++;

                var token = tokens[i];
                var addToCompile = true;
                switch ((SyntaxKind)token.RawKind)
                {
                    case SyntaxKind.OpenParenToken:
                        parenthesis++;
                        break;
                    case SyntaxKind.CloseParenToken:
                        parenthesis--; //td: verify 
                        break;
                    case SyntaxKind.CommaToken:
                        if (parenthesis == 0)
                        {
                            addToCompile = false;
                            var expr = parseConstructorExpression(toCompile);
                            if (expr == null)
                                return false;

                            expressions.Add(expr);
                        }
                        break;
                    //for simplcity, we'll just switch the type of the type token  
                    //so it compiles as a expression. Note the operator we choose is illegal otherwise.
                    case SyntaxKind.ColonToken:
                    case SyntaxKind.InKeyword:
                        token = CSharp.Token(SyntaxKind.GreaterThanGreaterThanToken).WithTriviaFrom(token);
                        break;
                }

                if (addToCompile)
                    toCompile.Append(token.ToFullString());
            }

            if (toCompile.Length > 0)
            {
                var expr = parseConstructorExpression(toCompile);
                if (expr == null)
                    return false;

                expressions.Add(expr);
            }

            return buildConstructor(variables, expressions, out constructor);
        }

        static readonly ExpressionSyntax _ellipsis = CSharp.ParseName("ellipsis");
        private static bool parseCustomConstructor(SyntaxToken[] tokens, int index, out int consumed, out ExpressionSyntax expr)
        {
            consumed = 0;
            expr = null;

            var token = tokens[index];
            switch (token.Kind())
            {
                //check ellipsis
                case SyntaxKind.DotToken:
                    if (index + 4 < tokens.Length
                        && tokens[index + 1].Kind() == SyntaxKind.DotToken
                        && tokens[index + 2].Kind() == SyntaxKind.DotToken)
                    {
                        if (tokens[index + 3].Kind() != SyntaxKind.CommaToken)
                            return false; //td: error, bad ellipsis

                        consumed = 4;
                        expr = _ellipsis;
                    }
                    break;
            }

            return true;
        }

        private static ExpressionSyntax parseConstructorExpression(StringBuilder text)
        {
            var result = CSharp.ParseExpression(text.ToString());
            text.Clear();
            return result;
        }

        private static bool buildConstructor(List<VariableSyntax> variables, List<ExpressionSyntax> expressions, out ConstructorSyntax constructor)
        {
            constructor = null;
            foreach (var expression in expressions)
            {
                if (applyToVariable(expression, variables))
                    continue;

                if (applyToMatch(expression, variables, constructor, out constructor))
                    continue;

                if (applyToIndexedInduction(expression, variables, constructor, out constructor))
                    continue;

                if (applyToGeneralInduction(expression, variables, constructor, out constructor))
                    continue;

                if (applyToPredicate(expression, variables, constructor, out constructor))
                    continue;

                return false;
            }

            return true;
        }

        private static bool applyToMatch(ExpressionSyntax expression, List<VariableSyntax> variables, ConstructorSyntax current, out ConstructorSyntax constructor)
        {
            throw new NotImplementedException();
        }

        private static bool applyToPredicate(ExpressionSyntax expression, List<VariableSyntax> variables, ConstructorSyntax current, out ConstructorSyntax constructor)
        {
            throw new NotImplementedException();
        }

        private static bool applyToGeneralInduction(ExpressionSyntax expression, List<VariableSyntax> variables, ConstructorSyntax current, out ConstructorSyntax constructor)
        {
            throw new NotImplementedException();
        }

        private static bool applyToIndexedInduction(ExpressionSyntax expression, List<VariableSyntax> variables, ConstructorSyntax current, out ConstructorSyntax constructor)
        {
            throw new NotImplementedException();
        }

        private static bool applyToVariable(ExpressionSyntax expression, List<VariableSyntax> variables)
        {
            throw new NotImplementedException();
        }
    }
}
