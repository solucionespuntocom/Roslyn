using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace RoslynMetadata.LocalConsole.Extensions
{
    public static class ValidExtension
    {
        public static bool ValidCustomer(this Customer customer)
        {
            List<ValidResult> valids = new List<ValidResult>();
            var customerRules = GetRules();

            var asm = CompilerRules(customerRules);

            if (asm == null)
                return false;

            foreach (var rule in customerRules)
            {
                valids.Add(asm.ExecuteRule(rule.Key, customer));
 
            }
            foreach (var valid in valids.Where(v => !v.Success))
            {
                Console.WriteLine(valid.Message);
            }
            return !valids.Any(v => !v.Success);
        }

        private static ValidResult ExecuteRule(this Assembly asm, string name, Customer customer)
        {
            ValidResult result = default;
            try
            {
                Type ruleType = asm.GetTypes().FirstOrDefault(t => t.Name == name);
                if (ruleType == null)
                    throw new Exception($"No existe la validación {name}");

                ICustomerRule rule = (ICustomerRule)Activator.CreateInstance(ruleType);
                if(rule == null)
                    throw new Exception($"Error instanciando la validación {name}");

                result = rule.Valid(customer);
            }
            catch (Exception ex)
            {
                result = new ValidResult(false, ex.Message);
            }
            return result;
        }
        private static Dictionary<string, string> GetRules()
        {
            var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}/Validations/Customers", "*.txt");

            return files.ToDictionary(k => Path.GetFileNameWithoutExtension(k), v=>  File.ReadAllText(v));
        }


        private static Assembly CompilerRules(Dictionary<string, string> customerRules)
        {
            Assembly asm = null;
            var assemblyName = Path.GetRandomFileName();

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            references.Add(MetadataReference.CreateFromFile(
                typeof(Regex).Assembly.Location));

            List<SyntaxTree> codeSource = new List<SyntaxTree>();

            foreach (var rule in customerRules)
            {
                codeSource.Add(CSharpSyntaxTree.ParseText(TemplateCodeSyntaxFactory(rule.Key, rule.Value)));
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: codeSource.ToArray(),
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            
            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var failure in failures)
                {
                    Console.WriteLine($"{failure.Id}: {failure.GetMessage()}");
                }
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                var assemblyContext = new AssemblyLoadContext("MyContext", true);
                asm = assemblyContext.LoadFromStream(ms);
                assemblyContext.Unload();
            }
            return asm;

        }

        public static string TemplateCodeSyntaxFactory(string validName, string body)
        {
            var unitCompiler = SyntaxFactory.CompilationUnit()
                .AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text.RegularExpressions")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("RoslynMetadata.LocalConsole")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq"))
                );

            var nameSpace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("CustomerValidations"));

            var @class = SyntaxFactory.ClassDeclaration(validName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeof(ICustomerRule).Name)));

            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(typeof(ValidResult).Name), SyntaxFactory.Identifier("Valid"))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("customer"))
                        .WithType(SyntaxFactory.ParseTypeName("Customer")))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement(body).NormalizeWhitespace()));

            @class = @class.AddMembers(method);
            nameSpace = nameSpace.AddMembers(@class);
            unitCompiler = unitCompiler.AddMembers(nameSpace);

            return unitCompiler
                .NormalizeWhitespace()
                .ToFullString();
                
        }
        public static string TemplateCode(string validName, string body)
        {
            var code = (@"using System;
                      using RoslynMetadata.LocalConsole;
                      using System.Text.RegularExpressions;
                      using System.Linq;

                     namespace CustomerValidations
                     { 
                        public class [CLASSNAME]
                            : ICustomerRule
                        {
                            public ValidResult Valid(Customer customer)
                            {
                                [BODY]
                            }
                        }
                      }").Replace("[CLASSNAME]", validName)
                      .Replace("[BODY]", body);

            return SyntaxFactory.ParseCompilationUnit(code)
                .NormalizeWhitespace()
                .ToFullString();
        }
    }
}
