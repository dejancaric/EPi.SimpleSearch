﻿using System.Text.RegularExpressions;
using System.Web;

namespace DC.EPi.SimpleSearch.Helpers
{
    public class StringUtils
    {
        public static string DecodeAndRemoveSpaces(string text)
        {
            var trimed = HttpUtility.HtmlDecode(text.Trim());
            trimed = trimed.Replace("\t", " ");
            // replace double spaces
            trimed = Regex.Replace(trimed, @"[ ]{2,}", " ");

            return trimed;
        }
    }
}