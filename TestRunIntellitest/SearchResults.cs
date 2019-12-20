using System.Collections.Generic;

namespace FDB.CC.Core.Search
{

    public class SearchResults<T>   where T : class
    {
        public SearchResults()
        {
        }

        public int TotalResultCount { get; set; }
       
        public IEnumerable<T> Items { get; set; }
    }
}
