using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace PagingEx
{
    public class FrameEx : Control, INavigate
    {
        public event EventHandler<EventArgs> Navigated;
        public event EventHandler<EventArgs> Navigating;

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), typeof(object), typeof(FrameEx), new PropertyMetadata(default));

        public static readonly DependencyProperty ContentTransitionsProperty = DependencyProperty.Register(
            nameof(ContentTransitions), typeof(TransitionCollection), typeof(FrameEx),
            new PropertyMetadata(default));

        public static readonly DependencyProperty SourcePageTypeProperty = DependencyProperty.Register(
            nameof(SourcePageType), typeof(Type), typeof(FrameEx),
            new PropertyMetadata(default, OnSourcePageTypeChanged));

        private readonly PageStackManager _pageStackManager = new PageStackManager();

        public FrameEx()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            
            Loaded += delegate
            {
                Window.Current.VisibilityChanged += OnVisibilityChanged;
            };
            Unloaded += delegate
            {
                Window.Current.VisibilityChanged -= OnVisibilityChanged;
            };

            DefaultStyleKey = typeof(FrameEx);
        }

        public bool DisableCache { get; set; }

        public ContentPresenter InternalFrame { get; private set; }

        private PageModel CurrentPageModel => _pageStackManager?.CurrentPage;

        public PageEx CurrentPage => _pageStackManager?.CurrentPage?.GetPage(this);

        public bool CanGoBack => _pageStackManager.CanGoBack;
        
        public int BackStackDepth => _pageStackManager.BackStackDepth;

        public bool IsNavigating { get; private set; }

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public TransitionCollection ContentTransitions
        {
            get => (TransitionCollection) GetValue(ContentTransitionsProperty);
            set => SetValue(ContentTransitionsProperty, value);
        }

        public Type SourcePageType
        {
            get => (Type) GetValue(SourcePageTypeProperty);
            set => SetValue(SourcePageTypeProperty, value);
        }
        
        public bool GoHome()
        {
            return GoBackTo(0);
        }

        public bool GoBackTo(int newPageIndex)
        {
            if (!_pageStackManager.CanGoBackTo(newPageIndex)) return false;

            return RunNavigationWithCheck(() =>
            {
                var nextPage = _pageStackManager.GetPageAt(newPageIndex);
                var currentPage = CurrentPageModel;


                NavigateImpl(NavigationMode.Back, currentPage, nextPage, newPageIndex);

                _pageStackManager.ClearForwardStack();

                return true;
            });
        }

        public bool RemovePageFromStackAt(int pageIndex)
        {
            return _pageStackManager.RemovePageFromStackAt(pageIndex);
        }

        public bool GoBack()
        {
            return RunNavigationWithCheck(() =>
            {
                GoForwardOrBack(NavigationMode.Back);
                return true;
            });
        }

        public void Initialize(Type homePageType, object parameter = null)
        {
            Navigate(homePageType, parameter);
        }
        
        public bool Navigate(Type sourcePageType)
        {
            return Navigate(sourcePageType, null);
        }

        public bool Navigate(Type pageType, object parameter)
        {
            var newPage = new PageModel(pageType, parameter);
            return NavigateWithMode(newPage, NavigationMode.New);
        }

        private static void OnSourcePageTypeChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as FrameEx)?.OnSourcePageTypeChanged(e.NewValue as Type);
        }

        private void OnSourcePageTypeChanged(Type newValue)
        {
            Navigate(newValue);
        }

        public void ClearBackStack()
        {
            _pageStackManager.ClearBackStack();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            InternalFrame = (ContentPresenter) GetTemplateChild(nameof(FrameEx));
        }

        private bool NavigateWithMode(PageModel newPage, NavigationMode navigationMode)
        {
            return RunNavigationWithCheck(() =>
            {
                var currentPage = CurrentPageModel;

                _pageStackManager.ClearForwardStack();

                NavigateImpl(navigationMode, currentPage, newPage,
                    _pageStackManager.CurrentIndex + 1);

                return true;
            });
        }

        private bool RunNavigationWithCheck(Func<bool> task)
        {
            if (IsNavigating) return false;

            try
            {
                IsNavigating = true;
                return task();
            }
            finally
            {
                IsNavigating = false;
            }
        }


        private void GoForwardOrBack(NavigationMode navigationMode)
        {
            if (CanGoBack)
            {
                var currentPage = CurrentPageModel;
                var nextPageIndex =
                    _pageStackManager.CurrentIndex - 1;
                var nextPage = _pageStackManager.Pages[nextPageIndex];

                NavigateImpl(navigationMode, currentPage, nextPage, nextPageIndex);

                _pageStackManager.ClearForwardStack();
            }
            else
            {
                throw new InvalidOperationException("The frameEx cannot go back");
            }
        }

        private void NavigateImpl(NavigationMode navigationMode,
            PageModel currentPage, PageModel nextPage, int nextPageIndex)
        {
            if (Content is FrameworkElement element) element.IsHitTestVisible = false;

            switch (navigationMode)
            {
                case NavigationMode.New:
                    currentPage?.GetPage(this)?.OnPause();
                    nextPage?.GetPage(this)?.OnStart();
                    break;
                case NavigationMode.Back:
                    currentPage?.GetPage(this)?.OnClose();
                    nextPage?.GetPage(this).OnRestart();
                    break;
                case NavigationMode.Forward:
                    break;
                case NavigationMode.Refresh:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigationMode), navigationMode, null);
            }
            currentPage?.GetPage(this)?.PrepareConnectedAnimation();
            _pageStackManager.ChangeCurrentPage(nextPage, nextPageIndex);
            OnCurrentPageChanged(currentPage?.Page, nextPage?.Page);
            Navigating?.Invoke(this, EventArgs.Empty);

            Content = nextPage?.GetPage(this).InternalPage;

            switch (navigationMode)
            {
                case NavigationMode.New:
                    currentPage?.GetPage(this)?.OnStop();
                    nextPage?.GetPage(this)?.OnResume();
                    break;
                case NavigationMode.Back:
                    currentPage?.GetPage(this)?.OnDestory();
                    nextPage?.GetPage(this).OnResume();
                    break;
                case NavigationMode.Forward:
                    break;
                case NavigationMode.Refresh:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigationMode), navigationMode, null);
            }
            nextPage?.GetPage(this)?.UsingConnectedAnimation();

            if (Content is FrameworkElement frameworkElement) frameworkElement.IsHitTestVisible = true;

            ReleasePageIfNecessary(currentPage);

            Navigated?.Invoke(this, EventArgs.Empty);
        }

        private void ReleasePageIfNecessary(PageModel page)
        {
            if (page != null && (page.Page.NavigationCacheMode == NavigationCacheMode.Disabled || DisableCache))
                page.ReleasePage();
        }

        protected virtual void OnCurrentPageChanged(PageEx currentPageEx, PageEx newPageEx)
        {
        }

        private void OnVisibilityChanged(object sender, VisibilityChangedEventArgs args)
        {
            if (args.Visible)
            {
                CurrentPage?.OnResume();
            }
            else
            {
                CurrentPage?.OnPause();
            }
        }
    }
}