using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedProject;

namespace Server.Services
{
    public class ParentConferencePdfService
    {
        public byte[] GenerateParentConferencePdf(SimplifiedStudentProfileCaseRecordModel record, string? podName = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial").FontColor(Colors.Black));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Header
                        column.Item().AlignCenter().Column(headerColumn =>
                        {
                           
                            headerColumn.Item().PaddingTop(5).Text("PARENT CONFERENCE REQUEST FORM")
                                .FontSize(14).Bold().Underline();
                        });

                        column.Item().PaddingVertical(15);

                        // Date and Dear section
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Dear ");
                                text.Span(string.IsNullOrWhiteSpace(record.ParentContactName)
                                    ? "__________________________"
                                    : record.ParentContactName)
                                    .Underline();
                            });
                            row.AutoItem().Text($"Date: {(record.ParentMeetingDate ?? DateTime.Now).ToString("MM/dd/yyyy")}");
                        });

                        column.Item().PaddingVertical(10);

                        // Body content
                        column.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.LineHeight(1.6f));
                            text.Span("Good day!\n\n");
                            text.Span("We would like to invite you for a conference on ");
                            text.Span((record.ParentMeetingDate ?? DateTime.Now).ToString("MMMM dd, yyyy hh:mm tt"))
                                .Underline();
                            text.Span(" at the Prefect of Discipline Office to discuss the academic/behavioral/personal concerns of your child/ward ");
                            text.Span(record.RespondentName)
                                .Underline();
                            text.Span(" in the school.\n\n");
                            text.Span("We are hoping to see you. Thank you and God bless.");
                        });

                        column.Item().PaddingVertical(30);

                        // Signature section
                        column.Item().AlignCenter().Column(signatureColumn =>
                        {
                            signatureColumn.Item().PaddingTop(40);
                            signatureColumn.Item().BorderTop(1).BorderColor(Colors.Black)
                                .PaddingTop(5).Text(string.IsNullOrWhiteSpace(podName) ? "PREFECT OF DISCIPLINE" : podName)
                                .AlignCenter();
                            signatureColumn.Item().PaddingTop(2).Text("Prefect of Discipline In-Charge")
                                .FontSize(9).AlignCenter();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
