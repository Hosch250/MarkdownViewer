using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Toolkit.Parsers.Markdown;
using Microsoft.Toolkit.Parsers.Markdown.Blocks;
using Microsoft.Toolkit.Parsers.Markdown.Inlines;

namespace MarkdownViewer
{
    public partial class MarkdownBox : RichTextBox
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(MarkdownBox), new UIPropertyMetadata(default(string), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            if (source is MarkdownBox control)
            {
                var newValue = (string)args.NewValue;
                switch (args.Property.Name)
                {
                    case nameof(Text):
                        control.Text = newValue;
                        break;
                }
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                var old = GetValue(TextProperty);
                SetValue(TextProperty, value);
                OnPropertyChanged(new DependencyPropertyChangedEventArgs(TextProperty, old, value));

                SetTextboxContent();
            }
        }

        public MarkdownBox()
        {
            InitializeComponent();
        }

        private void Hlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void SetTextboxContent()
        {
            Content.Document.Blocks.Clear();

            var doc = new MarkdownDocument();
            doc.Parse(Text ?? string.Empty);

            Content.Document.Blocks.AddRange(GetBlocks(doc.Blocks));
        }

        private IEnumerable<Block> GetBlocks(IList<MarkdownBlock> blocks)
        {
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case HeaderBlock header:
                        yield return GetHeaderBlock(header);
                        break;
                    case ParagraphBlock paragraph:
                        yield return GetParagraphBlock(paragraph);
                        break;
                    case ListBlock list:
                        yield return GetListBlock(list);
                        break;
                    case CodeBlock code:
                        yield return GetCodeBlock(code);
                        break;
                    case QuoteBlock quote:
                        yield return GetQuoteBlock(quote);
                        break;
                    case HorizontalRuleBlock rule:
                        yield return GetRuleBlock(rule);
                        break;
                    case TableBlock table:
                        yield return GetTableBlock(table);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private Block GetHeaderBlock(HeaderBlock header)
        {
            var headerLevels = new Dictionary<int, double>
            {
                [1] = 28,
                [2] = 21,
                [3] = 16.3833,
                [4] = 14,
                [5] = 11.6167,
                [6] = 9.38333,
            };

            var content = header.Inlines.Select(GetInline);
            var span = new Span();
            span.Inlines.AddRange(content);

            var labelElement = new Label
            {
                Content = span,
                FontSize = headerLevels[header.HeaderLevel]
            };
            var blockElement = new BlockUIContainer(labelElement);
            return blockElement;
        }

        private Block GetParagraphBlock(ParagraphBlock paragraph)
        {
            var paragraphElement = new Paragraph();
            paragraphElement.Inlines.AddRange(paragraph.Inlines.Select(GetInline));
            return paragraphElement;
        }

        private Block GetListBlock(ListBlock list)
        {
            var listElement = new List
            {
                MarkerStyle = list.Style == ListStyle.Bulleted ? TextMarkerStyle.Disc : TextMarkerStyle.Decimal
            };
            foreach (var item in list.Items)
            {
                var listItemElement = new ListItem();
                listItemElement.Blocks.AddRange(GetBlocks(item.Blocks));
                listElement.ListItems.Add(listItemElement);
            }

            return listElement;
        }

        private Block GetCodeBlock(CodeBlock code)
        {
            var typeConverter = new HighlightingDefinitionTypeConverter();
            var avalon = new TextEditor
            {
                Text = code.Text,
                SyntaxHighlighting = (IHighlightingDefinition)typeConverter.ConvertFrom("C#"),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Padding = new Thickness(10),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                ShowLineNumbers = true,
                MaxHeight = 250
            };

            return new BlockUIContainer(avalon);
        }

        private Block GetQuoteBlock(QuoteBlock quote)
        {
            var sectionElement = new Section
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xDC)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xff, 0xeb, 0x8e)),
                BorderThickness = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(5)
            };
            var quoteBlocks = GetBlocks(quote.Blocks).ToList();
            for (var i = 0; i < quoteBlocks.Count; i++)
            {
                var item = quoteBlocks[i];
                item.Padding = new Thickness(5, 0, 5, 0);
                item.Margin = new Thickness(0);
                sectionElement.Blocks.Add(item);
            }

            return sectionElement;
        }

        private Block GetRuleBlock(HorizontalRuleBlock rule)
        {
            var line = new Line
            {
                Stretch = Stretch.Fill,
                Stroke = Brushes.DarkGray,
                X2 = 1
            };
            return new Paragraph(new InlineUIContainer(line));
        }

        private Block GetTableBlock(TableBlock table)
        {
            var alignments = new Dictionary<ColumnAlignment, TextAlignment>
            {
                [ColumnAlignment.Center] = TextAlignment.Center,
                [ColumnAlignment.Left] = TextAlignment.Left,
                [ColumnAlignment.Right] = TextAlignment.Right,
                [ColumnAlignment.Unspecified] = TextAlignment.Justify
            };

            var tableElement = new Table
            {
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xdf, 0xe2, 0xe5)),
                CellSpacing = 0
            };
            var tableRowGroup = new TableRowGroup();
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                var tableRow = new TableRow();

                if (rowIndex % 2 == 0 && rowIndex != 0)
                {
                    tableRow.Background = new SolidColorBrush(Color.FromRgb(0xf6, 0xf8, 0xfa));
                }

                for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                {
                    var cell = row.Cells[cellIndex];

                    var cellContent = new Paragraph();
                    cellContent.Inlines.AddRange(cell.Inlines.Select(GetInline));

                    var tableCell = new TableCell
                    {
                        TextAlignment = alignments[table.ColumnDefinitions[cellIndex].Alignment],
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xdf, 0xe2, 0xe5)),
                        BorderThickness = new Thickness(1, 1, 0, 0),
                        Padding = new Thickness(13, 6, 13, 6)
                    };
                    tableCell.Blocks.Add(cellContent);

                    if (rowIndex == 0)
                    {
                        tableCell.FontWeight = FontWeights.Bold;
                    }

                    tableRow.Cells.Add(tableCell);
                }

                tableRowGroup.Rows.Add(tableRow);
            }
            tableElement.RowGroups.Add(tableRowGroup);

            return tableElement;
        }

        private Inline GetInline(MarkdownInline element)
        {
            switch (element)
            {
                case BoldTextInline bold:
                    return GetBoldInline(bold);
                case TextRunInline text:
                    return GetTextRunInline(text);
                case ItalicTextInline italic:
                    return GetItalicInline(italic);
                case StrikethroughTextInline strikethrough:
                    return GetStrikethroughInline(strikethrough);
                case CodeInline code:
                    return GetCodeInline(code);
                case MarkdownLinkInline markdownLink:
                    return GetMarkdownLinkInline(markdownLink);
                case HyperlinkInline hyperlink:
                    return GetHyperlinkInline(hyperlink);
                case ImageInline image:
                    return GetImageInline(image);
                case SubscriptTextInline subscript:
                    return GetSubscriptInline(subscript);
                case SuperscriptTextInline superscript:
                    return GetSuperscriptInline(superscript);
                default:
                    throw new NotImplementedException();
            }
        }

        private Inline GetBoldInline(BoldTextInline bold)
        {
            var boldElement = new Bold();
            foreach (var inline in bold.Inlines)
            {
                boldElement.Inlines.Add(GetInline(inline));
            }
            return boldElement;
        }

        private static Inline GetTextRunInline(TextRunInline text)
        {
            return new Run(text.ToString());
        }

        private Inline GetItalicInline(ItalicTextInline italic)
        {
            var italicElement = new Italic();
            foreach (var inline in italic.Inlines)
            {
                italicElement.Inlines.Add(GetInline(inline));
            }
            return italicElement;
        }

        private Inline GetStrikethroughInline(StrikethroughTextInline strikethrough)
        {
            var strikethroughElement = new Span();
            strikethroughElement.TextDecorations.Add(TextDecorations.Strikethrough);
            foreach (var inline in strikethrough.Inlines)
            {
                strikethroughElement.Inlines.Add(GetInline(inline));
            }
            return strikethroughElement;
        }

        private static Inline GetCodeInline(CodeInline code)
        {
            return new Run(code.Text)
            {
                Background = new SolidColorBrush(Color.FromRgb(0xef, 0xf0, 0xf1))
            };
        }

        private Inline GetMarkdownLinkInline(MarkdownLinkInline markdownLink)
        {
            var markdownLinkElement = new Hyperlink();
            markdownLinkElement.Inlines.AddRange(markdownLink.Inlines.Select(GetInline));
            markdownLinkElement.NavigateUri = new Uri(markdownLink.Url);
            markdownLinkElement.ToolTip = markdownLink.Tooltip;
            markdownLinkElement.RequestNavigate += Hlink_RequestNavigate;
            return markdownLinkElement;
        }

        private Inline GetHyperlinkInline(HyperlinkInline hyperlink)
        {
            var hyperlinkElement = new Hyperlink();
            hyperlinkElement.Inlines.Add(hyperlink.Text);
            hyperlinkElement.NavigateUri = new Uri(hyperlink.Url);
            hyperlinkElement.RequestNavigate += Hlink_RequestNavigate;
            return hyperlinkElement;
        }

        private static Inline GetImageInline(ImageInline image)
        {
            var uri = new Uri(image.RenderUrl);
            var bitmap = new BitmapImage(uri);
            var imageElement = new Image
            {
                Source = bitmap,
                Height = image.ImageHeight == 0 ? double.NaN : image.ImageHeight,
                Width = image.ImageWidth == 0 ? double.NaN : image.ImageWidth,
                ToolTip = image.Tooltip
            };
            return new InlineUIContainer(imageElement);
        }

        private Inline GetSubscriptInline(SubscriptTextInline subscript)
        {
            var subscriptElement = new Span();
            subscriptElement.Typography.Variants = FontVariants.Subscript;
            foreach (var inline in subscript.Inlines)
            {
                subscriptElement.Inlines.Add(GetInline(inline));
            }
            return subscriptElement;
        }

        private Inline GetSuperscriptInline(SuperscriptTextInline superscript)
        {
            var superscriptElement = new Span();
            superscriptElement.Typography.Variants = FontVariants.Superscript;
            foreach (var inline in superscript.Inlines)
            {
                superscriptElement.Inlines.Add(GetInline(inline));
            }
            return superscriptElement;
        }
    }
}
