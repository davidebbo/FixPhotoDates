using ExifLibrary;
using System;

namespace FixPhotoDates
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var file = ImageFile.FromFile("path_to_image");
        }
    }
}
