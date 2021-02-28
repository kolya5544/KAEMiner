using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace KaeMiner
{
    class Program
    {
        public static string FKAE(long value)
        {
            return (double)value / 10000 + " KAE";
        }

        public static string URL = "https://kae.nk.ax";
        static void Main(string[] args)
        {
            string addr = args[0];
            int THREAD_AMOUNT = int.Parse(args[1]);

            Block b = JsonConvert.DeserializeObject<Block>(Get($"{URL}/currentblock"));
            bool isFound = false;
            new Thread(() =>
            {
                while (true)
                {
                    var bNew = JsonConvert.DeserializeObject<Block>(Get($"{URL}/currentblock"));
                    if (bNew.id != b.id)
                    {
                        isFound = true;
                        Console.WriteLine("New block!");
                        b = bNew;
                    }
                    Thread.Sleep(2000);
                }
            }).Start();

            while (true)
            {
                int THREADS_DONE = 0;
                
                Console.WriteLine($"New block [{b.id}]. Difficulty: {b.difficulty}, reward: {FKAE(b.reward)}");
                

                

                for (int tID = 0; tID < THREAD_AMOUNT; tID++)
                {
                    int lID = tID;
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        long amountOfCombinations = (long)Math.Round(Math.Pow(Utils.base58.Length, Utils.base58entry(b.difficulty).Length));

                        string hash = b.hash;
                        string id = b.id.ToString();

                        //int searchFrom = i != 1 ? (int)Math.Round(Math.Pow(Az09Alphabet.Length, i-1)) : 0;
                        int searchFrom = (int)(lID * (amountOfCombinations / THREAD_AMOUNT));
                        int searchTo = (int)(searchFrom + (amountOfCombinations / THREAD_AMOUNT) + 1);
                        for (int o = searchFrom; o < searchTo; o++)
                        {
                            if (isFound) return;
                            string currPass = Utils.base58entry(o);

                            if (Utils.sha256(currPass + "_" + id) == hash)
                            {
                                Console.WriteLine($"FOUND {currPass + "_" + b.id}");
                                isFound = true;
                                Get($"{URL}/submitblock/{currPass + "_" + b.id}/{addr}");
                                break;
                            }
                        }
                        THREADS_DONE++;
                    }).Start();
                    Thread.Sleep(500);
                }

                while (true)
                {
                    if (THREADS_DONE == THREAD_AMOUNT ||
                        isFound)
                    {
                        break;
                    }
                    Thread.Sleep(500);
                }
                isFound = false;
            }
		}

        public static string Get(string url)
        {
            using (WebClient wc = new WebClient())
            {
                return wc.DownloadString(url);
            }
        }
    }

    public class Block
    {
        public long id;
        public long difficulty;
        public long createdTimestamp;
        public long reward;
        public string hash;
    }

    public class Utils
    {
        public static Random rng = new Random();

        public static string sha256(string text)
        {
            using (SHA256 s = SHA256.Create())
            {
                return BitConverter.ToString(s.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLower();
            }
        }

        public static char[] base58 = new char[] {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

        public static string base58entry(long entry)
        {
            if (entry == 0)
            {
                return base58[0].ToString();
            }

            List<char> res = new List<char>();
            while (entry != 0)
            {
                res.Insert(0, base58[entry % base58.Length]);
                entry = entry / base58.Length;
            }

            return new string(res.ToArray());
        }
    }
}
