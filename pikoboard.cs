using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Threading;
using System.Net;

namespace pikoboard {
  class crawler_runner 
  {
    public static void run() {
      bool running = true;
      var @crawler = new crawler();
      @crawler.changed += () => {
        if (@crawler.pending == 0) running = false;
        else running = true;
      };
      @crawler.crawl();
      while (running) Thread.Sleep(1000);
      Console.WriteLine("Finished downloading.");
    }
  }

  class crawler {
    private const string user_agent_file = "useragent.config";
    private const string downloaded_file = "downloaded.txt";
    private const string places_file = "places.txt";
    private const string img_pattern = "href=\"[:A-Za-z0-9/\\-\\.]*\\.jpe?g\"";
    private int _pending = 0;
    public int pending { 
      get { return _pending; }
      set {
        if (value < 0) value = 0;
        _pending = value;
        changed();
      }
    }
    public event Action changed = delegate {};
    List<string> places;
    readonly HashSet<string> downloaded;
    readonly WebHeaderCollection headers;
    void check_places() {
      if (File.Exists(places_file))
        places = new List<string>(File.ReadAllLines(places_file));
      else {
        File.WriteAllText(places_file, "# put urls to threads here, each at new line:\n");
        places = new List<string>();
      }
    }
    public crawler() {
      try {
        if (!File.Exists(user_agent_file))
          File.WriteAllText(user_agent_file, "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.1 Safari/537.36");
        string user_agent = File.ReadAllLines(user_agent_file).First(l => !l.StartsWith("#")).Trim();
        headers = new WebHeaderCollection();
        headers[HttpRequestHeader.UserAgent] = user_agent;
        headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8";
        headers[HttpRequestHeader.CacheControl] = "max-age=0";
        if (File.Exists(downloaded_file))
          downloaded = new HashSet<string>(File.ReadAllLines(downloaded_file));
        else
          downloaded = new HashSet<string>();
        check_places();
      } catch (Exception e) {
        Console.WriteLine("Error while creating crawler:\n" + e.Message);
      }
    }
    public void crawl() {
      try {
        check_places();
        bool empty = true;
        foreach (string place in places) {
          if (!place.StartsWith("#")) {
            empty = false;
            parse_text(place);
          }
        }
        if (empty) pending = 0;
      } catch (Exception e) {
        Console.WriteLine("Error while parsing from places.txt:\n" + e.Message);
      }
    }
    private static void add_proxy(WebClient client) {
      if (File.Exists("proxy.txt")) {
        var url = File.ReadAllText("proxy.txt");
        WebProxy proxy = new WebProxy();
        proxy.Address = new Uri(url);
        proxy.BypassProxyOnLocal = true;
        client.Proxy = proxy;
      }
    }
    private void parse_text(string address) {
      var client = new WebClient();
      add_proxy(client);
      client.Headers = headers;
      client.DownloadDataCompleted += (object sender, DownloadDataCompletedEventArgs e) => {
        Console.WriteLine("Finished: " + address);
        string image_url = "";
        try {
          string text = Encoding.UTF8.GetString(e.Result);
          string host = Regex.Match(address, "https?://[A-z\\.0-9-]*").Value;
          var images = Regex.Matches(text, img_pattern);
          foreach (Match im in images) {
            image_url = im.Value.Replace("href=", "").Trim('"');
            if (!(image_url.Contains("http://") || image_url.Contains("https://"))) 
              image_url = host + image_url;
            parse_image(image_url);
          }
        } catch (Exception ex) {
          Console.WriteLine("Error:" + image_url);
          if (e.Error != null) Console.WriteLine(e.Error.Message);
          Console.WriteLine(ex.Message);
        }
        pending -= 1;
      };
      pending += 1;
      Console.WriteLine("Starting download: " + address);
      client.DownloadDataAsync(new Uri(address));
    }
    private void parse_image(string address) {
      if (downloaded.Contains(address)) return;
      downloaded.Add(address);
      try {
        File.AppendAllText(downloaded_file, address + "\n");
      } catch {
        System.Threading.Thread.Sleep(1000);
        try {
          File.AppendAllText(downloaded_file, address + "\n");
        } catch (Exception e) {
          Console.WriteLine("downloaded.txt appending error:\n" + e.Message);
        }
      }
      var client = new WebClient();
      add_proxy(client);
      client.Headers = headers;
      client.DownloadDataCompleted += (object sender, DownloadDataCompletedEventArgs e) => {
        pending -= 1;
        try {
          utils.mkdir("temp");
          utils.mkdir(app.download_dir);
          var name = Guid.NewGuid().ToString().Trim('{', '}');
          File.WriteAllBytes("temp" + app.slash + name, e.Result);
          File.Move("temp" + app.slash + name, app.download_dir + app.slash + name);
          GC.Collect();
          Console.WriteLine("Downloaded: " + address);
        } catch (Exception ex) {
          Console.WriteLine("Error: " + address);
          if (e.Error != null) Console.WriteLine(e.Error.Message);
          Console.WriteLine(ex.Message);
        }
      };
      pending += 1;
      address = address.Replace("2ch.hk", "m2-ch.ru");
      Console.WriteLine("Starting download: " + address);
      client.DownloadDataAsync(new Uri(address));
    }
  }

