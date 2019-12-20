//using FDB.CC.Core.Common.Exceptions;
//using FDB.CC.Core.Common.Logging;
//using FDB.CC.Core.Common.Utilities;
//using FDB.CC.Core.Search;
//using FDB.CC.Model.V1_3;
//using FDB.CC.Model.V1_3.CoreDrug;
//using FDB.CC.Screening.Common;
//using FDB.CC.Screening.Models.V1_4.Common;
//using FDB.CC.Screening.Models.V1_4.SingleDrugScreening;

//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;

//using Nest;

//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Linq;

//using ScreenCondition = FDB.CC.Model.V1_4.Screening.ScreenCondition;
//using ScreenDrug = FDB.CC.Model.V1_4.Screening.ScreenDrug;
//using NoteType = FDB.CC.Model.V1_3.NoteType;
//using Note = FDB.CC.Model.V1_3.Note;

//namespace FDB.CC.Screening.Managers.V1_4.SingleDrugScreen
//{
//    public class SingleDrugScreenManager : ManagerBase
//    {
//        private readonly HttpContext _httpContext;
//        private readonly AppConfig _appConfig;
//        private readonly ILogger _logger;
//        // temporary hack for active SDA DxID
//        private const string HyperKalemiaDxID = "753";
//        private const string QtDxID = "1566";
//        private readonly HashSet<string> _HyperkalemiaDxIds = new HashSet<string> { $"{HyperKalemiaDxID}" };
//        private readonly HashSet<string> _QTDxIds = new HashSet<string> { $"{QtDxID}" };
//        private const string SINGLE_DRUG_SCREEN = "Single Drug Service";
//        private readonly string _elasticSearchServer;
//        private readonly Core.Caching.Models.External.ICacheProvider _cacheProvider;




//        ConcurrentBag<SDClinicalConsequenceScreenResult> sdaScreenResults = new ConcurrentBag<SDClinicalConsequenceScreenResult>();
//        ConcurrentDictionary<string, string> Hic3List = new ConcurrentDictionary<string, string>();
//        List<ScreenDrugConverted> screenDrugConvertedList = new List<ScreenDrugConverted>();
//        QueryContainer drugFilter = new QueryContainer();
//        bool isFilter = false;



//        public SingleDrugScreenManager(ILogger logger, AppConfig appConfig, HttpContext httpContext, IServiceProvider serviceProvider, Core.Caching.Models.External.ICacheProvider cacheProvider)
//        {
//            _logger = logger;
//            _appConfig = appConfig;
//            _httpContext = httpContext;
//            _elasticSearchServer = Infrastructure.Util.GetApplicationSetting(_appConfig, "ElasticSearchServer");
//            _cacheProvider = cacheProvider;
//        }

//        public SDScreenResponse SingleDrugScreen(SDScreenRequest request, RxNormManager rxNormManager)
//        {

//            ClaimsIdentityHelper.ThrowIfNotInRole(_httpContext, SINGLE_DRUG_SCREEN);
//            ClaimsIdentityHelper.ThrowIfNotInRole(_httpContext, ClaimsIdentityHelper.WIP);
//            List<ScreenDrug> drugsToScreen = request.ScreenProfile.ScreenDrugs;
//            List<string> unscreenedConditions = new List<string>();

//            SDScreenResponse response = new SDScreenResponse
//            {
//                IsSuccessful = true,
//                SDClinicalConsequenceScreenResponse = new SDClinicalConsequenceScreenResponse { IsSuccessful = true, Notes = new List<Note>() }
//            };

//            List<ScreenCondition> screenConditions = new List<ScreenCondition>();

//            HashSet<string> dxIds = new HashSet<string>();

            
//            bool isFromExternalCDS = request.ScreenProfile.ScreenDrugs.Where(x => x.Tag.ToUpper().Contains("180419EXTERNALCDS")).Any();

//            switch (request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceID)
//            {
//                case HyperKalemiaDxID:
//                    dxIds = _HyperkalemiaDxIds;
//                    break;
//                case QtDxID:
//                    dxIds = _QTDxIds;
//                    break;
//            }
//            foreach (var dxId in dxIds)
//            {
//                ScreenCondition screenCondition = new ScreenCondition
//                {
//                    ConditionID = dxId,
//                    ConditionConceptType = ConditionConceptType.DXID
//                };
//                screenConditions.Add(screenCondition);
//            }

