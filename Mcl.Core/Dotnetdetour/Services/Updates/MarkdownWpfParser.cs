using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mcl.Core.Updater
{
    public static class MarkdownWpfParser
    {
        private static readonly Regex InlineRegex = new Regex(
            @"(!\[(?<imgAlt>[^\]]*)\]\((?<imgUrl>[^\)]+)\))|(\[(?<linkText>[^\]]+)\]\((?<linkUrl>[^\)]+)\))|(\*\*(?<bold>[^\*]+)\*\*)", 
            RegexOptions.Compiled);

        public static FlowDocument Parse(string markdown)
        {
            FlowDocument doc = new FlowDocument
            {
                FontFamily = new FontFamily("Microsoft YaHei, Segoe UI"),
                FontSize = 14,
                LineHeight = 24,
                PagePadding = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1C1C"))
            };

            if (string.IsNullOrWhiteSpace(markdown)) return doc;
            string[] lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            Paragraph currentParagraph = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    currentParagraph = null; 
                    continue;
                }

                Paragraph p = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };

                if (line.StartsWith("#"))
                {
                    string content = line.TrimStart('#').Trim();
                    p.Inlines.Add(new Run(content) { FontWeight = FontWeights.Bold });
                    p.FontSize = line.StartsWith("##") ? 18 : 22;
                    p.Margin = new Thickness(0, 16, 0, 10);
                    doc.Blocks.Add(p);
                }
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    p.Margin = new Thickness(20, 2, 0, 2);
                    ParseInlineElements(line.Substring(2).Trim(), p);
                    p.Inlines.InsertBefore(p.Inlines.FirstInline, new Run("•  ") { FontWeight = FontWeights.Bold, Foreground = Brushes.Gray });
                    doc.Blocks.Add(p);
                }
                else
                {
                    if (currentParagraph == null)
                    {
                        currentParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                        doc.Blocks.Add(currentParagraph);
                    }
                    else
                    {
                        currentParagraph.Inlines.Add(new LineBreak());
                    }
                    ParseInlineElements(line, currentParagraph);
                }
            }
            return doc;
        }

        private static void ParseInlineElements(string text, Paragraph p)
        {
            int lastIndex = 0;
            foreach (Match match in InlineRegex.Matches(text))
            {
                if (match.Index > lastIndex) p.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));

                if (match.Groups["imgUrl"].Success)
                {
                    try
                    {
                        string imgUrl = match.Groups["imgUrl"].Value;

                        // 【核心改动】自动为 GitHub 的图片链接添加代理，解决大陆无法加载问题
                        if (imgUrl.Contains("github.com") || imgUrl.Contains("githubusercontent.com"))
                        {
                            if (!imgUrl.StartsWith("https://gh-proxy.com/") && !imgUrl.StartsWith("https://ghproxy.net/"))
                            {
                                imgUrl = $"https://gh-proxy.com/{imgUrl}";
                            }
                        }

                        Image img = new Image
                        {
                            MaxWidth = 450, 
                            Margin = new Thickness(0, 10, 0, 10),
                            Stretch = Stretch.Uniform,
                            Source = new BitmapImage(new Uri(imgUrl, UriKind.Absolute)) // WPF 默认会在后台异步加载图片网络流
                        };
                        p.Inlines.Add(new InlineUIContainer(img));
                    }
                    catch { }
                }
                //// 处理超链接
                else if (match.Groups["linkUrl"].Success)
                {
                    Hyperlink link = new Hyperlink(new Run(match.Groups["linkText"].Value))
                    {
                        NavigateUri = new Uri(match.Groups["linkUrl"].Value),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#005FB8")),
                        TextDecorations = TextDecorations.Underline
                    };
                    
                    // 核心修复：不用 RequestNavigate，直接拦截 WPF 路由点击事件
                    link.Click += (s, e) =>
                    {
                        try
                        {
                            var uri = ((Hyperlink)s).NavigateUri.AbsoluteUri;
                            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                        }
                        catch { }
                    };
                    p.Inlines.Add(link);
                }
                else if (match.Groups["bold"].Success)
                {
                    p.Inlines.Add(new Run(match.Groups["bold"].Value) { FontWeight = FontWeights.Bold });
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length) p.Inlines.Add(new Run(text.Substring(lastIndex)));
        }
    }
}