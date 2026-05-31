# PROPGRID

A lightweight WPF property grid control for .NET Framework 4.8.  
Wraps the Workflow Foundation `PropertyInspector` to give you a Visual Studio-style property grid that drops into any WPF application.

---

## Background

WPF shipped without a built-in property grid. The Windows Forms `PropertyGrid` can be hosted via `WindowsFormsHost`, but it looks out of place and drags in WinForms dependencies. Meanwhile, the Workflow Foundation designer has a perfectly good property inspector sitting inside `System.Activities.Presentation` -- it just was never exposed as a standalone control.

PROPGRID wraps that inspector in a single `Grid`-derived class (`WpfPropertyGrid`) and exposes a clean public API: set `SelectedObject`, toggle a few properties, and you have a fully functional property grid with categories, sorting, description help text, and support for custom editors.

This is a fork of Jaime Olivares' [wpf-propertygrid](https://github.com/jaime-olivares/wpf-propertygrid) with additional features including theme/color customization, a `SelectedPropertyChanged` event, and bug fixes.

## Requirements

- **.NET Framework 4.8** or later (SDK-style project)
- **Windows only** -- WPF is a Windows desktop technology

> **Note about Limitations:** This control depends on `System.Activities.Presentation`, which is part of .NET Framework's Workflow Foundation. These assemblies are **not available in .NET Core / .NET 5+**, so the control is limited to .NET Framework targets.

## Getting Started

### Add the project reference

Add a reference to `PROPGRID.csproj` from your WPF application project:

```xml
<ItemGroup>
  <ProjectReference Include="..\PROPGRID\PROPGRID.csproj" />
</ItemGroup>
```

Or reference the compiled `PROPGRID.DLL` directly.

Your project also needs to reference the Workflow Foundation assemblies (these are GAC assemblies on any Windows machine with .NET Framework 4.8):

```xml
<ItemGroup>
  <Reference Include="System.Activities" />
  <Reference Include="System.Activities.Presentation" />
  <Reference Include="System.Activities.Core.Presentation" />
</ItemGroup>
```

### Add the namespace in XAML

```xml
<Window ...
        xmlns:pg="clr-namespace:PROPGRID;assembly=PROPGRID">
```

### Drop in the control

```xml
<pg:WpfPropertyGrid x:Name="PropertyGrid1"
                    HelpVisible="True"
                    ToolbarVisible="True"
                    PropertySort="CategorizedAlphabetical" />
```

### Set the selected object in code-behind

```csharp
PropertyGrid1.SelectedObject = myViewModel;
```

That's it. The grid will reflect all public, browsable properties on the object using standard `System.ComponentModel` attributes.

## Supported Attributes

The grid respects the standard .NET property attributes you're already familiar with:

| Attribute | Effect |
|-----------|--------|
| `[Category("...")]` | Groups the property under a collapsible category header |
| `[DisplayName("...")]` | Overrides the property name shown in the grid |
| `[Description("...")]` | Shown in the help text area when the property is selected |
| `[Browsable(false)]` | Hides the property from the grid |
| `[ReadOnly(true)]` | Makes the property non-editable |
| `[Editor(typeof(MyEditor), typeof(PropertyValueEditor))]` | Assigns a custom inline, popup, or dialog editor |

## API Reference

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SelectedObject` | `object` | `null` | The object whose properties are displayed. Set to `null` to clear. |
| `SelectedObjects` | `object[]` | `[]` | For multi-object editing. Common properties are shown; differing values appear blank. |
| `HelpVisible` | `bool` | `false` | Shows or hides the description panel at the bottom of the grid. |
| `ToolbarVisible` | `bool` | `true` | Shows or hides the categorize/alphabetize toolbar. |
| `PropertySort` | `PropertySort` | `CategorizedAlphabetical` | Controls property ordering. Values: `NoSort`, `Alphabetical`, `Categorized`, `CategorizedAlphabetical`. |
| `FontAndColorData` | `string` (set-only) | -- | Raw XAML string for `WorkflowDesigner.PropertyInspectorFontAndColorData`. Prefer the theme brush properties below. |

### Theme Properties

All theme properties are `Brush`-typed dependency properties and support data binding:

| Property | Controls |
|----------|----------|
| `GridBackground` | Main background of the property list area |
| `GridBorderBrush` | Border around the grid |
| `TextForeground` | Default text color for property names and values |
| `CategoryHeaderForeground` | Text color of category headers |
| `ToolBarBackground` | Background of the sort/categorize toolbar |
| `SelectedBackground` | Highlight color for the currently focused row |
| `SelectedForeground` | Text color for the currently focused row |

Example -- dark theme in XAML:

```xml
<pg:WpfPropertyGrid x:Name="PropertyGrid1"
                    GridBackground="#1E1E1E"
                    TextForeground="#DCDCDC"
                    CategoryHeaderForeground="#569CD6"
                    SelectedBackground="#264F78"
                    SelectedForeground="#FFFFFF"
                    ToolBarBackground="#2D2D30"
                    GridBorderBrush="#3F3F46" />
