using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VPet_Simulator.Core;
using VPet_Simulator.Windows;
using VPet_Simulator.Windows.Interface;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Data;
using Path = System.IO.Path;
using File = System.IO.File;
using Color = System.Windows.Media.Color;

namespace VPet.Plugin.SearchBoxForMod
{
    public class SearchBoxForMod : MainPlugin
    {
        private string path;
        private string lastItemName;
        private int attempts = 0;

        private ListBox listMod;
        private TextBox searchBox;
        private Label labelMods;

        public List<string> favorities = new List<string>();
        public List<string> disabledMods = new List<string>();
        private List<CoreMOD> CoreMODs = new List<CoreMOD>();

        public override string PluginName => nameof(SearchBoxForMod);

        public SearchBoxForMod(IMainWindow mainwin)
          : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            this.path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            this.LoadFavorities();
            this.EditListMod();
        }

        private void LoadFavorities()
        {
            string filePath = Path.Combine(this.path, "stars.txt");
            if (!File.Exists(filePath)) return;
            foreach (string line in File.ReadAllLines(filePath))
                this.favorities.Add(line);
        }

        private async void EditListMod()
        {
            WindowX winGameSettings = null;
            foreach (object winX in Application.Current.Windows)
                if (winX.ToString() == "VPet_Simulator.Windows.winGameSetting")
                    winGameSettings = (WindowX)winX;

            if (winGameSettings == null)
            {
                await Task.Delay(1000);
                if (this.attempts <= 10)
                    this.EditListMod();
                this.attempts++;
                return;
            }

            ListBox listBox = (ListBox)winGameSettings.FindName("ListMod");
            if (listBox == null) return;
            this.listMod = listBox;
            this.CreateSearchBox();
            this.GetEnabledMods();
            this.LoadMods();
            this.listMod.SelectionChanged += UpdateLastItemName;

            TextBlock buttonEnable = (TextBlock)winGameSettings.FindName("ButtonEnable");
            if (buttonEnable != null) buttonEnable.MouseDown += ButtonEnable_MouseDown;

            TextBlock buttonDisEnable = (TextBlock)winGameSettings.FindName("ButtonDisEnable");
            if (buttonDisEnable != null) buttonDisEnable.MouseDown += ButtonEnable_MouseDown;
        }

