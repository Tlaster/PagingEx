using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using PagingEx.Handlers;

namespace PagingEx
{
    public class FrameEx : Control, INavigate
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), typeof(object), typeof(FrameEx), new PropertyMetadata(default));

        public static readonly DependencyProperty ContentTransitionsProperty = DependencyProperty.Register(
            nameof(ContentTransitions), typeof(TransitionCollection), typeof(FrameEx),
            new PropertyMetadata(default(TransitionCollection)));

        public static readonly DependencyProperty SourcePageTypeProperty = DependencyProperty.Register(
            nameof(SourcePageType), typeof(Type), typeof(FrameEx),
            new PropertyMetadata(default(Type), OnSourcePageTypeChanged));

        private readonly PageStackManager _pageStackManager = new PageStackManager();

        public FrameEx()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            Loaded += delegate { Window.Current.VisibilityChanged += OnVisibilityChanged; };
            Unloaded += delegate { Window.Current.VisibilityChanged -= OnVisibilityChanged; };

            DefaultStyleKey = typeof(FrameEx);
        }

        public bool DisableCache { get; set; }

        public ContentPresenter InternalFrame { get; private set; }

        public bool AutomaticBackButtonHandling
        {
            get => _pageStackManager.AutomaticBackButtonHandling;
            set => _pageStackManager.AutomaticBackButtonHandling = value;
        }

        public bool IsFirstPage => _pageStackManager.IsFirstPage;

        private PageModel CurrentPageModel => _pageStackManager?.CurrentPage;

        public PageEx CurrentPage => _pageStackManager?.CurrentPage?.Page;

        public bool CanGoBack => _pageStackManager.CanGoBack;

        //public bool CanGoForward => _pageStackManager.CanGoForward;

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



        private PageModel GetNearestPageOfTypeInBackStack(Type pageType)
        {
            return _pageStackManager.GetNearestPageOfTypeInBackStack(pageType);
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


        public bool Navigate(Type pageType, object parameter = null)
        {
            var newPage = new PageModel(pageType, parameter);
            return NavigateWithMode(newPage, NavigationMode.New);
        }

        private static void OnSourcePageTypeChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as FrameEx).OnSourcePageTypeChanged(e.NewValue as Type);
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

            _pageStackManager.ChangeCurrentPage(nextPage, nextPageIndex);
            OnCurrentPageChanged(currentPage?.Page, nextPage?.Page);

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

            if (Content is FrameworkElement frameworkElement) frameworkElement.IsHitTestVisible = true;

            ReleasePageIfNecessary(currentPage);
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
            CurrentPageModel?.GetPage(this).OnVisibilityChanged(args);
        }
    }
}