//            ValidateSDClinicalConsequenceScreenRequest(request, ref response, ref screenConditions);
            
//            if (screenConditions.Count > 0)
//            {
//                var searchClient = new SearchClient<ClinicalConsequenceSingleDrug>(null, _elasticSearchServer, _appConfig.ElasticSearchIndexName);
//                SearchResults<ClinicalConsequenceSingleDrug> singleDrugScreenSearchResults;



//                // these filters are the same for all drugs so only build them once
//                QueryContainer queryContainer = new QueryContainer();

//                if (request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceMechanismOfActionFilter.HasValue)
//                {
//                    // var filter1 = Nest.Query<SingleDrugScreenManager>.Term("ClinicalConsequenceMechanismOfAction", request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceMechanismOfActionFilter);
//                    //queryContainer &= filter1;
//                    NumericRangeQuery rangeQuery = new NumericRangeQuery();
//                    rangeQuery.Field = "ClinicalConsequenceMechanismOfAction";
//                    rangeQuery.LessThanOrEqualTo = request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceMechanismOfActionFilter.Value;
//                    queryContainer &= rangeQuery;
//                }
//                foreach (ScreenCondition screenCondition in screenConditions)
//                {
//                    if (_HyperkalemiaDxIds.Contains(screenCondition.ConditionID, StringComparer.OrdinalIgnoreCase)
//                        || _QTDxIds.Contains(screenCondition.ConditionID, StringComparer.OrdinalIgnoreCase))
//                    {
//                        QueryContainer filter = new QueryContainer();
//                        filter &= Nest.Query<SingleDrugScreenManager>.Term("DxID", screenCondition.ConditionID); //Condition filter
//                        queryContainer &= filter;
//                    }
//                    else
//                    {
//                        unscreenedConditions.Add(screenCondition.ConditionID);
//                    }
//         // ===================================================================================================================
//                    ApplicationLogging.LogConsole(_logger, $"┌── Build Screen Filter ─────────────────────────────────────");
//                    foreach (ScreenDrug screenDrug in request.ScreenProfile.ScreenDrugs)
//                    {
//                        ApplicationLogging.LogConsole(_logger, $"│ ScreenDrug: {screenDrug.DrugID.ToString(),-9}: {screenDrug.DrugDesc} - {screenDrug.DrugConceptType}");
//                        //add RXNorm Support - for single RxNorm drug, multiple drugs are returned
//                        if (RxNormManager.IsSupportedRxNormType(screenDrug))
//                        {
//                            try
//                            {
//                                List<ScreenDrug> screenDrugFdbList = rxNormManager.GetRxNormToFDBConceptTypeScreenDrugSingle(screenDrug);
//                                ApplicationLogging.LogConsole(_logger, $"│   GetRxNormToFDBConceptTypes...");
//                                BuildFilterFromScreenDrugs(screenDrugFdbList, screenDrug);
//                            }
//                            catch (DrugNotMappedException)
//                            {
//                                ApplicationLogging.LogConsole(_logger, $"│   DrugNotMappedException: {screenDrug.DrugID} - {screenDrug.DrugDesc}");
//                                response.SDClinicalConsequenceScreenResponse.AddNote(new Note()
//                                {
//                                    NoteType = Model.V1_3.NoteType.NotFound,
//                                    ItemDescription = screenDrug.DrugDesc,
//                                    ItemID = screenDrug.DrugID,
//                                    ItemIDType = screenDrug.DrugConceptType.ToString(),
//                                    MessageText = "Drug not mapped"
//                                });
//                                response.SDClinicalConsequenceScreenResponse.IsSuccessful = false;
//                            }
//                            catch (RouteNotProvidedException)
//                            {
//                                ApplicationLogging.LogConsole(_logger, $"  RouteNotProvided");
//                                response.SDClinicalConsequenceScreenResponse.AddNote(new Note()
//                                {
//                                    NoteType = NoteType.RequestValidationFailed,
//                                    ItemDescription = screenDrug.DrugDesc,
//                                    ItemID = screenDrug.DrugID,
//                                    ItemIDType = screenDrug.DrugConceptType.ToString(),
//                                    MessageText = "Route not provided"
//                                });
//                                response.SDClinicalConsequenceScreenResponse.IsSuccessful = false;
//                            }
//                        }
//                        else if (screenDrug.DrugConceptType == DrugConceptType.PackagedDrug)
//                        {
//                            ScreenDrug sd = GetScreenDrugFromPackagedDrug(screenDrug.DrugID);
//                            if (sd != null)
//                            {
//                                BuildFilterFromScreenDrugs(new List<ScreenDrug> { sd }, screenDrug);
//                            }
//                            else
//                            {
//                                ApplicationLogging.LogConsole(_logger, $"│   DrugNotMappedException: {screenDrug.DrugID} - {screenDrug.DrugDesc}");
//                                response.SDClinicalConsequenceScreenResponse.AddNote(new Note()
//                                {
//                                    NoteType = NoteType.NotFound,
//                                    ItemDescription = screenDrug.DrugDesc,
//                                    ItemID = screenDrug.DrugID,
//                                    ItemIDType = screenDrug.DrugConceptType.ToString(),
//                                    MessageText = "Drug not mapped"
//                                });
//                                response.SDClinicalConsequenceScreenResponse.IsSuccessful = false;

