//using Microsoft.AspNetCore.Http;
//using System;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Web;

//namespace KFrame
//{
//    public static class KFrameManager
//    {
//        public static void GetK(IKFrameReferenceRepository referenceRepository)
//        {
//            var ctx = HttpContext.Current;
//            var res = ctx.Response;
//            var r = referenceRepository.GetReferenceKFrameAsync().Result;
//            res.StatusCode = (int)HttpStatusCode.OK;
//            res.Headers.Add("Access-Control-Allow-Origin", "*");
//            res.ContentType = "application/json";
//            //res.Cache.SetLastModified(UpdateTimestamp);
//            res.Cache.SetCacheability(HttpCacheability.Public);
//            res.Cache.SetExpires(DateTime.Today.ToUniversalTime().AddDays(1));
//            res.Cache.SetMaxAge(DateTime.Today.AddDays(1) - DateTime.Now);
//            res.Flush();
//            //
//            var formatter = new JsonMediaTypeFormatter();
//            new ObjectContent<object>(r, formatter, (MediaTypeHeaderValue)null)
//                .CopyToAsync(res.OutputStream).Wait();
//        }

//        public static void GetI(IKFrameReferenceRepository referenceRepository, long kframe)
//        {
//            var ctx = HttpContext.Current;
//            var res = ctx.Response;
//            var firstEtag = ctx.Request.Headers["If-None-Match"];
//            if (!string.IsNullOrEmpty(firstEtag) && referenceRepository.HasReferenceIFrame(firstEtag))
//            {
//                res.Clear();
//                res.StatusCode = (int)HttpStatusCode.NotModified;
//                res.SuppressContent = true;
//                //res.Cache.SetCacheability(HttpCacheability.Public);
//                res.End();
//                return;
//            }
//            var r = referenceRepository.GetReferenceIFrameAsync(kframe).Result;
//            res.StatusCode = (int)HttpStatusCode.OK;
//            res.ContentType = "application/json";
//            res.Headers.Add("Access-Control-Allow-Origin", "*");
//            //res.Cache.SetLastModified(UpdateTimestamp);
//            res.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
//            res.Cache.SetOmitVaryStar(true);
//            res.Cache.SetETag(r.ETag);
//            res.Flush();
//            //
//            var formatter = new JsonMediaTypeFormatter();
//            new ObjectContent<object>(r.Result, formatter, (MediaTypeHeaderValue)null)
//                .CopyToAsync(res.OutputStream).Wait();
//        }

//        public static string Install(IKFrameReferenceRepository referenceRepository) => referenceRepository.Install();
//    }
//}
