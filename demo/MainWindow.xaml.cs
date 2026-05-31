using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PROPGRID_DEMO
{
    public partial class MainWindow : Window
    {
        // Sample fields to be inspected via reflection
        private Person Person = new Person();
        private Vehicle Vehicle1 = new Vehicle { Model = "Tacoma", TypeOfCar = Vehicle.CarType.Pickup };
        private Vehicle Vehicle2 = new Vehicle { Model = "Corolla", TypeOfCar = Vehicle.CarType.Sedan };
        private Place Place = new Place();

        // Names must match the private fields above for reflection lookup
        private object[] ItemArray = { "Person", "Vehicle1", "Vehicle2", "Place" };

        public MainWindow()
        {
            InitializeComponent();

            // Register PropertyChanged listener for vehicles to support dynamic property lists
            this.Vehicle1.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Vehicle_PropertyChanged);
            this.Vehicle2.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Vehicle_PropertyChanged);

            // Set initial state
            this.Radio3.IsChecked = true;
            this.NoSelection_Click(this, null);
        }

        // Special handling for vehicle type change to dynamically update property list visibility
        private void Vehicle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.PropertyGrid1.RefreshPropertyList();
        }

        private void SingleSelect_Click(object sender, RoutedEventArgs e)
        {
            this.ItemList.ItemTemplate = this.Resources["RadioButtons"] as DataTemplate;
            this.ItemList.ItemsSource = this.ItemArray;
            this.PropertyGrid1.SelectedObject = null;
        }

        private void MultiSelect_Click(object sender, RoutedEventArgs e)
        {
            this.ItemList.ItemTemplate = this.Resources["CheckBoxes"] as DataTemplate;
            this.ItemList.ItemsSource = this.ItemArray;
            this.PropertyGrid1.SelectedObject = null;
        }

        private void NoSelection_Click(object sender, RoutedEventArgs e)
        {
            this.ItemList.ItemTemplate = null;
            this.ItemList.ItemsSource = new string[] { "(none)" };
            this.PropertyGrid1.SelectedObject = null;
        }

        private void Item_Checked(object sender, RoutedEventArgs e)
        {
            if (e.Source is RadioButton)
            {
                string fieldName = (e.Source as RadioButton).Content.ToString();
                object selected = this.GetType().GetField(fieldName, 
                    BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
                this.PropertyGrid1.SelectedObject = selected;
            }
            else if (e.Source is CheckBox && this.Radio2.IsChecked.GetValueOrDefault())
            {
                ArrayList selected = new ArrayList();

                for (int i = 0; i < ItemList.Items.Count; i++)
                {
                    ContentPresenter container = ItemList.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                    if (container != null)
                    {
                        DataTemplate dataTemplate = container.ContentTemplate;
                        if (dataTemplate != null)
                        {
                            CheckBox chk = (CheckBox)dataTemplate.FindName("chk", container);
                            if (chk != null && chk.IsChecked.GetValueOrDefault())
                            {
                                string fieldName = chk.Content.ToString();
                                object item = this.GetType().GetField(fieldName, 
                                    BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
                                selected.Add(item);
                            }
                        }
                    }
                }
                this.PropertyGrid1.SelectedObjects = selected.ToArray();
            }
        }

        private void PropertyGrid1_SelectedPropertyChanged(object sender, PROPGRID.PropertySelectedEventArgs e)
        {
            if (e.PropertyName != null)
                FocusedPropertyLabel.Text = $"{e.DisplayName} ({e.PropertyName})";
            else
                FocusedPropertyLabel.Text = "(none)";
        }
    }
}
