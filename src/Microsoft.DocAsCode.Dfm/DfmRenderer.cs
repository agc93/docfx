// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmRenderer : HtmlRenderer, IDisposable
    {
        private readonly DfmInclusionLoader _inlineInclusionHelper = new DfmInlineInclusionLoader(true);
        private readonly DfmInclusionLoader _blockInclusionHelper = new DfmInclusionLoader();
        private readonly DfmCodeRenderer _codeRenderer = new DfmCodeRenderer();

        public ImmutableDictionary<string, string> Tokens { get; set; }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer result = "<xref";
            result = AppendAttribute(result, "href", token.Href);
            result = AppendAttribute(result, "title", token.Title);
            result = AppendAttribute(result, "data-throw-if-not-resolved", token.ThrowIfNotResolved.ToString());
            result = AppendAttribute(result, "data-raw-source", token.SourceInfo.Markdown);
            result = AppendSourceInfo(result, renderer, token);
            result += ">";

            foreach (var item in token.Content)
            {
                result += renderer.Render(item);
            }

            result += "</xref>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            lock (_blockInclusionHelper)
            {
                return _blockInclusionHelper.Load(renderer, token.Src, token.Raw, token.SourceInfo, context, (DfmEngine)renderer.Engine);
            }
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            lock (_inlineInclusionHelper)
            {
                return _inlineInclusionHelper.Load(renderer, token.Src, token.Raw, token.SourceInfo, context, (DfmEngine)renderer.Engine);
            }
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            if (string.IsNullOrEmpty(token.Content))
            {
                return StringBuffer.Empty;
            }
            var startLine = token.SourceInfo.LineNumber;
            var endLine = token.SourceInfo.LineNumber + token.SourceInfo.ValidLineCount - 1;
            var sourceFile = token.SourceInfo.File;

            StringBuffer result = $"<yamlheader start=\"{startLine}\" end=\"{endLine}\"";
            if (!string.IsNullOrEmpty(sourceFile))
            {
                sourceFile = StringHelper.HtmlEncode(sourceFile);
                result += $" sourceFile=\"{sourceFile}\"";
            }
            result += ">";
            result += StringHelper.HtmlEncode(token.Content);
            return result + "</yamlheader>";
        }

        public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = string.Empty;
            var splitTokens = DfmBlockquoteHelper.SplitBlockquoteTokens(token.Tokens);
            foreach (var splitToken in splitTokens)
            {
                if (splitToken.Token is DfmSectionBlockToken)
                {
                    if (!splitToken.Token.SourceInfo.Markdown.EndsWith("\n"))
                    {
                        Logger.LogWarning("The content part of [!div] syntax is suggested to start in a new line.", file: splitToken.Token.SourceInfo.File, line: splitToken.Token.SourceInfo.LineNumber.ToString());
                    }
                    content += "<div";
                    content += ((DfmSectionBlockToken)splitToken.Token).Attributes;
                    content = AppendSourceInfo(content, renderer, splitToken.Token);
                    content += ">";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</div>\n";
                }
                else if (splitToken.Token is DfmNoteBlockToken)
                {
                    if (!splitToken.Token.SourceInfo.Markdown.EndsWith("\n"))
                    {
                        Logger.LogWarning("The content part of NOTE/WARNING/CAUTION/IMPORTANT/NEXT syntax is suggested to start in a new line.", file: splitToken.Token.SourceInfo.File, line: splitToken.Token.SourceInfo.LineNumber.ToString());
                    }
                    var noteToken = (DfmNoteBlockToken)splitToken.Token;
                    content += "<div class=\"";
                    content += noteToken.NoteType.ToUpper();
                    content += "\"";
                    content = AppendSourceInfo(content, renderer, splitToken.Token);
                    content += ">";
                    string heading;
                    if (Tokens != null && Tokens.TryGetValue(noteToken.NoteType.ToLower(), out heading))
                    {
                        content += heading;
                    }
                    else
                    {
                        content += "<h5>";
                        content += noteToken.NoteType.ToUpper();
                        content += "</h5>";
                    }
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</div>\n";
                }
                else if (splitToken.Token is DfmVideoBlockToken)
                {
                    var videoToken = splitToken.Token as DfmVideoBlockToken;
                    content += "<div class=\"embeddedvideo\"><iframe src=\"";
                    content += videoToken.Link;
                    content += "\" frameborder=\"0\" allowfullscreen=\"true\"";
                    content = AppendSourceInfo(content, renderer, splitToken.Token);
                    content += "></iframe></div>\n";
                    continue;
                }
                else
                {
                    content += "<blockquote";
                    content = AppendSourceInfo(content, renderer, splitToken.Token);
                    content += ">";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</blockquote>\n";
                }
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesToken token, IMarkdownContext context)
        {
            return _codeRenderer.Render(renderer, token, context);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            return Render(renderer, token, (IMarkdownContext)context);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return token.Content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmVideoBlockToken token, MarkdownBlockContext context)
        {
            return token.SourceInfo.Markdown;
        }

        public void Dispose()
        {
            _inlineInclusionHelper.Dispose();
            _blockInclusionHelper.Dispose();
        }
    }
}
