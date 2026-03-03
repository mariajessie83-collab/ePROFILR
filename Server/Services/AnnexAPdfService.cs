using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedProject;

namespace Server.Services
{
    public class AnnexAPdfService
    {
        public byte[] GenerateAnnexAPdf(List<SimplifiedStudentProfileCaseRecordModel> records, string schoolName = "", string division = "", string region = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Black));

                     page.Header().Column(headerCol =>
                    {
                        headerCol.Item().AlignCenter().Column(col =>
                        {
                            col.Item().Text("Department of Education").FontSize(10).SemiBold();
                            col.Item().Text($"{region}").FontSize(10);
                            col.Item().Text($"{division}").FontSize(10);
                            col.Item().PaddingBottom(5).Text($"{schoolName}").FontSize(11).Bold();
                        });

                        headerCol.Item().PaddingTop(10).AlignCenter().Column(col =>
                        {
                            col.Item().Text("ANNEX \"A\"").FontSize(12).Bold();
                            col.Item().Text("REPORT ON INCIDENTS OF ABUSE, VIOLENCE, EXPLOITATION, DISCRIMINATION, BULLYING OR PEER ABUSE").FontSize(11).Bold();
                            col.Item().Text("AND OTHER RELATED OFFENSES").FontSize(11).Bold();
                        });

                        headerCol.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Period Covered: ").SemiBold();
                                    string period = "N/A";
                                    if (startDate.HasValue && endDate.HasValue)
                                    {
                                        period = $"{startDate.Value:MMMM dd, yyyy} - {endDate.Value:MMMM dd, yyyy}";
                                    }
                                    r.RelativeItem().BorderBottom(0.5f).Text(period);
                                });
                            });
                            row.ConstantItem(100);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Row(r => {
                                    r.AutoItem().Text("Date Generated: ").SemiBold();
                                    r.RelativeItem().BorderBottom(0.5f).Text(DateTime.Now.ToString("MMMM dd, yyyy"));
                                });
                            });
                        });
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f); // Victim Name
                            columns.RelativeColumn(2.5f); // Respondent Name
                            columns.ConstantColumn(40);    // Age
                            columns.ConstantColumn(40);    // Sex
                            columns.RelativeColumn(3);    // Nature of Complaint
                            columns.RelativeColumn(3);    // Action Taken
                            columns.RelativeColumn(3);    // Recommendation
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("VICTIM NAME");
                            header.Cell().Element(HeaderStyle).Text("RESPONDENT NAME");
                            header.Cell().Element(HeaderStyle).Text("AGE");
                            header.Cell().Element(HeaderStyle).Text("SEX");
                            header.Cell().Element(HeaderStyle).Text("NATURE OF COMPLAINT");
                            header.Cell().Element(HeaderStyle).Text("ACTION TAKEN");
                            header.Cell().Element(HeaderStyle).Text("RECOMMENDATION");
                        });

                        foreach (var record in records)
                        {
                            table.Cell().Element(CellStyle).Text(record.VictimName ?? "N/A");
                            table.Cell().Element(CellStyle).Text(record.RespondentName ?? "N/A");
                            table.Cell().Element(CellStyle).AlignCenter().Text(record.Age?.ToString() ?? "-");
                            table.Cell().Element(CellStyle).AlignCenter().Text(record.Sex ?? "-");
                            table.Cell().Element(CellStyle).Text(record.ViolationCommitted ?? "N/A");
                            table.Cell().Element(CellStyle).Text(record.ActionTaken ?? record.Agreement ?? "N/A");
                            table.Cell().Element(CellStyle).Text(record.PenaltyAction ?? "N/A");
                        }
                    });

                    page.Footer().PaddingTop(30).Column(footerCol =>
                    {
                        footerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Prepared by:").FontSize(10).Italic();
                                c.Item().PaddingTop(20).PaddingRight(20).Column(sig =>
                                {
                                    sig.Item().BorderBottom(0.5f).PaddingBottom(2).AlignCenter().Text("").MinHeight(15);
                                    sig.Item().AlignCenter().Text("Signature over Printed Name").FontSize(9);
                                    sig.Item().AlignCenter().Text("Designation").FontSize(9);
                                });
                            });

                            row.ConstantItem(100);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Approved by:").FontSize(10).Italic();
                                c.Item().PaddingTop(20).PaddingRight(20).Column(sig =>
                                {
                                    sig.Item().BorderBottom(0.5f).PaddingBottom(2).AlignCenter().Text("").MinHeight(15);
                                    sig.Item().AlignCenter().Text("School Principal / School Head").FontSize(9);
                                    sig.Item().AlignCenter().Text("Date").FontSize(9);
                                });
                            });
                        });

                        footerCol.Item().PaddingTop(20).AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private IContainer HeaderStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(9).Bold())
                .Padding(5)
                .Border(0.5f)
                .BackgroundColor(Colors.Grey.Lighten4)
                .AlignCenter()
                .AlignMiddle();
        }

        private IContainer CellStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.FontSize(9))
                .PaddingHorizontal(5)
                .PaddingVertical(5)
                .Border(0.5f)
                .AlignLeft()
                .AlignMiddle();
        }
    }
}
