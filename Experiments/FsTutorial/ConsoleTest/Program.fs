// Learn more about F# at http://fsharp.net

module WebCrawler

open EmnExtensions
open System
open System.Net
open System.IO

let utf8 = Text.Encoding.UTF8
let cookies =  Net.CookieContainer()

let baseUrl = @"http://community.wizards.com/go/thread/view/75882/19713850/The_TenMinute_BackgroundPost_your_characters?pg={0}";
let url (n:int) = String.Format(baseUrl, n )

let getPage n = EmnExtensions.Web.UriRequest.Execute(Uri(url n), cookies, null);
let pageName n = "Page"+n.ToString()+".html"
let getPageCached n = 
    let name = pageName n
    if File.Exists name then
        let cached = File.ReadAllText(name,utf8)
        if String.IsNullOrEmpty cached then
            null
        else
            cached
    else
        let page = getPage n
        let pageContentRaw = page.ContentAsString
        let pageContent = 
            if page.StatusCode <> HttpStatusCode.OK || (pageContentRaw.Contains "No Posts" && not (pageContentRaw.Contains "mb_t_p_t_post")) || n > 100 then
                null
            else
                pageContentRaw
        File.WriteAllText(pageName n, pageContent, utf8)
        pageContent


let pageRange = [1..100]

let rec getPagesFrom n =
    Console.Write "."
    let nextpage = getPageCached n
    if nextpage = null  then
        []
    else
        (n, nextpage)::(getPagesFrom (n+1) )

Console.WriteLine "Getting Pages:"

let pages = getPagesFrom 1

Console.WriteLine("done({0}).", List.length pages)

for (n,page) in pages do
    

Console.WriteLine "Wrote pages to disk"