//                            }
//                        }
//                        else
//                        {
//                            BuildFilterFromScreenDrugs(new List<ScreenDrug> { screenDrug }, screenDrug);

//                        }

//                    }
//                    ApplicationLogging.LogConsole(_logger, $"└─────────────────────────────────────────────────────────────");
//                    if (isFilter)
//                    {
//                        queryContainer &= drugFilter;
                        
//                        ApplicationLogging.LogConsole(_logger, "Perform Search.");
//                        // ===============================================================================================
//                        singleDrugScreenSearchResults = searchClient.Search(queryContainer, 1000, 0);    //One search returns all results
//                        // ===============================================================================================
//                        ApplicationLogging.LogConsole(_logger, "Done with Screen.");

//                        QueryContainer genericRoutedDrugFilter = new QueryContainer();


//                        try
//                        {
//                            ApplicationLogging.LogConsole(_logger, $"┌── SDA Screen Response ──────────────────────────────────────");
//                            singleDrugScreenSearchResults.Items.ToList().ForEach(x => ApplicationLogging.LogConsole(_logger, $"│ SideEffect ID:{x.SideEffectID.ToString(),-4} DrugID: {x.DrugID.ToString(),-9}  DrugConceptType:  {x.DrugConceptType, -35} DrugDesc: {x.DrugDesc}  "));
//                            ApplicationLogging.LogConsole(_logger, $"└─────────────────────────────────────────────────────────────");
//                            //Now loop through the result and build the response. 
//                            //Parallel.ForEach(singleDrugScreenSearchResults.Items, result =>    
//                            foreach (var result in singleDrugScreenSearchResults.Items)
//                            {

//                                //ApplicationLogging.LogConsole(_logger,$"Search for screen drug: {result.DrugID} - {result.DrugConceptType} ");
//                                //find ScreenDrug to send on response 
//                                ScreenDrug screenDrug = request.ScreenProfile.ScreenDrugs.FirstOrDefault(x => x.DrugID == result.DrugID && x.DrugConceptType == result.DrugConceptType);
//                                string hic3 = string.Empty;
//                                if (screenDrug == null && screenDrugConvertedList != null)
//                                {
//                                    //ApplicationLogging.LogConsole(_logger,$" **Not found. Look in FdbToRxNormList:");
//                                    var screenDrugFdbToRxNorm = screenDrugConvertedList.FirstOrDefault(x => x.DrugIDFdb == result.DrugID && x.DrugConceptTypeFdb == result.DrugConceptType);
//                                    if (screenDrugFdbToRxNorm != null)
//                                    {
//                                        //ApplicationLogging.LogConsole(_logger," Found.");
//                                        screenDrug = screenDrugFdbToRxNorm.OriginalScreenDrug;
//                                        hic3 = screenDrugFdbToRxNorm.Hic3Fdb;
//                                    }
//                                    else
//                                    {
//                                        //ApplicationLogging.LogConsole(_logger,$"  **Not found.");
//                                    }
//                                }

