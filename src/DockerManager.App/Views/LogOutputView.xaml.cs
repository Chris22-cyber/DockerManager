using System.Collections.Specialized;
using System.Windows.Controls;

namespace DockerManager.App.Views;

public partial class LogOutputView : UserControl
{
    public LogOutputView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (LogList.ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += (_, _) =>
                {
                    if (LogList.Items.Count > 0)
                        LogList.ScrollIntoView(LogList.Items[^1]);
                };
            }
        };
    }
}
