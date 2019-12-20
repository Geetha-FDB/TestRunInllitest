//using System;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Nest;
//using System.IO;
//using System.Text.RegularExpressions;

//namespace FDB.CC.Core.Search
//{
//    public class SearchClient<T> where T : class
//    {
//        //only child classes to refer it
//        protected ElasticClientWrapper _client;
//        private string _typeName;
//        private static string _serverName;

//        public string Server
//        {
//            get { return _serverName; }
//            set { _serverName = value; }
//        }


//        /// <summary>
//        /// Type Name
//        /// </summary>
//        public string TypeName
//        {
//            get { return _typeName; }
//            set { _typeName = value; }
//        }

//        /// <summary>
//        /// Index Name
//        /// </summary>
//        private string _indexName;

//        public string IndexName
//        {
//            get { return _indexName; }
//            set { _indexName = value; }
//        }

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        public SearchClient()
//        {
//        }

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        public SearchClient(string indexName)
//            : this()
//        {
//            _indexName = indexName;
//        }

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        public SearchClient(string typeName, string indexName) : this(indexName)
//        {
//            _typeName = typeName;
//        }

//        public SearchClient(string typeName, string serverName, string indexName) : this(typeName, indexName)
//        {
//            _serverName = serverName;
//            if (_client == null)
//            {
//                _client = new ElasticClientWrapper(_serverName, _indexName);
//            }
//        }

//        /// <summary>
//        /// Searches the specified text on multiple fields.
//        /// </summary>
//        /// <param name="fieldNames">The field names.</param>
//        /// <param name="searchTerm">The search term.</param>
//        /// <param name="searchType">Type of the search.</param>
//        /// <param name="keyValueCollection">The key value collection.</param>
//        /// <param name="limit">The limit.</param>
//        /// <param name="offset">The offset.</param>
//        /// <param name="queryFilter">The query filter.</param>
//        /// <param name="sortField">The sort field.</param>
//        /// <param name="sortDirection">The sort direction.</param>
//        /// <param name="deviceCategories">The list of device categories to include</param>
//        /// <param name="useElasticSearch55OrLater">Flag to indicate if ElasticSearch version is new (>= 5.5). Defaults to false.</param>
//        /// <returns></returns>
//        public SearchResults<T> Search(List<string> fieldNames, string searchTerm, string searchType, NameValueCollection keyValueCollection, int limit, int offset, string queryFilter, string sortField = null, string sortDirection = null, List<int> deviceCategories = null, bool useElasticSearch55OrLater = false)
//        {
//            SearchResults<T> searchResults = new SearchResults<T>();
//            if (!string.IsNullOrEmpty(searchTerm))
//            {
//                //normalize search term
//                searchTerm = Normalize(searchTerm);
//            }

//            //handling null issue with string.equals
//            searchType = searchType ?? string.Empty;

//            //default StartsWith if SearchType null and SearchTerm not null
//            if (!string.IsNullOrWhiteSpace(searchTerm) && string.IsNullOrWhiteSpace(searchType)) searchType = "StartsWith";


//            //build body with indexname , typename, limit and offset
//            var body = BuildSearchDescriptor<T>(offset, limit);

//            BuildQuery(searchType, fieldNames, searchTerm, keyValueCollection, body, queryFilter, sortField, deviceCategories, useElasticSearch55OrLater);

//            if (sortDirection == null)
//            {
//                sortDirection = "asc";
//            }
//            else
//            {
//                sortDirection = sortDirection.ToLower();
//            }
//            if (sortField != null)
//            {
//                if (sortDirection == "asc")
//                {
//                    body.Sort(ss => ss.Ascending(sortField));
//                }
//                else
//                {
//                    body.Sort(ss => ss.Descending(sortField));
//                }
//            }
//#if DEBUG

//            ////this is to check the query passed to NEST client
//            //ElasticClient eClientWrapper = new ElasticClient(new ConnectionSettings(new Uri(Server)));

