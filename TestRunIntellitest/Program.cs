using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunIntellitest
{
    public class Program
    {
        public static void Main(string[] args)
        {

        }      


        public static Boolean isPrime(int number)
        {
            double boundary = Math.Floor(Math.Sqrt((number)));

            if (number == 1)
            {
                return false;
            }

            if (number == 2)
            {
                return true;
            }

            for (int i = 2; i <= boundary; ++i)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

     
    }
