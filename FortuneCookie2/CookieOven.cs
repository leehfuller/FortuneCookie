using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FortuneCookie2
{
    public class CookieOven
    {
        private string cookieFile = Directory.GetCurrentDirectory() + "\\Fortune.txt";
        private const string cookieSeperator = "%";

        #region ASCII Art
        public const string openLine = "_";
        public const string closeLine = "-";
        public const string leftLine = "< ";
        public const string rightLine = " >";

        private const string cowsay = @"
            \
             \  ^__^
              \ (oo)\_______
                (__)\       )\/\
                    ||----w |
                    ||     ||
        
        ";
        private const string tuxsay = @"
            \
             \
              \ .--.
               |o_o |
               |:_/ |
              //   \ \
             (|     | )
            /'\_   _/`\
            \___)=(___/

        ";
        private const string elephantsay = @"
         \
          \    /\  ___  /\
           \  // \/   \/ \\
             ((    O O    ))
              \\ /     \ //
               \/  | |  \/ 
                |  | |  |  
                |  | |  |  
                |   o   |  
                | |   | |  
                |m|   |m|  
        ";
        private const string koshsay = @"
             \
              \
               \  ___       _____     ___
                 /   \     /    /|   /   \
                |     |   /    / |  |     |
                |     |  /____/  |  |     |     
                |     |  |    |  |  |     |
                |     |  | {} | /   |     |
                |     |  |____|/    |     |
                |     |    |==|     |     |
                |      \___________/      |
                |                         |
                |                         |
        ";
        private const string sheepsay = @"
             \
              \
               __     
              UooU\.'@@@@@@`.
              \__/(@@@@@@@@@@)
                   (@@@@@@@@)
                   `YY~~~~YY'
                    ||    ||
        ";
        private const string atarisay = @"
                   \
                    \
                     \
                      $$ $$$$$ $$
                      $$ $$$$$ $$
                     .$$ $$$$$ $$.
                     :$$ $$$$$ $$:
                     $$$ $$$$$ $$$
                     $$$ $$$$$ $$$
                    ,$$$ $$$$$ $$$.
                   ,$$$$ $$$$$ $$$$.
                  ,$$$$; $$$$$ :$$$$.
                 ,$$$$$  $$$$$  $$$$$.
               ,$$$$$$'  $$$$$  `$$$$$$.
             ,$$$$$$$'   $$$$$   `$$$$$$$.
          ,s$$$$$$$'     $$$$$     `$$$$$$$s.
        $$$$$$$$$'       $$$$$       `$$$$$$$$$
        $$$$$Y'          $$$$$          `Y$$$$$

        ";
        private const string floppysay = @"
         \
          \
           \
            BBBBBBBBBBBBBBBBBBBBBBBBBBB
            BMB---------------------B B
            BBB---------------------BBB
            BBB---------------------BBB
            BBB---------------------BBB
            BBB---------------------BBB
            BBB---------------------BBB
            BBBBBBBBBBBBBBBBBBBBBBBBBBB
            BBBBB++++++++++++++++BBBBBB
            BBBBB++BBBBB+++++++++BBBBBB
            BBBBB++BBBBB+++++++++BBBBBB
            BBBBB++BBBBB+++++++++BBBBBB
            BBBBB++++++++++++++++BBBBBB

        ";
        private const string dogsay = @"
                 \
                  \
                   \ __
                    /  \
                   / ..|\
                  (_\  |_)
                  /  \@'
                 /     \
             _  /  `   |
            \\/  \  | _\
             \   /_ || \\_
              \____)|_) \_)
        ";
        private const string darthsay = @"
                 \
                  \
                   \                 
                 _.-'~~~~~~`-._
                /      ||      \
               /       ||       \
              |        ||        |
              | _______||_______ |
              |/ ----- \/ ----- \|
             /  (     )  (     )  \
            / \  ----- () -----  / \
           /   \      /||\      /   \
          /     \    /||||\    /     \
         /       \  /||||||\  /       \
        /_        \o========o/        _\
          `--...__|`-._  _.-'|__...--'
                  |    `'    |
        ";
        #endregion

        // read a cookie from the offset using the separator token
        public string readCookieFrom(string cookieFile, int startFrom)
        {
            string line = "";
            string buildCookie = "";

            try
            {
                FileStream fileStream = new FileStream(cookieFile, FileMode.Open, FileAccess.Read);
                fileStream.Seek(startFrom, SeekOrigin.Begin);

                StreamReader cookieStream = new StreamReader(fileStream);

                // find start of cookie using separator token
                while ((line = cookieStream.ReadLine()) != null && line != cookieSeperator)
                {
                }

                // now read cookie until next separator token
                while ((line = cookieStream.ReadLine()) != null && line != cookieSeperator)
                {
                    buildCookie += line + "\n";
                }
            }
            catch (Exception e)
            {
                // no special handling, just reflect error as cookie text
                buildCookie = e.Message;
            }

            if (buildCookie == "") buildCookie = "Cookie jar was empty, try again. (EOF)";

            return buildCookie;
        }

        public string pickCookie(int startFrom)
        {
            string cookie = "";

            FileInfo fi = new FileInfo(cookieFile);
            int size = (int)fi.Length;

            if (startFrom < size)
            {
                cookie = readCookieFrom(cookieFile, startFrom);
            }
            else
            {
                cookie = "Max of cookie file is " + size.ToString() + " characters";
            }

            return (cookie);
        }

        // use length of file to pick a random spot to read a cookie from
        public string randomCookie()
        {
            try
            {
                ILoggerFactory loggerFactory = new LoggerFactory();
                loggerFactory.AddConsole();
                loggerFactory.AddDebug();
                ILogger logger = loggerFactory.CreateLogger<Program>();

                FileInfo fi = new FileInfo(cookieFile);
                int size = (int)fi.Length;

                Random startFrom = new Random();
                int skip = startFrom.Next(0, size);

                logger.LogInformation("File: " + cookieFile + "\nSize: " + size.ToString() + "\nSkip to: " + skip.ToString());

                string cookieText = readCookieFrom(cookieFile, skip);

                logger.LogInformation(cookieText);

                return (cookieText);
            }
            catch (Exception e)
            {
                // just catch and fail transparently as cookie text
                return (e.Message);
            }
        }

        public (int, int) countCookies()
        {
            int cookieSize = 0, cookieCount = 0;

            try
            {
                // size of cookie file
                FileInfo fi = new FileInfo(cookieFile);
                cookieSize = (int)fi.Length;

                // count cookies                
                string allCookies = File.ReadAllText(cookieFile);

                cookieCount = new Regex(Regex.Escape(cookieSeperator)).Matches(allCookies).Count;
                //cookieCount = allCookies.Count(f => f == '%');
                //foreach (char c in allCookies) if (c == '%') cookieCount++;

                return (cookieSize, cookieCount);
            }
            catch //(Exception e)
            {
                // just catch and fail with negative numbers
                return (-1, -1);
            }
        }

        public string cowsayCookie(string cookieText, string whichsay = "cowsay")
        {
            string newCookie = "";
            string bubbleCookie = "";
            int maxLine = 0;

            // find longest line
            using (StringReader reader = new StringReader(cookieText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    if (line.Length > maxLine) maxLine = line.Length;
            }

            // Draw left/right of bubble and pad to longest line
            using (StringReader reader = new StringReader(cookieText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.PadRight(maxLine);
                    bubbleCookie += leftLine + line + rightLine + "\n";
                }

                cookieText = bubbleCookie;
            }


            // draw top/bottom of bubble and actual fortune
            newCookie += " ";
            for (int i = 0; i < maxLine+2; i++) newCookie += openLine;
            newCookie += "\n" + cookieText;
            newCookie += " ";
            for (int i = 0; i < maxLine+2; i++) newCookie += closeLine;

            // add the cowsay ascii art
            int cookieSize = newCookie.Length;
            if (whichsay == "cowsay") newCookie += cowsay;
            if (whichsay == "tuxsay") newCookie += tuxsay;
            if (whichsay == "koshsay") newCookie += koshsay;
            if (whichsay == "elephantsay") newCookie += elephantsay;            
            if (whichsay == "sheepsay") newCookie += sheepsay;
            if (whichsay == "atarisay") newCookie += atarisay;
            if (whichsay == "floppysay") newCookie += floppysay;
            if (whichsay == "darthsay") newCookie += darthsay;
            if (whichsay == "dogsay") newCookie += dogsay;
            if (newCookie.Length <= cookieSize)
                newCookie += "\nInvalid " + whichsay + ". Valid choices are cowsay, tuxsay, koshsay, elephantsay, sheepsay, atarisay, floppysay, darthsay, dogsay.";

            return (newCookie);
        }

    }
}