//            ////var jsonstring1 = eClientWrapper.Serializer.Serialize(body);
//            ////TODO: Review update
//            //MemoryStream writeableStream = new MemoryStream();
//            //eClientWrapper.Serializer.Serialize(body, writeableStream, Elasticsearch.Net.SerializationFormatting.Indented);

//            ////string jsonstring = System.Text.Encoding.Default.GetString(jsonstring1);
//            //string jsonstring = System.Text.Encoding.Default.GetString(writeableStream.ToArray());
//#endif

//            ///Build Query as per search type
//            var result = _client.Search<T>(body);

//            searchResults.TotalResultCount = Convert.ToInt32(result.Total);
//            searchResults.Items = result.Documents.ToList();
//            return searchResults;
//        }

//        /// <summary>
//        /// ES Search with a defined Nest QueryContainer
//        /// For advanced Use Cases
//        /// </summary>
//        public SearchResults<T> Search(Nest.QueryContainer queryContainer, int limit, int offset, string sortField = null, string sortDirection = null)
//        {
//            SearchResults<T> searchResults = new SearchResults<T>();

//            //build body with indexname , typename, limit and offset
//            var body = BuildSearchDescriptor<T>(offset, limit);

//            BuildQuery(queryContainer, body, sortField);

//            sortDirection = sortDirection == null ? "asc" : sortDirection.ToLower();

//            if (sortField != null)
//            {
//                if (sortDirection == "asc")
//                {
//                    body.Sort(ss => ss.Ascending(sortField));
//                }
//                else
//                {
//                    body.Sort(ss => ss.Descending(sortField));
//                }
//            }
//#if DEBUG
//            ////this is to check the query passed to NEST client
//            //ElasticClient eClientWrapper = new ElasticClient(new ConnectionSettings(new Uri(Server)));

//            ////var jsonstring1 = eClientWrapper.Serializer.Serialize(body);
//            ////TODO: Review update
//            //MemoryStream writeableStream = new MemoryStream();
//            //eClientWrapper.Serializer.Serialize(body, writeableStream, Elasticsearch.Net.SerializationFormatting.Indented);

//            ////string jsonstring = System.Text.Encoding.Default.GetString(jsonstring1);
//            //string jsonstring = System.Text.Encoding.Default.GetString(writeableStream.ToArray());
//#endif

//            ///Build Query as per search type
//            var result = _client.Search<T>(body);

//            searchResults.TotalResultCount = Convert.ToInt32(result.Total);
//            searchResults.Items = result.Documents.ToList();
//            return searchResults;
//        }

//        private static void BuildQuery(String searchType, List<string> fieldNames, string searchTerm, NameValueCollection filterCollection, Nest.SearchDescriptor<T> body, string queryFilter, string sortField = null, List<int> deviceCategories = null, bool useElasticSearch55OrLater = false)
//        {
//            try
//            {
//                //If you are doing multi fields search, for the StartsWith option to work, there should be a mapping created for the <fieldname>.Raw
//                //Check examples - MedicalCondition.Synonym.SynonymDesc, DispensableGeneric.NameType.DrugDesc