//                                if (String.IsNullOrWhiteSpace(hic3))
//                                {
//                                    Hic3List.TryGetValue(screenDrug.DrugID, out hic3);
//                                }
//                                var screenResult = new SDClinicalConsequenceScreenResult
//                                {
//                                    ScreenCondition = screenCondition,
//                                    ScreenDrug = screenDrug,
//                                    DrugConceptType = result.DrugConceptType,
//                                    DrugDesc = result.DrugDesc,
//                                    DrugID = result.DrugID,
//                                    DxID = result.DxID,
//                                    SideEffectID = result.SideEffectID,
//                                    SideEffectDiseaseDesc = result.SideEffectDiseaseDesc,
//                                    MoreInfoVerbage = result.MoreInfoVerbage,
//                                    PIReference = result.PIReference,
//                                    ClinicalConsequenceMechanismOfAction = result.ClinicalConsequenceMechanismOfAction,
//                                    ClinicalConsequenceLikelihood = result.ClinicalConsequenceLikelihood,
//                                    Hic3 = hic3 is null ? string.Empty : hic3

//                                };
//                                sdaScreenResults.Add(screenResult);

//                                //AddGenericRoutedMedFilter(ref genericRoutedDrugFilter, result.DrugID, result.DrugConceptType.Value);
//                                //});
//                            }
                            
                            
//                            response.SDClinicalConsequenceScreenResponse.SDScreenByConditonResult = sdaScreenResults.ToList();
//                        }
//                        catch (Exception ex)
//                        {
//                            ApplicationLogging.LogConsole(_logger,$"EXCEPTION: {ex.Message}");
//                            response.SDClinicalConsequenceScreenResponse.Notes.Add(
//                                    new Note
//                                    {
//                                        NoteType = NoteType.ScreenInputErrorMessage,
//                                        MessageText = ex.Message
//                                    });
//                            response.IsSuccessful = false;
//                            response.SDClinicalConsequenceScreenResponse.SDScreenByConditonResult = new List<SDClinicalConsequenceScreenResult>();
//                            response.SDClinicalConsequenceScreenResponse.IsSuccessful = false;
//                        }
//                    }
//                }
//            }
//            else
//            {
//                response.SDClinicalConsequenceScreenResponse.SDScreenByConditonResult = new List<SDClinicalConsequenceScreenResult>();
//                response.SDClinicalConsequenceScreenResponse.IsSuccessful = false;
//            }

//            Guid callId = Guid.Parse(ClaimsIdentityHelper.GetServiceCallID(_httpContext));
//            response.ServiceCallID = callId;

//            ApplicationLoggingHelper.BigDataLog<SDScreenRequest, SDScreenResponse>(request, response, _logger, _httpContext);

//            return response;
//        }


//        private void BuildFilterFromScreenDrugs(List<ScreenDrug> convertedScreenDrugs, ScreenDrug screenDrug)
//        {
//            string drugDesc = string.Empty;
//            string Hic3 = string.Empty;

//            drugDesc = string.Empty;
//            string genRoutedMedDrugName = string.Empty;
//            foreach (ScreenDrug screenDrugFdb in convertedScreenDrugs)  // loop because an RxCui could return more than one drug.
//            {
//                (genRoutedMedDrugName, Hic3) = SearchGenericRoutedMed(screenDrugFdb.DrugID, screenDrugFdb.DrugConceptType.ToString());
//                if (drugDesc == string.Empty)
//                {
//                    drugDesc = screenDrugFdb.DrugDesc;  // ignore the results from generic routed med.
//                }
//                if (!String.IsNullOrEmpty(drugDesc))
//                {
//                    ApplicationLogging.LogConsole(_logger, $"│   - FDBConcept: {screenDrugFdb.DrugID}: {drugDesc} - {screenDrugFdb.DrugConceptType}  Hic3: {Hic3}");
//                    screenDrug.DrugDesc = drugDesc;
//                    //screenDrug.DrugDesc = screenDrugFdb.DrugDesc;

//                }
//                else
//                {
//                    ApplicationLogging.LogConsole(_logger, $"│   - FDBConcept: {screenDrugFdb.DrugID}: {drugDesc} - Generic Routed Drug Not Found.");
//                }
//                ScreenDrugConverted screenDrugConverted = new ScreenDrugConverted()
//                {
//                    OriginalScreenDrug = screenDrug,
//                    DrugIDFdb = screenDrugFdb.DrugID,
//                    DrugConceptTypeFdb = screenDrugFdb.DrugConceptType,
//                    Hic3Fdb = Hic3
//                };
//                if (!String.IsNullOrWhiteSpace(Hic3))
//                {
//                    Hic3List.TryAdd(screenDrugConverted.DrugIDFdb, Hic3);
//                }

