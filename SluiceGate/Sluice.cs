﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SluiceGate
{
    internal class sluice
    {
        private static sluice instance;

        private sluice()
        {
        }

        public static sluice GetSluice()
        {
            if (instance == null)
            {
                instance = new sluice();
            }
            return instance;
        }

        public int GetStateOfSluice()
        {
            int sluice = 0;
            return sluice;
        }

        public bool IsDraftTooDeep(double draft, string name)
        {
            bool draftIsNotOk = false;
            if (draft > 2.75)
            {
                draftIsNotOk = true;
                Console.WriteLine("ship's draft is too deep");
                FileIO.WriteToLog($"{DateTime.Now}: ship {name} refused reason: Ship's draft too deep ({draft} > 2.75m).");
            }
            return draftIsNotOk;
        }

        public CanBeAdded CheckLength(Ship ship)
        {
            if ((int)ship.Length > (GlobalVar.SluiceLength - (ship.IsUpstream ? GlobalVar.LengthShipsInSluiceUpStream : GlobalVar.LengthShipsInSluiceDownStream)))

            {
                return CanBeAdded.NoNotCurrently;
            }
            else
            {
                return CanBeAdded.Yes;
            }
        }

        internal void ViewShips()
        {
            Console.Clear();
            if (GlobalVar.ShipList.Count > 0)
            {
                foreach (Ship ship in GlobalVar.ShipList)
                {
                    Console.WriteLine($"{ship.Name} arrived:{ship.ArrivalTime} in {(ship.IsUpstream ? "upstream" : "downstream")}cue " +
                        $" (length:{(int)ship.Length * 30}m)");
                }
            }
            else
            {
                Console.WriteLine("The Shiplist is currently empty.");
            }
            Console.WriteLine("\npress any key to return to the menu");
            Console.ReadKey();
        }

        public void ClearShipsLog()
        {
            FileIO.ClearShipsLogged();
            Console.WriteLine("Log Cleared, returning to the main menu");
            System.Threading.Thread.Sleep(2000);
        }

        public void ViewShipsLog()
        {
            double totalToll = 0;
            List<string> log = FileIO.ReadShipLogFromFile();
            if (log[0] == "empty")
            {
                Console.WriteLine("The log is empty.");
            }
            else
            {
                String[] separator = { "ship ", " arrived going ", " paying a toll of", " euro." };
                foreach (string item in log)
                {
                    int openRound = item.IndexOf('(');
                    int closeRound = item.IndexOf(')');
                    Console.WriteLine(item);
                    string lengthAndCargoRemoved = item.ToString().Remove(openRound, (closeRound - openRound + 2));
                    String[] subitems = lengthAndCargoRemoved.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (item.ToString().Contains("toll")) { totalToll += Convert.ToDouble(subitems[subitems.Length - 1]); }
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nThat's {log.Count} entries for a total of {totalToll} euro in toll.");
                Console.ResetColor();
            }
            Console.WriteLine("\npress any key to go back to the main menu");
            Console.ReadKey();
        }

        public void AddShips()
        {
            bool keeprunning;

            do
            {
                HelloManager();

                InputShip();

                keeprunning = QueryAddAnotherShip();
            } while (keeprunning);
            ChangeSluiceState();
        }

        private bool QueryAddAnotherShip()
        {
            bool keeprunning;
            bool sluiceDown = (GlobalVar.SluiceState == StateOfSluice.Down);

            if ((GlobalVar.SluiceLength - (sluiceDown ? GlobalVar.LengthShipsInSluiceUpStream : GlobalVar.LengthShipsInSluiceDownStream)) > 0)
            {
                Console.WriteLine("\nPress (S) to send sluice enroute, or another key to add another ship?");
                keeprunning = char.ToUpper(Console.ReadKey().KeyChar) != 'S';
            }
            else
            {
                Console.WriteLine("Sluice Full changing Direction");
                Thread.Sleep(2000);
                keeprunning = false;
            }
            return keeprunning;
        }

        private void HelloManager()
        {
            Console.Clear();
            bool sluiceUp = (GlobalVar.SluiceState == StateOfSluice.Up);
            int indexSluice = Convert.ToInt32(!sluiceUp);
            int spaceLeft = GlobalVar.SluiceLength - (!sluiceUp ? GlobalVar.LengthShipsInSluiceUpStream : GlobalVar.LengthShipsInSluiceDownStream);
            Console.WriteLine("Welcome Sluice Manager");
            string text = $"(sluice = {GlobalVar.SluiceState} {(GlobalVar.ShipsInStream[indexSluice].Count)} " +
                $"ships in {(!sluiceUp ? "upstream" : "downstream")}cue {spaceLeft * 30}m left)";
            Text.WriteRight(text, 0);
            Console.WriteLine("----------------------");
        }

        private void InputShip()
        {
            EnterNewShip();
            string path = GlobalVar.PathShipList;
            FileIO.WriteShipsToFile(path);
        }

        private void EnterNewShip()
        {
            Ship localShip = new Ship();
            double draft;
            string cargo;
            bool toUpdate = false;
            localShip.Name = InputName();
            if (CheckIfAlreadyInCue(localShip.Name))
            {
                AlreadyInCueError(localShip);
                return;
            }
            if ((GlobalVar.ShipList.Any(ship => ship.Name == localShip.Name)))
            {
                toUpdate = true;
                Ship ship = GetShipInfo(localShip.Name);
                localShip.Length = ship.Length;
                draft = InputDraft();
                if (IsDraftTooDeep(draft, localShip.Name)) { return; }
            }
            else
            {
                localShip.Length = InputLength();
            }
            localShip.IsUpstream = InputDirection();
            if (IsSluiceFull(localShip.IsUpstream)) { return; }
            cargo = InputCargo(localShip);
            CanBeAdded canBeAdded = CheckLength(localShip);
            switch (canBeAdded)
            {
                case CanBeAdded.Yes:
                    if (toUpdate) { UpdateShip(localShip, cargo); }
                    else { AddShip(localShip, cargo); }
                    break;

                case CanBeAdded.NoNotCurrently:
                    CantBeAddedAtThisTime(localShip);
                    break;
            }
        }

        private void AlreadyInCueError(Ship localShip)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"a ship with name {localShip.Name} was already entered in current cues");
            Console.ResetColor();
        }

        private bool CheckIfAlreadyInCue(string name)
        {
            return GlobalVar.ShipsInStream[0].Any(ship => ship.Name == name) || (GlobalVar.ShipsInStream[1].Any(ship => ship.Name == name));
        }

        private string InputCargo(Ship ship)
        {
            Console.WriteLine("what is your cargo?");

            string cargo = Console.ReadLine();
            if (HasGasOrExplosivesOrFlammable(cargo))
            {
                ship.Toll = PayToll(ship) + 7;
                Console.WriteLine($"Special cargo charge 7 euro: toll is now {ship.Toll} euro");
            }
            else
            {
                ship.Toll = PayToll(ship);
            }
            return cargo;
        }

        private bool HasGasOrExplosivesOrFlammable(string cargo)
        {
            List<string> listOfStrings = new List<string> { "GAS", "EXPLOSIVES", "FLAMMABLE" };
            bool hasGasOrExplosivesOrFlammable = listOfStrings.Any(cargo.ToUpper().Contains);
            return hasGasOrExplosivesOrFlammable;
        }

        private Ship GetShipInfo(string name)
        {
            Ship ship = GlobalVar.ShipList.FirstOrDefault(ship => ship.Name == name);
            string text = "ship already in database. reading info";
            Text.WriteinTime(text);
            Text.Clearline(0);
            return ship;
        }

        private void CantBeAddedAtThisTime(Ship ship)
        {
            string name = ship.Name;
            bool isUpstream = ship.IsUpstream;
            Length length = ship.Length;
            if (isUpstream)
            {
                Console.WriteLine($"Sorry this ship can't safely enter the sluice; current total length is " +
                    $"{30 * GlobalVar.LengthShipsInSluiceUpStream} meters");
                FileIO.WriteToLog($"{DateTime.Now}: ship {name} refused reason: Ship too long for current upstreamcue" +
                    $"(ship={30 * (int)length}m only {30 * (GlobalVar.SluiceLength - GlobalVar.LengthShipsInSluiceUpStream)}m left).");
            }
            else
            {
                Console.WriteLine($"Sorry this ship can't safely enter the sluice; current total length is " +
                    $"{30 * GlobalVar.LengthShipsInSluiceDownStream} meters");
                FileIO.WriteToLog($"{DateTime.Now}: ship {name} refused reason: Ship too long for current downstreamcue" +
                    $"(ship={30 * (int)length}m only {30 * (GlobalVar.SluiceLength - GlobalVar.LengthShipsInSluiceDownStream)}m left).");
            }

            System.Threading.Thread.Sleep(2000);
        }

        private void UpdateShip(Ship ship, string cargo)
        {
            FileIO.WriteToLog($"{ship.ArrivalTime}: ship {ship.Name} arrived (size:" +
                                $" {ship.Length} cargo:{cargo}) going " +
                                $"{(ship.IsUpstream ? "upstream" : "downstream")}{((ship.Toll > 0) ? $" paying a toll of {ship.Toll} euro" : "")}.");
            int shipIndex = GlobalVar.ShipList.FindIndex(ship1 => ship1.Name == ship.Name);
            double currentTollforShip = GlobalVar.ShipList[shipIndex].Toll;
            double newTollForShip = currentTollforShip + ship.Toll;
            ship.Toll = newTollForShip;
            GlobalVar.ShipList[shipIndex] = ship;

            AddInLocalStream(ship);
        }

        private void AddShip(Ship ship, string cargo)
        {
            FileIO.WriteToLog($"{ship.ArrivalTime}: ship {ship.Name} arrived (size:" +
                                $" {ship.Length} cargo:{cargo}) going " +
                                $"{(ship.IsUpstream ? "upstream" : "downstream")}{((ship.Toll > 0) ? $" paying a toll of {ship.Toll} euro" : "")}.");
            GlobalVar.ShipList.Add(ship);
            AddInLocalStream(ship);
        }

        private void AddInLocalStream(Ship ship)
        {
            bool isUpstream = ship.IsUpstream;
            GlobalVar.ShipsInStream[(isUpstream ? 1 : 0)].Add(ship);
            if (isUpstream)
            {
                GlobalVar.LengthShipsInSluiceUpStream += (int)ship.Length;
            }
            else
            {
                GlobalVar.LengthShipsInSluiceDownStream += (int)ship.Length;
            }
            int spaceLeftInSluice = GlobalVar.SluiceLength - (isUpstream ? GlobalVar.LengthShipsInSluiceUpStream : GlobalVar.LengthShipsInSluiceDownStream);

            Console.WriteLine($"space left in {(isUpstream ? "upstream" : "downstream")} cue {spaceLeftInSluice * 30}m");
        }

        private bool IsSluiceFull(bool isUpstream)
        {
            bool sluiceFull = false;
            if (GlobalVar.SluiceFull[(isUpstream ? 1 : 0)])
            {
                Console.WriteLine($"I'm sorry {(isUpstream ? "upstream" : "downstream")} cue is currently full.");
                sluiceFull = true;
            }
            return sluiceFull;
        }

        private string InputName()
        {
            Console.WriteLine("What's the shipsname?");

            string name = "";
            bool isValidName;
            do
            {
                Console.CursorTop = 3;
                string inputName = Console.ReadLine();
                if (inputName.Length < 1)
                {
                    isValidName = false;
                }
                else
                {
                    isValidName = true;
                    name = inputName;
                }
            } while (!isValidName);
            return name;
        }

        private bool InputDirection()
        {
            Console.WriteLine("Going? up? or down? type 1 for up, 0 for down");

            bool isInValidDirection;
            bool direction = true;
            int top = Console.CursorTop;
            do
            {
                Console.CursorLeft = 0;
                Console.CursorTop = top;
                Text.Clearline(0);
                string userInputDirection = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (userInputDirection == "1" || userInputDirection == "0")
                {
                    direction = Convert.ToBoolean((userInputDirection == "1") ? "True" : "False");
                    isInValidDirection = false;
                    Text.Clearline(-1);
                    Console.WriteLine((direction ? "Upstream" : "Downstream"));
                }
                else
                {
                    Text.Clearline(-1);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("I said 1 or 0!!");
                    System.Threading.Thread.Sleep(1000);
                    Console.ResetColor();
                    isInValidDirection = true;
                }
            } while (isInValidDirection);

            return direction;
        }

        public double PayToll(Ship ship)
        {
            bool isUpstream = ship.IsUpstream;
            Length length = ship.Length;
            int toll = 0;
            if (isUpstream)
            {
                toll = (int)length * 5;
                Console.WriteLine("Toll to Pay (going upstream): " + toll + " euro");
            }
            return toll;
        }

        private double InputDraft()
        {
            Console.WriteLine("What's the Draft of the ship? (in meters)");

            double draft = 0.0;
            bool noValidDraft;
            do
            {
                string input = Console.ReadLine();
                try
                {
                    draft = Convert.ToDouble(input);
                    noValidDraft = false;
                }
                catch
                {
                    Console.WriteLine("Input not valid");
                    noValidDraft = true;
                }
            } while (noValidDraft);
            return draft;
        }

        private Length InputLength()
        {
            Console.WriteLine("What's the length of the ship? (S)mall, (M)edium, (L)ong");

            Length length;
            do
            {
                Text.Clearline(0);
                char size = char.ToUpper(Console.ReadKey().KeyChar);
                length = CompleteInput(size);
            } while (length == Length.NotSet);
            return length;
        }

        private Length CompleteInput(char size)
        {
            Length noValidInput;

            switch (size)
            {
                case 'S':
                    noValidInput = Length.Small;
                    Text.Clearline(0);
                    Console.WriteLine("Small");
                    break;

                case 'M':
                    noValidInput = Length.Medium;
                    Text.Clearline(0);
                    Console.WriteLine("Medium");
                    break;

                case 'L':
                    noValidInput = Length.Long;
                    Text.Clearline(0);
                    Console.WriteLine("Long");
                    break;

                default:
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Invalid Length");
                    Console.ResetColor();
                    noValidInput = Length.NotSet;
                    System.Threading.Thread.Sleep(500);
                    break;
            }
            return noValidInput;
            ;
        }

        public void ChangeSluiceState()
        {
            Console.Clear();
            StateOfSluice currentState = GlobalVar.SluiceState;
            if (currentState == StateOfSluice.Up)
            {
                MoveSluiceDown();
                LetDownStreamShipsLeave();
                Console.WriteLine($"                        Sluice is {GlobalVar.SluiceState}     ");
            }
            else
            {
                MoveSluiceUp();
                LetUpstreamShipsLeave();
                Console.WriteLine($"                        Sluice is {GlobalVar.SluiceState}     ");
                System.Threading.Thread.Sleep(1500);
                Console.Clear();
            }
        }

        private void LetUpstreamShipsLeave()
        {
            Console.ResetColor();
            Console.CursorTop = 0;
            Console.CursorLeft = 5;
            GlobalVar.SluiceState = StateOfSluice.Up;
            if (GlobalVar.ShipsInStream[1].Count > 0)
            {
                foreach (Ship ship in GlobalVar.ShipsInStream[1])
                {
                    FileIO.WriteToLog($"{DateTime.Now}: ship {ship.Name} left sluice (size:" +
                                           $" {ship.Length}) leaving " +
                                           $"{(ship.IsUpstream ? "upstream" : "downstream")}.");
                }
            }
            GlobalVar.ShipsInStream[1].Clear();

            GlobalVar.LengthShipsInSluiceUpStream = 0;
        }

        private void MoveSluiceUp()
        {
            Console.CursorLeft = 5;
            Console.WriteLine($"Increasing water Level. Sluice is {GlobalVar.SluiceState}     ");
            for (int i = 5; i >= 0; i--)
            {
                for (int a = 0; a < i; a++)
                {
                    Console.CursorTop = a;
                    Console.CursorLeft = 0;

                    if (i > 0)
                    {
                        Text.Clearline(0);
                    }
                }
                for (int b = i; b < 5; b++)
                {
                    Console.CursorTop = b;
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("~~~~~");
                }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.CursorTop = 0;
                Console.CursorLeft = 5;

                Console.WriteLine($"Increasing water Level. Sluice is {GlobalVar.SluiceState}     ");
                System.Threading.Thread.Sleep(500);
                GlobalVar.SluiceState = StateOfSluice.EnRoute;
            }
        }

        private void LetDownStreamShipsLeave()
        {
            Console.ResetColor();
            Console.CursorTop = 0;
            Console.CursorLeft = 5;
            GlobalVar.SluiceState = StateOfSluice.Down;
            if (GlobalVar.ShipsInStream[0].Count > 0)
            {
                foreach (Ship ship in GlobalVar.ShipsInStream[0])
                {
                    FileIO.WriteToLog($"{DateTime.Now}: ship {ship.Name} left sluice (size:" +
                                           $" {ship.Length}) leaving " +
                                           $"{(ship.IsUpstream ? "upstream" : "downstream")}.");
                }
            }
            GlobalVar.ShipsInStream[0].Clear();
            GlobalVar.LengthShipsInSluiceDownStream = 0;
        }

        private void MoveSluiceDown()
        {
            Console.CursorTop = 0;
            Console.CursorLeft = 5;

            Console.WriteLine($"Decreasing water Level. Sluice is {GlobalVar.SluiceState}     ");
            for (int i = 0; i <= 5; i++)
            {
                for (int a = 0; a <= i; a++)
                {
                    Console.CursorTop = a;
                    Console.CursorLeft = 0;

                    Text.Clearline(0);
                }
                for (int b = i; b < 5; b++)
                {
                    Console.CursorTop = b;
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("~~~~");
                }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.CursorTop = 0;
                Console.CursorLeft = 5;

                Console.WriteLine($"Decreasing water Level. Sluice is {GlobalVar.SluiceState}     ");
                System.Threading.Thread.Sleep(500);
                GlobalVar.SluiceState = StateOfSluice.EnRoute;
            }
        }
    }
}