using System;

namespace PagingEx
{
    internal class PageModel
    {
        public PageModel(Type pageType, object parameter)
        {
            Type = pageType;
            Parameter = parameter;
            PageKey = Guid.NewGuid().ToString();
        }

        public object Parameter { get; internal set; }

        public PageEx Page { get; private set; }

        public Type Type { get; }

        private string PageKey { get; }

        internal PageEx GetPage(FrameEx frameEx)
        {
            if (Page != null)
            {
                return Page;
            }

            if (!(Activator.CreateInstance(Type) is PageEx page))
            {
                throw new InvalidOperationException(
                    $"The base type is not an {nameof(Page)}. Change the base type from Page to {nameof(Page)}. ");
            }

            page.SetFrame(frameEx);
            page.OnCreate(Parameter);
            Page = page;

            return Page;
        }


        internal void ReleasePage()
        {
            //Page.OnDestory();
            Page = null;
        }
    }
}