//                //Nest.FilterContainer filter = null;
//                Nest.QueryContainer filter = null;
//                Nest.QueryContainer query = null;
//                if (!string.IsNullOrEmpty(searchTerm))
//                {
//                    foreach (string field in fieldNames)
//                    {
//                        if (!string.IsNullOrEmpty(searchType))
//                        {
//                            string rawField = field + (useElasticSearch55OrLater ? "_Raw" : ".Raw");
//                            if (searchType.Equals("StartsWith", StringComparison.OrdinalIgnoreCase))
//                            {
//                                filter |= AddPrefixFilter(rawField, searchTerm);
//                            }
//                            else if (searchType.Equals("TextEqual", StringComparison.OrdinalIgnoreCase))
//                            {
//                                filter |= ExactMatch(rawField, searchTerm);
//                            }
//                            else if (searchType.Equals("Exhaustive", StringComparison.OrdinalIgnoreCase))
//                            {
//                                filter |= Exhaustive(field, searchTerm);
//                            }
//                            else if (searchType.Equals("Contains", StringComparison.OrdinalIgnoreCase))
//                            {
//                                filter |= ContainsWildCardQuery(rawField, searchTerm);
//                            }
//                            else if (searchType.EndsWith("Fuzzy", StringComparison.OrdinalIgnoreCase))
//                            {
//                                //devices
//                                if (deviceCategories != null)
//                                {
//                                    searchTerm = Regex.Replace(searchTerm, "\\(\\d\\d\\)", " ");
//                                }
//                                filter |= AddQueryString($"DescSearchFuzzy:{searchTerm}");
//                            }
//                            else if (searchType.Equals("StandardTextEqual", StringComparison.OrdinalIgnoreCase)) // same as "TextEqual" except it doesn't use the .Raw value
//                            {
//                                filter |= ExactMatch(field, searchTerm);
//                            }
//                        }
//                    }
//                }

//                //device search: only look for categories passed in
//                if (deviceCategories != null && deviceCategories.Count > 0)
//                {
//                    query &= AddTermsQuery("InternalCategories.InternalCategoryId", deviceCategories);
//                }

//                //if all the inputs are null , just return everything and sort by ascending order
//                //search type all
//                if (filter == null && query == null && filterCollection == null && string.IsNullOrWhiteSpace(queryFilter))
//                {
//                    //If SortField does not exists, only then add the default field and default sorting order.
//                    //The desired sortfield and sortdirection get attached to the query in the Search Method. So no need to add here 
//                    //If this check is skipped, then the query will have multiple sort fields and the sorting will be done on the first one (in this case it will be default field)
//                    if (string.IsNullOrEmpty(sortField))
//                    {
//                        body.Sort(ss => ss.Ascending(string.Format("{0}.Raw", fieldNames[0]))); //For now, sorting only on one field.
//                    }
//                }
//                else
//                {
//                    //Append Name Value filters to Base Filters
//                    //filter = GetSearchFilterValues(filterCollection, filter);
//                    query &= GetSearchFilterValues(filterCollection, filter);

//                    //add query string
//                    if (!string.IsNullOrWhiteSpace(queryFilter))
//                    {
//                        if (query == null)
//                        {
//                            query = AddQueryString(queryFilter);
//                        }
//                        else //combine with previous query
//                        {
//                            query &= AddQueryString(queryFilter);
//                        }
//                    }
//                    body.Query(q => query);
//                }

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }
//        }


//        private static void BuildQuery(Nest.QueryContainer queryContainer, Nest.SearchDescriptor<T> body, string sortField = null)
//        {
//            try
//            {
//                body.Query(q => queryContainer);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }
//        }

//        /// <summary>
//        /// build search desrcriptor
//        /// </summary>
//        /// <returns></returns>
//        protected Nest.SearchDescriptor<T1> BuildSearchDescriptor<T1>(int offset, int limit) where T1 : class
//        {
//            //by default type name will be class name
//            var type = TypeName ?? typeof(T1).Name;

//            offset = offset == 0 ? 1 : offset;

//            var body = new Nest.SearchDescriptor<T1>();
//            //Add Paging Parameters
//            return body.Type(type).From((offset - 1) * limit).Size(limit);
//        }

//        /// <summary>
//        /// Normalize the search term
//        /// </summary>
//        /// <param name="searchTerm"></param>
//        /// <returns></returns>
//        internal static string Normalize(string searchTerm)
//        {
//            if (!string.IsNullOrWhiteSpace(searchTerm))
//            {
//                searchTerm = searchTerm.ToLower().Trim();
//            }
//            return searchTerm;
//        }

//        /// <summary>
//        /// Query String
//        /// </summary>
//        /// <param name="fieldName"></param>
//        /// <param name="term"></param>
//        /// <returns></returns>
//        internal static Nest.QueryContainer AddQueryString(string queryFilter)
//        {
//            return Nest.Query<T>.QueryString(q => q.Query(queryFilter));
//        }


