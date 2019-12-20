using System.Collections.Generic;
using System.Collections.Specialized;

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


    public class SearchClient<T> where T : class
    {
        private string _typeName;
        private static string _serverName;

        public string Server
        {
            get { return _serverName; }
            set { _serverName = value; }
        }


        /// <summary>
        /// Type Name
        /// </summary>
        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        /// <summary>
        /// Index Name
        /// </summary>
        private string _indexName;

        public string IndexName
        {
            get { return _indexName; }
            set { _indexName = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SearchClient()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SearchClient(string indexName)
            : this()
        {
            _indexName = indexName;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SearchClient(string typeName, string indexName) : this(indexName)
        {
            _typeName = typeName;
        }

        public SearchClient(string typeName, string serverName, string indexName) : this(typeName, indexName)
        {
            _serverName = serverName;
            //if (_client == null)
            //{
            //    _client = new ElasticClientWrapper(_serverName, _indexName);
            //}
        }

        public SearchResults<T> Search(List<string> fieldNames, string searchTerm, string searchType, NameValueCollection keyValueCollection, int limit, int offset, string queryFilter, string sortField = null, string sortDirection = null, List<int> deviceCategories = null, bool useElasticSearch55OrLater = false)
        {
            SearchResults<T> searchResults = new SearchResults<T>();

            return searchResults;

        }
    }
}
