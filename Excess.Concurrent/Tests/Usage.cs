﻿using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Excess.Compiler.Roslyn;
using Excess.Concurrent.Compiler;

namespace Excess.Concurrent.Tests
{
    [TestClass]
    public class Usage
    {
        [TestMethod]
        public void BasicOperators()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            ConcurrentExtension.Apply(compiler);

            SyntaxTree tree = null;
            string text = null;

            tree = compiler.ApplySemanticalPass(@"
                concurrent class SomeClass 
                { 
                    void main() 
                    {
                        A | (B & C()) >> D(10);
                    }

                    public void A();
                    public void B();
                    public void F();
                    public void G();
                    
                    private string C()
                    {
                        if (2 > 1)
                            return ""SomeValue"";

                        F & G;

                        if (1 > 2)
                            return ""SomeValue"";
                        return ""SomeOtherValue"";
                    }

                    private int D(int v)
                    {
                        return v + 1;
                    }
                }", out text);

            Assert.IsTrue(tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Count(method =>
                    new[] {
                      "__concurrentmain",
                      "__concurrentA",
                      "__concurrentB",
                      "__concurrentC",
                      "__concurrentF",
                      "__concurrentG",}
                    .Contains(method
                        .Identifier
                        .ToString())) == 6); //must have created concurrent methods

            Assert.IsFalse(tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Any(method => method
                        .Identifier
                        .ToString() == "__concurrentD")); //but not for D
        }

        [TestMethod]
        public void BasicAssigment()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            ConcurrentExtension.Apply(compiler);

            SyntaxTree tree = null;
            string text = null;

            tree = compiler.ApplySemanticalPass(@"
                concurrent class SomeClass 
                { 
                    int E;
                    void main() 
                    {
                        string B;
                        A | (B = C()) & (E = D(10));
                    }

                    public void A();
                    public void F();
                    public void G();
                    
                    private string C()
                    {
                        F & G;

                        return ""SomeValue"";
                    }

                    private int D(int v)
                    {
                        return v + 1;
                    }
                }", out text);

            Assert.IsTrue(tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single(@class => @class.Identifier.ToString() == "__expr1")
                .Members
                .OfType<FieldDeclarationSyntax>()
                .Count(field => new[] { "B", "E" }
                    .Contains(field
                        .Declaration
                        .Variables[0]
                        .Identifier.ToString())) == 2); //must have added fields to the expression object

            Assert.IsTrue(tree.GetRoot()
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Count(assignment => new[] { "B", "E" }
                    .Contains(assignment
                        .Left
                        .ToString())) == 2); //must have added assignments from fields to the expression object
        }

        [TestMethod]
        public void BasicTryCatch()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            ConcurrentExtension.Apply(compiler);

            SyntaxTree tree = null;
            string text = null;

            tree = compiler.ApplySemanticalPass(@"
                concurrent class SomeClass 
                { 
                    public void A();
                    public void B();

                    void main() 
                    {
                        try
                        {
                            int someValue = 10;
                            int someOtherValue = 11;

                            A | B;

                            someValue++;

                            B >> A;

                            someOtherValue++;
                        }
                        catch
                        {
                        }
                    }
                }", out text);

            Assert.IsTrue(tree.GetRoot()
                .DescendantNodes()
                .OfType<TryStatementSyntax>()
                .Count() == 2); //must have added a a try statement
        }

        [TestMethod]
        public void BasicProtection()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            ConcurrentExtension.Apply(compiler);

            SyntaxTree tree = null;
            string text = null;

            tree = compiler.ApplySemanticalPass(@"
                concurrent class VendingMachine 
                { 
                    public    void coin();
                    protected void choc();
                    protected void toffee();

                    void main() 
                    {
                        for (;;)
                        {
                            coin >> (choc | toffee);
                        }
                    }
                }", out text);

            Assert.IsTrue(tree.GetRoot()
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .SelectMany(thrw => thrw
                        .DescendantNodes()
                        .OfType<LiteralExpressionSyntax>())
                .Select(s => s.ToString())
                .Count(s => new[] { "\"choc\"", "\"toffee\"" }
                    .Contains(s)) == 2); //must have added checks for choc and toffee
        }