//        internal static QueryContainer AddTermsQuery(string field, IEnumerable<int> terms)
//        {
//            return Query<T>.Terms(t => t.Field(field).Terms(terms));
//        }
//        /// <summary>
//        /// Bind NameValuePair Collection to Filters.
//        /// </summary>
//        /// <param name="filterCollection"></param>
//        internal static Nest.QueryContainer GetSearchFilterValues(NameValueCollection filterCollection, Nest.QueryContainer filter)
//        {
//            if (filterCollection != null && filterCollection.Count > 0)
//            {
//                //if filter is null set it to base filer
//                filter = filter ?? new Nest.QueryContainer();

//                // retrieve values:
//                foreach (string filterKey in filterCollection)
//                {
//                    var key = filterKey;
//                    var value = filterCollection[key];
//                    value = Normalize(value);
//                    var values = value?.Split(',');


//                    //Exclude the properties containing the value
//                    if (key.Contains("Exclude."))
//                    {
//                        key = key.Replace("Exclude.", ""); //remove the suffix exclude
//                        if (value == "null")
//                        {
//                            //filter &= !Nest.Filter<T>.Missing(key);
//                            filter &= Nest.Query<T>.Exists(c => c.Name(key));
//                        }
//                        else
//                        {
//                            //filter &= !Nest.Filter<T>.Term(key, value);
//                            filter &= !Nest.Query<T>.Term(key, value);
//                        }
//                    }
//                    /*else if (key.Equals("ImageImprintDateRangeFilter"))  //Custom Filter for ImageImprint
//                    {
//                        //if key equal then check if search is on ImageImprint
//                        if (typeof(T).ToString().Contains("ImageImprint.ImageImprint"))
//                        {
//                            //add filter on startdate and enddate
//                            string startDateKey = "StartDateInt";
//                            string endDateKey = "EndDateInt";
//                            int val = Convert.ToInt32(value);

//                            //create a dateRangeFilter and put the conditions as follows
//                            //Nest.FilterContainer dateRangeFilter = new Nest.FilterContainer();
//                            Nest.QueryContainer dateRangeFilter = new Nest.QueryContainer();

//                            //if EndDateInt does not exists then check for only StartDateInt condition
//                            //Nest.FilterContainer endDateNullFilter = new Nest.FilterContainer();
//                            Nest.QueryContainer endDateNullFilter = new Nest.QueryContainer();

//                            //endDateNullFilter &= !Nest.Filter<T>.Exists(endDateKey);
//                            //endDateNullFilter &= Nest.Filter<T>.Range(q => q.OnField(startDateKey).LowerOrEquals(val));
//                            endDateNullFilter &= !Nest.Query<T>.Exists(c => c.Name(endDateKey));
//                            endDateNullFilter &= Nest.Query<T>.Range(q => q.Field(startDateKey).LessThanOrEquals(val));

//                            //if EndDateInt is present then check for both StartDateInt and EndDateInt
//                            //Nest.FilterContainer endDateNotNullFilter = new Nest.FilterContainer();
//                            Nest.QueryContainer endDateNotNullFilter = new Nest.QueryContainer();

//                            //endDateNotNullFilter &= Nest.Filter<T>.Exists(endDateKey);
//                            //endDateNotNullFilter &= Nest.Filter<T>.Range(q => q.OnField(endDateKey).GreaterOrEquals(val));
//                            //endDateNotNullFilter &= Nest.Filter<T>.Range(q => q.OnField(startDateKey).LowerOrEquals(val));
//                            endDateNotNullFilter &= Nest.Query<T>.Exists(c => c.Name(endDateKey));
//                            endDateNotNullFilter &= Nest.Query<T>.Range(q => q.Field(endDateKey).GreaterThanOrEquals(val));
//                            endDateNotNullFilter &= Nest.Query<T>.Range(q => q.Field(startDateKey).LessThanOrEquals(val));

//                            //OR both the filter
//                            dateRangeFilter &= (endDateNullFilter | endDateNotNullFilter);