  abstract class piko_entry {
    public abstract string hash { get; }
    public abstract byte[] serialized { get; }
  }

  class hasher {
    public static string calc(string msg) {
      return calc(msg.bytes());
    }
    public static string calc(byte[] bytes) {
      bytes = new System.Security.Cryptography.SHA256Cng().ComputeHash(bytes);
      return bytes.Take(16).Aggregate("", (str, b) => str + b.ToString("x2"));
    }
  }

  class piko_post : piko_entry {
    public override byte[] serialized { get { return (thread + message).bytes(); } }
    public override string hash { get { return hasher.calc(thread + message); } }
    public string thread;
    public string message;
    public piko_post() {}
    public piko_post(string post) {
      thread = post.Substring(0, 32);
      message = post.Substring(32);
    }
  }

  class piko_file : piko_entry {
    public override byte[] serialized { get { return bytes; } }
    public byte[] bytes;
    public override string hash { get { return hasher.calc(bytes); } }
  }

  class piko {
    public static piko_entry[] read(byte[] bytes) {
      if (bytes == null || bytes.Length == 0) return new piko_entry[0];
      var list = new List<piko_entry>();
      for (int i = 0; i < bytes.Length; i++) {
        if (bytes[i] == 'E') break;
        bool is_post = bytes[i] == 'P';
        i += 1;
        int len = BitConverter.ToInt32(bytes, i);
        i += 4;
        var slice = new byte[len];
        for (int x = 0; x < len; x++) slice[x] = bytes[i + x];
        if (is_post) {
          var content = slice.utf8();
          if (content.Length <= app.max_post_size && Regex.IsMatch(content, "^[a-f0-9]{32}.*"))
            list.Add(new piko_post(content));
        }
        else list.Add(new piko_file { bytes = slice });
      }
      return list.ToArray();
    }
    public static byte[] write(piko_entry[] entries) {
      var bytes = new List<byte>();
      foreach (var entry in entries) {
        bytes.Add(entry is piko_file ? (byte)'F' : (byte)'P');
        var serialized = entry.serialized;
        bytes.AddRange(BitConverter.GetBytes(serialized.Length));
        bytes.AddRange(serialized);
      }
      bytes.Add((byte)'E');
      return bytes.ToArray();
    }
  }

  static class gzip {
    public static void copy(this Stream input, Stream output) {
      byte[] buffer = new byte[16384];
      int bytes_read;
      while ((bytes_read = input.Read(buffer, 0, buffer.Length)) > 0) output.Write(buffer, 0, bytes_read);
    }
    public static byte[] zip(byte[] input) {
      try {
        using (var output = new MemoryStream()) {
          using (var gz = new GZipStream(output, CompressionMode.Compress)) 
          using (var ms = new MemoryStream(input)) ms.CopyTo(gz);
          return output.ToArray();
        }
      } catch { return null; }
    }
    public static byte[] unzip(byte[] input) {
      try {
        using (var output = new MemoryStream()) {
          using (var ms = new MemoryStream(input)) 
          using (var gz = new GZipStream(ms, CompressionMode.Decompress)) gz.CopyTo(output);
          return output.ToArray();
        }
      } catch { return null; }
    }
  }

