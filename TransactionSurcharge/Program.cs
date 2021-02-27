using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TransactionSurcharge
{
    class Program
    {
        static void Main(string[] args)
        {
            decimal amountToBeTransferred = 0;
            decimal advisedTransferAmount = 0;
            decimal debitAmount = 0;
            decimal bankCharge = 0;

            //load config file.
            var configFile = LoadJson();

            while (true)
            {
                Console.WriteLine("Please enter any valid amount between 1 and 999999999");

                if (!decimal.TryParse(Console.ReadLine(), out amountToBeTransferred) || amountToBeTransferred < 1 || amountToBeTransferred > 999999999)
                {
                    Console.Clear();
                    continue;
                }

                foreach (var fee in configFile.fees)
                {
                    if (amountToBeTransferred >= fee.minAmount && amountToBeTransferred <= fee.maxAmount)
                    {
                        //calculate "Advised Transfer Amount"
                        advisedTransferAmount = amountToBeTransferred - fee.feeAmount;

                        //get what the bank charge will be for "advised transfer amount."
                        try
                        {
                            bankCharge = GetBankCharge(advisedTransferAmount, configFile.fees);
                        }
                        catch (ArgumentException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n{ex.Message}");
                            Console.ResetColor();
                            break;
                        }
                        
                        //get debit amount.
                        debitAmount = advisedTransferAmount + bankCharge;

                        break;
                    }

                    //gotten to the last loop
                    if (configFile.fees.Last() == fee)
                    {
                        //amount entered does not match any given range in config file.
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n{amountToBeTransferred} does not fall into any valid range.");
                        Console.ResetColor();
                    }
                }

                if (bankCharge >= 1)
                {
                    Console.WriteLine("|{0,-10} |{1,-15} |{2,-8} |{3,-14}|", "Amount", "Transfer Amount", "Charge", "Debit Amount");
                    Console.WriteLine("|{0,-10} |{1,-15} |{2,-8} |{3,-14}|",
                                      amountToBeTransferred,
                                      advisedTransferAmount,
                                      bankCharge,
                                      debitAmount);
                }

                Console.WriteLine("\nPress the Esc key to quit, any other key restarts the app.");
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }

                Console.WriteLine("\n...........................................................................");
                Console.WriteLine("...........................................................................\n\n");

            }

        }

        public static TransactionalFees LoadJson()
        {
            var jsonString = File.ReadAllText("Data.json");
            return JsonConvert.DeserializeObject<TransactionalFees>(jsonString);
        }

        /// <summary>
        /// Gets what the bank charge will be for an amount to be transferred.
        /// </summary>
        /// <param name="transferAmount">The amount to be transferred.</param>
        /// <param name="bankConfigurations"></param>
        /// <returns></returns>
        public static decimal GetBankCharge(decimal transferAmount, IEnumerable<TransactionalData> bankConfigurations)
        {
            if (bankConfigurations == null || !bankConfigurations.Any())
                throw new ArgumentNullException($"{nameof(bankConfigurations)} cannot be null or empty.");

            //get minimum bank charge fee.
            decimal leastBankChargeFee = bankConfigurations.Select((x) => x.feeAmount).Min();

            if (transferAmount < (leastBankChargeFee + 1))
                throw new ArgumentException($"Amount to be transferred must be greater than {leastBankChargeFee}");

            decimal bankCharge = 0;

            foreach (var item in bankConfigurations)
            {
                
                if (transferAmount <= item.maxAmount)
                {
                    bankCharge = item.feeAmount;
                    break;
                    //get debit amount.
                    //debitAmount = advisedTransferAmount + bankCharge;
                }
               
            }
            return bankCharge;
        }
    }

    public class TransactionalFees
    {
        public List<TransactionalData> fees { get; set; }
    }

    public class TransactionalData
    {
        public decimal minAmount { get; set; }
        public decimal maxAmount { get; set; }
        public decimal feeAmount { get; set; }
    }
}