//                screenDrugConvertedList.Add(screenDrugConverted);
//                bool reply = AddScreenDrugFilters(ref drugFilter, screenDrugFdb.DrugID, screenDrugFdb.DrugConceptType);
//                if (reply)
//                {
//                    isFilter = true;
//                }
//            }

//        }


//        private void ValidateSDClinicalConsequenceScreenRequest(SDScreenRequest request, ref SDScreenResponse response, ref List<ScreenCondition> screenConditions)
//        {
//            Validate(request);
//            if ((request.ScreenProfile.ScreenDrugs == null) || (request.ScreenProfile.ScreenDrugs.Count == 0))
//            {
//                throw new ValidationException(string.Format(Util.GetResourceMessage("RequiredFieldMissing"), "ScreenDrugs"), ValidationType.Missing);
//            }
//            // if we want to get really strict, you could check to make sure every drug has an ID and ConceptType
//            if (string.IsNullOrWhiteSpace(request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceID))
//            {
//                throw new ValidationException(string.Format(Util.GetResourceMessage("RequiredFieldMissing"), "ClinicalConsequenceID"), ValidationType.Missing);
//            }
//            if ((request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceID != HyperKalemiaDxID && request.SDClinicalConsequenceScreenRequest.ClinicalConsequenceID != QtDxID))
//            {
//                throw new NotImplementedException();
//            }
//            //if ((request.SDClinicalConsequenceScreenRequest.ProspectiveOnly.GetValueOrDefault() == true))
//            //{            
//            //    // TODO: put text in resource file (there are 2 so we need to decide which one to use)
//            //    response.SDClinicalConsequenceScreenResponse.Notes.Add(
//            //        new Note
//            //        {
//            //            NoteType = NoteType.RequestValidationFailed, 
//            //            MessageText = "Screen request was Prospective Only, but there were no Screen Condition with Prospective equal to true."                        
//            //        });
//            //}
//        }



//        private bool AddScreenDrugFilters(ref QueryContainer queryContainer, string drugId, DrugConceptType? nullableDrugConceptType)
//        {
//            ApplicationLogging.LogConsole(_logger, $"│     = Add To Filter: {drugId}, {nullableDrugConceptType}");
//            QueryContainer andQuery = null;
//            QueryContainer matchQuery = null;
            
//            DrugConceptType drugConceptType = nullableDrugConceptType.GetValueOrDefault();
//            if ((drugConceptType == DrugConceptType.RoutedGeneric) ||
//                (drugConceptType == DrugConceptType.DispensableDrug) ||
//                (drugConceptType == DrugConceptType.DispensableGeneric)
//            )
//            {

//                andQuery = Query<SingleDrugScreenManager>.Term("DrugID", drugId);
//                matchQuery = Query<SingleDrugScreenManager>.Match(m => m.Field("DrugConceptType").Query(drugConceptType.ToString()));
//                andQuery &= matchQuery;
//                queryContainer |= andQuery;
//                return true;
//            }
//            else
//            {
//                ApplicationLogging.LogConsole(_logger, $"│   = NOT ADDED TO FILTER. Invalid DrugConcept: {drugConceptType.ToString()}");
//                return false;
//            }
//        }

//        private (string,string) SearchGenericRoutedMed(string DrugID, string DrugConceptType)
//        {
//            bool useCache = true;

//            string GRMDrugDesc = string.Empty;
//            string Hic3 = string.Empty;

//            string cacheKey = $"ScreenDrug_GenericRoutedDrug_{DrugID}_{DrugConceptType}";
//            if (useCache)
//            {
//                string cacheResult = (string)_cacheProvider.Get(cacheKey);
//                if (!string.IsNullOrWhiteSpace(cacheResult))
//                {
//                    if (cacheResult.Contains("|"))
//                    {
//                        GRMDrugDesc = cacheResult.Split('|')[0];
//                        Hic3 = cacheResult.Split('|')[1];
//                    }
//                    else
//                    {
//                        GRMDrugDesc = cacheResult;
//                        Hic3 = string.Empty;
//                    }
//                }
//            }
            