  static class utils {
    public static void mkdir(string dir) {
      if (Directory.Exists(dir)) return;
      Directory.CreateDirectory(dir);
    }
    public static string[] files(string dir) {
      return Directory.GetFiles(dir);
    }
    public static void write(string path, byte[] bytes) {
      File.WriteAllBytes(path, bytes);
    }
    public static byte[] read(string path) {
      return File.ReadAllBytes(path);
    }
    public static string utf8(this byte[] bytes) {
      return Encoding.UTF8.GetString(bytes);
    }
    public static byte[] bytes(this string str) {
      return Encoding.UTF8.GetBytes(str);
    }
    public static List<piko_entry> get_refs(string text) {
      var list = new List<piko_entry>();
      var refs = Regex.Matches(text, "\\[ref=[a-f0-9]{32}\\]");
      foreach (Match r in refs) {
        var hash = r.Value.Substring(5, 32);
        if (File.Exists(app.files_dir + app.slash + hash)) 
          list.Add(new piko_file { bytes = utils.read(app.files_dir + app.slash + hash) });
      }
      return list;
    }
    public static void random_pack(List<piko_entry> entries, string uploadName) {
      var containers = utils.files(app.containers_dir);
      var container = containers[new Random().Next(containers.Length)];
      jpg.hide(container, uploadName, piko.write(entries.ToArray()));
    }
    public static piko_entry[] unpack(string jpeg) {
      try {
        return piko.read(jpg.extract(jpeg));
      } catch { }
      return new piko_entry[0];
    }
  }

  class jpg {
    public static void hide(string input, string output, byte[] data) {
      data = gzip.zip(data);
      var jpeg = utils.read(input);
      var list = new List<byte>();
      list.AddRange(jpeg);
      list.AddRange(data);
      list.AddRange(BitConverter.GetBytes((int)data.Length));
      utils.write(output, list.ToArray());
    }
    public static byte[] extract(string input) {
      var bytes = utils.read(input);
      int len = BitConverter.ToInt32(bytes, bytes.Length - 4);
      if (len >= bytes.Length) return null;
      var res = new byte[len];
      for (int i = bytes.Length - len - 4; i < bytes.Length - 4; i++) {
        res[i - (bytes.Length - len - 4)] = bytes[i];
      }
      return gzip.unzip(res);
    }
  }

