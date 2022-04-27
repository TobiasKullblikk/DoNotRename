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
            var test = @"
    using DoNotRename;

    namespace ConsoleApplication1
    {
        [DoNotRenameClass(""TYPENAME"")]
        class TYPENAME { }
    }";

            TestCode = test;
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
