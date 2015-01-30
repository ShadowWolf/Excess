﻿using Excess.Compiler.Core;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Excess.Compiler.Roslyn
{
    public class RoslynSyntaxTransform : BaseSyntaxTransform<SyntaxToken, SyntaxNode, SemanticModel>
    {

        public RoslynSyntaxTransform()
        {
        }

        public RoslynSyntaxTransform(Func<SyntaxNode, Scope, IEnumerable<SyntaxNode>, SyntaxNode> handler)
        {
            _selectors.Add(null);
            _transformers.Add(handler);
        }

        protected override SyntaxNode addToNode(SyntaxNode node, IEnumerable<SyntaxNode> nodes)
        {
            throw new NotImplementedException(); //td: 
        }

        protected override SyntaxNode removeNodes(SyntaxNode node, IEnumerable<SyntaxNode> nodes)
        {
            return node.RemoveNodes(nodes, SyntaxRemoveOptions.KeepEndOfLine);
        }

        protected override SyntaxNode replaceNodes(SyntaxNode node, Scope scope, IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, Scope, SyntaxNode> handler)
        {
            return node.ReplaceNodes(nodes, (oldNode, newNode) =>
            {
                //change the result temporarily, this might need revisiting
                var oldResulSyntaxNode = node;
                var returnValue = handler(newNode, scope);
                return returnValue;
            });
        }

        protected override SyntaxNode resolveScope(SyntaxNode node, bool type, bool @namespace)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<SyntaxNode> findNodes(SyntaxNode parent, string annotation, string data)
        {
            return parent.GetAnnotatedNodes(annotation)
                .Where(node => node.GetAnnotations(annotation).First().Data == data);
        }

        protected override SyntaxNode markNodes(SyntaxNode parent, string annotation, Dictionary<SyntaxNode, string> nodeIds)
        {
            return parent.ReplaceNodes(nodeIds.Keys, (oldNode, newNode) => {
                return newNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotation, nodeIds[oldNode]));
            });
        }

        protected override SyntaxNode getParent(SyntaxNode node)
        {
            return node.Parent;
        }
    }

    public class RoslynSyntacticalMatch : BaseSyntacticalMatch<SyntaxToken, SyntaxNode, SemanticModel>
    {
        public RoslynSyntacticalMatch(ISyntaxAnalysis<SyntaxToken, SyntaxNode, SemanticModel> syntax) :
            base(syntax)
        {
        }

        protected override IEnumerable<SyntaxNode> children(SyntaxNode node)
        {
            return node.ChildNodes();
        }

        protected override IEnumerable<SyntaxNode> descendants(SyntaxNode node)
        {
            return node.DescendantNodes();
        }
    }

    public class RoslynSyntaxAnalysis : BaseSyntaxAnalysis<SyntaxToken, SyntaxNode, SemanticModel>
    {
        protected override ISyntaxTransform<SyntaxNode> createTransform()
        {
            return new RoslynSyntaxTransform();
        }

        protected override ISyntacticalMatch<SyntaxToken, SyntaxNode, SemanticModel> createMatch(Func<SyntaxNode, bool> selector)
        {
            RoslynSyntacticalMatch result = new RoslynSyntacticalMatch(this);
            result.addMatcher(selector);
            return result;
        }

        protected override ISyntaxTransform<SyntaxNode> createTransform(Func<SyntaxNode, Scope, IEnumerable<SyntaxNode>, SyntaxNode> handler)
        {
            return new RoslynSyntaxTransform(handler);
        }
    }
}