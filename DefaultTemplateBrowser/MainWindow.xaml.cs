using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using Xavalon.XamlStyler;
using Xavalon.XamlStyler.Options;

namespace DefaultTemplateBrowser;

public partial class MainWindow
{
    private static readonly StylerService StylerService = new(new StylerOptions(""), new XamlLanguageOptions());
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, EventArgs e)
    {
        TypeList.ItemsSource = Assembly.GetAssembly(typeof(Control))!
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Control)) && t is { IsAbstract: false, IsPublic: true })
            .OrderBy(t => t.Name)
            .ToArray();
    }

    private void TypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var type = (Type)TypeList.SelectedItem;
        var info = type.GetConstructor(Type.EmptyTypes);

        Control? control = default;

        try
        {
            control = (Control)info!.Invoke(null);
            control.Visibility = Visibility.Collapsed;

            if (control is Window window)
            {
                window.Width = 0d;
                window.Height = 0d;
                window.WindowStyle = WindowStyle.None;
                window.Background = Brushes.Transparent;
                window.AllowsTransparency = true;
                window.Show();
            }
            else
            {
                Base.Children.Add(control);
            }

            ControlTemplateTextBox.Text = GetControlTemplateXaml(control);
            DefaultStyleTextEditor.Text = GetDefaultStyleXaml(control);
        }
        catch
        {
            ControlTemplateTextBox.Text = "";
            DefaultStyleTextEditor.Text = "";
        }
        finally
        {
            if (control is not null)
            {
                if (control is Window window)
                {
                    window.Close();
                }
                else
                {
                    int index = Base.Children.IndexOf(control);

                    if (index >= 0)
                        Base.Children.RemoveAt(index);
                }
            }
        }
    }

    private static string GetControlTemplateXaml(Control control)
    {
        var template = control.Template;
        return GetXaml(template);
    }

    private static string GetDefaultStyleXaml(Control control)
    {
        var fieldInfo = typeof(FrameworkElement)
            .GetField("DefaultStyleKeyProperty", BindingFlags.Static | BindingFlags.NonPublic);

        if (fieldInfo == null)
            return "";
        
        var defaultStyleKeyProperty = (DependencyProperty)fieldInfo.GetValue(control)!;
        var defaultStyleKey = control.GetValue(defaultStyleKeyProperty);
        var style = (Style)Application.Current.FindResource(defaultStyleKey)!;

        return GetXaml(style);
    }

    private static string GetXaml(object source)
    {
        var settings = new XmlWriterSettings { Indent = true };
        var sb = new StringBuilder();

        using XmlWriter writer = XmlWriter.Create(sb, settings);
        XamlWriter.Save(source, writer);
        return StylerService.StyleDocument(sb.ToString());
    }
}