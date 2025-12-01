using DinkToPdf;
using DinkToPdf.Contracts;

namespace ProjectQLDCCT.Helpers.Services
{
    public interface IPdfService
    {
        byte[] ConvertHtmlToPdf(string html);
    }

    public class PdfService : IPdfService
    {
        private readonly IConverter _converter;

        public PdfService(IConverter converter)
        {
            _converter = converter;
        }

        public byte[] ConvertHtmlToPdf(string html)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    // Sửa chỗ margin: dùng MarginSettings
                    Margins = new MarginSettings
                    {
                        Top = 15,
                        Bottom = 15,
                        Left = 15,
                        Right = 15
                    },
                    DocumentTitle = "De cuong chi tiet"
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings = new WebSettings
                        {
                            DefaultEncoding = "utf-8"
                        },
                        LoadSettings = new LoadSettings
                        {
                            BlockLocalFileAccess = false
                        }
                    }
                }
            };

            return _converter.Convert(doc);
        }
    }
}