```

### Events

**`SelectedPropertyChanged`** -- Raised when the user focuses a different property row. The event args (`PropertySelectedEventArgs`) provide:

- `PropertyName` -- the CLR property name (e.g., `"FirstName"`)
- `DisplayName` -- the display name from `[DisplayName]`, or the CLR name if not set
- `Description` -- the text from `[Description]`, or empty string

```csharp
PropertyGrid1.SelectedPropertyChanged += (sender, e) =>
{
    if (e.PropertyName != null)
        StatusBar.Text = $"{e.DisplayName}: {e.Description}";
};
```

### Methods

| Method | Description |
|--------|-------------|
| `RefreshPropertyList()` | Forces the grid to re-read the selected object's properties. Useful after `ICustomTypeDescriptor` changes or dynamic property list updates. |

## Multi-Object Editing

Pass an array of objects to `SelectedObjects` to edit common properties across multiple instances. The type label will show the type name with `<multiple>` appended when all objects are the same type, or `Object <multiple>` when they differ.

```csharp
PropertyGrid1.SelectedObjects = new object[] { vehicle1, vehicle2 };
```

## Dynamic Property Lists

If your class implements `ICustomTypeDescriptor`, you can control which properties appear at runtime. When the property set changes (e.g., based on the value of another property), call `RefreshPropertyList()` to update the grid.

The demo application shows this with a `Vehicle` class that conditionally shows cargo-related properties only when the vehicle type is `Pickup` or `Truck`:

```csharp
// In the Vehicle class -- ICustomTypeDescriptor.GetProperties()
public PropertyDescriptorCollection GetProperties()
{
    var props = new PropertyDescriptorCollection(null);
    foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this, true))
    {
        if (prop.Category == "Capacity"
            && TypeOfCar != CarType.Pickup
            && TypeOfCar != CarType.Truck)
            continue;
        props.Add(prop);
    }
    return props;
}

// Listen for changes and refresh
vehicle.PropertyChanged += (s, e) => PropertyGrid1.RefreshPropertyList();
```

## Custom Editors

The grid supports the Workflow Foundation custom editor infrastructure. Derive from one of:

- `PropertyValueEditor` -- inline editor only
- `ExtendedPropertyValueEditor` -- inline editor with a dropdown popup
- `DialogPropertyValueEditor` -- inline editor with a `[...]` button that opens a dialog

Apply the editor to a property with the `[Editor]` attribute:

```csharp
[Editor(typeof(CountryEditor), typeof(PropertyValueEditor))]
public CountryInfo Country { get; set; }
```

The `demo/` folder contains working examples of both `ExtendedPropertyValueEditor` (cascading continent/country dropdowns) and `DialogPropertyValueEditor` (image picker dialog).

## Demo Application

The `demo/` directory contains a standalone WPF application that exercises all of the control's features:

- Single and multi-object selection
- Dynamic property lists with `ICustomTypeDescriptor`
- Custom popup and dialog editors
- Sort mode switching
- Help panel toggle
- `SelectedPropertyChanged` event handling

To build and run the demo, open `demo/PROPGRID-DEMO.sln` in Visual Studio and press F5.

## Project Structure

```
PROPGRID/
  WpfPropertyGrid.cs      Core control (single file)
  PROPGRID.csproj          Library project (.NET Framework 4.8)
  demo/
    PROPGRID-DEMO.sln      Demo solution (includes both projects)
    PROPGRID-DEMO.csproj    Demo application project
    MainWindow.xaml/.cs     Demo window with sidebar controls
    DemoClasses.cs          Sample model classes (Person, Vehicle, Place)
    CustomEditor.cs         Custom editor examples (country picker, image picker)
```

## Known Limitations

- **No .NET Core / .NET 5+ support.** The control depends on `System.Activities.Presentation` from Workflow Foundation, which was never ported to .NET Core. This is a hard dependency with no workaround.
- **Reflection-based internals.** The control uses reflection to access non-public members of the `PropertyInspector`. Future .NET Framework servicing updates could theoretically break this, though the Workflow Foundation surface has been stable for years.
- **Theme brushes must be `SolidColorBrush`.** The `WorkflowDesignerColors` resource keys expect solid color brushes. Gradient or image brushes will be silently ignored.

## License and Attribution

Originally created by [Jaime Olivares](https://github.com/jaime-olivares/wpf-propertygrid) (2011-2024).  
