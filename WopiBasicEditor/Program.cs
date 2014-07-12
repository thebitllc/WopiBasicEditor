// <copyright file="Program.cs" company="Bit, LLC">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author>ock</author>
// <date></date>
// <summary></summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WopiBasicEditor
{
    class Program
    {
        static void Main()
        {
            WopiServer s = new WopiServer(@"C:\\src\\");
            s.Start();

            Console.WriteLine("A simple wopi webserver. Press any key to quit.");
            Console.ReadKey();

            s.Stop();
        }
    }
}
