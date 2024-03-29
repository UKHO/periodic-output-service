﻿using Newtonsoft.Json;

namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class SearchBatchResponse
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public List<BatchDetail> Entries { get; set; }
        public PagingLinks _Links { get; set; }
        [JsonIgnore]
        public int QueryCount { get; set; }
    }
}
