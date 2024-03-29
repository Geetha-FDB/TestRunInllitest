// <copyright file="ProgramTest.cs">Copyright ©  2019</copyright>

using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestRunIntellitest;

namespace TestRunIntellitest.Tests
{
    [TestClass]
    [PexClass(typeof(Program))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class ProgramTest
    {

        [PexMethod]
        public bool isPrime(int number)
        {
            bool result = Program.isPrime(number);
            return result;
            // TODO: add assertions to method ProgramTest.isPrime(Int32)
        }

        //[PexMethod]
        //public ScreenResponse Screen([PexAssumeUnderTest]Program target, ScreenRequest screenRequest)
        //{
        //    ScreenResponse result = target.Screen(screenRequest);
        //    return result;
        //    // TODO: add assertions to method ProgramTest.Screen(Program, ScreenRequest)
        //}
    }
}
