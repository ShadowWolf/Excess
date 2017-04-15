﻿using System;

namespace Excess.Compiler
{
    //port: !!! some extensions want to play with this
    //but at the same time, it is too msbuild-ish to be justified
    //need solutions
    public interface ICompilation<TToken, TNode, TModel>
    {
        Scope Scope { get; }
        TModel GetSemanticModel(TNode node);
        string GetContent(string path);
        void AddContent(string path, string contents);
        void AddNativeDocument(string path, TNode root);
        void AddNativeDocument(string path, string contents);
        void AddDocument(string path, string contents);
        void AddDocument(string path, IDocument<TToken, TNode, TModel> document);
        void ReplaceNode(TNode old, TNode @new);
    }

    public interface ICompilationMatch<TToken, TNode, TModel>
    {
        ICompilationAnalysis<TToken, TNode, TModel> then(Action<TNode, ICompilation<TToken, TNode, TModel>, Scope> handler);

        bool matched(TNode node, ICompilation<TToken, TNode, TModel> compilation, Scope scope);
    }

    public interface ICompilationAnalysis<TToken, TNode, TModel>
    {
        ICompilationMatch<TToken, TNode, TModel> match<T>(Func<T, ICompilation<TToken, TNode, TModel>, Scope, bool> matcher) where T : TNode;
        ICompilationAnalysis<TToken, TNode, TModel> after(Action<ICompilation<TToken, TNode, TModel>, Scope> handler);
    }
}
