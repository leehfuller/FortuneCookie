using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FortuneCookie2.Controllers
{
    //took out core route, explicit full path on methods
    //[Route("api/[controller]")]

    [ApiController]
    public class CookieController : ControllerBase
    {
        CookieOven cookieOven = new CookieOven();

        // GET fortune/cookie
        [HttpGet("fortune/cookie")]
        [HttpGet("fortune")]
        public string GetFortune()
        {
            return cookieOven.randomCookie();
        }

        // GET fortune/id/{from}
        [HttpGet("fortune/id/{from}")]
        public string Get(int from)
        {
            return cookieOven.pickCookie(from);
        }

        // GET cowsay
        [HttpGet("cowsay")]
        public string GetCowsay()
        {
            string cowSay = "";
            cowSay = cookieOven.randomCookie();
            cowSay = cookieOven.cowsayCookie(cowSay);

            return cowSay;
        }

        // GET cowsay/id/{from}
        [HttpGet("cowsay/id/{from}")]
        public string GetCowsay(int from)
        {
            string cowSay = "";
            cowSay = cookieOven.pickCookie(from);
            cowSay = cookieOven.cowsayCookie(cowSay);

            return cowSay;
        }

        // GET fortune/cowsay
        [HttpGet("fortune/{whichsay}")]
        public string GetCowsays(string whichsay)
        {
            string cowSay = "";
            cowSay = GetFortune();
            cowSay = cookieOven.cowsayCookie(cowSay, whichsay);

            return cowSay;
        }

        // GET fortune/count
        [HttpGet("fortune/count")]
        public string GetCountCookies()
        {
            int cookieCount = 0;
            int cookieSize = 0;

            (cookieSize, cookieCount) = cookieOven.countCookies();

            string messageCount = "Cookie File Size: " + cookieSize + "\nNumber of Cookies: " + cookieCount + "\n";

            return messageCount;
        }

        // GET fortune/twitter
        [HttpGet("fortune/twitter")]
        public string GetFortuneTwitter()
        {
            string twitterFeed = "TODO - Twitter API\n";

            twitterFeed += cookieOven.randomCookie();

            // TODO - POST TO TWITTER ACCOUNT

            return twitterFeed;
        }

        // GET fortune/twitter
        [HttpGet("cowsay/twitter")]
        public string GetCowsayTwitter()
        {
            string cowSay = "";
            cowSay = GetFortune();
            cowSay = cookieOven.cowsayCookie(cowSay, "dogsay");

            // TODO - POST TEXT TO TWITTER ACCOUNT

            return cowSay;
        }

        // GET fortune/twitter
        [HttpGet("cowsay/twitterpng")]
        public IActionResult GetCowsayTwitterImage()
        {
            string cowSay = "";
            cowSay = GetFortune();
            cowSay = cookieOven.cowsayCookie(cowSay, "dogsay");

            byte[] imageStream = CowsayImage.getCowsayImage(cowSay);

            // TODO - POST IMAGE TO TWITTER ACCOUNT

            //return File(imageStream, "image/png");
            return File(imageStream, "image/png", "DogsayFortune.png", true);
        }

        /*
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
        */

    }

}
