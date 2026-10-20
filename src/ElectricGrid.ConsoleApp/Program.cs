using System;
using System.Text;
using ElectricGrid.ConsoleApp.UI;
using ElectricGrid.Core.Services;

namespace ElectricGrid.ConsoleApp
{
    /// <summary>
    /// Console entry point (composition root). It creates the Core service and hands it to the menu.
    /// A future GUI project can replace this project and reuse <c>ElectricGrid.Core</c> untouched.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Municipal Electrical Grid Management";

            PowerGridService service = new PowerGridService();
            MainMenu menu = new MainMenu(service, service);
            menu.Run();
        }
    }
}
