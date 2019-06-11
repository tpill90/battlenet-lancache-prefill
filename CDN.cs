using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace BuildBackup
{
    public class CDN
    {
        public HttpClient client;
        public string cacheDir;
        public bool isEncrypted = false;
        public string decryptionKeyName = "";
        public List<string> cdnList;

        public byte[] Get(string path, bool returnstream = true, bool redownload = false)
        {
            if (redownload || !File.Exists(Path.Combine(cacheDir, path)))
            {
                var found = false;

                foreach(var cdn in cdnList)
                {
                    if (found) continue;

                    var url = "http://" + cdn + "/" + path.ToLower();
                    var uri = new Uri(url);
                    string cleanname = uri.AbsolutePath;

                    try
                    {
                        if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                        using (HttpResponseMessage response = client.GetAsync(uri).Result)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                using (MemoryStream mstream = new MemoryStream())
                                using (HttpContent res = response.Content)
                                {
                                    res.CopyToAsync(mstream);

                                    found = true;

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
                            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                Logger.WriteLine("File not found on CDN " + cdn + " trying next CDN (if available)..");
                            }
                            else
                            {
                                throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + url);
                            }
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        if (!e.CancellationToken.IsCancellationRequested)
                        {
                            Logger.WriteLine("!!! Timeout while retrieving file " + url);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine("!!! Error retrieving file " + url + ": " + e.Message);
                    }
                }

                if (!found)
                {
                    Logger.WriteLine("Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve it!", true);
                }
            }

            if (returnstream)
            {
                return File.ReadAllBytes(Path.Combine(cacheDir, path));
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
