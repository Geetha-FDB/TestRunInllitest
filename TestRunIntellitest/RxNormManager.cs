
using FDB.CC.Core.Search;

using FDB.CC.Screening.Models.V1_4.Common;


using Microsoft.Build.Framework;


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using TestRunIntellitest.Model;
using static FDB.CC.Screening.Managers.V1_4.RxNormManager.KinesisLogging;

namespace FDB.CC.Screening.Managers.V1_4
{
    public class RxNormManager //: ManagerBase
    {
        public static List<DrugConceptType> supportedRxNorms = new List<DrugConceptType>{
            DrugConceptType.RxNormSemanticBrandedDrug,
            DrugConceptType.RxNormSemanticClinicalDrug,
            DrugConceptType.RxNormBrandedPack,
            DrugConceptType.RxNormGenericPack,
            DrugConceptType.RxNorm_BrandName,
            DrugConceptType.RxNorm_Ingredient,
            DrugConceptType.RxNorm_MultipleIngredients,
            DrugConceptType.RxNorm_PreciseIngredient,
            DrugConceptType.RxNorm_SemanticBrandedDrugForm,
            DrugConceptType.RxNorm_SemanticClinicalDrugForm,
            DrugConceptType.RxNorm_SemanticBrandedDoseFormGroup,
            DrugConceptType.RxNorm_SemanticClinicalDoseFormGroup,
            DrugConceptType.RxNormUnknown
        };
        private HttpContext _httpContext;
        private readonly ILogger _logger;
        private readonly AppConfig _appConfig;
        private readonly ICacheProvider _cacheProvider;
        public RxNormManager(AppConfig appConfig, HttpContext httpContext, IServiceProvider serviceProvider, ILogger logger, ICacheProvider cacheProvider)
        {
            _appConfig = appConfig;
            _httpContext = httpContext;
            _logger = logger;
            _cacheProvider = cacheProvider;
        }
        public static bool IsSupportedRxNormType(ScreenDrug drug)
        {
            if (drug.DrugConceptType.HasValue)
            {
                return RxNormManager.supportedRxNorms.Contains(drug.DrugConceptType.Value);
            }
            return false;
        }


        public List<ScreenDrug> GetRxNormToFDBConceptTypeScreenDrugSingle(ScreenDrug drug)
        {
            List<ScreenDrug> returnDrugs = null;

            string cacheKey = $"ScreenDrugs_ExternalVocabID_{drug.DrugID}";
            returnDrugs =  (List<ScreenDrug>)_cacheProvider.Get(cacheKey);
            if (returnDrugs != null)
            {
                return returnDrugs;
            }

            string elasticSearchServer = Util.GetApplicationSetting(_appConfig, "ElasticSearchServer");
            if (RxNormManager.IsSupportedRxNormType(drug))
            {
                NameValueCollection filter = new NameValueCollection();
                filter.Add("ExternalVocabID", drug.DrugID);
                SearchResults<RxNormToFDBConcept> searchResults = null;
                try
                {
                    searchResults = new SearchClient<RxNormToFDBConcept>(null, elasticSearchServer, _appConfig.ElasticSearchIndexName).Search(null, null, null, filter, 100, 0, null);

                }
                catch (Exception ex)
                {
                  //  ApplicationLogging.LogConsole(_logger, $"***ERROR on RxNormToFDBConcept search: {ex.Message} ");
                    throw ex;
                }

                //throws an exception which is handled in referencing function
                var convertedDrugs = filterDrugs(searchResults.Items.ToList(), drug);

                if (convertedDrugs == null || !convertedDrugs.Any())
                {
                   // throw new DrugNotMappedException();
                }
                returnDrugs = new List<ScreenDrug>();
                foreach (var d in convertedDrugs)
                {
                    var sd = new ScreenDrug()
                    {
                        DrugConceptType = (DrugConceptType)Enum.Parse(typeof(DrugConceptType), d.DrugConceptType),
                        DrugDesc = drug.DrugDesc ?? d.DrugDesc,
                        DrugID = d.DrugID,
                        DrugDose = drug.DrugDose,
                        GroupSetID = drug.GroupSetID,
                        LinkedOrderID = drug.LinkedOrderID,
                        PreferredRenalDoseAdjustmentCode = drug.PreferredRenalDoseAdjustmentCode,
                        Prospective = drug.Prospective,
                        Tag = drug.Tag

                    };
                    returnDrugs.Add(sd);
                }
            }
            else
            {
                returnDrugs = new List<ScreenDrug> {drug};
            }

            if (returnDrugs.Any())
            {
                _cacheProvider.Set(cacheKey, returnDrugs, new TimeSpan(1, 0, 0, 0));  //cache....1 day
            }

            return returnDrugs;
        }