//                            //Add dateRangefilter to existing filter;
//                            filter  &= dateRangeFilter;
                       
//                        }
//                    } */
//                    else if (value == "null") //To handle null filter
//                    {
//                        //filter &= Nest.Filter<T>.Missing(key);
//                        filter &= !Nest.Query<T>.Exists(c => c.Name(key));
//                    }
//                    else if (values?.Length > 1) //or filter for values
//                    {
//                        filter &= GetOrFilter(key, values);
//                    }
//                    else //Default And filter which matches the exact term
//                        //filter &= Nest.Filter<T>.Term(key, value);
//                        filter &= Nest.Query<T>.Term(key, value);
//                }
//            }

//            return filter;
//        }


//        /// <summary>
//        /// Get OR Filter for example Sql In statement In("a","b","c")
//        /// </summary>
//        /// <param name="bFilter"></param>
//        /// <param name="key"></param>
//        /// <param name="values"></param>
//        /// <returns></returns>
//        //internal static Nest.FilterContainer GetOrFilter(string key, string[] values)
//        internal static Nest.QueryContainer GetOrFilter(string key, string[] values)
//        {
//            //var bFilter = new Nest.FilterContainer();
//            var bFilter = new Nest.QueryContainer();
//            foreach (var keyValue in values)
//            {
//                //bFilter |= Nest.Filter<T>.Term(key, keyValue);
//                bFilter |= Nest.Query<T>.Term(key, keyValue);
//            }
//            return bFilter;
//        }

//        /// <summary>
//        /// Excact Match
//        /// </summary>
//        /// <param name="fieldName"></param>
//        /// <param name="searchTerm"></param>
//        /// <returns></returns>
//        //internal static Nest.FilterContainer ExactMatch(string fieldName, string searchTerm)
//        internal static Nest.QueryContainer ExactMatch(string fieldName, string searchTerm)
//        {
//            //return Nest.Filter<T>.Term(fieldName, searchTerm);
//            //return Nest.Query<T>.Bool(b => b.Must(m => m.Term(t => fieldName, searchTerm)));
//            return Query<T>.Match(x => x.Field(fieldName).Query(searchTerm));
//        }

//        /// <summary>
//        /// Add Prefix Filter
//        /// </summary>
//        /// <param name="fieldName"></param>
//        /// <param name="word"></param>
//        /// <returns></returns>
//        //internal static Nest.FilterContainer AddPrefixFilter(string fieldName, string term)
//        internal static Nest.QueryContainer AddPrefixFilter(string fieldName, string term)
//        {
//            //return Nest.QueryFilter<T>.Prefix(fieldName, term);
//            return Nest.Query<T>.Prefix(c => c.Field(fieldName)
//                                              .Value(term));
//        }

//        /// <summary>
//        /// Exhaustive filer
//        /// term : BENZALKONIUM CHLORIDE
//        /// match :ch ben [multiple words]
//        /// filter :words should contain prefix("ch") and prefix("ben)
//        /// </summary>
//        /// <param name="fieldName"></param>
//        /// <param name="term"></param>
//        /// <returns></returns>
//        //internal static Nest.FilterContainer Exhaustive(string fieldName, string term)
//        internal static Nest.QueryContainer Exhaustive(string fieldName, string term)
//        {
//            //array of filters
//            //var filter = new Nest.FilterContainer();
//            var filter = new Nest.QueryContainer();

//            //spit the words into terms 
//            //and do a and operation on all those prefixes
//            //as the word should contain all the prefixes.
//            var words = term.Split(' ');

//            foreach (var word in words)
//            {
//                filter &= AddPrefixFilter(fieldName, word.ToString());

//            }

//            return filter;
//        }

