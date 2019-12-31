using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Solution 1
// Adapted from - https://retifrav.github.io/blog/2017/11/24/csharp-dotnet-core-publish-twitter/
/*
    string ConsumerKey = "YOUR-CONSUMER-KEY",
    ConsumerKeySecret = "YOUR-CONSUMER-KEY-SECRET",
    AccessToken = "YOUR-ACCESS-TOKEN",
    AccessTokenSecret = "YOUR-ACCESS-TOKEN-SECRET";

    var twitter = new Twitter(ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret);

// 1) if we want to upload image and post it together with text
    string response = twitter.PublishToTwitter("some text", "/path/to/some/image.png");
    Console.WriteLine(response);

// 2) if want just to send text
    var rez = Task.Run(async () =>
    {
        var response = await twitter.TweetText("some another text", string.Empty);
        return response;
    });
    Console.WriteLine(rez.Result.Item1);
    var rezJson = JObject.Parse(rez.Result.Item2);
    Console.WriteLine(rezJson);
    string error = rezJson["errors"] == null ? "OK" : rezJson["errors"][0]["message"].Value();
    Console.WriteLine(error);
*/

// Solution 2
// Adapted from https://blog.dantup.com/2016/07/simplest-csharp-code-to-post-a-tweet-using-oauth/
/*
    var twitter = new TwitterApi(ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret);
    var response = await twitter.Tweet("This is my first automated tweet!");
    Console.WriteLine(response);
*/

namespace FortuneCookie2
{
    public class FDTwitter
    {
        string _consumerKey;
        string _consumerKeySecret;
        string _accessToken;
        string _accessTokenSecret;
        HMACSHA1 _sigHasher;
        DateTime _epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        string _TwitterTextAPI;
        string _TwitterImageAPI;
        int _limit;

        public FDTwitter(string consumerKey, string consumerKeySecret, string accessToken, string accessTokenSecret, int limit = 280)
        {
            _TwitterTextAPI = "https://api.twitter.com/1.1/statuses/update.json";
            _TwitterImageAPI = "https://upload.twitter.com/1.1/media/upload.json";

            _consumerKey = consumerKey;
            _consumerKeySecret = consumerKeySecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
            _limit = limit;

            _sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes($"{_consumerKeySecret}&{_accessTokenSecret}"));
        }

        public string PublishToTwitter(string post, string pathToImage, byte[] imageArray, bool sendBytes = false)
        {
            try
            {
                // first, upload the image
                string mediaID = string.Empty;
                var rezImage = Task.Run(async () =>
                {
                    if (sendBytes == true)
                    {
                        var response = await TweetImageBytes(imageArray);
                        return response;
                    }
                    else
                    {
                        var response = await TweetImage(pathToImage);
                        return response;
                    }
                });
                var rezImageJson = JObject.Parse(rezImage.Result.Item2);

                if (rezImage.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error uploading image to Twitter. {rezImageJson["errors"][0]["message"].Value<string>()}";
                    }
                    catch //(Exception ex) // return unknown error
                    {
                        // log exception somewhere
                        return "Unknown error uploading image to Twitter";
                    }
                }
                mediaID = rezImageJson["media_id_string"].Value<string>();

                // second, send the text with the uploaded image
                var rezText = Task.Run(async () =>
                {
                    var response = await TweetText(CutTweetToLimit(post), mediaID);
                    return response;
                });
                var rezTextJson = JObject.Parse(rezText.Result.Item2);

                if (rezText.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error sending post to Twitter. {rezTextJson["errors"][0]["message"].Value<string>()}";
                    }
                    catch //(Exception ex) // return unknown error
                    {
                        // log exception somewhere
                        return "Unknown error sending post to Twitter";
                    }
                }