        private List<RxNormConversionDrug> filterDrugs(List<RxNormToFDBConcept> drugs, ScreenDrug drug)
        {
            #region Preferred
            var prefDisp = drugs.Where((d) =>

                d.ConceptType == "FDB.DISPGEN" && d.PreferredIndicator
            );
            if (prefDisp.Any())
            {
                return new List<RxNormConversionDrug>(){new RxNormConversionDrug()
                {
                    DrugID = prefDisp.First().ConceptID,
                    DrugConceptType = "DispensableGeneric",
                    DrugDesc = prefDisp.First().Description
                }
                };
            }
            #endregion
            else
            {

                var actives = drugs.Where((d) => d.LinkInactiveDate == null);
                if (actives.Any())
                {
                    #region Active dispgen
                    var dispGens = actives.Where((d) => d.ConceptType == "FDB.DISPGEN");
                    if (dispGens.Any())
                    {
                        var ls = new List<RxNormConversionDrug>();
                        foreach (var d in dispGens)
                        {
                            ls.Add(new RxNormConversionDrug()
                            {
                                DrugConceptType = "DispensableGeneric",
                                DrugDesc = d.Description,
                                DrugID = d.ConceptID
                            });
                        }
                        return ls;
                    }
                    #endregion
                    #region Active rtgen
                    else
                    {
                        var rtdGens = actives.Where((d) => d.ConceptType == "FDB.RTGEN");
                        if (rtdGens.Any())
                        {
                            var ls = new List<RxNormConversionDrug>();
                            foreach (var d in rtdGens)
                            {
                                ls.Add(new RxNormConversionDrug()
                                {
                                    DrugConceptType = "RoutedGeneric",
                                    DrugDesc = d.Description,
                                    DrugID = d.ConceptID
                                });
                            }
                            return ls;
                        }


                    }
                    #endregion
                }
                else
                {
                    #region inactive dispgen
                    var dispGens = drugs.Where((d) => d.ConceptType == "FDB.DISPGEN");
                    if (dispGens.Any())
                    {
                        var ls = new List<RxNormConversionDrug>();
                        foreach (var d in dispGens)
                        {
                            ls.Add(new RxNormConversionDrug()
                            {
                                DrugConceptType = "DispensableGeneric",
                                DrugDesc = d.Description,
                                DrugID = d.ConceptID
                            });
                        }
                        return ls;
                    }
                    #endregion
                    #region inactive rtgen
                    else
                    {
                        var rtdGens = drugs.Where((d) => d.ConceptType == "FDB.RTGEN");
                        if (rtdGens.Any())
                        {
                            var ls = new List<RxNormConversionDrug>();
                            foreach (var d in rtdGens)
                            {
                                ls.Add(new RxNormConversionDrug()
                                {
                                    DrugConceptType = "RoutedGeneric",
                                    DrugDesc = d.Description,
                                    DrugID = d.ConceptID
                                });
                            }
                            return ls;
                        }


                    }
                    #endregion
                }
                #region ingredient
                //Only care about route if we've gotten here because this is the first place we absolutely 100% require it. Per chat with Divya.
                if (string.IsNullOrWhiteSpace(drug?.DrugDose?.Route))
                {
                    //throw new RouteNotProvidedException();
                }
                return GetFallbackDrugsBasedOnRoute(drug);
                #endregion

            }

        }
        internal List<RxNormConversionDrug> GetFallbackDrugsBasedOnRoute(ScreenDrug drugToSearch)
        {
            List<RxNormConversionDrug> returnDrugs = null;
            if (drugToSearch?.DrugDose != null && !string.IsNullOrEmpty(drugToSearch.DrugDose.Route))
            {
                string cacheKey = $"RxCuiID_RouteDesc_{drugToSearch.DrugID}_{drugToSearch.DrugDose.Route}";
                returnDrugs = (List<RxNormConversionDrug>)_cacheProvider.Get(cacheKey);
                if (returnDrugs != null)
                {
                    return returnDrugs;
                }
                string elasticSearchServer = Util.GetApplicationSetting(_appConfig, "ElasticSearchServer");
                NameValueCollection filter = new NameValueCollection();
                filter.Add("RxCuiID", drugToSearch.DrugID);
                //filter.Add("RouteDescLong", drugToSearch.DrugDose.Route);
                //filter.Add("RouteDescShort", drugToSearch.DrugDose.Route);
                string searchFilter =
                    $"(RouteDescLong:'{drugToSearch.DrugDose.Route}' OR RouteDescShort:'{drugToSearch.DrugDose.Route}')";
                //search for drugs that are mapped to the rxCui and route.
                int limit = 100;
                int offset = 0;
                var searchResults =
                    new SearchClient<RxCuiToFDBDrugConcept>(null, elasticSearchServer,
                            _appConfig.ElasticSearchIndexName)
                        .Search(null, null, null, filter, limit, offset, searchFilter);
                if (searchResults != null && searchResults.TotalResultCount > 0)
                {
                    returnDrugs = new List<RxNormConversionDrug>();
                    var drugs = searchResults.Items;
                    if (drugs.Any())
                    {
                        foreach (var item in drugs.OrderBy(d => d.Score).Take(1))
                        {
                            returnDrugs.Add(new RxNormConversionDrug()
                            {
                                DrugConceptType = item.DrugConceptType,
                                DrugDesc = item.DrugDesc,
                                DrugID = item.DrugID
                            });

                        }
                    }
                }
                if (returnDrugs != null && returnDrugs.Count > 0)
                {
                    _cacheProvider.Set(cacheKey, returnDrugs, new TimeSpan(1, 0, 0, 0));  //cache....1 day
                }
            }

            return returnDrugs;
        }
        public class RxCuiToFDBDrugConcept
        {
            public string DrugID { get; set; }
            public int RouteID { get; set; }
            public string RouteDesc { get; set; }
            public string RoutedGenID { get; set; }
            public string RoutedGenDescription { get; set; }
            public string DrugDesc { get; set; }
            public string DrugConceptType { get; set; }
            public int Score { get; set; }
        }
        public class RxNormToFDBConcept
        {
            public RxNormToFDBConcept()
            {

            }