  class css {
    public const string style = @"g { color: #888; font-style: italic; font-size: 75%; } 
.post { background-color: #ddd; border: 1px solid #aaa; border-radius: .4em; margin: .4em 0; max-width: 600px; margin-right: auto; } 
.post:not(:first-child) { margin-left: 20px; } 
body { background: #eee; font-size: 14px; font-family: Helvetica; } 
textarea { width: 300px; height: 100px; font-size: 12px; } 
a { text-decoration: none; color: darkorange; } 
img { max-width: 150px; transition: max-width 1s ease-in-out; -moz-transition: max-width 1s ease-in-out; -webkit-transition: max-width 1s ease-in-out; } 
img:hover { max-width: 100%; transition: max-width 1s ease-in-out; -moz-transition: max-width 1s ease-in-out; -webkit-transition: max-width 1s ease-in-out; }
img:target { max-width: 100%; }
.post:target { border: 1px dashed #bbb; background-color: #fed; }
a:hover { color: salmon; } 
spoiler { background-color: #000; } 
spoiler:hover { background-color: #ddd; } 
.post-inner { word-break: normal; overflow: auto; max-height: 40em; padding: 0em 0.25em; margin: .5em; }";
  }

  static class html {
    public const string head = "<!DOCTYPE html><html><head><link rel='stylesheet' type='text/css' href='../styles/piko.css' /><script src='../scripts/piko.js'></script></head>";
    static int index_of(this piko_post[] posts, string hash) {
      for (int i = 0; i < posts.Length; i++)
        if (posts[i].hash == hash) return i;
      return -1;
    }
    static string first_ref(this piko_post post) {
      if (Regex.IsMatch(post.message, ">>[a-f0-9]{32}"))
        return Regex.Match(post.message, ">>[a-f0-9]{32}").Value.Substring(2);
      return "";
    }
    static string create_ref(bool image, string hash, string rel) {
      if (image)
        return "<img src='" + rel + app.files_dir + "/" + hash + "'>";
      else
        return "<a target=_blank href='" + rel + app.files_dir + "/" + hash + "'>[file:"+hash+"]</a>";
    }
    static piko_post[] sort(piko_post[] posts) {
      int max = 100000;
      for (int i = 0; i < posts.Length; i++) {
        if (posts.index_of(posts[i].first_ref()) > i) {
          if (max-- <= 0) continue;
          var wrong = posts[i];
          var newarr = posts.ToList();
          newarr.Remove(wrong);
          newarr.Insert(newarr.ToArray().index_of(wrong.first_ref()) + 1, wrong);
          posts = newarr.ToArray();
          i = 0;
        }
      }
      return posts;
    }
    public static string wrap_post(piko_post p) {
      return "<div id='" + p.hash + "' class='post'><div class='post-inner'><g>" + p.hash + "</g><br/>" + format(p.message) + "</div></div>";
    }
    public static string format(string msg, string rel = "../") {
      msg = msg.Replace("<", "&lt;");
      msg = msg.Replace(">", "&gt;");
      msg = msg.Replace("\n", "<br/>");
      var refs = Regex.Matches(msg, "\\[(ref|raw)=[a-f0-9]{32}\\]");
      foreach (Match r in refs) {
        msg = msg.Replace(r.Value, create_ref(r.Value.StartsWith("[ref"), r.Value.Substring(5, 32), rel));
      }
      var thread_links = Regex.Matches(msg, "&gt;&gt;&gt;[a-f0-9]{32}");
      foreach (Match m in thread_links) {
        var hash = m.Value.Substring(12);
        msg = msg.Replace(m.Value, "<a href='" + hash + ".html'>" + m.Value + "</a>");
      }
      var post_links = Regex.Matches(msg, "&gt;&gt;[a-f0-9]{32}");
      foreach (Match m in post_links) {
        var hash = m.Value.Substring(8);
        msg = msg.Replace(m.Value, "<a href='#" + hash + "'>" + m.Value + "</a>");
      }
      foreach (var ch in "biusg")
        msg = msg.Replace("["+ch+"]", "<"+ch+">").Replace("[/"+ch+"]", "</"+ch+">");
      msg = msg.Replace("[spoiler]", "<spoiler>");
      msg = msg.Replace("[/spoiler]", "</spoiler>");
      msg = msg.Replace("[sp]", "<spoiler>");
      msg = msg.Replace("[/sp]", "</spoiler>");
      return msg;
    }
    public static void refresh(string thread) {
      var files = utils.files(app.db_dir + app.slash + thread);
      files = files.Where(f => Regex.IsMatch(f, ".*[a-f0-9]{32}$")).ToArray();
      files = files.OrderBy(f => Directory.GetCreationTime(f).ToFileTimeUtc()).ToArray();
      var posts = new List<piko_post>();
      foreach (var f in files) {
        var content = utils.read(f).utf8();
        if (Regex.IsMatch(content, "^[a-f0-9]{32}.*"))
          posts.Add(new piko_post(content));
      }
      posts = sort(posts.ToArray()).ToList();
      var sb = new StringBuilder();
      sb.Append(head);
      sb.Append("<body>");
      foreach (var p in posts) sb.Append(wrap_post(p));
      sb.Append("<textarea>thread="+thread+"\nenter your message... >>hash references the post within a thread, >>>hash - thread\nand save this as post.txt and feed to the app</textarea>");
      sb.Append("</body></html>");
      utils.write(app.html_dir + app.slash + thread + ".html", sb.ToString().bytes());
    }
  }

  class app {
    public const string containers_dir = "jpeg_containers";
    public const string files_dir = "board_files";
    public const string download_dir = "download";
    public const string db_dir = "database";
    public const string html_dir = "html";
    public const string upload_dir = "for_upload";
    const string styles_dir = "styles";
    const string pikocss = "piko.css";
    public const int max_post_size = 5 * 1000 + 32;
    public const int max_ref_retranslate = 1536;
    public const int max_jpeg_size = (int)(3.5 * 1024 * 1024);
    public static char slash = Path.DirectorySeparatorChar;
    public const string posttxt = "post.txt";
    public static void add_post(string text) {
      var list = new List<piko_entry>();
      var lines = text.Replace("\r\n", "\n").Split('\n');
      var thread = lines[0].Split('=')[1];
      bool is_oppost = thread.Length == 0;
      int byte_count = 0, index = 0;
      list.AddRange(utils.get_refs(text));
      list.ForEach(e => byte_count += e.serialized.Length);
      var post = (is_oppost ? new string('0', 32) : thread) + string.Join("\n", lines.Skip(1));
      if (post.Length > max_post_size) {
        Console.WriteLine("Error: max post size " + max_post_size + " exceeded.");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
        return;
      }
      var post_hash = hasher.calc(post);
      var prefix = upload_dir + slash + "upload" + post_hash + "_";
      if (is_oppost) thread = post_hash;
      utils.mkdir(db_dir + slash + thread);
      utils.write(db_dir + slash + thread + slash + post_hash, post.bytes());
      html.refresh(thread);
      var files = utils.files(db_dir + slash + thread);
      foreach (var file in files) {
        if (byte_count > max_jpeg_size) {
          utils.random_pack(list, prefix + (++index) + ".jpg");
          list.Clear();
          byte_count = 0;
        }
        var bytes = utils.read(file);
        var str = bytes.utf8();
        utils.get_refs(str).Cast<piko_file>().Where(r => r.bytes.Length <= max_ref_retranslate)
          .Take(1).ToList().ForEach(r => list.Add(r));
        list.Add(new piko_post(str));
      }
      if (list.Count > 0) utils.random_pack(list, prefix + (++index) + ".jpg");
    }
    public static void Main(string[] args) {
      new[]{ styles_dir, containers_dir, download_dir, files_dir, db_dir, upload_dir, html_dir }.ToList().ForEach(utils.mkdir);
      if (!File.Exists(styles_dir + slash + pikocss)) utils.write(styles_dir + slash + pikocss, css.style.bytes());
      if (args.Length == 0) {
        Console.WriteLine("pikoboard -a    collect jpegs from places.txt urls");
        Console.WriteLine("pikoboard -r hash    refresh html of some thread");
        Console.WriteLine("pikoboard -ra  refresh html of all threads (warning may be long operation)");
        Console.WriteLine("pikoboard file_with_post.txt    create container(s) from thread of this post and its file(s)");
        Console.WriteLine("   /" + containers_dir + " should have one or more jpegs");
        Console.WriteLine("   result will be in /" + upload_dir);
        Console.WriteLine("pikoboard any_file   create post template with this file referenced");
        Console.WriteLine("pikoboard       create template of post - post.txt and show this help");
        File.WriteAllText(posttxt, "thread=\r\nmessage");
        return;
      }
      if (args.Length == 2 && args[0] == "-r") {
        var hash = args[1];
        html.refresh(hash);
        return;
      }
      if (args.Length == 1 && args[0] == "-ra") {
        Directory.GetDirectories(db_dir).ToList().ForEach(hash => html.refresh(hash));
        return;
      }
      if (args.Length == 1 && args[0] == "-a") {
        Console.WriteLine("Running crawler...");
        crawler_runner.run();
        Console.WriteLine("Checking new images...");
        HashSet<string> to_upd = new HashSet<string>(), fresh = new HashSet<string>();
        var files = utils.files(download_dir);
        foreach (var f in files) {
          var entries = piko.read(jpg.extract(f));
          foreach (var e in entries) {
            if (e is piko_post) {
              var pp = e as piko_post;
              utils.mkdir(db_dir + slash + pp.thread);
              var file = db_dir + slash + pp.thread + slash + pp.hash;
              if (File.Exists(file)) continue;
              utils.write(file, pp.serialized);
              to_upd.Add(pp.thread);
              fresh.Add(pp.thread + pp.hash + pp.message);
            } else if (e is piko_file) {
              var pf = e as piko_file;
              var file = files_dir + slash + pf.hash;
              if (File.Exists(file)) continue;
              utils.write(file, pf.serialized);
            }
          }
        }
        foreach (var u in to_upd) html.refresh(u);
        var sb = new StringBuilder();
        sb.Append(html.head.Replace("../",""));
        sb.Append("<body>");
        sb.Append(html.wrap_post(new piko_post(new string('0', 32) + "Recently recevied posts:")));
        foreach (var f in fresh) sb.Append(html.wrap_post(new piko_post { thread = f.Substring(0, 32), message = f.Substring(64) }));
        sb.Append("</body></html>");
        utils.write("updates_" + DateTime.UtcNow.ToFileTimeUtc().ToString("x") + ".html", sb.ToString().bytes());
        Console.WriteLine("Cleaning up...");
        foreach (var f in files) File.Delete(f);
        Console.WriteLine("Done!");
        return;
      }
      var bytes = utils.read(args[0]);
      if (bytes[0] == 't' && bytes[1] == 'h' && bytes[2] == 'r' && bytes[3] == 'e' && bytes[4] == 'a' && bytes[5] == 'd') {
        add_post(File.ReadAllText(args[0]));
      } else {
        var hash = hasher.calc(bytes);
        File.WriteAllText(posttxt, 
          "thread=enter hash of thread here or just leave thread= for new thread\r\n" +
          "[ref=" + hash + "]\r\n" +
          "change ref to raw to link file not image, put your message here, limit is " + max_post_size / 1000 + "k chars.");
        utils.write(files_dir + slash + hash, bytes);
      }
    }
  }
}
