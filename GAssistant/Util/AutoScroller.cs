using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;

namespace GoogleAssistantWindows.Util
{
    public class AutoScroller : Behavior<ListBox>
    {
        private ScrollViewer scrollViewer;
        private INotifyCollectionChanged currentCollection;

        protected override void OnAttached()
        {
            base.OnAttached();

            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            if (dpd != null)
            {
                dpd.AddValueChanged(AssociatedObject, ItemsSourceChanged);
            }

            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            scrollViewer = FindVisualChild<ScrollViewer>(AssociatedObject);
        }

        private void ItemsSourceChanged(object sender, EventArgs e)
        {
            var collection = ((ListBox)sender).ItemsSource as INotifyCollectionChanged;
            if(collection != null)
            {
                if(currentCollection != null)
                    currentCollection.CollectionChanged -= OnCollectionChanged;

                collection.CollectionChanged += OnCollectionChanged;
                currentCollection = collection;
            }
        }

        private void OnCollectionChanged(object sender, EventArgs args)
        {
            App.Current.Dispatcher.Invoke(delegate {
                scrollViewer.ScrollToBottom();
            });
        }

        private static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                {
                    return (childItem)child;
                }
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }
    }
}
