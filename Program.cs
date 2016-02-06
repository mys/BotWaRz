using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BotWaRz
{
    class Program
    {
        // main account
        // miccelinski@gmail.com
#if (!DEBUG)
        private static string TOKEN = "39711345-3cdc-4466-ba94-356a2c1eab50";
        private static string NICKNAME = "mys";
#endif

        // test account
        // mic.celinski@gmail.com
#if (DEBUG)
        private static string TOKEN = "552c67b7-8c0e-4b98-a398-9a4e10936721";
        private static string NICKNAME = "my.s";
#endif

        private static int DELAY = 230;

        private static GameInfo gameInfo;
        private static GameUpdate gameUpdate;
        private static GameResult gameResult;
        private static int lastCmdID = -1;
        private static int currentCmdID = 0;
        private static Stopwatch stopwatch = new Stopwatch();


        // --------------------------------------------------------------------
        static void Main(string[] args)
        {
            while (true)
            {
                gameInfo = null;
                gameUpdate = null;
                gameResult = null;
                lastCmdID = -1;
                currentCmdID = 0;
                stopwatch.Start();

                try
                {
                    using (Client client = new Client())
                    {
                        // handshake
                        Status status = GetStatus(client, 0, "socket_connected");

                        Connect connect = new Connect
                        {
                            login = new Login()
                            {
                                nickname = NICKNAME,
                                hash = GenerateHashValue(status.random, TOKEN)
                            }
                        };

                        // send login info
                        client.Send(connect);
                        GetStatus(client, 0, "login_ok");

                        // main loop
                        while (true)
                        {
                            // get game data info
                            GetStatus(client, 0, "");

                            // loop for one game
                            while (GetStatus(client, DELAY - (int)stopwatch.ElapsedMilliseconds) != null)
                            {
                                if (stopwatch.ElapsedMilliseconds > DELAY)
                                {
                                    // send comand on server
                                    MoveBots(client);

                                    Console.WriteLine("Before restart: " + stopwatch.ElapsedMilliseconds);

                                    stopwatch.Restart();
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception:\n");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }


        // --------------------------------------------------------------------
        private static Bot SearchNearestBotID(Bot myBot)
        {
            double distance = 9999;
            Bot bot = null;

            foreach (Bot enemyBot in gameUpdate.players.Single(
                player => !player.nickname.Equals(NICKNAME)).bots)
            {
                double dist = Math.Sqrt(
                    Math.Pow(myBot.x - enemyBot.x, 2) +
                    Math.Pow(myBot.y - enemyBot.y, 2));

                if (dist < distance)
                {
                    distance = dist;
                    bot = enemyBot;
                }
            }

            if (bot == null)
                throw new Exception("No enemy bot found!");

            return bot;
        }


        // --------------------------------------------------------------------
        private static double GetAngleToEnemy(Bot myBot, Bot enemyBot)
        {
            return Math.Atan2(enemyBot.y - myBot.y, enemyBot.x - myBot.x) * 180 / Math.PI;
        }


        // --------------------------------------------------------------------
        private static double BetweenAngle(double myAngle, double enemyAngle)
        {
            //double angle = Math.Abs(myAngle) - Math.Abs(enemyAngle);
            double angle = enemyAngle - myAngle;
            if (angle < -180)
                angle += 360;
            return angle;
        }


        // --------------------------------------------------------------------
        private static bool DetectEndOfWorld(Bot bot)
        {
            int radius = gameInfo.botRadius;

            // top end
            if (bot.y - radius == 0 && bot.angle < 0)
            {
                Log("Top end detected");
                return true;
            }
            // bot end
            if (bot.y + radius == gameInfo.world.height && bot.angle > 0)
            {
                Log("Bot end detected");
                return true;
            }
            // left end
            if (bot.x - radius == 0
                && (bot.angle > 90 || bot.angle < -90))
            {
                Log("Left end detected");
                return true;
            }
            // right end
            if (bot.x + radius == gameInfo.world.width
                && (bot.angle < 90 && bot.angle > -90))
            {
                Log("Right end detected");
                return true;
            }

            return false;
        }


        // --------------------------------------------------------------------
        private static void MoveBots(Client client)
        {
            if (gameInfo == null)
                return;

            if (gameUpdate == null)
                return;

            IList<Bot> bots = gameUpdate.players.Single(
                player => player.nickname.Equals(NICKNAME)).bots;

            Log("GameUpdate");
            Log(JsonConvert.SerializeObject(gameUpdate, Formatting.Indented));

            foreach (Bot bot in bots)
            {
                Bot enemyBot = SearchNearestBotID(bot);
                double angleToEnemy = GetAngleToEnemy(bot, enemyBot);
                double betweenAngle = BetweenAngle(bot.angle, angleToEnemy);

                Log("MyBot: " + bot.x + ", " + bot.y + " , angle: " + bot.angle);
                Log("EnemyBot: " + enemyBot.x + ", " + enemyBot.y);
                Log("AngleToEnemy: " + angleToEnemy);
                Log("Between angle: " + betweenAngle);

                if (Math.Abs(betweenAngle) < 5 && !DetectEndOfWorld(bot))
                {
                    bot.SetCommand(BotCommand.accelerate);
                }
                else if (bot.speed > gameInfo.speedLevels.Min(speedLevel => speedLevel.speed))
                {
                    bot.SetCommand(BotCommand.brake);
                }
                else
                {
                    double maxAngle = gameInfo.speedLevels.First(speedLevel => speedLevel.speed >= bot.speed).maxAngle;
                    if (Math.Abs(betweenAngle) > maxAngle)
                        betweenAngle = maxAngle;
                    if (DetectEndOfWorld(bot))
                        betweenAngle = maxAngle;

                    if (betweenAngle > 0)
                    {
                        bot.angle = betweenAngle;
                    }
                    else if (betweenAngle <= 0)
                    {
                        bot.angle = -betweenAngle;
                    }

                    bot.SetCommand(BotCommand.steer);
                }
            }

            Command command = new Command(++currentCmdID, bots);
            Console.WriteLine("Before sending: " + stopwatch.ElapsedMilliseconds);
            client.Send(command);

            Log("Command");
            Log(JsonConvert.SerializeObject(command, Formatting.Indented));
        }


        // --------------------------------------------------------------------
        private static Status GetStatus(Client client, int timeout = 0, string msg = "")
        {
            Status status = new Status();

            string message = client.ReadString(timeout);
            foreach (string mess in message.Split('\n'))
            {
                if (mess.StartsWith(@"{""status"":"))
                {
                    status = JsonConvert.DeserializeObject<Status>(mess);
                    Console.WriteLine(JsonConvert.SerializeObject(status, Formatting.Indented));

                    if (status.status.Equals("command_no_cmd_during_game"))
                        //throw new Exception("command_no_cmd_during_game");
                        return null;

                    if (!msg.Equals(status.status))
                        //throw new Exception(
                        //    "Expected status '" + status.status + "'. Actual message '" + msg + "'");
                        return null;

                }

                else if (mess.StartsWith(@"{""game"":"))
                {
                    gameInfo = JsonConvert.DeserializeObject<Game>(mess).game;
                    Console.WriteLine(JsonConvert.SerializeObject(gameInfo, Formatting.Indented));
                }

                else if (mess.StartsWith(@"{""play"":"))
                {
                    try
                    {
                        gameUpdate = JsonConvert.DeserializeObject<Game>(mess).play;
                        //Console.WriteLine("Last cmd ID: " + gameUpdate.lastCmdId);
                    }
                    catch (Exception)
                    {
                        // ignore TODO
                    }
                    //Console.WriteLine(JsonConvert.SerializeObject(gameUpdate, Formatting.Indented));
                }
                else if (mess.StartsWith(@"{""result"":"))
                {
                    gameResult = JsonConvert.DeserializeObject<Game>(mess).result;
                    Console.WriteLine(JsonConvert.SerializeObject(gameResult, Formatting.Indented));

                    //Console.ReadKey();
                    return null;
                }
            }
            return status;
        }


        // --------------------------------------------------------------------
        private static void Log(string text)
        {
            Console.WriteLine(text);

            using (FileStream fs = new FileStream("log.txt", FileMode.Append, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(text);
            }
        }


        // --------------------------------------------------------------------
        private static string GenerateHashValue(string random, string token)
        {
            string text = string.Format("{0}{1}", random, token);

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
