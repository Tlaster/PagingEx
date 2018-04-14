using System;
using System.Collections.Generic;

namespace PagingEx
{
    internal class PageStackManager
    {
        private readonly List<PageModel> _pages = new List<PageModel>();

        public int CurrentIndex { get; private set; } = -1;

        public PageModel CurrentPage => _pages.Count > 0 ? _pages[CurrentIndex] : null;

        public bool CanGoBack => CurrentIndex > 0;

        public IReadOnlyList<PageModel> Pages => _pages;

        public int BackStackDepth => CurrentIndex + 1;

        public PageModel GetPageAt(int index)
        {
            return _pages[index];
        }

        public bool RemovePageFromStackAt(int pageIndex)
        {
            if (pageIndex == CurrentIndex)
                throw new ArgumentException("The current page cannot be removed from the stack. ");

            _pages.RemoveAt(pageIndex);
            if (pageIndex < CurrentIndex) CurrentIndex--;

            return true;
        }

        public void ClearBackStack()
        {
            for (var i = CurrentIndex - 1; i >= 0; i--)
                RemovePageFromStackAt(i);
        }

        public void ClearForwardStack()
        {
            for (var i = _pages.Count - 1; i > CurrentIndex; i--)
                RemovePageFromStackAt(i);
        }

        public void ChangeCurrentPage(PageModel newPage, int nextPageIndex)
        {
            if (_pages.Count <= nextPageIndex) _pages.Add(newPage);

            CurrentIndex = nextPageIndex;
        }

        public bool CanGoBackTo(int newPageIndex)
        {
            if (newPageIndex == CurrentIndex) return false;

            return newPageIndex >= 0 && newPageIndex <= CurrentIndex;
        }
    }
}