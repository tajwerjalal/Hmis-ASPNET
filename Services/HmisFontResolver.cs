using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;

namespace HmisWeb.Services
{
    public sealed class HmisFontResolver : IFontResolver
    {
        private static readonly string FontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        public string DefaultFontName => "arial.ttf";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(isBold ? "arialbd.ttf" : "arial.ttf", XStyleSimulations.None);
        }

        public byte[] GetFont(string faceName)
        {
            return File.ReadAllBytes(Path.Combine(FontsDir, faceName));
        }

        public static void Install()
        {
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new HmisFontResolver();
        }
    }
}