//            if (! String.IsNullOrEmpty(GRMDrugDesc))
//            {
//                ApplicationLogging.LogConsole(_logger, $"│    - Generic Routed Drug ID: {DrugID, -9}  Drug Concept Type: {DrugConceptType, -35}  Generic Routed Drug: {GRMDrugDesc} Hic3: {Hic3} Found in cache.");
//                return (GRMDrugDesc, Hic3);
//            }

//            QueryContainer searchQuery = Query<SingleDrugScreenManager>.Term("DrugID", DrugID);
//            QueryContainer matchQuery = Query<SingleDrugScreenManager>.Match(m => m.Field("DrugConceptType").Query(DrugConceptType.ToString()));
//            searchQuery &= matchQuery;
//            var searchClient = new SearchClient<GenericRoutedDrug>(null, _elasticSearchServer, _appConfig.ElasticSearchIndexName);
//            SearchResults<GenericRoutedDrug> genericRoutedMedResults = null;
//            try
//            {
//                genericRoutedMedResults = searchClient.Search(searchQuery, 1000, 0);

//            }
//            catch (Exception ex)
//            {
//                ApplicationLogging.LogConsole(_logger, $"│   ***ERROR on GenericRoutedMed search: {ex.Message} ");
//                throw ex;
//            }

//            var result = genericRoutedMedResults.Items.ToList().FirstOrDefault();
//            if (result is null)
//            {
//                ApplicationLogging.LogConsole(_logger, $"│   Generic Routed Drug ID: {DrugID, -9}  Drug Concept Type: {DrugConceptType, -35}  == NOT FOUND ==");
//                return (string.Empty,string.Empty);
//            }

//            GRMDrugDesc = result.GenericRoutedDrugDesc.ToLower();
//            Hic3 = result.Hic3;
//            ApplicationLogging.LogConsole(_logger, $"│   Generic Routed Drug ID: {DrugID, -9}  Drug Concept Type: {DrugConceptType, -35}  Generic Routed Drug: {GRMDrugDesc} Hic3: {Hic3} Found with search.");

//            _cacheProvider.Set(cacheKey, $"{GRMDrugDesc}|{Hic3}", new TimeSpan(1, 0, 0, 0));  //cache....1 day

//            return (GRMDrugDesc, Hic3);
//        }

//        private ScreenDrug GetScreenDrugFromPackagedDrug(string packagedDrugId)
//        {
//            ScreenDrug screenDrug = null;

//            string cacheKey = $"PackagedDrug_{packagedDrugId}";
//            screenDrug = (ScreenDrug)_cacheProvider.Get(cacheKey);
//            if (screenDrug != null)
//            {
//                return screenDrug;
//            }

//            NameValueCollection filter = new NameValueCollection
//            {
//                { "PackagedDrugID", packagedDrugId }
//            };
//            var searchClient = new SearchClient<FDB.CC.Model.V1_3.CoreDrug.PackagedDrug>(null, _elasticSearchServer, _appConfig.ElasticSearchIndexName);
//            var searchResults = searchClient.Search(null, null, null,filter, 1, 0, null);            

//            GetPackagedDrugResponse response = new GetPackagedDrugResponse();
//            if (searchResults.Items != null && searchResults.Items.Any())
//            {
//                PackagedDrug drug = searchResults.Items.FirstOrDefault();
//                if (drug != null)
//                {
//                    screenDrug = new ScreenDrug
//                    {
//                        DrugConceptType = DrugConceptType.DispensableGeneric,
//                        DrugID = drug.DispensableGenericID,
//                        DrugDesc = drug.DispensableGenericDesc
//                    };
//                }
//            }
//            if (screenDrug == null)
//            {
//                ApplicationLogging.LogConsole(_logger, $"│ Drug ID: {packagedDrugId}  Drug Concept Type: PackagedDrug  == NOT FOUND ==");
//            }


//            if (screenDrug != null)
//            {
//                _cacheProvider.Set(cacheKey, screenDrug, new TimeSpan(1, 0, 0, 0));  //cache....1 day
//            }

//            return screenDrug;
//        }

//    }
//}
