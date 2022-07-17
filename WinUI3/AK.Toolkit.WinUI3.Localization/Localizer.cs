﻿using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AK.Toolkit.WinUI3.Localization;

public partial class Localizer : DependencyObject, ILocalizer
{
    private readonly Dictionary<string, StringResources> _languageResources = new();

    private readonly HashSet<UIElement> _rootElements = new();

    private string CurrentLanguage { get; set; } = "en-US";

    public IEnumerable<string> GetAvailableLanguages() => _languageResources.Keys;

    public void Initalize(string resourcesFolderPath, string resourcesFileName = "Resources.resw", string defaultLanguage = "en-US")
    {
        CurrentLanguage = defaultLanguage;

        _languageResources.Clear();

        foreach (string folder in Directory.GetDirectories(resourcesFolderPath))
        {
            string resourceFilePath = Path.Combine(folder, resourcesFileName);

            if (LoadLanguageResources(resourceFilePath) is StringResources resources &&
                new DirectoryInfo(resourceFilePath).Parent?.Name is string languageCode)
            {
                _languageResources.Add(languageCode, resources);
            }
        }

        RegisterDefaultUIElementChildrenGetters();
    }

    public string GetCurrentLanguage() => CurrentLanguage;

    public bool TrySetCurrentLanguage(string language)
    {
        if (GetAvailableLanguages().Contains(language) is true)
        {
            CurrentLanguage = language;
            return true;
        }

        return false;
    }

    public void RegisterRootElement(FrameworkElement rootElement) => _rootElements.Add(rootElement);

    public void RunLocalizationOnRegisteredRootElements(string? language = null)
    {
        foreach (FrameworkElement rootElement in _rootElements)
        {
            RunLocalization(rootElement, language);
        }
    }

    public void RunLocalization(FrameworkElement rootElement, string? language = null)
    {
        language ??= CurrentLanguage;

        if (GetLanguageResources(language) is StringResources resources)
        {
            Localize(rootElement, resources);
        }
    }

    public StringResources? GetLanguageResources(string language)
    {
        if (_languageResources.TryGetValue(language, out StringResources? resources) is true)
        {
            return resources;
        }

        return null;
    }

    public string? GetLocalizedString(string key, string? language = null)
    {
        language ??= CurrentLanguage;

        if (_languageResources.TryGetValue(language, out StringResources? resources) is true &&
            resources.TryGetValue(key, out StringResource? resource) is true)
        {
            return resource.Value;
        }

        return null;
    }

    private void Localize(UIElement element, StringResources resources)
    {
        if (GetUid(element) is string uid &&
            resources.TryGetValue(uid, out StringResource? resource) is true &&
            GetDependencyProperty(element, resource.DependencyPropertyName) is DependencyProperty dependencyProperty)
        {
            element.SetValue(dependencyProperty, resource.Value);
        }

        if (_childrenGetters.TryGetValue(element.GetType(), out var childrenGetter) is true &&
            childrenGetter is not null)
        {
            foreach (UIElement child in childrenGetter(element))
            {
                Localize(child, resources);
            }
        }
    }
}