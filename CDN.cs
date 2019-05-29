using System;
using System.IO;
using System.Net.Http;

namespace BuildBackup
{
    public class CDN
    {
        public HttpClient client;
        public string cacheDir;
        public bool isEncrypted = false;
        public string decryptionKeyName = "";

        public byte[] Get(string url, bool returnstream = true, bool redownload = false)
        {
            client.Timeout = new TimeSpan(0, 0, 5);

            url = url.ToLower();
            var uri = new Uri(url);

            string cleanname = uri.AbsolutePath;

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    //Console.Write("\nDownloading " + cleanname);
                    using (HttpResponseMessage response = client.GetAsync(uri).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (MemoryStream mstream = new MemoryStream())
                            using (HttpContent res = response.Content)
                            {
                                res.CopyToAsync(mstream);

                                if (isEncrypted)
                                {
                                    var cleaned = Path.GetFileNameWithoutExtension(cleanname);
                                    var decrypted = BLTE.DecryptFile(cleaned, mstream.ToArray(), decryptionKeyName);

                                    File.WriteAllBytes(cacheDir + cleanname, decrypted);
                                    return decrypted;
                                }
                                else
                                {
                                    File.WriteAllBytes(cacheDir + cleanname, mstream.ToArray());
                                }
                            }
                        }
                        else if(response.StatusCode == System.Net.HttpStatusCode.NotFound && !url.StartsWith("http://client04"))
                        {
                            Console.WriteLine("Not found on primary mirror, retrying on secondary mirror...");
                            return Get("http://client04.pdl.wow.battlenet.com.cn/" + cleanname, returnstream, redownload);
                        }
                        else
                        {
                            throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + url);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("!!! Error retrieving file " + url + ": " + e.Message);
                    File.AppendAllText("failedfiles.txt", url + "\n");
                }
            }

            if (returnstream)
            {
                return File.ReadAllBytes(cacheDir + cleanname);
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
