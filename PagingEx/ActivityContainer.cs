using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace PagingEx
{
    public class ActivityContainer : Control, INavigate
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content), typeof(object), typeof(ActivityContainer), new PropertyMetadata(default));

        public static readonly DependencyProperty ContentTransitionsProperty = DependencyProperty.Register(
            nameof(ContentTransitions), typeof(TransitionCollection), typeof(ActivityContainer),
            new PropertyMetadata(default));

        public static readonly DependencyProperty SourceActivityTypeProperty = DependencyProperty.Register(
            nameof(SourceActivityType), typeof(Type), typeof(ActivityContainer),
            new PropertyMetadata(default, OnSourceActivityTypeChanged));

        private readonly ActivityStackManager _activityStackManager = new ActivityStackManager();

        public ActivityContainer()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            Loaded += delegate { Window.Current.VisibilityChanged += OnVisibilityChanged; };
            Unloaded += delegate { Window.Current.VisibilityChanged -= OnVisibilityChanged; };

            DefaultStyleKey = typeof(ActivityContainer);
        }

        public bool DisableCache { get; set; }

        public ContentPresenter InternalContentPresenter { get; private set; }

        private ActivityModel CurrentActivityModel => _activityStackManager?.CurrentActivity;

        public Activity CurrentActivity => _activityStackManager?.CurrentActivity?.GetActivity(this);

        public bool CanGoBack => _activityStackManager.CanGoBack;

        public int BackStackDepth => _activityStackManager.BackStackDepth;

        public bool IsNavigating { get; private set; }

        public IActivityTransition ActivityTransition { get; set; }

        private IActivityTransition ActualActivityTransition
        {
            get
            {
                if (ContentTransitions != null)
                    return null;

                var currentActivity = CurrentActivity;
                return currentActivity?.ActivityTransition != null
                    ? CurrentActivity.ActivityTransition
                    : ActivityTransition;
            }
        }

        private Grid ContentRoot
        {
            get
            {
                if (Content == null)
                    Content = new Grid();
                return (Grid) Content;
            }
        }
        
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

        public Type SourceActivityType
        {
            get => (Type) GetValue(SourceActivityTypeProperty);
            set => SetValue(SourceActivityTypeProperty, value);
        }

        public bool Navigate(Type sourceActivityType)
        {
            Navigate(sourceActivityType, null);
            return true;
        }

        public event EventHandler<EventArgs> Navigated;
        public event EventHandler<EventArgs> Navigating;

        public Task<bool> GoHome()
        {
            return GoBackTo(0);
        }

        public async Task<bool> GoBackTo(int index)
        {
            if (!_activityStackManager.CanGoBackTo(index)) return false;

            return await RunNavigationWithCheck(async () =>
            {
                var nextActivity = _activityStackManager.GetActivityAt(index);
                var currentActivity = CurrentActivityModel;


                await NavigateImplAsync(NavigationMode.Back, currentActivity, nextActivity, index);

                _activityStackManager.ClearForwardStack();

                return true;
            });
        }

        public bool RemoveActivityAt(int index)
        {
            return _activityStackManager.RemoveActivityAt(index);
        }

        public Task<bool> GoBack()
        {
            return RunNavigationWithCheck(async () =>
            {
                await GoForwardOrBack(NavigationMode.Back);
                return true;
            });
        }

        public void Initialize(Type homeActivityType, object parameter = null)
        {
            Navigate(homeActivityType, parameter);
        }

        public Task<bool> Navigate(Type activityType, object parameter)
        {
            var newActivity = new ActivityModel(activityType, parameter);
            return NavigateWithMode(newActivity, NavigationMode.New);
        }

        private static void OnSourceActivityTypeChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as ActivityContainer)?.OnSourceActivityTypeChanged(e.NewValue as Type);
        }

        private void OnSourceActivityTypeChanged(Type newValue)
        {
            Navigate(newValue);
        }

        public void ClearBackStack()
        {
            _activityStackManager.ClearBackStack();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            InternalContentPresenter = (ContentPresenter) GetTemplateChild(nameof(ActivityContainer));
        }

        private Task<bool> NavigateWithMode(ActivityModel newActivity, NavigationMode navigationMode)
        {
            return RunNavigationWithCheck(async () =>
            {
                var currentActivity = CurrentActivityModel;

                _activityStackManager.ClearForwardStack();

                await NavigateImplAsync(navigationMode, currentActivity, newActivity,
                    _activityStackManager.CurrentIndex + 1);

                return true;
            });
        }

        private async Task<bool> RunNavigationWithCheck(Func<Task<bool>> task)
        {
            if (IsNavigating) return false;

            try
            {
                IsNavigating = true;
                return await task();
            }
            finally
            {
                IsNavigating = false;
            }
        }


        private async Task GoForwardOrBack(NavigationMode navigationMode)
        {
            if (CanGoBack)
            {
                var currentActivity = CurrentActivityModel;
                var nextActivityIndex =
                    _activityStackManager.CurrentIndex - 1;
                var nextActivity = _activityStackManager.Activities[nextActivityIndex];

                await NavigateImplAsync(navigationMode, currentActivity, nextActivity, nextActivityIndex);

                _activityStackManager.ClearForwardStack();
            }
            else
            {
                throw new InvalidOperationException($"The {nameof(ActivityContainer)} cannot go back");
            }
        }

        private async Task NavigateImplAsync(NavigationMode navigationMode,
            ActivityModel currentActivity, ActivityModel nextActivity, int nextIndex)
        {
            if (Content is FrameworkElement element) element.IsHitTestVisible = false;

            InvokeLifecycleBeforeContentChanged(navigationMode, currentActivity, nextActivity);

            _activityStackManager.ChangeCurrentActivity(nextActivity, nextIndex);

            OnCurrentActivityChanged(currentActivity?.Activity, nextActivity?.Activity);

            Navigating?.Invoke(this, EventArgs.Empty);

            await UpdateContent(navigationMode, currentActivity, nextActivity);

            InvokeLifecycleAfterContentChanged(navigationMode, currentActivity, nextActivity);

            if (Content is FrameworkElement frameworkElement) frameworkElement.IsHitTestVisible = true;

            ReleaseActivity(currentActivity);

            Navigated?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdateContent(NavigationMode navigationMode, ActivityModel currentActivity,
            ActivityModel nextActivity)
        {
            var animation = ActualActivityTransition;
            var current = currentActivity?.GetActivity(this)?.InternalPage;
            var next = nextActivity?.GetActivity(this)?.InternalPage;
            currentActivity?.GetActivity(this)?.PrepareConnectedAnimation();
            if (animation != null)
            {
                switch (animation.InsertionMode)
                {
                    case ActivityInsertionMode.NewAbove:
                        switch (navigationMode)
                        {
                            case NavigationMode.New:
                                ContentRoot.Children.Add(next);
                                nextActivity?.GetActivity(this)?.UsingConnectedAnimation();
                                await animation.OnStart(next, current);
                                break;
                            case NavigationMode.Back:
                                ContentRoot.Children.Insert(0, next);
                                nextActivity?.GetActivity(this)?.UsingConnectedAnimation();
                                await animation.OnClose(current, next);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(navigationMode), navigationMode, null);
                        }

                        break;
                    case ActivityInsertionMode.NewBelow:
                        switch (navigationMode)
                        {
                            case NavigationMode.New:
                                ContentRoot.Children.Insert(0, next);
                                nextActivity?.GetActivity(this)?.UsingConnectedAnimation();
                                await animation.OnStart(next, current);
                                break;
                            case NavigationMode.Back:
                                ContentRoot.Children.Add(next);
                                nextActivity?.GetActivity(this)?.UsingConnectedAnimation();
                                await animation.OnClose(current, next);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(navigationMode), navigationMode, null);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                ContentRoot.Children.Remove(current);
            }
            else
            {
                if (current != null) ContentRoot.Children.Remove(current);

                ContentRoot.Children.Add(next);
            }
        }

        private void InvokeLifecycleBeforeContentChanged(NavigationMode navigationMode, ActivityModel currentActivity,
            ActivityModel nextActivity)
        {
            switch (navigationMode)
            {
                case NavigationMode.New:
                    currentActivity?.GetActivity(this)?.OnPause();
                    nextActivity?.GetActivity(this)?.OnStart();
                    break;
                case NavigationMode.Back:
                    currentActivity?.GetActivity(this)?.OnClose();
                    nextActivity?.GetActivity(this).OnRestart();
                    break;
                case NavigationMode.Forward:
                    break;
                case NavigationMode.Refresh:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigationMode), navigationMode, null);
            }
        }

        private void InvokeLifecycleAfterContentChanged(NavigationMode navigationMode, ActivityModel currentActivity,
            ActivityModel nextActivity)
        {
            switch (navigationMode)
            {
                case NavigationMode.New:
                    currentActivity?.GetActivity(this)?.OnStop();
                    nextActivity?.GetActivity(this)?.OnResume();
                    break;
                case NavigationMode.Back:
                    currentActivity?.GetActivity(this)?.OnDestroy();
                    nextActivity?.GetActivity(this).OnResume();
                    break;
                case NavigationMode.Forward:
                    break;
                case NavigationMode.Refresh:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigationMode), navigationMode, null);
            }
        }

        private void ReleaseActivity(ActivityModel activity)
        {
            if (activity != null &&
                (activity.Activity.NavigationCacheMode == NavigationCacheMode.Disabled || DisableCache))
                activity.Release();
        }

        protected virtual void OnCurrentActivityChanged(Activity currentActivity, Activity newActivity)
        {
        }

        private void OnVisibilityChanged(object sender, VisibilityChangedEventArgs args)
        {
            if (args.Visible)
                CurrentActivity?.OnResume();
            else
                CurrentActivity?.OnPause();
        }
    }
}