﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Python.Runtime;
using PythonSourceGenerator.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace PythonSourceGenerator
{
    [Generator]
    public class PythonStaticGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.PythonVersion", out string pythonVersion))
            {
                System.Console.WriteLine("Testing");
                pythonVersion = "3.12";
            }

            var home = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "PythonSourceGenerator"));

            var environment = new PyEnv(home.FullName, pythonVersion);
            var pyFiles = context.AdditionalFiles.Where(f => f.Path.EndsWith(".py", System.StringComparison.OrdinalIgnoreCase));
            foreach (var file in pyFiles)
            {
                // Add environment path
                environment.AddPath(System.IO.Path.GetDirectoryName(file.Path));
                var @namespace = "Python.Generated"; // TODO : Infer from project

                var fileName = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                // Convert snakecase to pascal case
                var pascalFileName = string.Join("", fileName.Split('_').Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)));
                var pythonSource = file.GetText(context.CancellationToken);
                List<MethodDeclarationSyntax> methods;
                using (Py.GIL())
                {
                    // create a Python scope
                    using (PyModule scope = Py.CreateScope())
                    {
                        var pythonModule = scope.Import(fileName);
                        methods = ModuleReflection.MethodsFromModule(pythonModule, scope);
                    }
                }
                var source = $@"// <auto-generated/>
using PythonEnvironments;

namespace {@namespace}
{{
    public static class {pascalFileName}
    {{
        {methods.Compile()}
    }}
}}
";
                context.AddSource($"{pascalFileName}.py.cs", source);
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("PSG002", "PythonStaticGenerator", $"Generated {pascalFileName}.py.cs", "PythonStaticGenerator", DiagnosticSeverity.Warning, true), Location.None));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // TODO
        }
    }
}