using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SkiaSharp;

// https://fiddle.skia.org/

namespace FortuneCookie2
{
    public static class CowsayImage
    {
        // @FortuneDogsay
        // Apps/Dogsay
        // developer.twitter.com
        static string ConsumerKey = "XXXXXXXXXXXX";
        static string ConsumerKeySecret = "XXXXXXXXXXXX";
        static string AccessToken = "XXXXXXXXXXXX";
        static string AccessTokenSecret = "XXXXXXXXXXXX";

        public static byte[] getCowsayImage(string fcText, bool writeFile = false, bool postTwitter = true)
        {
            SKBitmap fcBitmap;
            SKImage fcImage;

            using (SKPaint fortunePaint = new SKPaint { TextSize = 12.0f, IsAntialias=true, Color=SKColors.Black, IsStroke=false, Typeface=SKTypeface.FromFamilyName("Courier New") })
            {
                // analyse the text a little
                int textWidth = 0, textHeight = 0;
                string longestText = "";
                (textWidth, textHeight, longestText) = calculateTextBounds(fcText);

                // set some bounds
                SKRect bounds = new SKRect();
                fortunePaint.MeasureText(longestText, ref bounds);
                bounds.Bottom = textHeight*fortunePaint.TextSize;
                fcBitmap = new SKBitmap((int)bounds.Right, (int)bounds.Height);

                using (SKCanvas cowsayCanvas = new SKCanvas(fcBitmap))
                {
                    cowsayCanvas.Clear(SKColors.Transparent);
                    drawTextLines(fcText, 0, 0, fortunePaint, cowsayCanvas);

                    fcImage = SKImage.FromBitmap(fcBitmap);
                    SKData fcPNG = fcImage.Encode(SKEncodedImageFormat.Png, 100);

                    // write image to local file system
                    if (writeFile == true)
                    {
                        using (var filestream = File.OpenWrite("twitter.png"))
                        {
                            fcPNG.SaveTo(filestream);
                        }
                    }

                    // Post Image to Twitter
                    if (postTwitter == true)
                    {
                        FDTwitter tweetThis = new FDTwitter(ConsumerKey, ConsumerKeySecret, AccessToken, AccessTokenSecret);
                        string response = tweetThis.PublishToTwitter("Dogsays on " + DateTime.Now.ToShortDateString(), "", fcPNG.ToArray(), true);
                    }

                    return (fcPNG.ToArray());
                }
            }
        }

        private static (int, int, string) calculateTextBounds(string str)
        {
            string[] lines = str.Split("\n");
            int maxWidth = 0;
            int maxHeight = 0;
            string longestString = "";

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > maxWidth)
                {
                    maxWidth = lines[i].Length;
                    longestString = lines[i];
                }
                maxHeight++;
            }

            return (maxWidth, maxHeight, longestString); 
        }

        private static string TrimNonAscii(string value)
        {
            string pattern = "[^ -~]+";
            Regex reg_exp = new Regex(pattern);
            return reg_exp.Replace(value, "");
        }

        private static void drawTextLines(string str, float x, float y, SKPaint paint, SKCanvas canvas)
        {
            string[] lines = str.Split("\n");
            float txtSize = paint.TextSize;

            for (int i = 0; i < lines.Length; i++)
            {
                canvas.DrawText(TrimNonAscii(lines[i]), x, y+(txtSize*i), paint);
            }
        }
    }
}

/*  
// Draw bitmap
     Stream fileStream = File.OpenRead ("MyImage.png");

    // clear the canvas / fill with white
    canvas.DrawColor (SKColors.White);

    // decode the bitmap from the stream
    using (var stream = new SKManagedStream(fileStream))
    using (var bitmap = SKBitmap.Decode(stream))
    using (var paint = new SKPaint()) {
      canvas.DrawBitmap(bitmap, SKRect.Create(Width, Height), paint);
    }


// Draw with image filters
    Stream fileStream = File.OpenRead ("MyImage.png"); // open a stream to an image file

    // clear the canvas / fill with white
    canvas.DrawColor (SKColors.White);

    // decode the bitmap from the stream
    using (var stream = new SKManagedStream(fileStream))
    using (var bitmap = SKBitmap.Decode(stream))
    using (var paint = new SKPaint()) {
      // create the image filter
      using (var filter = SKImageFilter.CreateBlur(5, 5)) {
        paint.ImageFilter = filter;

        // draw the bitmap through the filter
        canvas.DrawBitmap(bitmap, SKRect.Create(width, height), paint);
      }
    }

 */

