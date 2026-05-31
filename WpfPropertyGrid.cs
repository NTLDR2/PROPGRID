// *********************************************************************
// PLEASE DO NOT REMOVE THIS DISCLAIMER
//
// WpfPropertyGrid - By Jaime Olivares
// Copyright (c) 2011 - 2024
// Code repository: https://github.com/jaime-olivares/wpf-propertygrid
//
// *********************************************************************

using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Xaml;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PROPGRID
{
    public enum PropertySort
    {
        NoSort = 0,
        Alphabetical = 1,
        Categorized = 2,
        CategorizedAlphabetical = 3
    }

    /// <summary>Event arguments for the SelectedPropertyChanged event.</summary>
    public class PropertySelectedEventArgs : RoutedEventArgs
    {
        /// <summary>The CLR property name (e.g., "FirstName"). Null when selection is cleared.</summary>
        public string PropertyName { get; }

        /// <summary>The display name from [DisplayName] attribute, or the CLR name if no attribute exists.</summary>
        public string DisplayName { get; }

        /// <summary>The description from [DescriptionAttribute], or empty string if none.</summary>
        public string Description { get; }

        public PropertySelectedEventArgs(RoutedEvent routedEvent, string propertyName, string displayName, string description)
            : base(routedEvent)
        {
            PropertyName = propertyName;
            DisplayName = displayName;
            Description = description;
        }
    }

    /// <summary>WPF Native PropertyGrid class, uses Workflow Foundation's PropertyInspector</summary>
    public class WpfPropertyGrid : Grid
    {
        #region Private fields
        private WorkflowDesigner Designer;
        private MethodInfo RefreshMethod;
        private MethodInfo OnSelectionChangedMethod;
        private MethodInfo IsInAlphaViewMethod;
        private TextBlock SelectionTypeLabel;
        private Control PropertyToolBar;
        private Border HelpText;
        private GridSplitter Splitter;
        private double HelpTextHeight = 60;
        private string _lastSelectedPropertyName;
        #endregion

        #region Public properties
        /// <summary>Get or sets the selected object. Can be null.</summary>
        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }
        /// <summary>Get or sets the selected object collection. Returns empty array by default.</summary>
        public object[] SelectedObjects
        {
            get { return GetValue(SelectedObjectsProperty) as object[]; }
            set { SetValue(SelectedObjectsProperty, value); }
        }
        /// <summary>XAML information with PropertyGrid's font and color information</summary>
        /// <seealso>Documentation for WorkflowDesigner.PropertyInspectorFontAndColorData</seealso>
        public string FontAndColorData
        {
            set 
            { 
                Designer.PropertyInspectorFontAndColorData = value; 
            }
        }
        /// <summary>Shows the description area on the top of the control</summary>
        public bool HelpVisible
        {
            get { return (bool)GetValue(HelpVisibleProperty); }
            set { SetValue(HelpVisibleProperty, value); }
        }
        /// <summary>Shows the toolbar on the top of the control</summary>
        public bool ToolbarVisible
        {
            get { return (bool)GetValue(ToolbarVisibleProperty); }
            set { SetValue(ToolbarVisibleProperty, value); }
        }
        public PropertySort PropertySort
        {
            get { return (PropertySort)GetValue(PropertySortProperty); }
            set { SetValue(PropertySortProperty, value); }
        }

        #region Theme Customization Properties
        public Brush GridBackground
        {
            get { return (Brush)GetValue(GridBackgroundProperty); }
            set { SetValue(GridBackgroundProperty, value); }
        }
        public Brush GridBorderBrush
        {
            get { return (Brush)GetValue(GridBorderBrushProperty); }
            set { SetValue(GridBorderBrushProperty, value); }
        }
        public Brush TextForeground
        {
            get { return (Brush)GetValue(TextForegroundProperty); }
            set { SetValue(TextForegroundProperty, value); }
        }
        public Brush CategoryHeaderForeground
        {
            get { return (Brush)GetValue(CategoryHeaderForegroundProperty); }
            set { SetValue(CategoryHeaderForegroundProperty, value); }
        }
        public Brush ToolBarBackground
        {
            get { return (Brush)GetValue(ToolBarBackgroundProperty); }
            set { SetValue(ToolBarBackgroundProperty, value); }
        }
        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }
        public Brush SelectedForeground
        {
            get { return (Brush)GetValue(SelectedForegroundProperty); }
            set { SetValue(SelectedForegroundProperty, value); }
        }
        #endregion
        #endregion

        #region Dependency properties registration
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedObjectPropertyChanged));

        public static readonly DependencyProperty SelectedObjectsProperty =
            DependencyProperty.Register("SelectedObjects", typeof(object[]), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(new object[0], FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedObjectsPropertyChanged, CoerceSelectedObjects));

        public static readonly DependencyProperty HelpVisibleProperty =
            DependencyProperty.Register("HelpVisible", typeof(bool), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HelpVisiblePropertyChanged));
        public static readonly DependencyProperty ToolbarVisibleProperty =
            DependencyProperty.Register("ToolbarVisible", typeof(bool), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ToolbarVisiblePropertyChanged));
        public static readonly DependencyProperty PropertySortProperty =
            DependencyProperty.Register("PropertySort", typeof(PropertySort), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(PropertySort.CategorizedAlphabetical, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertySortPropertyChanged));

        #region Theme Dependency Properties registration
        public static readonly DependencyProperty GridBackgroundProperty =
            DependencyProperty.Register("GridBackground", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        public static readonly DependencyProperty GridBorderBrushProperty =
            DependencyProperty.Register("GridBorderBrush", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        public static readonly DependencyProperty TextForegroundProperty =
            DependencyProperty.Register("TextForeground", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        public static readonly DependencyProperty CategoryHeaderForegroundProperty =
            DependencyProperty.Register("CategoryHeaderForeground", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        public static readonly DependencyProperty ToolBarBackgroundProperty =
            DependencyProperty.Register("ToolBarBackground", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        public static readonly DependencyProperty SelectedForegroundProperty =
            DependencyProperty.Register("SelectedForeground", typeof(Brush), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, ThemePropertyChanged));
        #endregion

        /// <summary>Identifies the SelectedPropertyChanged routed event.</summary>
        public static readonly RoutedEvent SelectedPropertyChangedEvent =
            EventManager.RegisterRoutedEvent(
                "SelectedPropertyChanged",
                RoutingStrategy.Bubble,
                typeof(EventHandler<PropertySelectedEventArgs>),
                typeof(WpfPropertyGrid));

        /// <summary>Raised when the user focuses a different property row in the grid.</summary>
        public event EventHandler<PropertySelectedEventArgs> SelectedPropertyChanged
        {
            add { AddHandler(SelectedPropertyChangedEvent, value); }
            remove { RemoveHandler(SelectedPropertyChangedEvent, value); }
        }
        #endregion

        #region Dependency properties events
        private static object CoerceSelectedObject(DependencyObject d, object value)
        {
            WpfPropertyGrid pg = d as WpfPropertyGrid;

            object[] collection = pg.GetValue(SelectedObjectsProperty) as object[];

            return collection.Length == 0 ? null : value;
        }
        private static object CoerceSelectedObjects(DependencyObject d, object value)
        {
            WpfPropertyGrid pg = d as WpfPropertyGrid;

            object single = pg.GetValue(SelectedObjectProperty);

            return single == null ? new object[0] : new object[] { single };
        }

        private static void SelectedObjectPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            pg.CoerceValue(SelectedObjectsProperty);

            pg.SelectionTypeLabel.Text = e.NewValue == null ? string.Empty : e.NewValue.GetType().Name;
            pg.ChangeHelpText(string.Empty, string.Empty);
            pg._lastSelectedPropertyName = null;
            pg.RaiseEvent(new PropertySelectedEventArgs(SelectedPropertyChangedEvent, null, null, null));
        }
        private static void SelectedObjectsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;

            object[] collection = e.NewValue as object[];

            if (collection.Length == 0)
            {
                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { null });
                pg.SelectionTypeLabel.Text = string.Empty;
            }
            else
            {
                bool same = true;
                Type first = null;

                var context = new EditingContext();
                var mtm = new ModelTreeManager(context);
                Selection selection = null;

                // Accumulates the selection and determines the type to be shown in the top of the PG
                for (int i = 0; i < collection.Length; i++)
                {
                    mtm.Load(collection[i]);
                    if (i == 0)
                    {
                        selection = Selection.Select(context, mtm.Root);
                        first = collection[0].GetType();
                    }
                    else
                    {
                        selection = Selection.Union(context, mtm.Root);
                        if (!collection[i].GetType().Equals(first))
                            same = false;
                    }
                }

                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { selection });
                pg.SelectionTypeLabel.Text = collection.Length == 1 ? first.Name : (same ? first.Name + " <multiple>" : "Object <multiple>");
            }

            pg.ChangeHelpText(string.Empty, string.Empty);
            pg._lastSelectedPropertyName = null;
            pg.RaiseEvent(new PropertySelectedEventArgs(SelectedPropertyChangedEvent, null, null, null));
        }
        private static void HelpVisiblePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;

            if (e.NewValue != e.OldValue)
            {
                if (e.NewValue.Equals(true))
                {
                    pg.RowDefinitions[1].Height = new GridLength(5);
                    pg.RowDefinitions[2].Height = new GridLength(pg.HelpTextHeight);
                }
                else
                {
                    pg.HelpTextHeight = pg.RowDefinitions[2].Height.Value;
                    pg.RowDefinitions[1].Height = new GridLength(0);
                    pg.RowDefinitions[2].Height = new GridLength(0);
                }
            }        
        }
        private static void ToolbarVisiblePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            pg.PropertyToolBar.Visibility = e.NewValue.Equals(true) ? Visibility.Visible : Visibility.Collapsed;
        }
        private static void PropertySortPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            PropertySort sort = (PropertySort)e.NewValue;

            bool isAlpha = (sort == PropertySort.Alphabetical || sort == PropertySort.NoSort);
            pg.IsInAlphaViewMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { isAlpha });
        }
        private static void ThemePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            pg.UpdateTheme();
        }
        #endregion

        /// <summary>Default constructor, creates the UIElements including a PropertyInspector</summary>
        public WpfPropertyGrid()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition());
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });

            this.Designer = new WorkflowDesigner();
            TextBlock title = new TextBlock()
            {
                Visibility = Visibility.Visible,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeights.Bold
            };
            TextBlock descrip = new TextBlock()
            {
                Visibility = Visibility.Visible,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel dock = new DockPanel()
            {
                Visibility = Visibility.Visible,
                LastChildFill = true,
                Margin = new Thickness(3,0,3,0)
            };

            title.SetValue(DockPanel.DockProperty, Dock.Top);
            dock.Children.Add(title);
            dock.Children.Add(descrip);
            this.HelpText = new Border()
            {
                Visibility = Visibility.Visible,
                BorderBrush = SystemColors.ActiveBorderBrush,
                Background = SystemColors.ControlBrush,
                BorderThickness = new Thickness(1),
                Child = dock
            };
            this.Splitter = new GridSplitter() 
            { 
                Visibility = Visibility.Visible,
                ResizeDirection = GridResizeDirection.Rows, 
                Height = 5, 
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var inspector = Designer.PropertyInspectorView;
            inspector.Visibility = Visibility.Visible;
            inspector.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            this.Splitter.SetValue(Grid.RowProperty, 1);
            this.Splitter.SetValue(Grid.ColumnProperty, 0);

            this.HelpText.SetValue(Grid.RowProperty, 2);
            this.HelpText.SetValue(Grid.ColumnProperty, 0);

            Binding binding = new Binding("Parent.Background");
            title.SetBinding(BackgroundProperty, binding);
            descrip.SetBinding(BackgroundProperty, binding);

            this.Children.Add(inspector);
            this.Children.Add(this.Splitter);
            this.Children.Add(this.HelpText);

            Type inspectorType = inspector.GetType();
            
            this.RefreshMethod = inspectorType.GetMethod("RefreshPropertyList",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.IsInAlphaViewMethod = inspectorType.GetMethod("set_IsInAlphaView",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.OnSelectionChangedMethod = inspectorType.GetMethod("OnSelectionChanged", 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.SelectionTypeLabel = inspectorType.GetMethod("get_SelectionTypeLabel",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Invoke(inspector, new object[0]) as TextBlock;
            this.PropertyToolBar = inspectorType.GetMethod("get_PropertyToolBar",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Invoke(inspector, new object[0]) as Control;
            
            inspectorType.GetEvent("GotFocus").AddEventHandler(this,
                Delegate.CreateDelegate(typeof(RoutedEventHandler), this, "GotFocusHandler", false));

            this.SelectionTypeLabel.Text = string.Empty;

            UpdateTheme();
        }

        /// <summary>Updates the PropertyGrid's properties</summary>
        public void RefreshPropertyList()
        {
            RefreshMethod.Invoke(Designer.PropertyInspectorView, new object[] { false });
        }

        private void UpdateTheme()
        {
            if (Designer == null) return;
            try
            {
                var dict = new ResourceDictionary();
                
                if (GridBackground is SolidColorBrush bg)
                    dict[WorkflowDesignerColors.PropertyInspectorBackgroundBrushKey] = bg;
                if (GridBorderBrush is SolidColorBrush border)
                    dict[WorkflowDesignerColors.PropertyInspectorBorderBrushKey] = border;
                if (TextForeground is SolidColorBrush text)
                    dict[WorkflowDesignerColors.PropertyInspectorTextBrushKey] = text;
                if (CategoryHeaderForeground is SolidColorBrush catFore)
                    dict[WorkflowDesignerColors.PropertyInspectorCategoryCaptionTextBrushKey] = catFore;
                if (ToolBarBackground is SolidColorBrush tbBg)
                    dict[WorkflowDesignerColors.PropertyInspectorToolBarBackgroundBrushKey] = tbBg;
                if (SelectedBackground is SolidColorBrush selBg)
                    dict[WorkflowDesignerColors.PropertyInspectorSelectedBackgroundBrushKey] = selBg;
                if (SelectedForeground is SolidColorBrush selFg)
                    dict[WorkflowDesignerColors.PropertyInspectorSelectedForegroundBrushKey] = selFg;

                var hashTable = new Hashtable();
                foreach (var key in dict.Keys)
                {
                    hashTable.Add(key, dict[key]);
                }
                
                Designer.PropertyInspectorFontAndColorData = XamlServices.Save(hashTable);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to apply PROPGRID theme: " + ex.Message);
            }
        }

        /// <summary>Traps the change of focused property and updates the help text</summary>
        /// <param name="sender">Not used</param>
        /// <param name="args">Points to the source control containing the selected property</param>
        private void GotFocusHandler(object sender, RoutedEventArgs args)
        {
            string title = string.Empty;
            string descrip = string.Empty;
            string propName = null;
            var theSelectedObjects = this.GetValue(SelectedObjectsProperty) as object[];

            if (theSelectedObjects != null && theSelectedObjects.Length > 0)
            {
                Type first = theSelectedObjects[0].GetType();
                for (int i = 1; i < theSelectedObjects.Length; i++)
                {
                    if (!theSelectedObjects[i].GetType().Equals(first))
                    {
                        ChangeHelpText(title, descrip);
                        return;
                    }
                }

                // Bug #3 fix: OriginalSource may not be a FrameworkElement
                if (args.OriginalSource is FrameworkElement fe)
                {
                    object data = fe.DataContext;
                    if (data != null)
                    {
                        PropertyInfo propEntry = data.GetType().GetProperty("PropertyEntry");
                        if (propEntry == null)
                        {
                            propEntry = data.GetType().GetProperty("ParentProperty");
                        }

                        if (propEntry != null)
                        {
                            object propEntryValue = propEntry.GetValue(data, null);
                            if (propEntryValue != null)
                            {
                                propName = propEntryValue.GetType().GetProperty("PropertyName").GetValue(propEntryValue, null) as string;
                                title = propEntryValue.GetType().GetProperty("DisplayName").GetValue(propEntryValue, null) as string;
                                PropertyInfo property = theSelectedObjects[0].GetType().GetProperty(propName);
                                if (property != null)
                                {
                                    object[] attrs = property.GetCustomAttributes(typeof(DescriptionAttribute), true);

                                    if (attrs != null && attrs.Length > 0)
                                        descrip = (attrs[0] as DescriptionAttribute).Description;
                                }
                            }
                        }
                    }
                }

                // Update help text first (must not be affected by consumer event handlers)
                ChangeHelpText(title, descrip);

                // Raise event only when a valid property was resolved and it differs from the last one
                if (propName != null && propName != _lastSelectedPropertyName)
                {
                    _lastSelectedPropertyName = propName;
                    RaiseEvent(new PropertySelectedEventArgs(SelectedPropertyChangedEvent, propName, title, descrip));
                }
            }
        }

        /// <summary>Changes the text help area contents</summary>
        /// <param name="title">Title in bold</param>
        /// <param name="descrip">Description with ellipsis</param>
        private void ChangeHelpText(string title, string descrip)
        {
            DockPanel dock = this.HelpText.Child as DockPanel;
            if (dock != null && dock.Children.Count >= 2)
            {
                (dock.Children[0] as TextBlock).Text = title;
                (dock.Children[1] as TextBlock).Text = descrip;
            }
        }
    }
}
