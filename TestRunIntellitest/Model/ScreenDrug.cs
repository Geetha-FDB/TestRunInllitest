using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunIntellitest.Model
{
    public class ScreenDrug
    {
       
        
        public string DrugID { get; set; }
        
        public DrugConceptType? DrugConceptType { get; set; }
        
        public string DrugDesc { get; set; }
        
        public bool? Prospective { get; set; }
        
        public string GroupSetID { get; set; }
        
        public string LinkedOrderID { get; set; }
        
        public string Tag { get; set; }
        
        public string PreferredRenalDoseAdjustmentCode { get; set; }
        
        public DrugDose DrugDose { get; set; }

       
    }

    
    public class DrugDose
    {
        public DrugDose()
        {

        }

      
        public string DosingFrequencyInterval { get; set; }
      
        public decimal? QuantityDispensed { get; set; }
        
        public string Route { get; set; }
        
        public string PreviousDoseUnit { get; set; }
       
        public decimal? PreviousDoseAmount { get; set; }
        
        public string ConditionDesc { get; set; }
       
        public ConditionConceptType? ConditionConceptType { get; set; }
       
        public string ConditionID { get; set; }
        
        public decimal? DaysSupply { get; set; }
       
        public string DurationUnit { get; set; }
        
        public decimal? DurationAmount { get; set; }
       
        public DoseType? DoseType { get; set; }
       
        public string SingleDoseUnit { get; set; }
       
        public decimal? SingleDoseAmount { get; set; }
        
        public string DrugFormName { get; set; }

       
    }

    public enum DrugConceptType
    {
        PackagedDrug = 1,
        DispensableGeneric = 2,
        DispensableDrug = 3,
        RoutedDoseFormDrug = 4,
        RoutedDoseFormGeneric = 5,
        RoutedDrug = 6,
        RoutedGeneric = 7,
        RxNormSemanticBrandedDrug = 8,
        RxNormSemanticClinicalDrug = 9,
        RxNormBrandedPack = 10,
        RxNormGenericPack = 11,
        RxNorm_BrandName = 12,
        RxNorm_Ingredient = 13,
        RxNorm_MultipleIngredients = 14,
        RxNorm_PreciseIngredient = 15,
        RxNorm_SemanticBrandedDrugComponent = 16,
        RxNorm_SemanticBrandedDrugForm = 17,
        RxNorm_SemanticClinicalDrugComponent = 18,
        RxNorm_SemanticClinicalDrugForm = 19,
        RxNorm_UNICode_NLMSpecified = 20,
        RxNorm_SemanticBrandedDoseFormGroup = 21,
        RxNorm_SemanticClinicalDoseFormGroup = 22,
        DrugName = 23,
        GenericDrug = 24,
        CVXVaccinesAdministered = 25,
        MVXManufacturersOfVaccines = 26,
        RxNormUnknown = 27,
        OrderableMed = 28
    }

    public enum ConditionConceptType : int
    {
       
        ICD9DiseaseInjury = 1,
        
        ICD9Procedures = 2,
       
        ICD9VCodes = 3,
        
        ICD9ECodes = 4,
        
        ICD10CM = 5,
        
        ICD10PCS = 6,
        
        DXID = 7,
        
        SNOMED = 9
    }

    public enum DoseType : int
    {
        
        Loading = 1,
        
        Maintenance = 2,
        
        Post_Dialysis = 3,
        
        Test = 6,
        
        Single = 7
    }
}
