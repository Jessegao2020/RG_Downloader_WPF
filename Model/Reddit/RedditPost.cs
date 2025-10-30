using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedgifsDownloader.Model.Reddit
{
    public class RedditPost
    {
        public string Title { get; set; } = "";
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsImage { get; set; }
    }
}