        private void ButtonEnable_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.lastItemName == null) return;
            CoreMOD mod = this.CoreMODs.FirstOrDefault(m => m.Name == this.lastItemName);
            if (mod == null) return;
            if (mod.IsOn)
            {
                mod.Tag.Add("该模组已停用");
                mod.Tag.Add("disabled");
                mod.Tag.Remove("enabled");
                mod.IsOn = false;
            }
            else
            {
                mod.Tag.Remove("该模组已停用");
                mod.Tag.Remove("disabled");
                mod.Tag.Add("enabled");
                mod.IsOn = true;
            }
            this.ShowModList();
        }

        private void LoadMods()
        {
            foreach (DirectoryInfo di in this.MW.MODPath)
            {
                if (!File.Exists(di.FullName + @"\info.lps"))
                    continue;
                this.CoreMODs.Add(new CoreMOD(di, this));
            }
            this.ShowModList();
        }

        private void GetEnabledMods()
        {
            foreach (ListBoxItem item in listMod.Items)
            {
                if (item.Foreground.ToString() == "#FF646464")
                    this.disabledMods.Add(item.Content.ToString());
            }
        }

        private void CreateSearchBox()
        {
            TextBox searchBox = new TextBox();
            searchBox.Name = "ModSearchBox";
            searchBox.Height = 30;
            searchBox.VerticalAlignment = VerticalAlignment.Top;
            searchBox.TextChanged += ShowModList_TextChanged;
            searchBox.FontSize = 16;
            TextBoxHelper.SetWatermark(searchBox, "Search mods".Translate());
            searchBox.BorderThickness = new Thickness(0, 0, 0, 1);
            TextBoxHelper.SetCornerRadius(searchBox, new CornerRadius(0));
            searchBox.BorderBrush = Brushes.LightGray;
            searchBox.Style = (Style)Application.Current.FindResource("StandardTextBoxStyle");
            searchBox.Margin = new Thickness(3, 6, 7, 0);

            listMod.Margin = new Thickness(0, 40, 0, 0);
            Grid grid = (Grid)listMod.Parent;
            this.searchBox = searchBox;

            grid.Children.Insert(0, searchBox);
            this.CreateGridSplitter(grid);
            this.CreateModsCount(grid);
        }

        private void CreateGridSplitter(Grid grid)
        {
            GridSplitter gridSplitter = new GridSplitter();
            gridSplitter.Background = Brushes.LightGray;
            gridSplitter.Margin = new Thickness(3, 0, 0, 0);
            
            ControlTemplate controlTemplate = new ControlTemplate(typeof(GridSplitter));
            
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            FrameworkElementFactory rectangleFactory = new FrameworkElementFactory(typeof(Rectangle));
            rectangleFactory.SetValue(Rectangle.MarginProperty, new Thickness(2, 0, 0, 0));
            rectangleFactory.SetValue(Rectangle.WidthProperty, 1.0);
            rectangleFactory.SetBinding(Rectangle.FillProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

            borderFactory.AppendChild(rectangleFactory);
            controlTemplate.VisualTree = borderFactory;
            gridSplitter.Template = controlTemplate;

            grid.Children.Insert(2, gridSplitter);
        }

        private void CreateModsCount(Grid grid)
        {
            Label labelCount = new Label();
            labelCount.HorizontalAlignment = HorizontalAlignment.Right;
            labelCount.VerticalAlignment = VerticalAlignment.Bottom;
            labelCount.FontSize = 10;
            labelCount.Foreground = (Brush)Application.Current.FindResource("SecondaryText");
            labelCount.FontWeight = FontWeights.Bold;
            labelCount.Opacity = 0.6f;
            labelCount.Background = (Brush)Application.Current.FindResource("PrimaryLight");
            labelCount.Content = $"{this.CoreMODs.Count}/{this.CoreMODs.Count}";
            this.labelMods = labelCount;
            grid.Children.Add(labelCount);
        }

        private void ShowModList_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.ShowModList();
        }

        private void ShowModList()
        {
            string searchText = this.searchBox.Text.Trim();
            string[] args = searchText.Split(" ");

            string[] desc = args.Where(e => new string[] { "@", "#", "=" }.All(s => !e.StartsWith(s.ToLower()))).ToArray();

            string[] authors = args.Where(e => e.StartsWith("@")).ToArray();
            string[] tags = args.Where(e => e.StartsWith("#")).ToArray();
            string[] verEquals = args.Where(e => e.StartsWith("=")).ToArray();

            List<CoreMOD> filtered = this.CoreMODs;

            if (desc.Length > 0)
                filtered = filtered.Where(m => 
                    desc.All(d => m.Name.ToLower().Contains(d.ToLower())) ||
                    desc.All(d => m.Name.Translate().ToLower().Contains(d.ToLower())) ||
                    desc.All(d => m.Intro.Translate().ToLower().Contains(d.ToLower()))
                ).ToList();
            if (authors.Length > 0)
                filtered = filtered.Where(m => authors.All(a => m.Author.ToLower().Contains(a.Substring(1).ToLower()))).ToList();
            if (tags.Length > 0)
                filtered = filtered.Where(m => tags.All(t => m.Tag.Contains(t.Substring(1).ToLower()))).ToList();
            if (verEquals.Length > 0)
                filtered = filtered.Where(m => verEquals.All(v => m.GameVer.ToString().Contains(v.Substring(1).Replace(".", "")))).ToList();

            filtered = filtered.OrderByDescending(mod => mod.IsStar).ThenByDescending(mod => mod.IsOn).ThenBy(mod => mod.Name).ToList();
            this.labelMods.Content = $"{filtered.Count}/{this.CoreMODs.Count}";

            this.listMod.Items.Clear();
            foreach (CoreMOD mod in filtered)
            {
                ListBoxItem moditem = (ListBoxItem)this.listMod.Items[this.listMod.Items.Add(new ListBoxItem())];
                moditem.Padding = new Thickness(5, 0, 5, 0);
                moditem.Content = mod.Name;

                if (!mod.IsOn)
                {
                    if (mod.IsStar)
                        moditem.Foreground = new SolidColorBrush(Color.FromRgb(195, 199, 111));
                    else
                        moditem.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else
                {
                    if (mod.IsStar)
                        moditem.Foreground = new SolidColorBrush(Color.FromRgb(225, 235, 0));
                    else if (mod.GameVer / 1000 == this.MW.version / 1000)
                        moditem.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                    else if (mod.Tag.Contains("plugin"))
                        moditem.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                }

                ContextMenu contextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = mod.IsStar ? "Remove from favorites".Translate() : "Add to favorites".Translate();
                menuItem.Tag = mod.Name;
                menuItem.Click += ChangeFavorites;
                contextMenu.Items.Add(menuItem);
                moditem.ContextMenu = contextMenu;
            }
        }

        private void UpdateLastItemName(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItem == null) return;

            ListBoxItem moditem = listBox.SelectedItem as ListBoxItem;
            if (moditem == null) return;

            string name = moditem.Content.ToString();
            this.lastItemName = name;
        }

        private void ChangeFavorites(object sender, EventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            string name = menuItem.Tag.ToString();
            CoreMOD mod = this.CoreMODs.Find(x => x.Name == name);
            if (mod == null) return;
            mod.IsStar = !mod.IsStar;
            menuItem.Header = mod.IsStar ? "Remove from favorites".Translate() : "Add to favorites".Translate();
            if (mod.IsStar)
                this.favorities.Add(mod.Name);
            else
                this.favorities.Remove(mod.Name);
            this.SaveFavorities();
            this.ShowModList();
        }

        private void SaveFavorities()
        {
            string filePath = Path.Combine(this.path, "stars.txt");
            File.WriteAllLines(filePath, this.favorities);
        }

        private T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}