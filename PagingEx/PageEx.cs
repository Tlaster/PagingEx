using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PagingEx
{
    public class PageEx : ContentControl
    {
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(PageEx), new PropertyMetadata(default));

        public static readonly DependencyProperty TopAppBarProperty =
            DependencyProperty.Register(nameof(TopAppBar), typeof(AppBar), typeof(PageEx),
                new PropertyMetadata(default(AppBar), (o, args) => ((PageEx) o).OnUpdateTopAppBar()));

        public static readonly DependencyProperty BottomAppBarProperty =
            DependencyProperty.Register(nameof(BottomAppBar), typeof(AppBar), typeof(PageEx),
                new PropertyMetadata(default(AppBar), (o, args) => ((PageEx) o).OnUpdateBottomAppBar()));

        private Page _internalPage;
        private bool _isLoaded;

        public PageEx()
        {
            NavigationCacheMode = NavigationCacheMode.Required;

            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        public bool IsBusy
        {
            get => (bool) GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public FrameEx Frame { get; private set; }

        public Page InternalPage => _internalPage ?? (_internalPage = new Page {Content = this});

        public NavigationCacheMode NavigationCacheMode { get; set; }

        public AppBar TopAppBar
        {
            get => (AppBar) GetValue(TopAppBarProperty);
            set => SetValue(TopAppBarProperty, value);
        }

        public AppBar BottomAppBar
        {
            get => (AppBar) GetValue(BottomAppBarProperty);
            set => SetValue(BottomAppBarProperty, value);
        }

        protected internal virtual void SetFrame(FrameEx frameEx, string pageKey)
        {
            Frame = frameEx;
        }

        protected internal virtual void OnKeyActivated(AcceleratorKeyEventArgs args)
        {
            // Must be empty
        }


        protected internal virtual void OnKeyUp(AcceleratorKeyEventArgs args)
        {
            // Must be empty
        }

        protected internal virtual void OnVisibilityChanged(VisibilityChangedEventArgs args)
        {
            // Must be empty
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }


        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _isLoaded = true;

            OnUpdateTopAppBar();
            OnUpdateBottomAppBar();
            OnDataContextChanged();
        }

        private void OnDataContextChanged()
        {
            if (_isLoaded) InternalPage.DataContext = DataContext;
        }

        private void OnUpdateTopAppBar()
        {
            if (_isLoaded && TopAppBar != null)
            {
                InternalPage.TopAppBar = TopAppBar;
                foreach (var item in Resources.Where(item => !(item.Value is DependencyObject)))
                    InternalPage.TopAppBar.Resources[item.Key] = item.Value;
            }
        }

        private void OnUpdateBottomAppBar()
        {
            if (_isLoaded && BottomAppBar != null)
            {
                InternalPage.BottomAppBar = BottomAppBar;
                foreach (var item in Resources.Where(item => !(item.Value is DependencyObject)))
                    InternalPage.BottomAppBar.Resources[item.Key] = item.Value;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (InternalPage.TopAppBar != null) InternalPage.TopAppBar = null;

            if (InternalPage.BottomAppBar != null) InternalPage.BottomAppBar = null;
        }

        protected internal virtual void OnCreate(object paramter)
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnCreate)}");
        }

        protected internal virtual void OnStart()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnStart)}");
        }

        protected internal virtual void OnRestart()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnRestart)}");
        }

        protected internal virtual void OnStop()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnStop)}");
        }

        protected internal virtual void OnResume()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnResume)}");
        }

        protected internal virtual void OnPause()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnPause)}");
        }

        protected internal virtual void OnClose()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnClose)}");
        }

        protected internal virtual void OnDestory()
        {
            Debug.WriteLine($"{GetType().Name}: {nameof(OnDestory)}");
        }

    }
}