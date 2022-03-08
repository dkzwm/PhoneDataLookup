using System;
using PhoneDataLookup;
namespace PhoneDataLookupTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            PhoneLookup lookup = new PhoneLookup("phone.dat");
            string phoneNumber= "186000000000";
            string output;
            output = phoneNumber + "\n" + lookup.Lookup(phoneNumber).ToString();
            Console.WriteLine(output);
            phoneNumber = "17313070000";
            output = phoneNumber + "\n" + lookup.Lookup(phoneNumber).ToString();
            Console.WriteLine(output);
            phoneNumber = "18312340000";
            output = phoneNumber + "\n" + lookup.Lookup(phoneNumber).ToString();
            Console.WriteLine(output);
            Console.ReadKey();
        }
    }
}
