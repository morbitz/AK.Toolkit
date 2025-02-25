﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace AK.Toolkit.WinUI3.GridExtensions;

[Generator(LanguageNames.CSharp)]
internal class GridIndexerGenerator : IIncrementalGenerator
{
    private static string GridIndexerClassName { get; } = nameof(GridIndexerGenerator).Replace("ridIndexerGenerator", "I");

    private static string GeneratedFileExtension { get; } = "xaml.g.cs";

    private List<string> GeneratedFileNames { get; } = new();

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        initContext.RegisterPostInitializationOutput(postInitContext => postInitContext.AddSource(
            $"{GridIndexerClassName}.{GeneratedFileExtension}",
            SourceText.From(CreateGridIndexerClassSourceString(), Encoding.UTF8)));

        IncrementalValueProvider<string> projectDirectory =
            initContext.AnalyzerConfigOptionsProvider.Select(
                (provider, _) => provider.GlobalOptions.TryGetValue("build_property.ProjectDir", out string? projectDirectory) is true
                ? projectDirectory
                : string.Empty);

        initContext.RegisterSourceOutput(projectDirectory, GenerateSources);
    }

    private void GenerateSources(SourceProductionContext sourceProductionContext, string projectDirectory)
    {
        foreach (string xamlFilePath in Directory.EnumerateFiles(projectDirectory, "*.xaml", SearchOption.AllDirectories))
        {
            if (GetXamlStringFromFile(xamlFilePath) is string xamlString &&
                IsUsingGridIndexer(xamlString) is true &&
                GetXamlClassString(xamlString) is string xamlClassName && xamlClassName.Length > 0)
            {
                GeneratePartialClassFile(sourceProductionContext, xamlClassName);
            }
        }
    }

    private void GeneratePartialClassFile(SourceProductionContext sourceProductionContext, string xamlClassName)
    {
        if (GeneratedFileNames.Contains(xamlClassName) is not true)
        {
            string fileName = CreateUniqueFileName(xamlClassName);
            string filePath = $@"{fileName}.{GeneratedFileExtension}";
            string sourceString = CreatePartialClassSourceString(xamlClassName);

            sourceProductionContext.AddSource(filePath, SourceText.From(sourceString, Encoding.UTF8));

            GeneratedFileNames.Add(xamlClassName);
        }
    }

    private string CreatePartialClassSourceString(string xamlClassName)
    {
        (string @namespace, string className) = GetNamespaceAndClassName(xamlClassName);
        return
$@"
// Auto-Generated by {nameof(GridIndexerGenerator)}

using AK.Toolkit.WinUI3.GridExtensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#nullable enable

namespace {@namespace};

public partial class {className}
{{
    private void RunGridIndexer()
    {{
        UpdateRelativeRows(this.Content);
        UpdateRelativeColumns(this.Content);
    }}

    private void UpdateRelativeRows(UIElement parent)
    {{
        int rowIndex = 0;

        if (parent is Grid grid)
        {{
            foreach (UIElement child in grid.Children)
            {{
                if (child is FrameworkElement frameworkElement)
                {{
                    if ({GridIndexerClassName}.GetRowTypeAndValue(frameworkElement) is ({GridIndexerClassName}.ValueType type, int value))
                    {{
                        rowIndex = type switch
                        {{
                            {GridIndexerClassName}.ValueType.Absolute => value,
                            {GridIndexerClassName}.ValueType.Relative => rowIndex + value,
                            _ => value,
                        }};

                        int additionalRowsCount = (rowIndex + 1) - grid.RowDefinitions.Count;

                        for (int i = 0; i < additionalRowsCount; i++)
                        {{
                            grid.RowDefinitions.Add(new RowDefinition());
                        }}

                        Grid.SetRow(frameworkElement, rowIndex);
                    }}
                }}

                UpdateRelativeRows(child);
            }}
        }}
        else if (parent is Panel panel)
        {{
            foreach (UIElement child in panel.Children)
            {{
                UpdateRelativeRows(child);
            }}
        }}
    }}

    private void UpdateRelativeColumns(UIElement parent)
    {{
        int columnIndex = 0;

        if (parent is Grid grid)
        {{
            foreach (UIElement child in grid.Children)
            {{
                if (child is FrameworkElement frameworkElement)
                {{
                    if ({GridIndexerClassName}.GetColumnTypeAndValue(frameworkElement) is ({GridIndexerClassName}.ValueType type, int value))
                    {{
                        columnIndex = type switch
                        {{
                            {GridIndexerClassName}.ValueType.Absolute => value,
                            {GridIndexerClassName}.ValueType.Relative => columnIndex + value,
                            _ => value,
                        }};

                        int additionalColumnsCount = (columnIndex + 1) - grid.ColumnDefinitions.Count;

                        for (int i = 0; i < additionalColumnsCount; i++)
                        {{
                            grid.ColumnDefinitions.Add(new ColumnDefinition());
                        }}

                        Grid.SetColumn(frameworkElement, columnIndex);
                    }}
                }}

                UpdateRelativeColumns(child);
            }}
        }}
        else if (parent is Panel panel)
        {{
            foreach (UIElement child in panel.Children)
            {{
                UpdateRelativeColumns(child);
            }}
        }}
    }}
}}
";
    }

    private (string @namespace, string className) GetNamespaceAndClassName(string xamlClassName)
    {
        string[] subStrings = xamlClassName.Split('.');
        return subStrings.Length switch
        {
            1 => (string.Empty, subStrings[0]),
            > 1 => (xamlClassName.Substring(0, xamlClassName.LastIndexOf('.')), subStrings[subStrings.Length - 1]),
            _ => (string.Empty, string.Empty),
        };
    }

    private string CreateUniqueFileName(string originalClassName)
    {
        string newFileName = originalClassName;
        int suffixNumber = 1;

        while (GeneratedFileNames.Contains(newFileName) is true)
        {
            suffixNumber++;

            if (suffixNumber > 100)
            {
                throw new ArgumentException($"Failed to create unique file name for {originalClassName}");
            }

            newFileName = $@"{originalClassName}-{suffixNumber}";
        }

        return newFileName;
    }

    private static string GetXamlClassString(string xamlString)
    {
        Match match = Regex.Match(xamlString, @"x:Class=""(.*?)""");
        return match.Success is true && match.Groups.Count > 1
            ? match.Groups[1].Value
            : string.Empty;
    }

    private static bool IsUsingGridIndexer(string xamlString)
    {
        return (xamlString.IndexOf($"{GridIndexerClassName}.Row") != -1) ||
               (xamlString.IndexOf($"{GridIndexerClassName}.Column") != -1);
    }

    private static string GetXamlStringFromFile(string xamlFilePath)
    {
        using StreamReader streamReader = new(xamlFilePath);
        return streamReader.ReadToEnd();
    }

    private string CreateGridIndexerClassSourceString()
    {
        return
$@"
// Auto-Generated by {nameof(GridIndexerGenerator)}

using Microsoft.UI.Xaml;
using System;

#nullable enable

namespace AK.Toolkit.WinUI3.GridExtensions;

public class {GridIndexerClassName} : DependencyObject
{{
    public static readonly DependencyProperty RowProperty =
        DependencyProperty.RegisterAttached(
            ""Row"",
            typeof(string),
            typeof({GridIndexerClassName}),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ColumnProperty =
        DependencyProperty.RegisterAttached(
            ""Column"",
            typeof(string),
            typeof({GridIndexerClassName}),
            new PropertyMetadata(null));

    public enum ValueType
    {{
        Absolute,
        Relative
    }}

    public static void SetRow(DependencyObject target, string value) => target.SetValue(RowProperty, value);

    public static string GetRow(DependencyObject target) => (string)target.GetValue(RowProperty);

    public static (ValueType, int)? GetRowTypeAndValue(DependencyObject target) => GetTypeAndValue(target.GetValue(RowProperty) as string);

    public static void SetColumn(DependencyObject target, string value) => target.SetValue(ColumnProperty, value);

    public static string GetColumn(DependencyObject target) => (string)target.GetValue(ColumnProperty);

    public static (ValueType, int)? GetColumnTypeAndValue(DependencyObject target) => GetTypeAndValue(target.GetValue(ColumnProperty) as string);

    private static (ValueType, int)? GetTypeAndValue(string? stringValue)
    {{
        if (stringValue is not null)
        {{
            if ((stringValue.Length > 0) &&
                (stringValue.StartsWith(""+"") is true || stringValue.StartsWith(""-"") is true) &&
                (int.TryParse(stringValue, out var relativeValue) is true))
            {{
                return (ValueType.Relative, relativeValue);
            }}
            else if (int.TryParse(stringValue, out var absoluteValue) is true)
            {{
                return (ValueType.Absolute, absoluteValue);
            }}

            throw new {GridIndexerClassName}InvalidValueException($""{{stringValue}}"");
        }}

        return null;
    }}
}}

public class {GridIndexerClassName}InvalidValueException : Exception
{{
    public {GridIndexerClassName}InvalidValueException(string message) : base(message)
    {{
    }}
}}
";
    }
}