                return "OK";
            }
            catch //(Exception ex)
            {
                // log exception somewhere
                return "Unknown error publishing to Twitter";
            }
        }

        public Task<Tuple<int, string>> TweetText(string text, string mediaID)
        {
            var textData = new Dictionary<string, string> {
                { "status", text },
                { "trim_user", "1" },
                { "media_ids", mediaID}
            };

            return SendText(_TwitterTextAPI, textData);
        }

        public Task<Tuple<int, string>> TweetImage(string pathToImage)
        {
            byte[] imgdata = System.IO.File.ReadAllBytes(pathToImage);
            var imageContent = new ByteArrayContent(imgdata);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(imageContent, "media");

            return SendImage(_TwitterImageAPI, multipartContent);
        }

        public Task<Tuple<int, string>> TweetImageBytes(byte[] imgdata)
        {
            var imageContent = new ByteArrayContent(imgdata);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(imageContent, "media");

            return SendImage(_TwitterImageAPI, multipartContent);
        }

        async Task<Tuple<int, string>> SendText(string URL, Dictionary<string, string> textData)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", PrepareOAuth(URL, textData));

                var httpResponse = await httpClient.PostAsync(URL, new FormUrlEncodedContent(textData));
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }

        async Task<Tuple<int, string>> SendImage(string URL, MultipartFormDataContent multipartContent)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", PrepareOAuth(URL, null));

                var httpResponse = await httpClient.PostAsync(URL, multipartContent);
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }

        #region Some OAuth magic
        string PrepareOAuth(string URL, Dictionary<string, string> data)
        {
            // seconds passed since 1/1/1970
            var timestamp = (int)((DateTime.UtcNow - _epochUtc).TotalSeconds);

            // Add all the OAuth headers we'll need to use when constructing the hash
            Dictionary<string, string> oAuthData = new Dictionary<string, string>();
            oAuthData.Add("oauth_consumer_key", _consumerKey);
            oAuthData.Add("oauth_signature_method", "HMAC-SHA1");
            oAuthData.Add("oauth_timestamp", timestamp.ToString());
            oAuthData.Add("oauth_nonce", Guid.NewGuid().ToString());
            oAuthData.Add("oauth_token", _accessToken);
            oAuthData.Add("oauth_version", "1.0");

            if (data != null) // add text data too, because it is a part of the signature
            {
                foreach (var item in data)
                {
                    oAuthData.Add(item.Key, item.Value);
                }
            }

            // Generate the OAuth signature and add it to our payload
            oAuthData.Add("oauth_signature", GenerateSignature(URL, oAuthData));

            // Build the OAuth HTTP Header from the data
            return GenerateOAuthHeader(oAuthData);
        }

        /// <summary>
        /// Generate an OAuth signature from OAuth header values
        /// </summary>
        string GenerateSignature(string url, Dictionary<string, string> data)
        {
            var sigString = string.Join(
                "&",
                data
                    .Union(data)
                    .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
            );

            var fullSigData = string.Format("{0}&{1}&{2}",
                "POST",
                Uri.EscapeDataString(url),
                Uri.EscapeDataString(sigString.ToString()
                )
            );

            return Convert.ToBase64String(
                _sigHasher.ComputeHash(
                    new ASCIIEncoding().GetBytes(fullSigData.ToString())
                )
            );
        }

        /// <summary>
        /// Generate the raw OAuth HTML header from the values (including signature)
        /// </summary>
        string GenerateOAuthHeader(Dictionary<string, string> data)
        {
            return string.Format(
                "OAuth {0}",
                string.Join(
                    ", ",
                    data
                        .Where(kvp => kvp.Key.StartsWith("oauth_"))
                        .Select(
                            kvp => string.Format("{0}=\"{1}\"",
                            Uri.EscapeDataString(kvp.Key),
                            Uri.EscapeDataString(kvp.Value)
                            )
                        ).OrderBy(s => s)
                    )
                );
        }
        #endregion

        /// <summary>
        /// Cuts the tweet text to fit the limit
        /// </summary>
        /// <returns>Cutted tweet text</returns>
        /// <param name="tweet">Uncutted tweet text</param>
        string CutTweetToLimit(string tweet)
        {
            while (tweet.Length >= _limit)
            {
                tweet = tweet.Substring(0, tweet.LastIndexOf(" ", StringComparison.Ordinal));
            }
            return tweet;
        }
    }

    /**************************************************************************************************/

    /// <summary>
    /// Simple class for sending tweets to Twitter using Single-user OAuth.
    /// https://dev.twitter.com/oauth/overview/single-user
    /// 
    /// Get your access keys by creating an app at apps.twitter.com then visiting the
    /// "Keys and Access Tokens" section for your app. They can be found under the
    /// "Your Access Token" heading.
    /// </summary>
    class TwitterApi
    {
        const string TwitterApiBaseUrl = "https://api.twitter.com/1.1/";
        readonly string consumerKey, consumerKeySecret, accessToken, accessTokenSecret;
        readonly HMACSHA1 sigHasher;
        readonly DateTime epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Creates an object for sending tweets to Twitter using Single-user OAuth.
        /// 
        /// Get your access keys by creating an app at apps.twitter.com then visiting the
        /// "Keys and Access Tokens" section for your app. They can be found under the
        /// "Your Access Token" heading.
        /// </summary>
        public TwitterApi(string consumerKey, string consumerKeySecret, string accessToken, string accessTokenSecret)
        {
            this.consumerKey = consumerKey;
            this.consumerKeySecret = consumerKeySecret;
            this.accessToken = accessToken;
            this.accessTokenSecret = accessTokenSecret;

            sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", consumerKeySecret, accessTokenSecret)));
        }

        /// <summary>
        /// Sends a tweet with the supplied text and returns the response from the Twitter API.
        /// </summary>
        public Task<string> Tweet(string text)
        {
            var data = new Dictionary<string, string> {
            { "status", text },
            { "trim_user", "1" }
        };

            return SendRequest("statuses/update.json", data);
        }

        Task<string> SendRequest(string url, Dictionary<string, string> data)
        {
            var fullUrl = TwitterApiBaseUrl + url;

            // Timestamps are in seconds since 1/1/1970.
            var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

            // Add all the OAuth headers we'll need to use when constructing the hash.
            data.Add("oauth_consumer_key", consumerKey);
            data.Add("oauth_signature_method", "HMAC-SHA1");
            data.Add("oauth_timestamp", timestamp.ToString());
            data.Add("oauth_nonce", "a"); // Required, but Twitter doesn't appear to use it, so "a" will do.
            data.Add("oauth_token", accessToken);
            data.Add("oauth_version", "1.0");

            // Generate the OAuth signature and add it to our payload.
            data.Add("oauth_signature", GenerateSignature(fullUrl, data));

            // Build the OAuth HTTP Header from the data.
            string oAuthHeader = GenerateOAuthHeader(data);

            // Build the form data (exclude OAuth stuff that's already in the header).
            var formData = new FormUrlEncodedContent(data.Where(kvp => !kvp.Key.StartsWith("oauth_")));

            return SendRequest(fullUrl, oAuthHeader, formData);
        }

        /// <summary>
        /// Generate an OAuth signature from OAuth header values.
        /// </summary>
        string GenerateSignature(string url, Dictionary<string, string> data)
        {
            var sigString = string.Join(
                "&",
                data
                    .Union(data)
                    .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
            );

            var fullSigData = string.Format(
                "{0}&{1}&{2}",
                "POST",
                Uri.EscapeDataString(url),
                Uri.EscapeDataString(sigString.ToString())
            );

            return Convert.ToBase64String(sigHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSigData.ToString())));
        }

        /// <summary>
        /// Generate the raw OAuth HTML header from the values (including signature).
        /// </summary>
        string GenerateOAuthHeader(Dictionary<string, string> data)
        {
            return "OAuth " + string.Join(
                ", ",
                data
                    .Where(kvp => kvp.Key.StartsWith("oauth_"))
                    .Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
            );
        }

        /// <summary>
        /// Send HTTP Request and return the response.
        /// </summary>
        async Task<string> SendRequest(string fullUrl, string oAuthHeader, FormUrlEncodedContent formData)
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", oAuthHeader);

                var httpResp = await http.PostAsync(fullUrl, formData);
                var respBody = await httpResp.Content.ReadAsStringAsync();

                return respBody;
            }
        }
    }


}