//        /// <summary>
//        /// Contains Wild Card Query
//        /// term : BETA-BLOCKERS (BETA-ADRENERGIC BLOCKING AGTS)
//        /// match :be ta in [multiple words]
//        /// wildcard: be*ta*in*
//        /// </summary>
//        /// <param name="fieldName"></param>
//        /// <param name="term"></param>
//        /// <returns></returns>
//        internal static Nest.QueryContainer ContainsWildCardQuery(string fieldName, string term)
//        {
//            // remove consecutive spaces and other whitespace characters...
//            term = Regex.Replace(term, @"\s+", " ");

//            var inTerm = term.Length > 50 ? term.Substring(0, 50).Trim() : term.Trim();

//            //for single words use full text search
//            if (inTerm.Contains(" "))
//                fieldName = string.Format("{0}", fieldName);

//            inTerm = "*" + inTerm.Replace(" ", "*") + "*";

//            return Nest.Query<T>.Wildcard(c => c.Field(fieldName).Value(inTerm));
//        }

//        internal static Nest.QueryContainer ContainsWildCardQueryOnExactField(string fieldName, string term)
//        {
//            term = Regex.Replace(term, @"\s+", " ");
//            var inTerm = term.Length > 50 ? term.Substring(0, 50).Trim() : term.Trim();

//            inTerm = "*" + inTerm.Replace(" ", "*") + "*";

//            //for multiple words use Raw analyser
//            //for single words use full text search
//            if (inTerm.Contains(" "))
//                fieldName = string.Format("{0}", fieldName);

//            inTerm = "*" + inTerm.Replace(" ", "*") + "*";

//            return Nest.Query<T>.Wildcard(c => c.Field(fieldName)
//                                                .Value(inTerm));

//        }

//        internal static Nest.QueryContainer AddMoreLikeThisQuery(IEnumerable<string> fields, string likeThis)
//        {
//            //TODO: probalby should parameraterize the other fields, but not doing it now
//            //return Nest.Query<T>.MoreLikeThis(x =>
//            //{
//            //    x.OnFields(fields);
//            //    x.LikeText(likeThis);
//            //    x.MinTermFrequency(1);
//            //    x.MaxQueryTerms(25);
//            //    x.MinWordLength(2);
//            //});

//            return Nest.Query<T>.MoreLikeThis(x =>
//                x.Fields(fields.ToArray())
//                .Like(l => l.Text(likeThis))
//                .MinTermFrequency(1)
//                .MaxQueryTerms(25)
//                .MinWordLength(2)
//            );
//        }

//        internal static Nest.QueryContainer AddMultiMatchQuery(IEnumerable<string> fields, string likeThis)
//        {
//            //TODO: probalby should parameraterize the other fields, but not doing it now
//            //return Nest.Query<T>.MultiMatch(x =>
//            //{
//            //    x.OnFields(fields);
//            //    x.Type(TextQueryType.CrossFields);
//            //    x.Query(likeThis);
//            //});

//            return Nest.Query<T>.MultiMatch(x =>
//                x.Fields(fields.ToArray())
//                .Type(TextQueryType.CrossFields)
//                .Query(likeThis)
//            );

//        }

//        /// <summary>
//        /// Updates an existing record, if present. If not, it inserts a new record.
//        /// </summary>
//        /// <param name="upsertRecord">The updated record to be updated/inserted.</param>
//        /// <param name="fieldNames">The field names.</param>
//        /// <param name="searchTerm">The search term.</param>
//        /// <param name="keyValueCollection">The key value collection (filter) that would return this (and only this) record.</param>
//        /// <param name="uniqueIdentifier">If one of the record's fields is the ID field for the type, pass this record's value. If the ID for the type is GUID, pass null.</param>
//        public SearchResults<T> UpsertDocument(object upsertRecord, List<string> fieldNames, string searchTerm, NameValueCollection keyValueCollection, string uniqueIdentifier)
//        {
//            string searchUniqueIdentifier = uniqueIdentifier;
//            SearchResults<T> searchResults = new SearchResults<T>();

//            // first, build query to get the record
//            //build body with indexname , typename, limit and offset
//            var body = BuildSearchDescriptor<T>(0, 100);

