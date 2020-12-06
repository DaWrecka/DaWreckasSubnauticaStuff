using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FabricatorsResumeFromLast
{
    public class Main
    {
        public static Dictionary<ITreeActionReceiver, CraftTree.Type> savedPages = new Dictionary<ITreeActionReceiver, CraftTree.Type>();

        public static CraftTree.Type GetLastOpenPage(ITreeActionReceiver receiver)
        {
            CraftTree.Type lastPage;
            if (savedPages.TryGetValue(receiver, out lastPage))
                return lastPage;

            return CraftTree.Type.None;
        }

        public void SetOpenPage(ITreeActionReceiver receiver, CraftTree.Type page)
        {
            CraftTree.Type lastPage;
            if (savedPages.TryGetValue(receiver, out lastPage))
            {
                savedPages[receiver] = page;
            }
            else
                savedPages.Add(receiver, page);
        }
    }
}
