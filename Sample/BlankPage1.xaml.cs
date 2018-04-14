using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Sample
{
    /// <summary>
    /// 可用于自身或导航至 _frame 内部的空白页。
    /// </summary>
    public sealed partial class BlankPage1
    {
        public BlankPage1()
        {
            this.InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            OpenPage(typeof(MainPage));
        }

        protected override void OnPrepareConnectedAnimation(ConnectedAnimationService service)
        {
            base.OnPrepareConnectedAnimation(service);
            service.PrepareToAnimate("text", TextBlock);
        }

        protected override void OnUsingConnectedAnimation(ConnectedAnimationService service)
        {
            base.OnUsingConnectedAnimation(service);
            service.GetAnimation("text")?.TryStart(TextBlock);
        }
    }
}
