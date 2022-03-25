using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageAdjust
{
    class Program
    {
        static void Main(string[] args)
        {
            if(System.IO.File.Exists(args[0]))
            {
                Console.WriteLine("Modifying " + args[0]);
                Console.WriteLine("Applying HSV "+ args[3]+"% "+args[2]+"% "+args[1]+"%");
                try
                {
                    using (MagickImage tex = new MagickImage(args[0]))
                    {
                        tex.Modulate(new Percentage(float.Parse(args[3])), new Percentage(float.Parse(args[2])), new Percentage(float.Parse(args[1])));
                        System.IO.File.WriteAllBytes(args[0], tex.ToByteArray());
                    }
                    Console.WriteLine("Transformation Complete");
                }
                catch(Exception x)
                {
                    Console.WriteLine("Transformation Exception: "+x);
                }
            }
            else
            {
                Console.WriteLine("Source '"+args[0]+"' Not Found");
            }
        }
    }
}
