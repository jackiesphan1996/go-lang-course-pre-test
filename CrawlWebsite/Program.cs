using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace CrawlWebsite
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var path = @"https://www.thesaigontimes.vn/121624/Cuoc-cach-mang-dau-khi-da-phien.html";
            var allPosts = new List<Post>();
            await DoCrawlJob(path, allPosts);
        }

        private static async Task DoCrawlJob(string path, List<Post> posts)
        {
            if (!posts.Any(x => x.Url.ToLower() == path.ToLower()))
            {
                var httpClient = new HttpClient();
                HttpResponseMessage response = null;
                try
                {
                    response = await httpClient.GetAsync(path);
                }
                catch (Exception ex)
                {
                    return;
                }
                
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    
                    var html = await response.Content.ReadAsStringAsync();
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(html);
                    var postTileNode = htmlDocument.DocumentNode.Descendants("span")
                        .FirstOrDefault(x => x.HasClass("Title") && x.Id.Contains("cphContent_lblTitleHtml"));
                    var postAuthorNode = htmlDocument.DocumentNode.Descendants("span")
                           .FirstOrDefault(x => x.HasClass("ReferenceSourceTG") && x.Id.Contains("cphContent_Lbl_Author"));
                    var dateAuthorNode = htmlDocument.DocumentNode.Descendants("span")
                           .FirstOrDefault(x => x.HasClass("Date") && x.Id.Contains("cphContent_lblCreateDate"));
                    if (postTileNode != null)
                    {
                        var post = new Post
                        {
                            Title = postTileNode.InnerText.Replace(',', ' '),
                            Author = postAuthorNode.InnerText,
                            PublishedDate = System.Net.WebUtility.HtmlDecode(dateAuthorNode.InnerText),
                            Url = path.ToLower()
                        };

                        posts.Add(post);
                        LogToCsv(post);
                    }

                    Uri myUri = new Uri(path);
                    string host = myUri.Host; 

                    var test = htmlDocument.DocumentNode.Descendants("a").ToList();
                    var otherPosts = htmlDocument.DocumentNode.Descendants("a")
                        .Select(x =>
                        {
                            var result = "";
                            var href = x.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(href))
                            {
                                if (href[0] == '/')
                                {
                                    result = host + href;
                                }
                                else
                                {
                                    Uri pathUri = null;
                                    try
                                    {
                                        pathUri = new Uri(href);
                                        if (pathUri.Host.ToLower() == host.ToLower())
                                        {
                                            result = href;
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                        result = "";
                                    }
                                    
                                }
                                return !string.IsNullOrWhiteSpace(result) ? myUri.Scheme + "://" + result : "";
                            }
                            return "";
                        }).Where(x => !string.IsNullOrEmpty(x) && x.EndsWith(".html")).ToList();

                    foreach (var item in otherPosts)
                    {
                        await DoCrawlJob(item, posts);
                    }

                }
            }
        }

        private static void LogToCsv(Post post)
        {
            var path = "post.csv";
            if (!File.Exists(path))
            {
                File.CreateText(path);
            }
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine($"{post.Title},{post.Author},{post._publishedDate}");
            } 
        }
    }
    public class Post
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string PublishedDate { get; set; }
        public string _publishedDate
        {
            get
            {
                if (!string.IsNullOrEmpty(PublishedDate))
                {
                    var dateAndTime = PublishedDate.Split(",").ToList();
                    var dateTime = dateAndTime[1].TrimStart() + dateAndTime[2];
                    return dateTime;
                }
                else
                {
                    return "";
                }

            }
        }
    }
}
