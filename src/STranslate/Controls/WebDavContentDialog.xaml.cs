using STranslate.ViewModels.Pages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WebDav;

namespace STranslate.Controls;

public partial class WebDavContentDialog
{
    public WebDavContentDialog(
        WebDavClient client,
        string absolutePath,
        ObservableCollection<WebDavResult> collections)
    {
        InitializeComponent();

        DataContext = this;

        _client = client;
        _absolutePth = absolutePath;
        Collections = collections;
    }

    private readonly WebDavClient _client;
    private readonly string _absolutePth;

    public ObservableCollection<WebDavResult> Collections { get; }

    public ICommand? DownloadCommand
    {
        get => (ICommand?)GetValue(DownloadCommandProperty);
        set => SetValue(DownloadCommandProperty, value);
    }

    public static readonly DependencyProperty DownloadCommandProperty =
        DependencyProperty.Register(
            nameof(DownloadCommand),
            typeof(ICommand),
            typeof(WebDavContentDialog));

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(
            nameof(DeleteCommand),
            typeof(ICommand),
            typeof(WebDavContentDialog));

    //public ICommand? UpdateCommand
    //{
    //    get => (ICommand?)GetValue(UpdateCommandProperty);
    //    set => SetValue(UpdateCommandProperty, value);
    //}

    //public static readonly DependencyProperty UpdateCommandProperty =
    //    DependencyProperty.Register(
    //        nameof(UpdateCommand),
    //        typeof(ICommand),
    //        typeof(WebDavContentDialog));

    //public ICommand? ConfirmCommand
    //{
    //    get => (ICommand?)GetValue(ConfirmCommandProperty);
    //    set => SetValue(ConfirmCommandProperty, value);
    //}

    //public static readonly DependencyProperty ConfirmCommandProperty =
    //    DependencyProperty.Register(
    //        nameof(ConfirmCommand),
    //        typeof(ICommand),
    //        typeof(WebDavContentDialog));

    //public ICommand? CancelCommand
    //{
    //    get => (ICommand?)GetValue(CancelCommandProperty);
    //    set => SetValue(CancelCommandProperty, value);
    //}

    //public static readonly DependencyProperty CancelCommandProperty =
    //    DependencyProperty.Register(
    //        nameof(CancelCommand),
    //        typeof(ICommand),
    //        typeof(WebDavContentDialog));

}
