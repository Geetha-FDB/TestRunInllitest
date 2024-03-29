using System.Collections.Generic;
using TestRunIntellitest.Model;
// <copyright file="RxNormManagerTest.cs">Copyright ©  2019</copyright>

using System;
using FDB.CC.Screening.Managers.V1_4;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FDB.CC.Screening.Managers.V1_4.Tests
{
    [TestClass]
    [PexClass(typeof(RxNormManager))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class RxNormManagerTest
    {

        [PexMethod]
        [PexAllowedException(typeof(NullReferenceException))]
        public List<ScreenDrug> GetRxNormToFDBConceptTypeScreenDrugSingle([PexAssumeUnderTest]RxNormManager target, ScreenDrug drug)
        {
            List<ScreenDrug> result = target.GetRxNormToFDBConceptTypeScreenDrugSingle(drug);
            return result;
            // TODO: add assertions to method RxNormManagerTest.GetRxNormToFDBConceptTypeScreenDrugSingle(RxNormManager, ScreenDrug)
        }
    }
}