            public string ExternalVocabID { get; set; }
            public string ExternalVocabTypeCode { get; set; }
            public string Description { get; set; }
            public string ExternalVocabCategoryCode { get; set; }
            public string FDBUnivID { get; set; }
            public bool PreferredIndicator { get; set; }
            public bool DAMPicklistIndicator { get; set; }
            public bool MultiSetIndicator { get; set; }
            public bool RelatedIndicator { get; set; }
            public string RxCui { get; set; }
            public string LinkAddDate { get; set; }
            public string LinkInactiveDate { get; set; }
            public string SnomedValueSetID { get; set; }
            public string ConceptType { get; }
            public string ConceptID { get; }
        }
       

        public class AppConfig
        {

            public string AWSRegion { get; set; }

            public string ElasticSearchServer { get; set; }

            public string ElasticSearchIndexName { get; set; }

            public KinesisLogging KinesisLogging { get; set; } = new KinesisLogging();

            public string TokenKey { get; set; }

        }

        public class KinesisLogging
        {
            private string _responseStream;
            private string _exceptionStream;

            private string _exceptionDetailStream;

            private string _dataStream;

            private string _logSearchStream;

            private string _kinesisCustomStream;
            private string _kinesisSigStream;

            public string KinesisRegion { get; set; }

            public string KinesisResponseStream
            {
                get
                {
                    var responseStream = Environment.GetEnvironmentVariable("KinesisResponseStream");
                    if (String.IsNullOrWhiteSpace(responseStream))
                    {
                        responseStream = _responseStream;
                    }

                    return responseStream;
                }
                set { _responseStream = value; }
            }

            public string KinesisExceptionStream
            {
                get
                {
                    var responseStream = Environment.GetEnvironmentVariable("KinesisExceptionStream");
                    if (String.IsNullOrWhiteSpace(responseStream))
                    {
                        responseStream = _exceptionStream;
                    }

                    return responseStream;
                }
                set { _exceptionStream = value; }
            }

            public string KinesisExceptionDetailStream
            {
                get
                {
                    var responseStream = Environment.GetEnvironmentVariable("KinesisExceptionDetailStream");
                    if (String.IsNullOrWhiteSpace(responseStream))
                    {
                        responseStream = _exceptionDetailStream;
                    }

                    return responseStream;
                }
                set { _exceptionDetailStream = value; }
            }
            public class RxNormConversionDrug
            {
                public RxNormConversionDrug()
                {

                }

                
                public string DrugID { get; set; }
               
                public string DrugConceptType { get; set; }
               
                public string DrugDesc { get; set; }
            }

            public static class Util
            {
                public static string GetApplicationSetting(AppConfig appConfig, string variable)
                {
                    string response = "";
                    // first, see if there is an environmental variable the requested setting
                    try
                    {
                        response = Environment.GetEnvironmentVariable(variable);
                    }
                    catch (Exception)
                    {
                    }

                    // if no environmental variable, look in the app config
                    if (string.IsNullOrEmpty(response))
                    {
                        response = (string)appConfig.GetType().GetProperty(variable).GetValue(appConfig, null);
                    }
                    return response;
                }


                // The DrugConceptType Enum values don't match up with the Drug Codes used in the MKF tables
                // so if you are using MKF table directly, you will need to get the Drug Code for the Drug Concept Type you are using.
                public static string GetDrugCodeFromDrugConceptType(DrugConceptType drugConceptType)
                {
                    string drugCode = "";

                    switch (drugConceptType)
                    {
                        case DrugConceptType.DispensableGeneric:
                            drugCode = "01";
                            break;
                        case DrugConceptType.DispensableDrug:
                            drugCode = "06";
                            break;
                        case DrugConceptType.RoutedDoseFormDrug:
                            drugCode = "05";
                            break;
                        case DrugConceptType.RoutedDoseFormGeneric:
                            drugCode = "03";
                            break;
                        case DrugConceptType.RoutedDrug:
                            drugCode = "04";
                            break;
                        case DrugConceptType.RoutedGeneric:
                            drugCode = "02";
                            break;
                        default:
                            break;
                    }
                    return drugCode;
                }
            }
            public string KinesisDataStream
            {
                get
                {
                    var responseStream = Environment.GetEnvironmentVariable("KinesisDataStream");
                    if (String.IsNullOrWhiteSpace(responseStream))
                    {
                        responseStream = _dataStream;
                    }

                    return responseStream;
                }
                set { _dataStream = value; }
            }

        }
    }
}
