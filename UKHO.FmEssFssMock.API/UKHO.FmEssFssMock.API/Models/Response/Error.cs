﻿namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class Error
    {
        public string Source { get; set; }
        public string Description { get; set; }
    }

    public class ErrorDescription
    {
        public List<Error> Errors { get; set; } = new ();
    }
}