using System.Collections.Generic;

namespace BotWaRz
{
    // --------------------------------------------------------------------
    public class Status
    {
        public string status { get; set; }
        public string msg { get; set; }
        public string random { get; set; }
    }


    // --------------------------------------------------------------------
    public class Connect
    {
        public Login login { get; set; }
    }


    // --------------------------------------------------------------------
    public class Login
    {
        public string nickname { get; set; }
        public string hash { get; set; }
    }


    // --------------------------------------------------------------------
    public class Game
    {
        public GameInfo game { get; set; }
        public GameUpdate play { get; set; }
        public GameResult result { get; set; }
    }


    // --------------------------------------------------------------------
    public class GameInfo
    {
        public int time { get; set; }
        public int botRadius { get; set; }
        public World world { get; set; }
        public IList<SpeedLevel> speedLevels { get; set; }
        public IList<Player> players { get; set; }
    }


    // --------------------------------------------------------------------
    public class GameUpdate
    {
        public int time { get; set; }
        public IList<Player> players { get; set; }
        public int lastCmdId { get; set; }
    }


    // --------------------------------------------------------------------
    public class GameResult
    {
        public int time { get; set; }
        public string status { get; set; }
        public Player winner { get; set; }
    }


    // --------------------------------------------------------------------
    public class World
    {
        public int width { get; set; }
        public int height { get; set; }
    }


    // --------------------------------------------------------------------
    public class SpeedLevel
    {
        public double speed { get; set; }
        public double maxAngle { get; set; }
    }


    // --------------------------------------------------------------------
    public class Player
    {
        public string nickname { get; set; }
        public string asset { get; set; }
        public IList<Bot> bots { get; set; }
    }


    // --------------------------------------------------------------------
    public class Bot
    {
        public int id { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double angle { get; set; }
        public double speed { get; set; }
        public string cmd { get; set; }

        public void SetCommand(BotCommand cmd)
        {
            this.cmd = cmd.ToString();
        }
    }


    // --------------------------------------------------------------------
    public class Command
    {
        public int cmdId { get; set; }
        public IList<Bot> bots { get; set; }

        public Command(int cmdId, IList<Bot> bots)
        {
            this.cmdId = cmdId;
            this.bots = bots;
        }
    }


    // --------------------------------------------------------------------
    public enum BotCommand
    {
        steer,
        accelerate,
        brake,
    }
}
