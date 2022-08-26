using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWiFiAnalyzer
{
    class HelpPageHistory
    {
        private Stack<string> Breadcrumbs = new Stack<string>();

        /// <summary>
        /// Call this when you navigate to a new page
        /// </summary>
        /// <param name="place"></param>
        public void NavigatedTo(string place)
        {
            if (Breadcrumbs.Count >= 1 && place == Breadcrumbs.Peek()) return;
            Breadcrumbs.Push(place);
        }

        /// <summary>
        /// Call this to get the right page to "pop" to.
        /// </summary>
        /// <returns></returns>
        public string PopLastPage()
        {
            // If stack is help.md                          then return help.md and don't change the stack
            // If the stack is help.md thingy.md            then pop thingy.md pop and return help.md
            // If the stack is help.md thingy.md excel.md   then pop excel.md pop and return thingy.md 
            if (Breadcrumbs.Count == 1) return Breadcrumbs.Peek();
            if (Breadcrumbs.Count == 2)
            {
                Breadcrumbs.Pop();
                return Breadcrumbs.Peek();
            }
            if (Breadcrumbs.Count > 2)
            {
                // Pop twice, not once. The next action is almost certainly going to be a push
                // of the thing I am now returning.
                Breadcrumbs.Pop();
                var retval = Breadcrumbs.Pop();
                return retval;
            }
            return "Help.md"; // emergency back-up plan!
        }
    }
}
