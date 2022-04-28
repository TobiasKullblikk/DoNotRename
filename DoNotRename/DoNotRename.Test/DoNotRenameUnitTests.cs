using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace DoNotRename.Test
{
    [TestClass]
    public class DoNotRenameUnitTest : CSharpAnalyzerTest<DoNotRenameAnalyzer, MSTestVerifier>
    {
        private static readonly MetadataReference DoNotRenameReference
            = MetadataReference.CreateFromFile(typeof(DoNotRenameClassAttribute).Assembly.Location);

        protected override async Task<Solution> CreateSolutionAsync(
            ProjectId projectId,
            EvaluatedProjectState projectState,
            CancellationToken cancellationToken)
        {
            var solution = await base.CreateSolutionAsync(projectId, projectState, cancellationToken);
            return solution
                .AddMetadataReference(projectId, DoNotRenameReference);
        }

        [TestMethod]
        public async Task NoErrorTest()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(""TYPENAME"")]
        class TYPENAME { }
    }";

            await RunAsync();
        }

        [TestMethod]
        public async Task NoAttributeTest()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        class TYPENAME { }
    }";

            await RunAsync();
        }

        [TestMethod]
        public async Task NotClassTest()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(""TYPENAME"")]
        enum TYPENAME { }
    }";

            var expected = DiagnosticResult.CompilerError("CS0592")
                .WithSpan(6, 10, 6, 26) // TODO: can this be more readable?
                .WithArguments("DoNotRenameClass", "class");
            ExpectedDiagnostics.Add(expected);

            await RunAsync();
        }

        [TestMethod]
        public async Task AnotherAttributeTest()
        {
            TestCode = @"
    using System;

    namespace ConsoleApplication1
    {
        class ATTRIBUTEAttribute : Attribute { }

        [ATTRIBUTE]
        class TYPENAME { }
    }";

            await RunAsync();
        }

        [TestMethod]
        public async Task DoNotRenameClassAttributeWorksWithAnotherAttributeTest_1()
        {
            TestCode = @"
    using System;
    using DoNotRename;

    namespace ConsoleApplication1
    {
        class ATTRIBUTEAttribute : Attribute { }

        [ATTRIBUTE]
        [DoNotRenameClass(""NOT_TYPENAME"")]
        class TYPENAME { }
    }";

            var expected = new DiagnosticDescriptor(
                "DoNotRenameAnalyzer_NotMatchingClassName",
                "Class should not be renamed",
                "Class 'TYPENAME' does not match DoNotRenameClassAttribute 'className' argument value",
                "Naming",
                DiagnosticSeverity.Error,
                true);
            ExpectedDiagnostics.Add(new DiagnosticResult(expected)
                .WithSpan(11, 15, 11, 23) // TODO: can this be more readable?
                .WithArguments("TYPENAME"));

            await RunAsync();
        }

        [TestMethod]
        public async Task DoNotRenameClassAttributeWorksWithAnotherAttributeTest_2()
        {
            TestCode = @"
    using System;
    using DoNotRename;

    namespace ConsoleApplication1
    {
        class ATTRIBUTEAttribute : Attribute { }

        [ATTRIBUTE, DoNotRenameClass(""NOT_TYPENAME"")]
        class TYPENAME { }
    }";

            var expected = new DiagnosticDescriptor(
                "DoNotRenameAnalyzer_NotMatchingClassName",
                "Class should not be renamed",
                "Class 'TYPENAME' does not match DoNotRenameClassAttribute 'className' argument value",
                "Naming",
                DiagnosticSeverity.Error,
                true);
            ExpectedDiagnostics.Add(new DiagnosticResult(expected)
                .WithSpan(10, 15, 10, 23) // TODO: can this be more readable?
                .WithArguments("TYPENAME"));

            await RunAsync();
        }

        [TestMethod]
        public async Task NotMathcingClassNameTest()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(""NOT_TYPENAME"")]
        class TYPENAME { }
    }";

            var expected = new DiagnosticDescriptor(
                "DoNotRenameAnalyzer_NotMatchingClassName",
                "Class should not be renamed",
                "Class 'TYPENAME' does not match DoNotRenameClassAttribute 'className' argument value",
                "Naming",
                DiagnosticSeverity.Error,
                true);
            ExpectedDiagnostics.Add(new DiagnosticResult(expected)
                .WithSpan(7, 15, 7, 23) // TODO: can this be more readable?
                .WithArguments("TYPENAME"));

            await RunAsync();
        }

        [TestMethod]
        public async Task NotMathcingClassNameWithReasonTest()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(""NOT_TYPENAME"", ""REASON"")]
        class TYPENAME { }
    }";

            var expected = new DiagnosticDescriptor(
                "DoNotRenameAnalyzer_NotMatchingClassNameWithReason",
                "Class should not be renamed",
                "Class 'TYPENAME' does not match DoNotRenameClassAttribute 'className' argument value. Reason: REASON",
                "Naming",
                DiagnosticSeverity.Error,
                true);
            ExpectedDiagnostics.Add(new DiagnosticResult(expected)
                .WithSpan(7, 15, 7, 23) // TODO: can this be more readable?
                .WithArguments("TYPENAME"));

            await RunAsync();
        }

        [TestMethod]
        public async Task NotStringLiteralTest_AddExpression()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(""TYPE"" + ""NAME"")]
        class TYPENAME { }
    }";

            var expected = new DiagnosticDescriptor(
                "DoNotRenameAnalyzer_NotStringLiteralArgumentValue", 
                "Class should not be renamed",
                "DoNotRenameClassAttribute constructor argument 'className' should only be set with a string litteral",
                "Naming",
                DiagnosticSeverity.Error,
                true);
            ExpectedDiagnostics.Add(new DiagnosticResult(expected)
                .WithSpan(6, 27, 6, 42)); // TODO: can this be more readable?

            await RunAsync();
        }

        [TestMethod]
        public async Task NotStringLiteralTest_ConstantStringVariable()
        {
            TestCode = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(TYPENAME.CLASS_NAME)]
        class TYPENAME
        {
            public const string CLASS_NAME = ""TYPENAME"";
        }
    }";

            var expected = new DiagnosticDescriptor(
                "DoNotRenameAnalyzer_NotStringLiteralArgumentValue",
                "Class should not be renamed",
                "DoNotRenameClassAttribute constructor argument 'className' should only be set with a string litteral",
                "Naming",
                DiagnosticSeverity.Error,
                true);
            ExpectedDiagnostics.Add(new DiagnosticResult(expected)
                .WithSpan(6, 27, 6, 46)); // TODO: can this be more readable?

            await RunAsync();
        }
    }
}
