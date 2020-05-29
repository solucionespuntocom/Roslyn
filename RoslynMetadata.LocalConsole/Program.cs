using RoslynMetadata.LocalConsole.Extensions;
using System;
using System.Linq;

namespace RoslynMetadata.LocalConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string command = "";
            string createForm = "";
            Customer customer = null;

            while (command.ToLower() != "x")
            {

                if (createForm == "s" || customer == null)
                {
                    customer = CreateForm<Customer>();
                }

                if (customer.ValidCustomer())
                    Console.WriteLine("Validación completada con exito");

                Console.WriteLine("Presione x para salir");
                command = Console.ReadLine();

                if (command.ToLower() == "x")
                    break;

                Console.WriteLine("¿(s)Desea generar un nuevo cliente o (n)seguir validando el mismo?");
                createForm = Console.ReadLine();

            }
        }

        private static T  CreateForm<T>()
            where T: class, new()
        {
            T record = new T();
            Console.Clear();
            typeof(T).GetProperties()
                .ToList()
                .ForEach(p =>
                {
                    Console.WriteLine(p.DisplayName());
                    if(p.PropertyType != typeof(string))
                        p.SetValue(record, Convert.ChangeType(Console.ReadLine(), p.PropertyType));
                    else
                        p.SetValue(record, Console.ReadLine());
                });

            return record;
        }
    }

}
