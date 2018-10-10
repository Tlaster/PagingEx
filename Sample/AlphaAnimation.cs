using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp.UI.Animations;
using PagingEx;

namespace Sample
{
    class AlphaAnimation : IActivityTransition
    {
        public ActivityInsertionMode InsertionMode { get; } = ActivityInsertionMode.NewAbove;
        public async Task OnStart(FrameworkElement newPage, FrameworkElement currentPage)
        {
            newPage.Opacity = 0;
            newPage.Scale(scaleX: 0.8F, scaleY: 0.8F, centerX: Convert.ToSingle(newPage.ActualWidth / 2F),
                centerY: Convert.ToSingle(newPage.ActualWidth / 2F), duration: 0D).Start();
            if (currentPage != null)
            {
                currentPage.Scale(scaleX: 1F, scaleY: 1F, centerX: Convert.ToSingle(currentPage.ActualWidth / 2F),
                    centerY: Convert.ToSingle(currentPage.ActualWidth / 2F), duration: 0D).Start();
                currentPage.Opacity = 1;
                currentPage.Scale(scaleX: 1.2F, scaleY: 1.2F, centerX: Convert.ToSingle(currentPage.ActualWidth / 2F),
                    centerY: Convert.ToSingle(currentPage.ActualWidth / 2F), duration: 250D).Start();
                await currentPage.Fade(0F, duration: 250D).StartAsync();
            }
            newPage.Scale(scaleX: 1F, scaleY: 1F, centerX: Convert.ToSingle(newPage.ActualWidth / 2F),
                centerY: Convert.ToSingle(newPage.ActualWidth / 2F), duration: 250D).Start();
            await newPage.Fade(1F, duration:250D).StartAsync();
        }

        public async Task OnClose(FrameworkElement closePage, FrameworkElement previousPage)
        {
            closePage.Opacity = 1;
            closePage.Scale(scaleX: 1F, scaleY: 1F, centerX: Convert.ToSingle(closePage.ActualWidth / 2F),
                centerY: Convert.ToSingle(closePage.ActualWidth / 2F), duration: 0D).Start();
            previousPage.Opacity = 0;
            previousPage.Scale(scaleX: 1.2F, scaleY: 1.2F, centerX: Convert.ToSingle(previousPage.ActualWidth / 2F),
                centerY: Convert.ToSingle(previousPage.ActualWidth / 2F), duration: 0D).Start();
            closePage.Scale(scaleX: 0.8F, scaleY: 0.8F, centerX: Convert.ToSingle(closePage.ActualWidth / 2F),
                centerY: Convert.ToSingle(closePage.ActualWidth / 2F), duration: 250D).Start();
            await closePage.Fade(0F, duration: 250D).StartAsync();
            previousPage.Scale(scaleX: 1F, scaleY: 1F, centerX: Convert.ToSingle(previousPage.ActualWidth / 2F),
                centerY: Convert.ToSingle(previousPage.ActualWidth / 2F), duration: 250D).Start();
            await previousPage.Fade(1F, duration: 250D).StartAsync();
        }
    }
}
