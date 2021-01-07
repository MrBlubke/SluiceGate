﻿using System;
using System.Collections.Generic;

namespace SluiceGate
{
    internal class Menu
    {
        public static void MainMenu()
        {
            sluice sluice = sluice.GetSluice();
            bool quit = false;
            do
            {
                ShowMenu();
                char ManagersChoice = InputManagersChoice();
                switch (ManagersChoice)
                {
                    case '1':
                        sluice.AddShips();
                        break;
                    case '2':
                        sluice.ViewShips();
                        break;
                    case '3':
                        Console.Clear();
                        ViewShipsLog();
                        break;
                    case '4':
                        ClearShipsLog();
                        break;
                    case 'Q':
                        quit = true;
                        break;
                    default:
                        break;
                }
            } while (!quit);
        }

        private static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine($"Welcome Sluice Manager  (sluice = {GlobalVar.SluiceState})");
            Console.WriteLine("----------------------\n");
            Console.WriteLine("1) add Ships");
            Console.WriteLine("2) view ships (and their latest updates)");
            Console.WriteLine("3) view ShipsLog");
            Console.WriteLine("4) clear ShipsLog");
            Console.WriteLine("Q) Quit Application");
        }

        private static void ClearShipsLog()
        {
            FileIO.ClearShipsLogged();
            Console.WriteLine("Log Cleared, returning to the main menu");
            System.Threading.Thread.Sleep(2000);
        }

        private static void ViewShipsLog()
        {
            List<string> log = FileIO.ReadShipLogFromFile();
            if (log[0] == "empty")
            {
                Console.WriteLine("The log is empty.");
            }
            else
            {
                foreach (string item in log)
                {
                    Console.WriteLine(item.ToString());
                }
            }
            Console.WriteLine("press any key to go back to the main menu");
            Console.ReadKey();
        }

        private static char InputManagersChoice()
        {
            bool isInValidChoice = true;
            char choice;
            do
            {
                Console.CursorLeft = 0;
                choice = Char.ToUpper(Console.ReadKey().KeyChar);
                if (choice == '1' || choice == '2' || choice == '3' || choice == '4' || choice == 'Q')
                {
                    isInValidChoice = false;
                }
            } while (isInValidChoice);
            return choice;
        }
    }
}