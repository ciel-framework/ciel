using System.Text;

namespace Ciel.Birb.Extra;

public class FileServer(string root, bool listing = false, bool showHidden = false) : IHandler
{
    public async Task HandleAsync(Request req, ResponseWriter resp)
    {
        var fullpath = Path.GetFullPath(
            Path.Combine(root, req.Path.TrimStart('/'))
        );

        var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!fullpath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullpath, root, StringComparison.OrdinalIgnoreCase))
        {
            await resp.NotFoundAsync();
            return;
        }

        if (listing && Directory.Exists(fullpath))
        {
            await IndexOfAsync(req, resp, fullpath);
            return;
        }

        var index = Path.Combine(root, fullpath, "index.html");
        if (File.Exists(index))
        {
            await resp.SendFileAsync(index);
            return;
        }

        await resp.SendFileAsync(Path.Combine(root, fullpath));
    }

    private async Task IndexOfAsync(Request req, ResponseWriter resp, string fullpath)
    {
        StringBuilder sb = new();
        sb.Append(
            $"""<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><title>Index of {req.Path}</title></head>""");

        sb.Append("""
                  <style>
                    :root {
                      color-scheme: light dark;
                  }

                  body {
                      font-family: system-ui, sans-serif;
                      max-width: 720px;
                      margin: 2rem auto;
                      padding: 0 1rem;
                      background: var(--bg);
                      color: var(--fg);
                      transition: background .2s, color .2s;
                  }

                  h1 {
                      font-size: 1.4rem;
                      margin-bottom: 1rem;
                      border-bottom: 1px solid var(--border);
                      padding-bottom: .3rem;
                  }

                  footer {
                      font-size: 1rem;
                      margin-top: 1rem;
                      border-top: 1px solid var(--border);
                      padding-top: .3rem;
                  }


                  ul {
                      list-style: none;
                      margin: 0;
                      padding: 0;
                  }

                  li {
                      padding: .25rem 0;
                  }

                  a {
                      color: var(--link);
                      text-decoration: none;
                  }

                  a:hover {
                      text-decoration: underline;
                  }

                  /* folder/file/parent icons */
                  .dir a::before {
                      content: "📁 ";
                  }

                  .file a::before {
                      content: "📄 ";
                  }

                  .parent a::before {
                      content: "↖️ ";
                  }

                  /* Light mode */
                  @media (prefers-color-scheme: light) {
                      :root {
                          --bg: #fafafa;
                          --fg: #222;
                          --border: #ddd;
                          --link: #0064c8;
                      }
                  }

                  /* Dark mode */
                  @media (prefers-color-scheme: dark) {
                      :root {
                          --bg: #111;
                          --fg: #ddd;
                          --border: #333;
                          --link: #4ea3ff;
                      }
                  }
                  </style>
                  """);

        sb.Append($"<body><h1>Index of {req.Path}</h1>");
        sb.Append("<ul>");

        if (req.Path != "/")
        {
            var parent = Path.GetDirectoryName(req.Path.TrimEnd('/')) ?? "/";
            if (!parent.StartsWith("/")) parent = "/" + parent;
            sb.Append($"""<li class="parent"><a href="{parent}">..</a></li>""");
        }

        foreach (var item in Directory.GetDirectories(fullpath)
                     .OrderBy(dir => Path.GetFileName(dir)))
        {
            var name = Path.GetFileName(item);
            if (name.StartsWith('.')) continue;
            var href = Path.Join(req.Path, name);
            sb.Append($"""<li class="dir"><a href="{href}">{name}/</a></li>""");
        }

        foreach (var item in Directory.GetFiles(fullpath)
                     .OrderBy(dir => Path.GetFileName(dir)))
        {
            var name = Path.GetFileName(item);
            if (name.StartsWith('.')) continue;
            var href = Path.Join(req.Path, name);
            sb.Append($"""<li class="file"><a href="{href}">{name}</a></li>""");
        }


        sb.Append("</ul>");
        sb.Append("<footer>Powered by Ciel 🌌</footer>");
        sb.Append("</body></html>");

        await resp.WriteAsync(sb.ToString());
    }
}