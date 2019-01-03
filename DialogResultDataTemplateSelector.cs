using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleAssistantWindows
{
    public class DialogResultDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is DialogResult)
            {
                DialogResult dialogResult = item as DialogResult;

                if (dialogResult.Actor == DialogResultActor.GoogleAssistant)
                    return
                        element.FindResource("googleAssistantResultTemplate") as DataTemplate;
                else
                    return
                        element.FindResource("userResultTemplate") as DataTemplate;
            }

            return null;
        }
    }
}
