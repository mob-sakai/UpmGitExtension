using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public class InternalAccessableCompiler
{
	public static void Main(string[] args)
	{
		string inputCsProjPath = args[0];
		string inputCsProjDir = Path.GetDirectoryName(inputCsProjPath);
		string outputAsemblyPath = args[1];
		string outputAsemblyName = Path.GetFileNameWithoutExtension(outputAsemblyPath);

		Console.WriteLine($"Input Project File: {inputCsProjPath}");
		Console.WriteLine($"Output Asembly Path: {outputAsemblyPath}");
		Console.WriteLine($"Output Asembly Name: {outputAsemblyName}");

		var csproj = File.ReadAllLines(inputCsProjPath);

		// CSharpCompilationOptions
		// MetadataImportOptions.All
		var compilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				allowUnsafe: true,
				optimizationLevel: OptimizationLevel.Release,
				deterministic: true
			)
			.WithMetadataImportOptions(MetadataImportOptions.All);

		// BindingFlags.IgnoreAccessibility
		typeof(CSharpCompilationOptions)
			.GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic)
			.SetValue(compilationOptions, (uint)1 << 22);

		// Get all references.
		var reg_dll = new Regex("<HintPath>(.*)</HintPath>", RegexOptions.Compiled);
		var metadataReferences = csproj
			.Select(line => reg_dll.Match(line))
			.Where(match => match.Success)
			.Select(match => match.Groups[1].Value)
			//.Concat (new [] { Assembly.Load (new AssemblyName ("System.Runtime")).Location })
			.Select(path => MetadataReference.CreateFromFile(path));

		// Get all preprocessor symbols.
		var reg_preprocessorSymbols = new Regex("<DefineConstants>(.*)</DefineConstants>", RegexOptions.Compiled);
		var preprocessorSymbols = csproj
			.Select(line => reg_preprocessorSymbols.Match(line))
			.Where(match => match.Success)
			.SelectMany(match => match.Groups[1].Value.Split(';'))
			.Where(x=>x != "DEBUG");

		// Get all source codes.
		var parserOption = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: preprocessorSymbols);
		var reg_cs = new Regex("<Compile Include=\"(.*\\.cs)\"", RegexOptions.Compiled);
		var syntaxTrees = csproj
			.Select(line => reg_cs.Match(line))
			.Where(match => match.Success)
			.Select(match => Path.Combine(inputCsProjDir, match.Groups[1].Value).Replace('\\', Path.DirectorySeparatorChar))
			.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parserOption, path));

		// Start compiling.
		var result = CSharpCompilation.Create(outputAsemblyName, syntaxTrees, metadataReferences, compilationOptions)
			.Emit(outputAsemblyPath);

		// Output compile errors.
		foreach (var d in result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error))
		{
			Console.WriteLine(string.Format("{0} ({1}): {2} {3}", d.Severity, d.Id, d.GetMessage(), d.Location.GetMappedLineSpan()));
		}
		Console.WriteLine(result.Success ? "Success" : "Failed");
	}
}
