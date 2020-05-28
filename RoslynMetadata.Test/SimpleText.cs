using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace RoslynMetadata.Test
{
    [TestClass]
    public class SimpleText
    {
        private string GetCode()
        {
            return @"namespcace Person
                     { 
                        public class Customer 
                        {
                            public string FullName 
                            {
                                get
                                {
                                    return $""{FirstName} {LastName}"";
                                }
                            }
                            public DateTime FullName {get; set;}
                        }
                      }";
        }

        [TestMethod]
        public void CompilerClassOk()
        {

        }

        private void Compiler()
        {
            var assemblyName = Path.GetRandomFileName();
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToList()
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(GetCode()) },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                throw new Exception(string.Join(",", failures.Select(f => $"{f.Id}: {f.GetMessage()}")));
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                var assemblyContext = new SimpleUnloadbleAssembly();
                source.Assembly = assemblyContext.LoadFromStream(ms);
                assemblyContext.Unload();
            }
        }
    }
}
