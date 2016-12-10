﻿namespace Microsoft.VisualStudio.Threading.Analyzers.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;
    using Xunit.Abstractions;

    public class MultiAnalyzerTests : DiagnosticVerifier
    {
        public MultiAnalyzerTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        protected override ImmutableArray<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return ImmutableArray.Create<DiagnosticAnalyzer>(
                new AsyncEventHandlerAnalyzer(),
                new AsyncSuffixAnalyzer(),
                new AsyncVoidLambdaAnalyzer(),
                new AsyncVoidMethodAnalyzer(),
                new AvoidImpliedTaskSchedulerCurrentAnalyzer(),
                new AvoidJtfRunInNonPublicMembersAnalyzer(),
                new JtfRunAwaitTaskAnalyzer(),
                new LazyOfTaskAnalyzer(),
                new SynchronousWaitAnalyzer(),
                new UseAwaitInAsyncMethodsAnalyzer(),
                new VsServiceUsageAnalyzer());
        }

        [Fact]
        public void JustOneDiagnosticPerLine()
        {
            var test = @"
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

class Test {
    JoinableTaskFactory jtf;

    Task<int> FooAsync() {
        Task t = Task.FromResult(1);
        t.GetAwaiter().GetResult(); // VSSDK001, VSSDK008, VSSDK009
        jtf.Run(async delegate { await BarAsync(); }); // VSSDK008, VSSDK009
        return Task.FromResult(1);
    }

    Task BarAsync() => null;
}";

            this.VerifyNoMoreThanOneDiagnosticPerLine(test);
        }

        private void VerifyNoMoreThanOneDiagnosticPerLine(string test)
        {
            this.LogFileContent(test);
            ImmutableArray<DiagnosticAnalyzer> analyzers = this.GetCSharpDiagnosticAnalyzers();
            var actualResults = GetSortedDiagnostics(new[] { test }, LanguageNames.CSharp, analyzers, false);
            string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzers, actualResults.ToArray()) : "    NONE.";
            this.logger.WriteLine("Actual diagnostics:\n" + diagnosticsOutput);

            // Assert that each line only merits at most one diagnostic.
            int lastDiagnosticLine = -1;
            Diagnostic lastDiagnostic = null;
            for (int i = 0; i < actualResults.Length; i++)
            {
                Diagnostic diagnostic = actualResults[i];
                int diagnosticLinePosition = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
                if (lastDiagnosticLine == diagnosticLinePosition)
                {
                    Assert.False(true, $"Both {lastDiagnostic.Id} and {diagnostic.Id} produced diagnostics for line {diagnosticLinePosition + 1}.");
                }

                lastDiagnosticLine = diagnosticLinePosition;
                lastDiagnostic = diagnostic;
            }
        }
    }
}