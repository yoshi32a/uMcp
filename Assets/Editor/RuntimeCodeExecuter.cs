// Assets/Editor/RuntimeCodeExecutor.cs

using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class RuntimeCodeExecutor : EditorWindow
{
    string scriptCode = @"using UnityEngine;
public static class RuntimeCode {
    public static void Run() {
        Debug.Log(""Hello from runtime code!"");
    }
}";

    [MenuItem("Tools/Runtime Code Executor")]
    static void OpenWindow()
    {
        GetWindow<RuntimeCodeExecutor>("Runtime Code Executor");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("C# Code to Execute", EditorStyles.boldLabel);
        scriptCode = EditorGUILayout.TextArea(scriptCode, GUILayout.ExpandHeight(true));

        if (GUILayout.Button("Run"))
        {
            CompileAndRun(scriptCode);
        }
    }

    void CompileAndRun(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Distinct()
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            "RuntimeScript",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var diag in result.Diagnostics)
                Debug.LogError($"[Roslyn Error] {diag}");
            return;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetType("RuntimeCode");
        var method = type?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);

        if (method == null)
        {
            Debug.LogError("No static Run() method found in RuntimeCode class.");
            return;
        }

        method.Invoke(null, null);
    }
}