        [TestMethod]
        public void BasicAwait()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            ConcurrentExtension.Apply(compiler);

            SyntaxTree tree = null;
            string text = null;

            tree = compiler.ApplySemanticalPass(@"
                concurrent class SomeClass
                { 
                    public void A();
                    public void B();

                    void main() 
                    {
                        await A;
                        int val = await C();
                        val++;
                    }

                    private int C()
                    {
                        await B;
                        return 10;
                    }
                }", out text);

            Assert.IsTrue(tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation
                    .Expression
                    .ToString() == "__listen")
                .Count(invocation => new[] { "\"A\"", "\"B\"" }
                    .Contains(invocation
                        .ArgumentList
                        .Arguments[0]
                        .Expression.ToString())) == 2); //must have listened to both signals
        }

        [TestMethod]
        public void BasicProtectionRuntime()
        {
            var errors = null as IEnumerable<Diagnostic>;
            var node = Mock.Build(@"
                concurrent class VendingMachine 
                { 
                    public    void coin();
                    protected void choc();
                    protected void toffee();

                    void main() 
                    {
                        for (;;)
                        {
                            coin >> (choc | toffee);
                        }
                    }
                }", out errors);

            //must not have compilation errors
            Assert.IsNull(errors);

            var vm = node.Spawn("VendingMachine");

            Mock.AssertFails(vm, "choc");
            Mock.AssertFails(vm, "toffee");

            Mock.Succeeds(vm, "coin", "choc");
            Mock.Succeeds(vm, "coin", "toffee");
        }

        [TestMethod, Ignore] //concurrent objects not yet operational
        public void BasicSingleton()
        {
            var errors = null as IEnumerable<Diagnostic>;
            var app = Mock
                .Build(@"
                    concurrent object VendingMachine 
                    { 
                        public    void coin();
                        protected void choc();
                        protected void toffee();

                        void main() 
                        {
                            for (;;)
                            {
                                coin >> (choc | toffee);
                            }
                        }
                    }", out errors);

            //must not have compilation errors
            Assert.IsNull(errors);
            bool throws = false;
            try
            {
                app.Spawn("VendingMachine");
            }
            catch
            {
                throws = true;
            }

            Assert.IsTrue(throws);

            var vm = app.GetSingleton("VendingMachine");
            Assert.IsNotNull(vm);

            Mock.AssertFails(vm, "choc");
            Mock.AssertFails(vm, "toffee");

            Mock.Succeeds(vm, "coin", "choc");
            Mock.Succeeds(vm, "coin", "toffee");
        }

        [TestMethod]

        public void DebugPrint()
        {
            var text = null as string;
            var tree = Mock.Compile(@"
                using xs.concurrent;

                namespace Santa
                {
	                concurrent class Reindeer
	                {
		                string _name;        
		                SantaClaus _santa;  
		                public Reindeer(string name, SantaClaus santa)
		                {
			                _name = name;
			                _santa = santa;
		                }

		                void main()
		                {
			                for(;;)
			                {
				                await vacation();
			                }
		                }

		                public void unharness()
		                {
			                Console.WriteLine(_name + "": job well done"");

                        }

                        private void vacation()
                        {
                            seconds(rand(3, 7))
                                >> Console.WriteLine(_name + "": back from vacation"")
                                >> (_santa.reindeer(this) & unharness);
                        }
                    }

                    concurrent class Elf
                    {
                        string _name;
                        SantaClaus _santa;
                        public Elf(string name, SantaClaus santa)
                        {
                            _name = name;
                            _santa = santa;
                        }

                        void main()
                        {
                            for (;;)
                            {
                                await work();
                            }
                        }

                        public void advice(bool given)
                        {
                            if (given)
                                Console.WriteLine(_name + "": great advice, santa!"");
                            else
                                Console.WriteLine(_name + "": Santa is busy, back to work"");
                        }

                        private void work()
                        {
                            seconds(rand(1, 5))
                                >> Console.WriteLine(_name + "": off to see Santa"")
                                >> (_santa.elf(this) & advice);
                        }
                    }
                }", out text, false, false); 

            Assert.IsNotNull(text);
        }
    }
}