//            BuildQuery("StandardTextEqual", fieldNames, searchTerm, keyValueCollection, body, null, null);
//#if DEBUG

//            ////this is to check the query passed to NEST client
//            //ElasticClient eClientWrapper = new ElasticClient(new ConnectionSettings(new Uri(Server)));

//            ////var jsonstring1 = eClientWrapper.Serializer.Serialize(body);
//            ////TODO: Review update
//            //MemoryStream writeableStream = new MemoryStream();
//            //eClientWrapper.Serializer.Serialize(body, writeableStream, Elasticsearch.Net.SerializationFormatting.Indented);

//            ////string jsonstring = System.Text.Encoding.Default.GetString(jsonstring1);
//            //string jsonstring = System.Text.Encoding.Default.GetString(writeableStream.ToArray());
//#endif

//            // if uniqueIdentifier is empty, it means the ES ID is a GUID, so we need to get the GUID, if available, using the keyValueCollection filter.
//            // If no record is found, leave the searchUniqueIdentifier null, and a new record will be created.
//            if (string.IsNullOrEmpty(searchUniqueIdentifier))
//            {
//                ISearchResponse<T> results = _client.Search<T>(body);

//                if (results.Hits.Count > 0)
//                {
//                    searchUniqueIdentifier = results.Hits.FirstOrDefault().Id;
//                }
//            }

//            IIndexResponse response = _client.Index(upsertRecord, i => i
//                    .Type(_typeName)
//                    .Id(searchUniqueIdentifier)
//                    .Refresh(Elasticsearch.Net.Refresh.True)
//                    );


//            if (response.ServerError == null)
//            {
//                var result = _client.Search<T>(body);

//                searchResults.TotalResultCount = Convert.ToInt32(result.Total);
//                searchResults.Items = result.Documents.ToList();
//                return searchResults;
//            }
//            else
//            {
//                return null;
//            };

//        }

//        public SearchResults<T> DeleteDocument(Object deleteRecord, List<string> fieldNames, string searchTerm, NameValueCollection keyValueCollection, string uniqueIdentifier)
//        {
//            try
//            {
//                SearchResults<T> searchResults = new SearchResults<T>();
//                IDeleteResponse deleteResponse = null;
//                string searchUniqueIdentifier = uniqueIdentifier;

//                // first, build query to get the record
//                //build body with indexname , typename, limit and offset
//                var body = BuildSearchDescriptor<T>(0, 100);

//                BuildQuery("StandardTextEqual", fieldNames, searchTerm, keyValueCollection, body, null, null);
//                if (string.IsNullOrEmpty(searchUniqueIdentifier))
//                {
//                    ISearchResponse<T> results = _client.Search<T>(body);

//                    if (results.Hits.Count > 0)
//                    {
//                        searchUniqueIdentifier = results.Hits.FirstOrDefault().Id;
//                    }
//                }
//                if (!string.IsNullOrEmpty(searchUniqueIdentifier))
//                {
//                    deleteResponse = _client.Delete(new DeleteRequest(
//                   _indexName,
//                   _typeName,
//                   new Id(searchUniqueIdentifier)));
//                }
//                //If in case the search identifier is empty
//                //else
//                //{
//                //    IDeleteByQueryRequest deleteRequest = new DeleteByQueryRequest<T>
//                //    {                      
//                //        Query = ((Nest.ISearchRequest)body).Query,                       
//                //    };
//                //    deleteByQueryResponse = _client.DeleteByQuery(deleteRequest);
//                //}


//                //if (deleteResponse?.Result == Result.Deleted || deleteByQueryResponse?.Deleted>0)
//                if (deleteResponse.ServerError == null)
//                {
//                    var result = _client.Search<T>(body);

//                    searchResults.TotalResultCount = Convert.ToInt32(result.Total);
//                    searchResults.Items = result.Documents.ToList();
//                    return searchResults;
//                }
//                else
//                {
//                    return null;
//                }
//            }
//            catch (Exception e)
//            {
//                throw e;
//            }
//        }

//    }

//}
